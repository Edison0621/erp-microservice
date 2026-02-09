FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Procurement/ErpSystem.Procurement/ErpSystem.Procurement.csproj", "Services/Procurement/ErpSystem.Procurement/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Procurement/ErpSystem.Procurement/ErpSystem.Procurement.csproj"
COPY . .
WORKDIR "/src/Services/Procurement/ErpSystem.Procurement"
RUN dotnet build "ErpSystem.Procurement.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Procurement.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Procurement.dll"]
