FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Assets/ErpSystem.Assets/ErpSystem.Assets.csproj", "Services/Assets/ErpSystem.Assets/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Assets/ErpSystem.Assets/ErpSystem.Assets.csproj"
COPY . .
WORKDIR "/src/Services/Assets/ErpSystem.Assets"
RUN dotnet build "ErpSystem.Assets.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Assets.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Assets.dll"]
