variable "api_name" {
  type = string
}

variable "lambda_invoke_arn" {
  type = string
}

variable "lambda_function_name" {
  type = string
}

variable "environment" {
  type = string
}

variable "log_retention_days" {
  type    = number
  default = 30
}

variable "throttling_burst_limit" {
  type    = number
  default = 100
}

variable "throttling_rate_limit" {
  type    = number
  default = 50
}

variable "cors_allow_origins" {
  type    = list(string)
  default = ["*"]
}

variable "create_api_gateway_logging_role" {
  description = "Set to false if the account already has an API GW logging IAM role."
  type        = bool
  default     = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
