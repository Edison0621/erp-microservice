FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/HR/ErpSystem.HR/ErpSystem.HR.csproj", "Services/HR/ErpSystem.HR/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/HR/ErpSystem.HR/ErpSystem.HR.csproj"
COPY . .
WORKDIR "/src/Services/HR/ErpSystem.HR"
RUN dotnet build "ErpSystem.HR.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.HR.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.HR.dll"]
