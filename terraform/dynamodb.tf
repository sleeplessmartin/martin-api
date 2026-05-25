resource "aws_dynamodb_table" "products" {
  name         = local.dynamodb_table_name
  billing_mode = var.dynamodb_billing_mode
  hash_key     = "PK"

  attribute {
    name = "PK"
    type = "S"
  }

  # Point-in-time recovery — always on for prod, optional elsewhere
  point_in_time_recovery {
    enabled = var.environment == "prod"
  }

  # Server-side encryption with AWS-managed key (upgrade to CMK for stricter compliance)
  server_side_encryption {
    enabled = true
  }

  tags = merge(local.common_tags, {
    DataClassification = "internal"
  })
}

# CloudWatch alarm: DynamoDB system errors spike
resource "aws_cloudwatch_metric_alarm" "dynamodb_errors" {
  alarm_name          = "${local.name_prefix}-dynamodb-errors"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "SystemErrors"
  namespace           = "AWS/DynamoDB"
  period              = 60
  statistic           = "Sum"
  threshold           = 5
  alarm_description   = "DynamoDB system errors for ${local.dynamodb_table_name} exceeded threshold"

  dimensions = {
    TableName = aws_dynamodb_table.products.name
  }

  tags = local.common_tags
}
