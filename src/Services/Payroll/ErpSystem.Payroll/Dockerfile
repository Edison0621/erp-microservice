FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Payroll/ErpSystem.Payroll/ErpSystem.Payroll.csproj", "Services/Payroll/ErpSystem.Payroll/"]
COPY ["BuildingBlocks/ErpSystem.BuildingBlocks/ErpSystem.BuildingBlocks.csproj", "BuildingBlocks/ErpSystem.BuildingBlocks/"]
RUN dotnet restore "Services/Payroll/ErpSystem.Payroll/ErpSystem.Payroll.csproj"
COPY . .
WORKDIR "/src/Services/Payroll/ErpSystem.Payroll"
RUN dotnet build "ErpSystem.Payroll.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErpSystem.Payroll.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpSystem.Payroll.dll"]
