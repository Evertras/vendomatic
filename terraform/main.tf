terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.16"
    }
  }

  # This is set up in another repository's terraform
  backend "s3" {
    bucket  = "evertras-home-terraform"
    key     = "global/s3/vendomatic.state"
    region  = "ap-northeast-1"
    encrypt = true
    profile = "admin"

    use_lockfile = true
  }

  required_version = ">= 1.2.0"
}

provider "aws" {
  region  = "ap-northeast-1"
  profile = "admin"
}