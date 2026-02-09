FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build "Gateways/ErpSystem.Gateway/ErpSystem.Gateway.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "ErpSystem.Gateway.dll"]
