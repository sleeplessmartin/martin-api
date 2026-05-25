locals {
  # Naming convention: {project}-{environment}-{resource}
  name_prefix = "${var.project_name}-${var.environment}"

  # All resources get these tags; modules can add extras
  common_tags = merge(var.tags, {
    Project     = var.project_name
    Environment = var.environment
    Region      = var.aws_region
    ManagedBy   = "terraform"
  })

  lambda_function_name = "${local.name_prefix}-api"
  log_group_name       = "/aws/lambda/${local.lambda_function_name}"
  dynamodb_table_name  = "${local.name_prefix}-products"
  api_name             = "${local.name_prefix}-http-api"
  dlq_name             = "${local.name_prefix}-api-dlq"
}
