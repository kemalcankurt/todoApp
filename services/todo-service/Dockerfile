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
ENV ASPNETCORE_URLS=http://+:5000  

# Expose port
EXPOSE 5000

# Healthcheck
HEALTHCHECK CMD curl --fail http://todo-service:5000/healthz || exit 1

ENTRYPOINT ["dotnet", "todo-service.dll"]
