FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TaxSystem.CitizenService/TaxSystem.CitizenService.csproj", "TaxSystem.CitizenService/"]
RUN dotnet restore "TaxSystem.CitizenService/TaxSystem.CitizenService.csproj"
COPY . .
WORKDIR "/src/TaxSystem.CitizenService"
RUN dotnet build "./TaxSystem.CitizenService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TaxSystem.CitizenService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaxSystem.CitizenService.dll"]
