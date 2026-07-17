FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY publish/TaxSystem.StatementGeneratorService/ .
ENTRYPOINT ["dotnet", "TaxSystem.StatementGeneratorService.dll"]
