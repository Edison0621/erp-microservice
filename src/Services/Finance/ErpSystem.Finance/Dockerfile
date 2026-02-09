FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Finance/ErpSystem.Finance/ErpSystem.Finance.csproj", "Services/Finance/ErpSystem.Finance/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
COPY ["Services/Sales/ErpSystem.Sales/ErpSystem.Sales.csproj", "Services/Sales/ErpSystem.Sales/"]
RUN dotnet restore "Services/Finance/ErpSystem.Finance/ErpSystem.Finance.csproj"
COPY . .
WORKDIR "/src/Services/Finance/ErpSystem.Finance"
RUN dotnet build "ErpSystem.Finance.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Finance.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Finance.dll"]
