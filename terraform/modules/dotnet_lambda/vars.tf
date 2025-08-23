variable "name" {
  description = "The name of the Lambda function"
  type        = string
}

variable "prefix" {
  description = "The prefix to use for all resources"
  type        = string
}

variable "dynamodb_table_arn" {
  description = "The ARN of the DynamoDB table to access"
  type        = string
}

locals {
  prefix          = "${var.prefix}-${var.name}"
  lambda_filename = "${path.module}/../../../bin/release/${var.name}.zip"
}