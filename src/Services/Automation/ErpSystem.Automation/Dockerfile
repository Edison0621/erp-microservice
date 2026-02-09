FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Automation/ErpSystem.Automation/ErpSystem.Automation.csproj", "Services/Automation/ErpSystem.Automation/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Automation/ErpSystem.Automation/ErpSystem.Automation.csproj"
COPY . .
WORKDIR "/src/Services/Automation/ErpSystem.Automation"
RUN dotnet build "ErpSystem.Automation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Automation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Automation.dll"]
