FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Production/ErpSystem.Production/ErpSystem.Production.csproj", "Services/Production/ErpSystem.Production/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Production/ErpSystem.Production/ErpSystem.Production.csproj"
COPY . .
WORKDIR "/src/Services/Production/ErpSystem.Production"
RUN dotnet build "ErpSystem.Production.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Production.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Production.dll"]
