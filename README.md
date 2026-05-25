# ProductsApi

Production-grade REST API built with **.NET 8** and **C#**, deployed on **AWS Lambda** behind **API Gateway HTTP API (v2)**.
Clean Architecture · CQRS · DynamoDB · Terraform · GitHub Actions OIDC · OpenTelemetry

---

## Folder Tree

```
martin-api/
├── .github/
│   └── workflows/
│       ├── ci.yml               # PR: build, test, coverage, tf plan
│       ├── cd.yml               # main/dispatch: build, package, tf apply, smoke test
│       └── infrastructure.yml   # Reusable workflow called by ci + cd
├── src/
│   ├── Domain/                  # Entities, value objects, repository interfaces (no deps)
│   ├── Application/             # CQRS commands/queries via MediatR, validators, behaviors
│   ├── Infrastructure/          # DynamoDB repository, in-memory store, AWS SDK config
│   └── Api/                     # Lambda entry, minimal API endpoints, middleware
├── tests/
│   ├── Application.UnitTests/   # Handler + validator tests (Moq, FluentAssertions)
│   ├── Api.UnitTests/           # Endpoint tests via WebApplicationFactory
│   └── Infrastructure.UnitTests/ # DynamoDB repo tests with mocked IAmazonDynamoDB
├── terraform/
│   ├── modules/
│   │   ├── lambda/              # Lambda function, log group, alarms
│   │   └── api_gateway/         # HTTP API, integration, stage, access logs
│   ├── main.tf / variables.tf / outputs.tf / locals.tf
│   ├── dynamodb.tf / iam.tf / ssm.tf
│   └── envs/                    # dev.tfvars, prod.tfvars, *.backend.hcl
├── scripts/
│   └── build-lambda.sh          # Local Lambda packaging
└── ProductsApi.sln
```

---

## Prerequisites

| Tool | Minimum version |
|------|----------------|
| .NET SDK | 8.0 |
| Terraform | 1.9+ |
| AWS CLI | 2.x |
| Docker (optional) | For DynamoDB Local |

---

## Local Development

### 1. Run with In-Memory Store (no AWS needed)

```bash
cd src/Api
dotnet run
# API available at https://localhost:7001 or http://localhost:5001
```

`appsettings.Development.json` sets `UseInMemoryDatabase: true` by default, so no DynamoDB or AWS credentials are needed.

### 2. Run with DynamoDB Local

```bash
# Start DynamoDB Local
docker run -p 8000:8000 amazon/dynamodb-local

# Create the table
aws dynamodb create-table \
  --table-name products-local \
  --attribute-definitions AttributeName=PK,AttributeType=S \
  --key-schema AttributeName=PK,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url http://localhost:8000

# Update appsettings.Development.json:
# "UseInMemoryDatabase": false
# "DynamoDb": { "ServiceUrl": "http://localhost:8000", "TableName": "products-local" }

cd src/Api
dotnet run
```

### 3. Test the API

```bash
# Health check
curl http://localhost:5001/health/live

# Create a product (no auth in dev by default)
curl -X POST http://localhost:5001/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Widget Pro","description":"The best widget","price":29.99,"currency":"USD"}'

# List products
curl http://localhost:5001/api/v1/products
```

---

## Running Tests

```bash
# Run all tests
dotnet test ProductsApi.sln

# With coverage report (requires coverlet)
dotnet test ProductsApi.sln \
  --collect:"XPlat Code Coverage" \
  --settings tests/coverlet.runsettings \
  --results-directory ./test-results

# Run a specific test project
dotnet test tests/Application.UnitTests

# Filter by test name
dotnet test ProductsApi.sln --filter "FullyQualifiedName~CreateProduct"
```

### Test structure

| Project | What it tests | Key tools |
|---------|--------------|-----------|
| `Application.UnitTests` | Command handlers, query handlers, validators | Moq, FluentAssertions, FluentValidation.TestHelper |
| `Api.UnitTests` | HTTP endpoints, middleware, error responses | WebApplicationFactory, Moq |
| `Infrastructure.UnitTests` | DynamoDB repository mapping and request shape | Mock\<IAmazonDynamoDB\> |

**Key pattern:** All tests inject a fixed `DateTime` via `IDateTimeProvider` — no test ever calls `DateTime.UtcNow` directly. This makes time-sensitive assertions deterministic.

---

## Architecture Decisions

### Clean Architecture layers

```
Api → Application → Domain   (dependency direction)
Infrastructure → Domain
Infrastructure → Application (implements interfaces)
```

- **Domain** has zero external dependencies. Entities use factory methods (`Product.Create(...)`) to enforce invariants and raise domain events.
- **Application** orchestrates use cases via MediatR CQRS. FluentValidation runs in a pipeline behaviour before handlers execute.
- **Infrastructure** implements `IProductRepository`. `DynamoDbProductRepository` uses the low-level AWS SDK (`IAmazonDynamoDB`) so the entire client can be mocked in tests.
- **Api** is the composition root. `Program.cs` wires everything together and calls `AddAWSLambdaHosting()` which transparently handles both local ASP.NET Core and Lambda invocations.

### Cold-start mindset

- `AddAWSLambdaHosting()` uses a minimal bootstrap with no heavy static initialisation.
- `AmazonDynamoDBClient` is registered as singleton — the SDK connection pool is reused across Lambda invocations (warm starts reuse the same process).
- Memory size defaults to 512 MB — Lambda allocates proportional vCPU, reducing JIT overhead.

### Security

- **No long-lived credentials.** GitHub Actions uses OIDC (`id-token: write`) to exchange a short-lived token for an IAM role. No `AWS_ACCESS_KEY_ID` ever touches GitHub Secrets.
- **Least-privilege IAM.** The Lambda execution role has only DynamoDB table-scoped actions. The GitHub Actions deploy role has only `UpdateFunctionCode` + Terraform state permissions.
- **Secrets Manager vs SSM.** Non-secret config (table name, auth authority) is in SSM Parameter Store (type `String`). Rotate actual secrets (signing keys, third-party API tokens) via Secrets Manager and inject as env vars or load at runtime.
- **JWT via Cognito (or any OIDC IdP).** The API Gateway stage itself can add a JWT authoriser as a second layer, or you can validate the token in the Lambda (as shown here via `AddJwtBearer`).

### Observability

- **Structured JSON logs** via Serilog. Every log line carries `CorrelationId`, `RequestId`, `Environment`, and `Application` properties — filterable in CloudWatch Insights.
- **OpenTelemetry** hooks are wired for traces and metrics. In production, swap `AddConsoleExporter()` for `AddOtlpExporter()` pointed at your collector (e.g., AWS Distro for OpenTelemetry → X-Ray + CloudWatch).
- **CloudWatch alarms** for Lambda error count, P99 duration, and DynamoDB system errors are provisioned by Terraform.

---

## Terraform Deploy

### One-time bootstrap

```bash
# Create S3 bucket + DynamoDB table for Terraform state (do this once per account/env)
aws s3api create-bucket --bucket products-api-tfstate-dev --region us-east-1
aws dynamodb create-table \
  --table-name products-api-tflock \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST

# Create the GitHub Actions OIDC provider (once per AWS account)
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com \
  --thumbprint-list 6938fd4d98bab03faadb97b34396831e3780aea1
```

### Deploy dev

```bash
# Build the Lambda artifact first
chmod +x scripts/build-lambda.sh
./scripts/build-lambda.sh

cd terraform

terraform init -backend-config=envs/dev.backend.hcl

# Edit envs/dev.tfvars: set github_repo, auth_authority, auth_audience
terraform plan -var-file=envs/dev.tfvars -var="lambda_artifact_path=../artifacts/lambda.zip"
terraform apply -var-file=envs/dev.tfvars -var="lambda_artifact_path=../artifacts/lambda.zip"

terraform output api_endpoint   # e.g. https://abc123.execute-api.us-east-1.amazonaws.com
```

### Deploy prod

```bash
cd terraform
terraform init -backend-config=envs/prod.backend.hcl
terraform plan  -var-file=envs/prod.tfvars -var="lambda_artifact_path=../artifacts/lambda.zip"
terraform apply -var-file=envs/prod.tfvars -var="lambda_artifact_path=../artifacts/lambda.zip"
```

---

## CI/CD Workflow

### On Pull Request (`ci.yml`)

1. Restore + Build (warnings-as-errors)
2. Run all tests + collect coverage (Coverlet / Cobertura)
3. Post coverage badge + summary comment to PR
4. `terraform fmt -check` + `validate`
5. `terraform plan` (read-only role) — posts plan diff as PR comment

### On Push to `main` / Manual Dispatch (`cd.yml`)

1. Build + test
2. `dotnet publish` → ZIP Lambda artifact
3. Assume deploy IAM role via OIDC (no static keys)
4. `terraform apply` with the new artifact
5. Smoke test: `GET /health/live` must return 200
6. prod requires manual approval via GitHub Environment protection rules

### GitHub Secrets to configure

| Secret | Where used |
|--------|-----------|
| `AWS_PLAN_ROLE_ARN` | CI — read-only Terraform plan |
| `AWS_DEPLOY_ROLE_ARN_DEV` | CD — deploy to dev |
| `AWS_DEPLOY_ROLE_ARN_PROD` | CD — deploy to prod (after approval) |

---

## API Reference

Base URL: `https://{api-id}.execute-api.{region}.amazonaws.com/api/v1`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/products` | ProductsRead | List products (paginated) |
| `GET` | `/products/{id}` | ProductsRead | Get product by ID |
| `POST` | `/products` | ProductsWrite | Create product |
| `PUT` | `/products/{id}` | ProductsWrite | Update product |
| `DELETE` | `/products/{id}` | ProductsWrite | Delete product |
| `GET` | `/health` | None | Combined health check |
| `GET` | `/health/live` | None | Liveness probe |
| `GET` | `/health/ready` | None | Readiness probe |

All error responses follow [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc7807):

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation failures occurred.",
  "instance": "/api/v1/products",
  "requestId": "0HNABC123:00001",
  "correlationId": "abc-def-ghi",
  "errors": {
    "Name": ["Name is required."],
    "Currency": ["Currency must be a 3-letter ISO 4217 code."]
  }
}
```

---

## Troubleshooting

### "AccessDenied" on DynamoDB

The Lambda execution role needs `dynamodb:GetItem` (etc.) scoped to your table ARN. Check `terraform/iam.tf` → `lambda_dynamodb` policy. Also verify `DynamoDb__TableName` env var matches the deployed table name.

### Terraform state lock conflict

```bash
# Release a stuck lock (get lock ID from error message)
terraform force-unlock <LOCK_ID>
```

### Lambda cold starts

- Increase memory (CPU scales proportionally): `lambda_memory_mb = 1024`
- Enable provisioned concurrency in the Lambda module (adds cost)
- Profile with `aws lambda invoke` + CloudWatch duration metrics before tuning

### "No module registered for command" (MediatR)

MediatR scans the Assembly passed to `RegisterServicesFromAssembly`. Ensure new handlers are in the `Application` project assembly, not a separate library.

### JWT 401 on local

Set `UseInMemoryDatabase: true` and configure a test auth scheme (the `Api.UnitTests` shows the pattern). For real JWT testing locally, use a tool like [mock-oauth2-server](https://github.com/navikt/mock-oauth2-server).

### Terraform plan shows no changes but Lambda code is stale

The Lambda module uses `source_code_hash = filebase64sha256(var.artifact_path)`. Ensure `./scripts/build-lambda.sh` runs before `terraform plan` so the hash reflects the new binary.
