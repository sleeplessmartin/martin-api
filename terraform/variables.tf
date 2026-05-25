variable "project_name" {
  description = "Short project identifier used in resource names."
  type        = string
  default     = "products-api"
}

variable "environment" {
  description = "Deployment environment (dev | int | prod)."
  type        = string
  validation {
    condition     = contains(["dev", "int", "prod"], var.environment)
    error_message = "environment must be one of: dev, int, prod."
  }
}

variable "aws_region" {
  description = "AWS region for all resources."
  type        = string
  default     = "us-east-1"
}

variable "lambda_artifact_path" {
  description = "Local path to the Lambda ZIP artifact produced by the build pipeline."
  type        = string
  default     = "../artifacts/lambda.zip"
}

variable "lambda_memory_mb" {
  description = "Lambda memory allocation in MB (128–10240). Higher = faster CPU too."
  type        = number
  default     = 512
}

variable "lambda_timeout_seconds" {
  description = "Max Lambda execution time before timeout. API GW hard-limits at 29 s."
  type        = number
  default     = 30
}

variable "log_retention_days" {
  description = "CloudWatch log group retention in days."
  type        = number
  default     = 30
}

variable "dynamodb_billing_mode" {
  description = "DynamoDB billing mode: PAY_PER_REQUEST or PROVISIONED."
  type        = string
  default     = "PAY_PER_REQUEST"
}

variable "auth_authority" {
  description = "JWT authority URL (e.g. Cognito user pool endpoint). Stored in SSM."
  type        = string
  sensitive   = true
}

variable "auth_audience" {
  description = "JWT audience (e.g. Cognito App Client ID). Stored in SSM."
  type        = string
  sensitive   = true
}

variable "github_repo" {
  description = "GitHub repo in owner/repo format used to scope the OIDC trust policy."
  type        = string
}

variable "tags" {
  description = "Additional tags merged with common_tags."
  type        = map(string)
  default     = {}
}
