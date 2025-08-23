resource "aws_cloudwatch_log_group" "lambda_logs" {
  name = "/aws/lambda/${aws_lambda_function.lambda.function_name}"

  retention_in_days = 3
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