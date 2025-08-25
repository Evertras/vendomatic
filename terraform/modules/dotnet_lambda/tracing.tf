resource "aws_iam_policy" "tracing_policy" {
  name        = "${local.prefix}-tracing-policy"
  description = "Policy for X-Ray tracing"
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "xray:PutTelemetryRecords",
          "xray:PutTraceSegments"
        ]
        Resource = "*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "attach_tracing_policy" {
  role       = aws_iam_role.lambda_exec.name
  policy_arn = aws_iam_policy.tracing_policy.arn
}