data "aws_acm_certificate" "main_cert" {
  domain      = "evertras.com"
  statuses    = ["ISSUED"]
  most_recent = true
}

data "aws_route53_zone" "main_site" {
  name         = "evertras.com."
  private_zone = false
}

resource "aws_apigatewayv2_domain_name" "api_gw" {
  domain_name = var.site_domain

  domain_name_configuration {
    certificate_arn = data.aws_acm_certificate.main_cert.arn
    endpoint_type   = "REGIONAL"
    security_policy = "TLS_1_2"
  }
}

resource "aws_route53_record" "site" {
  zone_id = data.aws_route53_zone.main_site.zone_id

  name = aws_apigatewayv2_domain_name.api_gw.domain_name

  type = "A"

  alias {
    name    = aws_apigatewayv2_domain_name.api_gw.domain_name_configuration[0].target_domain_name
    zone_id = aws_apigatewayv2_domain_name.api_gw.domain_name_configuration[0].hosted_zone_id

    evaluate_target_health = false
  }
}

resource "aws_apigatewayv2_api_mapping" "api" {
  api_id      = aws_apigatewayv2_api.vending_machine.id
  domain_name = aws_apigatewayv2_domain_name.api_gw.id
  stage       = aws_apigatewayv2_stage.prod.id
}
