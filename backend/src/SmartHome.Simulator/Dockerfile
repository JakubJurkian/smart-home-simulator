# .NET 10 Runtime for the final image (smaller)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

# .NET 10 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the csproj and restore
COPY ["SmartHome.Simulator/SmartHome.Simulator.csproj", "SmartHome.Simulator/"]
RUN dotnet restore "SmartHome.Simulator/SmartHome.Simulator.csproj"

# Copy the rest of the files and build
COPY ["SmartHome.Simulator/", "SmartHome.Simulator/"]
WORKDIR "/src/SmartHome.Simulator"
RUN dotnet build "SmartHome.Simulator.csproj" -c Release -o /app/build

# Publish the executable
FROM build AS publish
RUN dotnet publish "SmartHome.Simulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartHome.Simulator.dll"]