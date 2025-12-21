# --- Stage 1: Build the Application ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copy just the project file first (for better caching)
COPY ["sasipca_API/sasipca_API.csproj", "sasipca_API/"]

# 2. Restore dependencies (NuGet packages)
RUN dotnet restore "sasipca_API/sasipca_API.csproj"

# 3. Copy the rest of the source code
COPY . .

# 4. Build and Publish the app to a folder named /app/publish
WORKDIR "/src/sasipca_API"
RUN dotnet publish "sasipca_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 2: Run the Application ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy the compiled files from Stage 1
COPY --from=build /app/publish .

# 5. Starts the API
ENTRYPOINT ["dotnet", "sasipca_API.dll"]