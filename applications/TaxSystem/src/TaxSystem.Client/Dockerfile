FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY publish/TaxSystem.Client/ .
ENTRYPOINT ["dotnet", "TaxSystem.Client.dll"]
