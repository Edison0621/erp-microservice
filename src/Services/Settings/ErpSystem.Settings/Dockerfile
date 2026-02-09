FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Settings/ErpSystem.Settings/ErpSystem.Settings.csproj", "Services/Settings/ErpSystem.Settings/"]
RUN dotnet restore "Services/Settings/ErpSystem.Settings/ErpSystem.Settings.csproj"
COPY . .
WORKDIR "/src/Services/Settings/ErpSystem.Settings"
RUN dotnet build "ErpSystem.Settings.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Settings.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Settings.dll"]
