FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/CRM/ErpSystem.CRM/ErpSystem.CRM.csproj", "Services/CRM/ErpSystem.CRM/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/CRM/ErpSystem.CRM/ErpSystem.CRM.csproj"
COPY . .
WORKDIR "/src/Services/CRM/ErpSystem.CRM"
RUN dotnet build "ErpSystem.CRM.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.CRM.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.CRM.dll"]
