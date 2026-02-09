FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build "Services/Settings/ErpSystem.Settings/ErpSystem.Settings.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "ErpSystem.Settings.dll"]
