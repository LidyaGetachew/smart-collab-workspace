FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files
COPY SmartCollab.API/*.csproj SmartCollab.API/
COPY SmartCollab.Core/*.csproj SmartCollab.Core/
COPY SmartCollab.Infrastructure/*.csproj SmartCollab.Infrastructure/

# Restore dependencies
RUN dotnet restore SmartCollab.API/SmartCollab.API.csproj

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/SmartCollab.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Set environment variable for port
ENV ASPNETCORE_URLS=http://+:8000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartCollab.API.dll"]