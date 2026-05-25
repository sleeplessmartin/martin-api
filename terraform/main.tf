terraform {
  required_version = ">= 1.9.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Remote state — backend config is supplied per-environment via -backend-config flag
  # or via a backend.hcl file: terraform init -backend-config=envs/dev.backend.hcl
  backend "s3" {}
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = local.common_tags
  }
}

# ── Lambda ─────────────────────────────────────────────────────────────────────
module "lambda" {
  source = "./modules/lambda"

  function_name        = local.lambda_function_name
  artifact_path        = var.lambda_artifact_path
  memory_mb            = var.lambda_memory_mb
  timeout_seconds      = var.lambda_timeout_seconds
  log_group_name       = local.log_group_name
  log_retention_days   = var.log_retention_days
  execution_role_arn   = aws_iam_role.lambda_exec.arn
  dynamodb_table_name  = aws_dynamodb_table.products.name

  environment_variables = {
    ASPNETCORE_ENVIRONMENT = var.environment == "prod" ? "Production" : "Staging"
    DOTNET_ENVIRONMENT     = var.environment == "prod" ? "Production" : "Staging"
    UseInMemoryDatabase    = "false"
    DynamoDb__TableName    = aws_dynamodb_table.products.name
    AWS__Region            = var.aws_region
    # Non-secret config pulled from SSM at deploy time and injected as env vars
    Auth__Authority        = aws_ssm_parameter.auth_authority.value
    Auth__Audience         = aws_ssm_parameter.auth_audience.value
  }

  tags = local.common_tags
}

# ── API Gateway ────────────────────────────────────────────────────────────────
module "api_gateway" {
  source = "./modules/api_gateway"

  api_name            = local.api_name
  lambda_invoke_arn   = module.lambda.invoke_arn
  lambda_function_name = module.lambda.function_name
  environment         = var.environment
  tags                = local.common_tags
}

# ── SSM: store outputs for cross-stack lookups ──────────────────────────────────
resource "aws_ssm_parameter" "api_endpoint" {
  name  = "/${var.project_name}/${var.environment}/api-endpoint"
  type  = "String"
  value = module.api_gateway.api_endpoint

  lifecycle {
    ignore_changes = [value] # updated by API GW, not manually
  }
}

resource "aws_ssm_parameter" "lambda_arn" {
  name  = "/${var.project_name}/${var.environment}/lambda-arn"
  type  = "String"
  value = module.lambda.function_arn
}
