FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Inventory/ErpSystem.Inventory/ErpSystem.Inventory.csproj", "Services/Inventory/ErpSystem.Inventory/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
COPY ["Services/Procurement/ErpSystem.Procurement/ErpSystem.Procurement.csproj", "Services/Procurement/ErpSystem.Procurement/"]
COPY ["Services/Sales/ErpSystem.Sales/ErpSystem.Sales.csproj", "Services/Sales/ErpSystem.Sales/"]
COPY ["Services/Production/ErpSystem.Production/ErpSystem.Production.csproj", "Services/Production/ErpSystem.Production/"]
RUN dotnet restore "Services/Inventory/ErpSystem.Inventory/ErpSystem.Inventory.csproj"
COPY . .
WORKDIR "/src/Services/Inventory/ErpSystem.Inventory"
RUN dotnet build "ErpSystem.Inventory.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Inventory.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Inventory.dll"]
