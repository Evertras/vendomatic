module "lambda" {
  source = "./modules/dotnet_lambda"

  name   = "VendingMachine"
  prefix = local.prefix
}