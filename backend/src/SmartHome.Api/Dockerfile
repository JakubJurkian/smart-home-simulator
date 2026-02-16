# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SmartHome.Api/SmartHome.Api.csproj", "SmartHome.Api/"]
COPY ["SmartHome.Domain/SmartHome.Domain.csproj", "SmartHome.Domain/"]
COPY ["SmartHome.Infrastructure/SmartHome.Infrastructure.csproj", "SmartHome.Infrastructure/"]
RUN dotnet restore "SmartHome.Api/SmartHome.Api.csproj"

COPY . .
WORKDIR "/src/SmartHome.Api"
# -c Release optimizes the code for production
RUN dotnet publish "SmartHome.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false


# Run
# Use the lighter .NET 10 ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartHome.Api.dll"]