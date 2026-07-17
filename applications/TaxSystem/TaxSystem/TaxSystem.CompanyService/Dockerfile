FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TaxSystem.CompanyService/TaxSystem.CompanyService.csproj", "TaxSystem.CompanyService/"]
RUN dotnet restore "TaxSystem.CompanyService/TaxSystem.CompanyService.csproj"
COPY . .
WORKDIR "/src/TaxSystem.CompanyService"
RUN dotnet build "./TaxSystem.CompanyService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TaxSystem.CompanyService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaxSystem.CompanyService.dll"]
