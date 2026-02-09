FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Analytics/ErpSystem.Analytics/ErpSystem.Analytics.csproj", "Services/Analytics/ErpSystem.Analytics/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Analytics/ErpSystem.Analytics/ErpSystem.Analytics.csproj"
COPY . .
WORKDIR "/src/Services/Analytics/ErpSystem.Analytics"
RUN dotnet build "ErpSystem.Analytics.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Analytics.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Analytics.dll"]
