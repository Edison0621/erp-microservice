FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Identity/ErpSystem.Identity/ErpSystem.Identity.csproj", "Services/Identity/ErpSystem.Identity/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
COPY ["Services/Identity/ErpSystem.Identity/appsettings.json", "Services/Identity/ErpSystem.Identity/"]
COPY ["Services/Identity/ErpSystem.Identity/appsettings.Development.json", "Services/Identity/ErpSystem.Identity/"]
RUN dotnet restore "Services/Identity/ErpSystem.Identity/ErpSystem.Identity.csproj"
COPY . .
WORKDIR "/src/Services/Identity/ErpSystem.Identity"
RUN dotnet build "ErpSystem.Identity.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Identity.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Identity.dll"]
