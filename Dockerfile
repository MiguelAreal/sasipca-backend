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

# Instalar dependências necessárias para o motor PDF
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libx11-6 \
    libxcb1 \
    libxext6 \
    libxrender1 \
    libfontconfig1 \
    libx11-xcb1 \
    libice6 \
    libsm6 \
    libuuid1 \
    libpng16-16 \
    libjpeg62-turbo \
    xfonts-75dpi \
    xfonts-base \
    && rm -rf /var/lib/apt/lists/*

# Copy the compiled files from Stage 1
COPY --from=build /app/publish .

# GARANTIR que o ficheiro .so tem permissões de execução
RUN chmod +x libwkhtmltox.so

# 5. Starts the API
ENTRYPOINT ["dotnet", "sasipca_API.dll"]