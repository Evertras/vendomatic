resource "aws_apigatewayv2_api" "vending_machine" {
  name          = "${local.prefix}-api"
  protocol_type = "HTTP"
  cors_configuration {
    allow_headers  = ["*"]
    allow_methods  = ["*"]
    allow_origins  = [var.site_url]
    expose_headers = ["*"]
    max_age        = 3600
  }
}

resource "aws_cloudwatch_log_group" "apigw_prod_logs" {
  name              = "${local.prefix}-api-gw-prod-logs"
  retention_in_days = 3
}

resource "aws_apigatewayv2_stage" "prod" {
  api_id = aws_apigatewayv2_api.vending_machine.id

  name = "prod"

  auto_deploy = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.apigw_prod_logs.arn
    format = jsonencode({
      requestId               = "$context.requestId"
      integrationErrorMessage = "$context.integrationErrorMessage"
      status                  = "$context.status"
      protocol                = "$context.protocol"
      httpMethod              = "$context.httpMethod"
      path                    = "$context.path"
      responseLength          = "$context.responseLength"
    })
  }

  depends_on = [
    aws_cloudwatch_log_group.apigw_prod_logs
  ]
}

module "endpoint_vending_machine" {
  source = "./modules/dotnet_lambda_api_endpoint"

  name            = "VendingMachine"
  prefix          = local.prefix
  api_gateway_id  = aws_apigatewayv2_api.vending_machine.id
  api_gateway_arn = aws_apigatewayv2_api.vending_machine.arn
  api_gateway_execution_arn = aws_apigatewayv2_api.vending_machine.execution_arn
  method          = "POST"
  path            = "/machine"
}