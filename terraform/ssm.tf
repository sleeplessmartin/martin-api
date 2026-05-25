# Non-secret config stored as SSM String parameters.
# Secrets (DB passwords, API keys) belong in Secrets Manager — see README.

resource "aws_ssm_parameter" "auth_authority" {
  name  = "/${var.project_name}/${var.environment}/auth-authority"
  type  = "String"
  value = var.auth_authority

  lifecycle {
    # Prevent accidental wipe if value is rotated outside Terraform
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "auth_audience" {
  name  = "/${var.project_name}/${var.environment}/auth-audience"
  type  = "String"
  value = var.auth_audience

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "dynamodb_table" {
  name  = "/${var.project_name}/${var.environment}/dynamodb-table"
  type  = "String"
  value = aws_dynamodb_table.products.name
}
