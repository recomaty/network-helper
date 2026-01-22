.PHONY: test coverage coverage-html clean prepare-test

# Install required tools for coverage reports
prepare-test:
	dotnet tool restore || (dotnet new tool-manifest && dotnet tool install dotnet-reportgenerator-globaltool)

# Run tests
test:
	dotnet test

# Run tests with code coverage (generates Cobertura XML)
coverage:
	dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults

# Run tests and generate HTML coverage report
coverage-html: coverage
	dotnet tool restore
	dotnet reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coveragereport" -reporttypes:Html
	@echo "Coverage report generated at coveragereport/index.html"

# Clean test results and reports
clean:
	rm -rf ./TestResults ./coveragereport
