# --- Stage 2: Run the Application ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 1. Mudar explicitamente para ROOT para ter permissões de instalação
USER root

# 2. Instalação com pacotes atualizados para Debian Trixie (Base do .NET 10)
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

# 3. Copiar ficheiros da build
COPY --from=build /app/publish .

# 4. Criar pastas e dar permissões (Puppeteer precisa de permissão para o browser)
RUN mkdir -p Storage/Reports && chmod -R 777 Storage/Reports

# 5. Inicia a API
ENTRYPOINT ["dotnet", "sasipca_API.dll"]