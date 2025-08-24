resource "aws_apigatewayv2_api" "vending_machine" {
  name          = "${local.prefix}-api"
  protocol_type = "HTTP"
  cors_configuration {
    allow_headers  = ["*"]
    allow_methods  = ["*"]
    allow_origins  = ["https://${var.site_domain}"]
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

  default_route_settings {
    throttling_burst_limit = 1
    throttling_rate_limit  = 1
  }
}

module "lambda_machines" {
  source = "./modules/dotnet_lambda"

  name               = "VendingMachine"
  prefix             = local.prefix
  dynamodb_table_arn = aws_dynamodb_table.main.arn
}

module "endpoint_vending_machine" {
  source = "./modules/dotnet_lambda_api_endpoint"

  api_gateway_id            = aws_apigatewayv2_api.vending_machine.id
  api_gateway_execution_arn = aws_apigatewayv2_api.vending_machine.execution_arn
  lambda_invoke_arn         = module.lambda_machines.invoke_arn
  lambda_function_name      = module.lambda_machines.name

  route_keys = [
    "POST /api/v1/machines",
    "GET /api/v1/machines",
  ]
}