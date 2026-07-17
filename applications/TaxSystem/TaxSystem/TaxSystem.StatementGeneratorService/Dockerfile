FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TaxSystem.StatementGeneratorService/TaxSystem.StatementGeneratorService.csproj", "TaxSystem.StatementGeneratorService/"]
COPY ["TaxSystem.Shared/TaxSystem.Shared.csproj", "TaxSystem.Shared/"]
RUN dotnet restore "TaxSystem.StatementGeneratorService/TaxSystem.StatementGeneratorService.csproj"
COPY . .
WORKDIR "/src/TaxSystem.StatementGeneratorService"
RUN dotnet build "./TaxSystem.StatementGeneratorService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TaxSystem.StatementGeneratorService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaxSystem.StatementGeneratorService.dll"]

