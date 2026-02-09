FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Mrp/ErpSystem.Mrp/ErpSystem.Mrp.csproj", "Services/Mrp/ErpSystem.Mrp/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Mrp/ErpSystem.Mrp/ErpSystem.Mrp.csproj"
COPY . .
WORKDIR "/src/Services/Mrp/ErpSystem.Mrp"
RUN dotnet build "ErpSystem.Mrp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Mrp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Mrp.dll"]
