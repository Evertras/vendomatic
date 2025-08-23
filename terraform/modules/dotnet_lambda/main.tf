resource "aws_lambda_function" "lambda" {
  filename      = "${path.module}/../../../bin/release/${var.name}.zip"
  function_name = local.prefix
  role          = aws_iam_role.lambda_exec.arn
  handler       = "VendingMachine::VendingMachine.Function::FunctionHandler"
  runtime       = "dotnet8"

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

resource "aws_iam_policy" "log_policy" {
  name = "${local.prefix}-log-policy"

  description = "Policy to write logs"

  policy = <<EOF
{
 "Version": "2012-10-17",
 "Statement": [
   {
     "Action": [
       "logs:CreateLogStream",
       "logs:PutLogEvents"
     ],
     "Resource": "arn:aws:logs:*:*:*",
     "Effect": "Allow"
   }
 ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "attach_log_policy" {
  role       = aws_iam_role.lambda_exec.name
  policy_arn = aws_iam_policy.log_policy.arn
}

resource "aws_cloudwatch_log_group" "lambda_logs" {
  name = "/aws/lambda/${aws_lambda_function.lambda.function_name}"

  retention_in_days = 3
}