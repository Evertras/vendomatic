module "lambda" {
  source = "../dotnet_lambda"

  name               = "VendingMachine"
  prefix             = var.prefix
  dynamodb_table_arn = var.dynamodb_table_arn
}

resource "aws_lambda_permission" "execute_lambda_from_api_gw" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = module.lambda.name
  principal     = "apigateway.amazonaws.com"

  source_arn = "${var.api_gateway_execution_arn}/*/*"
}

resource "aws_apigatewayv2_integration" "api_integration" {
  api_id           = var.api_gateway_id
  integration_type = "AWS_PROXY"
  payload_format_version = "2.0"

  connection_type      = "INTERNET"
  integration_method   = "POST"
  integration_uri      = module.lambda.invoke_arn
  passthrough_behavior = "WHEN_NO_MATCH"
}

resource "aws_apigatewayv2_route" "api_route" {
  api_id    = var.api_gateway_id
  route_key = "${var.method} ${var.path}"
  target    = "integrations/${aws_apigatewayv2_integration.api_integration.id}"
}