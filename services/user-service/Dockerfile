# STAGE 1: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Only copy necessary files
COPY *.csproj ./
RUN dotnet restore

COPY . . 
RUN dotnet publish -c Release -o out

# STAGE 2: Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y curl 
COPY --from=build /app/out .

# Set environment variable for URLs
ENV ASPNETCORE_URLS=http://+:5001

# Expose correct port
EXPOSE 5000

# Healthcheck
HEALTHCHECK CMD curl --fail http://user-service:5001/healthz || exit 1

ENTRYPOINT ["dotnet", "user-service.dll"]