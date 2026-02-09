FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/MasterData/ErpSystem.MasterData/ErpSystem.MasterData.csproj", "Services/MasterData/ErpSystem.MasterData/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/MasterData/ErpSystem.MasterData/ErpSystem.MasterData.csproj"
COPY . .
WORKDIR "/src/Services/MasterData/ErpSystem.MasterData"
RUN dotnet build "ErpSystem.MasterData.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.MasterData.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.MasterData.dll"]
