.PHONY: build
build: install-lambda-toolkit bin/release/VendingMachine.zip

.PHONY: fmt
fmt:
	@terraform fmt -recursive terraform

.PHONY: install-lambda-toolkit
install-lambda-toolkit:
	@dotnet tool install -g Amazon.Lambda.Tools > /dev/null

.PHONY: apply
apply: build
	@make -C terraform apply

bin/release/VendingMachine.zip: $(shell find VendingMachine/src/VendingMachine -name '*.cs')
	dotnet lambda package --configuration Release --framework net8.0 \
		--output-package bin/release/VendingMachine.zip \
		--project-location VendingMachine/src/VendingMachine