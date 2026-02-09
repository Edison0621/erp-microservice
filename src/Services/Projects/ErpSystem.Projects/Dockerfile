FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Projects/ErpSystem.Projects/ErpSystem.Projects.csproj", "Services/Projects/ErpSystem.Projects/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Projects/ErpSystem.Projects/ErpSystem.Projects.csproj"
COPY . .
WORKDIR "/src/Services/Projects/ErpSystem.Projects"
RUN dotnet build "ErpSystem.Projects.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Projects.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Projects.dll"]
