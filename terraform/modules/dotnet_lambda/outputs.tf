output "arn" {
  value       = aws_lambda_function.lambda.arn
  description = "The ARN of the Lambda function"
  depends_on  = [aws_lambda_function.lambda]
}

output "name" {
  value       = aws_lambda_function.lambda.function_name
  description = "The name of the Lambda function"
  depends_on  = [aws_lambda_function.lambda]
}

output "invoke_arn" {
  value       = aws_lambda_function.lambda.invoke_arn
  description = "The invoke ARN of the Lambda function"
  depends_on  = [aws_lambda_function.lambda]
}
