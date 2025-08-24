.PHONY: build
build: install-lambda-toolkit bin/release/VendingMachine.zip

.PHONY: test
test:
	@dotnet test VendingMachine/test/VendingMachine.Tests

.PHONY: fmt
fmt:
	@terraform fmt -recursive terraform

.PHONY: install-lambda-toolkit
install-lambda-toolkit:
	@dotnet tool install -g Amazon.Lambda.Tools > /dev/null

.PHONY: apply
apply: build
	@make -C terraform apply

bin/release/VendingMachine.zip: $(shell find VendingMachine/src/VendingMachine -name '*.cs') VendingMachine/src/VendingMachine/Dtos/ApiDtos.cs
	dotnet lambda package --configuration Release --framework net8.0 \
		--output-package bin/release/VendingMachine.zip \
		--project-location VendingMachine/src/VendingMachine

# There are some nicer ways to do this with MSBuild, but I ran into trouble following the docs and
# really I'm just a sucker for makefiles anyway, so since I'm already using this...
VendingMachine/src/VendingMachine/Dtos/ApiDtos.cs: VendingMachine/src/VendingMachine/nswag.json specs/api-v1.yml
	dotnet nswag run VendingMachine/src/VendingMachine/nswag.json
	@touch VendingMachine/src/VendingMachine/Dtos/ApiDtos.cs