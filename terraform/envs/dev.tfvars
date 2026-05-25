environment            = "dev"
aws_region             = "us-east-1"
lambda_memory_mb       = 512
lambda_timeout_seconds = 30
log_retention_days     = 14
dynamodb_billing_mode  = "PAY_PER_REQUEST"
github_repo            = "YOUR_ORG/YOUR_REPO"

# Populated from your IdP — override via CI secret or aws ssm put-parameter
auth_authority = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_CHANGE_ME"
auth_audience  = "CHANGE_ME_CLIENT_ID"

tags = {
  CostCenter = "engineering"
  Owner      = "platform-team"
}
