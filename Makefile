.PHONY: clean build run test coverage

build:
	dotnet build

clean:
	dotnet clean

run:
	dotnet run --project ClimateProcessing --

test:
	dotnet test --collect:"XPlat Code Coverage"

# dotnet tool install -g dotnet-reportgenerator-globaltool
coverage: test
	reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
	xdg-open coveragereport/index.html
