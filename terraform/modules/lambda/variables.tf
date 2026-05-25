variable "function_name" {
  type = string
}

variable "artifact_path" {
  type = string
}

variable "memory_mb" {
  type    = number
  default = 512
}

variable "timeout_seconds" {
  type    = number
  default = 30
}

variable "log_group_name" {
  type = string
}

variable "log_retention_days" {
  type    = number
  default = 30
}

variable "execution_role_arn" {
  type = string
}

variable "dynamodb_table_name" {
  type = string
}

variable "environment_variables" {
  type    = map(string)
  default = {}
}

variable "api_gateway_execution_arn" {
  type    = string
  default = "*" # Overridden by api_gateway module output
}

variable "tags" {
  type    = map(string)
  default = {}
}
