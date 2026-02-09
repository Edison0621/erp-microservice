FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Reporting/ErpSystem.Reporting/ErpSystem.Reporting.csproj", "Services/Reporting/ErpSystem.Reporting/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Reporting/ErpSystem.Reporting/ErpSystem.Reporting.csproj"
COPY . .
WORKDIR "/src/Services/Reporting/ErpSystem.Reporting"
RUN dotnet build "ErpSystem.Reporting.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Reporting.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Reporting.dll"]
