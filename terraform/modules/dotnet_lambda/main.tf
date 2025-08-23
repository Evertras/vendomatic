resource "aws_lambda_function" "lambda" {
  filename         = "${path.module}/../../../bin/release/${var.name}.zip"
  function_name    = local.prefix
  role             = aws_iam_role.lambda_exec.arn
  handler          = "VendingMachine::VendingMachine.Function::FunctionHandler"
  runtime          = "dotnet8"
  #architectures    = ["arm64"]

  #depends_on = [aws_iam_role_policy_attachment.lambda_logs]
}

resource "aws_iam_role" "lambda_exec" {
  name = "${local.prefix}-exec-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      },
    ]
  })
}