# ─── Etapa 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["NotificacionesService.csproj", "./"]
RUN dotnet restore "NotificacionesService.csproj"

# Copiar todo el código fuente y compilar
COPY . .
RUN dotnet build "NotificacionesService.csproj" -c Release -o /app/build

# Publicar la aplicación
RUN dotnet publish "NotificacionesService.csproj" -c Release -o /app/publish

# ─── Etapa 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Crear directorio para logs
RUN mkdir -p /app/logs

# Copiar desde la etapa de build
COPY --from=build /app/publish .

# Puerto en el que corre el microservicio
EXPOSE 5050

# Variables de entorno (se pueden sobreescribir al correr el contenedor)
ENV ASPNETCORE_URLS=http://+:5050
ENV ASPNETCORE_ENVIRONMENT=Production

# Punto de entrada
ENTRYPOINT ["dotnet", "NotificacionesService.dll"]
