output "api_gateway_prod_url" {
  description = "Base URL for API Gateway stage."

  value = aws_apigatewayv2_stage.prod.invoke_url
}

output "api_gw_id" {
  value = aws_apigatewayv2_api.vending_machine.id
}

output "api_gw_stage" {
  value = aws_apigatewayv2_stage.prod.id
}