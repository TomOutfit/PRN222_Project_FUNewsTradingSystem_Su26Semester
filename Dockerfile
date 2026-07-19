# Use .NET 10 SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution file and project files first to cache NuGet restore
COPY FUNewsTradingSystem.sln .
COPY FUNewsTradingSystem/DataAccessLayer/DataAccessLayer.csproj FUNewsTradingSystem/DataAccessLayer/
COPY FUNewsTradingSystem/BusinessLayer/BusinessLayer.csproj FUNewsTradingSystem/BusinessLayer/
COPY FUNewsTradingSystem/MVC/MVC.csproj FUNewsTradingSystem/MVC/

# Restore all NuGet packages
RUN dotnet restore FUNewsTradingSystem/MVC/MVC.csproj

# Copy the rest of the source code
COPY . .

# Build and Publish the MVC project (the main executable)
WORKDIR /src/FUNewsTradingSystem/MVC
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the lightweight ASP.NET Core runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Render dynamically assigns a port via the PORT environment variable.
# We expose 8080 and tell ASP.NET Core to use it.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Command to run the application
ENTRYPOINT ["dotnet", "MVC.dll"]
