FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Reporting/ErpSystem.Reporting/ErpSystem.Reporting.csproj", "Services/Reporting/ErpSystem.Reporting/"]
COPY ["Services/Sales/ErpSystem.Sales/ErpSystem.Sales.csproj", "Services/Sales/ErpSystem.Sales/"]
COPY ["Services/Inventory/ErpSystem.Inventory/ErpSystem.Inventory.csproj", "Services/Inventory/ErpSystem.Inventory/"]
COPY ["Services/Procurement/ErpSystem.Procurement/ErpSystem.Procurement.csproj", "Services/Procurement/ErpSystem.Procurement/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Reporting/ErpSystem.Reporting/ErpSystem.Reporting.csproj"
COPY . .
WORKDIR "/src/Services/Reporting/ErpSystem.Reporting"
RUN dotnet build "ErpSystem.Reporting.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Reporting.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Reporting.dll"]
