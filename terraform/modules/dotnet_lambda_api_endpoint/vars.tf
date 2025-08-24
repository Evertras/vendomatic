variable "api_gateway_id" {
  description = "The ID of the API Gateway to integrate with"
  type        = string
}

variable "api_gateway_execution_arn" {
  description = "The execution ARN of the API Gateway to integrate with"
  type        = string
}

variable "lambda_invoke_arn" {
  description = "The ARN to invoke the Lambda function using AWS_PROXY with 2.0 payload"
  type        = string
}

variable "lambda_function_name" {
  description = "The name of the Lambda function"
  type        = string
}

variable "route_keys" {
  description = "A list of route keys to create for the API Gateway integration"
  type        = list(string)
}