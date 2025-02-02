# .NET 9 SDK build
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# .NET 9 Runtime
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Docker container port
ENV ASPNETCORE_URLS=http://+:5000  

EXPOSE 5000
ENTRYPOINT ["dotnet", "todo-service.dll"]
