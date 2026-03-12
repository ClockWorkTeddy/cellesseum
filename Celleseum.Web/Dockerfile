# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy NuGet.config and csproj files first for better layer caching
COPY NuGet.config .
COPY Celleseum.Web/Celleseum.Web.csproj Celleseum.Web/
COPY Celleseum.ServiceDefaults/Celleseum.ServiceDefaults.csproj Celleseum.ServiceDefaults/
RUN dotnet restore Celleseum.Web/Celleseum.Web.csproj

# Copy sources and publish
COPY . .
RUN dotnet publish Celleseum.Web/Celleseum.Web.csproj -c Release -o /app/publish

# Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Celleseum.Web.dll"]