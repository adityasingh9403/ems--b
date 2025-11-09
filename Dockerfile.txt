# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Expose Render's default port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run your app
ENTRYPOINT ["dotnet", "EMS.Api.dll"]
