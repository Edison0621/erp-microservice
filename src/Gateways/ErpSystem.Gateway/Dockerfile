FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Gateways/ErpSystem.Gateway/ErpSystem.Gateway.csproj", "Gateways/ErpSystem.Gateway/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Gateways/ErpSystem.Gateway/ErpSystem.Gateway.csproj"
COPY . .
WORKDIR "/src/Gateways/ErpSystem.Gateway"
RUN dotnet build "ErpSystem.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Gateway.dll"]
