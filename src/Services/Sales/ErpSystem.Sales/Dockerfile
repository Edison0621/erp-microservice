FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Sales/ErpSystem.Sales/ErpSystem.Sales.csproj", "Services/Sales/ErpSystem.Sales/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Sales/ErpSystem.Sales/ErpSystem.Sales.csproj"
COPY . .
WORKDIR "/src/Services/Sales/ErpSystem.Sales"
RUN dotnet build "ErpSystem.Sales.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Sales.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Sales.dll"]
