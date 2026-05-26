.PHONY: build-lambda
build-lambda:
	@chmod +x scripts/build-lambda.sh
	@./scripts/build-lambda.sh

.PHONY: dev
dev:
	@cd src/Api && dotnet run

.PHONY: test
test:
	@dotnet test ProductsApi.sln

.PHONY: tf-plan-dev
tf-plan-dev:
	@cd terraform && terraform plan -var-file=envs/dev.tfvars

.PHONY: run-local
run-local:
	ASPNETCORE_ENVIRONMENT=Development dotnet run \
		--project src/Api/Api.csproj \
		--urls http://localhost:5050