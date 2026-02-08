# Multi-stage Dockerfile for ERP Services
# Usage: docker build -t erp-{service} --build-arg SERVICE_NAME={ServiceFolder} .

ARG SERVICE_NAME=Finance
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE_NAME
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ["src/BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "src/BuildingBlocks/ErpSystem.BuildingBlocks/"]
COPY ["src/Services/${SERVICE_NAME}/ErpSystem.${SERVICE_NAME}/ErpSystem.${SERVICE_NAME}.csproj", "src/Services/${SERVICE_NAME}/ErpSystem.${SERVICE_NAME}/"]

# Restore dependencies
RUN dotnet restore "src/Services/${SERVICE_NAME}/ErpSystem.${SERVICE_NAME}/ErpSystem.${SERVICE_NAME}.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Services/${SERVICE_NAME}/ErpSystem.${SERVICE_NAME}"
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.${SERVICE_NAME}.dll"]
