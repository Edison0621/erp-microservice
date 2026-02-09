FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Quality/ErpSystem.Quality/ErpSystem.Quality.csproj", "Services/Quality/ErpSystem.Quality/"]
COPY ["Services/Procurement/ErpSystem.Procurement/ErpSystem.Procurement.csproj", "Services/Procurement/ErpSystem.Procurement/"]
COPY ["Services/Production/ErpSystem.Production/ErpSystem.Production.csproj", "Services/Production/ErpSystem.Production/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Quality/ErpSystem.Quality/ErpSystem.Quality.csproj"
COPY . .
WORKDIR "/src/Services/Quality/ErpSystem.Quality"
RUN dotnet build "ErpSystem.Quality.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Quality.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Quality.dll"]
