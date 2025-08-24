resource "aws_lambda_function" "lambda" {
  filename         = local.lambda_filename
  source_code_hash = filebase64sha256(local.lambda_filename)
  function_name    = local.prefix
  role             = aws_iam_role.lambda_exec.arn
  handler          = "VendingMachine::VendingMachine.Function::FunctionHandler"
  runtime          = "dotnet8"
  memory_size      = 256

  depends_on = [aws_iam_role_policy_attachment.attach_log_policy]
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