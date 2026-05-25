resource "aws_cloudwatch_log_group" "this" {
  name              = var.log_group_name
  retention_in_days = var.log_retention_days
  tags              = var.tags
}

resource "aws_lambda_function" "this" {
  function_name = var.function_name
  role          = var.execution_role_arn
  runtime       = "dotnet8"
  handler       = "ProductsApi.Api" # AddAWSLambdaHosting uses assembly name as handler
  architectures = ["x86_64"]

  # Upload via S3 for files > 50 MB; use filename for local development
  filename         = var.artifact_path
  source_code_hash = filebase64sha256(var.artifact_path)

  memory_size = var.memory_mb
  timeout     = var.timeout_seconds

  environment {
    variables = var.environment_variables
  }

  # Cold-start mitigation: keep code in /tmp warm by avoiding heavy static init
  # Provisioned concurrency (uncomment for prod if latency SLO demands it):
  # publish = true

  depends_on = [aws_cloudwatch_log_group.this]

  tags = var.tags
}

# Allow API Gateway to invoke the Lambda
resource "aws_lambda_permission" "api_gateway" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.this.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${var.api_gateway_execution_arn}/*/*"
}

# CloudWatch alarm: Lambda errors
resource "aws_cloudwatch_metric_alarm" "errors" {
  alarm_name          = "${var.function_name}-errors"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "Errors"
  namespace           = "AWS/Lambda"
  period              = 60
  statistic           = "Sum"
  threshold           = 5
  alarm_description   = "Lambda error count exceeded threshold for ${var.function_name}"

  dimensions = {
    FunctionName = aws_lambda_function.this.function_name
  }

  tags = var.tags
}

# CloudWatch alarm: Lambda P99 duration — alert before timeout
resource "aws_cloudwatch_metric_alarm" "duration_p99" {
  alarm_name          = "${var.function_name}-duration-p99"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  metric_name         = "Duration"
  namespace           = "AWS/Lambda"
  period              = 60
  extended_statistic  = "p99"
  threshold           = var.timeout_seconds * 800 # 80% of timeout in ms
  alarm_description   = "Lambda p99 duration approaching timeout for ${var.function_name}"

  dimensions = {
    FunctionName = aws_lambda_function.this.function_name
  }

  tags = var.tags
}
