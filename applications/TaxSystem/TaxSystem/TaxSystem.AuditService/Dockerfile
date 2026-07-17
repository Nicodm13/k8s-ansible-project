FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TaxSystem.AuditService/TaxSystem.AuditService.csproj", "TaxSystem.AuditService/"]
RUN dotnet restore "TaxSystem.AuditService/TaxSystem.AuditService.csproj"
COPY . .
WORKDIR "/src/TaxSystem.AuditService"
RUN dotnet build "./TaxSystem.AuditService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TaxSystem.AuditService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaxSystem.AuditService.dll"]
