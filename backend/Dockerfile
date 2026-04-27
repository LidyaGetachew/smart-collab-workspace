FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files
COPY SmartCollab.API/*.csproj SmartCollab.API/
COPY SmartCollab.Core/*.csproj SmartCollab.Core/
COPY SmartCollab.Infrastructure/*.csproj SmartCollab.Infrastructure/

# Restore
RUN dotnet restore SmartCollab.API/SmartCollab.API.csproj

# Copy all files
COPY . .

# Publish
WORKDIR /src/SmartCollab.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8000
ENTRYPOINT ["dotnet", "SmartCollab.API.dll"]