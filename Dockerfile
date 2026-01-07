# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["sasipca_API/sasipca_API.csproj", "sasipca_API/"]
RUN dotnet restore "sasipca_API/sasipca_API.csproj"
COPY . .
WORKDIR /src/sasipca_API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
USER root

# wkhtmltopdf + deps mínimas
RUN apt-get update && apt-get install -y --no-install-recommends \
    wkhtmltopdf \
    fontconfig \
    libxrender1 \
    libxext6 \
    libjpeg62-turbo \
    libpng16-16 \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p Storage/Reports && chmod -R 777 Storage

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "sasipca_API.dll"]
