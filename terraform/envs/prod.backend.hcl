bucket         = "products-api-tfstate-prod"
key            = "products-api/prod/terraform.tfstate"
region         = "us-east-1"
dynamodb_table = "products-api-tflock"
encrypt        = true
