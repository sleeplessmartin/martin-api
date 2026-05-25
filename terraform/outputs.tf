output "api_endpoint" {
  description = "HTTP API Gateway invoke URL."
  value       = module.api_gateway.api_endpoint
}

output "lambda_function_name" {
  description = "Lambda function name (use for aws lambda invoke in smoke tests)."
  value       = module.lambda.function_name
}

output "lambda_function_arn" {
  description = "Lambda function ARN."
  value       = module.lambda.function_arn
}

output "dynamodb_table_name" {
  description = "DynamoDB products table name."
  value       = aws_dynamodb_table.products.name
}

output "github_actions_role_arn" {
  description = "IAM role ARN for GitHub Actions OIDC — add to GitHub repo secrets as AWS_ROLE_ARN."
  value       = aws_iam_role.github_actions.arn
}
