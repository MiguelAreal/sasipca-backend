# --- Stage 1: Build the Application ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["sasipca_API/sasipca_API.csproj", "sasipca_API/"]
RUN dotnet restore "sasipca_API/sasipca_API.csproj"
COPY . .
WORKDIR "/src/sasipca_API"
RUN dotnet publish "sasipca_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 2: Run the Application ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 1. Mudar explicitamente para ROOT
USER root

# 2. Instalação de dependências (Puppeteer)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpangocairo-1.0-0 \
    libgtk-3-0 \
    ca-certificates \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# 3. Copiar ficheiros da build (Garante que o nome 'build' coincide com o AS build lá em cima)
COPY --from=build /app/publish .

# 4. Criar pastas e dar permissões
RUN mkdir -p Storage/Reports && chmod -R 777 Storage/Reports

# 5. Inicia a API
ENTRYPOINT ["dotnet", "sasipca_API.dll"]