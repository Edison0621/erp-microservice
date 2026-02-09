FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Maintenance/ErpSystem.Maintenance/ErpSystem.Maintenance.csproj", "Services/Maintenance/ErpSystem.Maintenance/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Maintenance/ErpSystem.Maintenance/ErpSystem.Maintenance.csproj"
COPY . .
WORKDIR "/src/Services/Maintenance/ErpSystem.Maintenance"
RUN dotnet build "ErpSystem.Maintenance.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Maintenance.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Maintenance.dll"]
