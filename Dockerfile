# --- Stage 1: Build the Application ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copiar apenas o ficheiro de projeto para cache eficiente
COPY ["sasipca_API/sasipca_API.csproj", "sasipca_API/"]

# 2. Restaurar pacotes NuGet
RUN dotnet restore "sasipca_API/sasipca_API.csproj"

# 3. Copiar o resto do código fonte
COPY . .

# 4. Publicar a aplicação
WORKDIR "/src/sasipca_API"
RUN dotnet publish "sasipca_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 2: Run the Application ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# --- NOVO: Instalação de dependências do Chromium para o Puppeteer ---
# Mudar para root para instalar pacotes
USER root

RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 \
    libatk1.0-0 \
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
    && rm -rf /var/lib/apt/lists/*

# Copiar os ficheiros publicados da Stage 1
COPY --from=build /app/publish .

# Criar pastas para armazenamento de relatórios e garantir permissões
RUN mkdir -p Storage/Reports && chmod -R 777 Storage/Reports

# Inicia a API
ENTRYPOINT ["dotnet", "sasipca_API.dll"]