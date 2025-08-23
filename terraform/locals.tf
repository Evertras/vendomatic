locals {
  prefix = terraform.workspace == "default" ? "evertras-vendomatic" : "evertras-vendomatic-${terraform.workspace}"
}