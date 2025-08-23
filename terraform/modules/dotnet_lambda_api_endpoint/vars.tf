variable "name" {
  description = "The name of the Lambda function"
  type        = string
}

variable "prefix" {
  description = "The prefix to use for all resources"
  type        = string
}

variable "api_gateway_id" {
  description = "The ID of the API Gateway to integrate with"
  type        = string
}

variable "api_gateway_arn" {
  description = "The ARN of the API Gateway to integrate with"
  type        = string
}

variable "api_gateway_execution_arn" {
  description = "The execution ARN of the API Gateway to integrate with"
  type        = string
}

variable "method" {
  description = "The HTTP method to use for the API Gateway integration"
  type        = string
  default     = "ANY"
}

variable "path" {
  description = "The path for the API Gateway integration, starting with a slash"
  type        = string
}

locals {
  prefix = "${var.prefix}-${var.name}"
}