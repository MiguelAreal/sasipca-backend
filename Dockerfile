# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar apenas o csproj primeiro para cache
COPY ["sasipca_API/sasipca_API.csproj", "sasipca_API/"]
RUN dotnet restore "sasipca_API/sasipca_API.csproj"

# Copiar restante do código
COPY . .
WORKDIR "/src/sasipca_API"
RUN dotnet publish "sasipca_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar arquivos compilados
COPY --from=build /app/publish .

# Criar pastas necessárias
RUN mkdir -p Storage/Reports && chmod -R 777 Storage

# Porta e ENTRYPOINT
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "sasipca_API.dll"]
