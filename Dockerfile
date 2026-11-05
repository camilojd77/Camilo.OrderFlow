# syntax=docker/dockerfile:1
#
# Imagen de la API. Multi-stage: el SDK solo existe durante la compilación; la imagen
# final lleva únicamente el runtime de ASP.NET y los binarios publicados.
#
#   docker build -t camilo-order-flow-api .
#   docker run --rm -p 8080:8080 camilo-order-flow-api
#
# Etapas:
#   build     → restore + publish
#   migrator  → aplica migraciones EF (la usa deploy/app/docker-compose.yml)
#   final     → imagen de ejecución (por defecto)

# Versiones fijadas (docs/adr/0024-versiones-fijadas-sdk-imagenes-y-acciones.md):
# el SDK coincide con global.json y el runtime es el que trae ese SDK.
ARG DOTNET_SDK_VERSION=10.0.403
ARG ASPNET_RUNTIME_VERSION=10.0.14

# ──────────────────────────────────────────────────────────────────────────────
# build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
WORKDIR /src

# Los manifiestos primero: mientras no cambien, Docker reutiliza la capa de restore
# aunque cambie el código. Central Package Management exige Directory.Packages.props.
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Camilo.OrderFlow.BuildingBlocks.SharedKernel/*.csproj      Camilo.OrderFlow.BuildingBlocks.SharedKernel/
COPY Camilo.OrderFlow.BuildingBlocks.Infrastructure/*.csproj    Camilo.OrderFlow.BuildingBlocks.Infrastructure/
COPY Camilo.OrderFlow.Modules.Identity.Domain/*.csproj          Camilo.OrderFlow.Modules.Identity.Domain/
COPY Camilo.OrderFlow.Modules.Identity.Application/*.csproj     Camilo.OrderFlow.Modules.Identity.Application/
COPY Camilo.OrderFlow.Modules.Identity.Infrastructure/*.csproj  Camilo.OrderFlow.Modules.Identity.Infrastructure/
COPY Camilo.OrderFlow.WebApi/*.csproj                           Camilo.OrderFlow.WebApi/
RUN dotnet restore Camilo.OrderFlow.WebApi/Camilo.OrderFlow.WebApi.csproj

COPY . .
RUN dotnet publish Camilo.OrderFlow.WebApi/Camilo.OrderFlow.WebApi.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ──────────────────────────────────────────────────────────────────────────────
# migrator — contenedor efímero que aplica las migraciones y termina
# ──────────────────────────────────────────────────────────────────────────────
FROM build AS migrator
# dotnet-ef sale del manifiesto .config/dotnet-tools.json, no de una instalación global:
# así la herramienta usa exactamente la misma versión dentro y fuera del contenedor.
# (Con el manifiesto presente, `dotnet ef` resuelve al tool local e ignora el global.)
RUN dotnet tool restore
ENTRYPOINT ["dotnet", "ef", "database", "update", \
            "--project", "Camilo.OrderFlow.Modules.Identity.Infrastructure", \
            "--startup-project", "Camilo.OrderFlow.WebApi"]

# ──────────────────────────────────────────────────────────────────────────────
# final — imagen de ejecución
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_RUNTIME_VERSION} AS final

# curl es solo para el HEALTHCHECK de abajo. Si tu orquestador ya sondea /health/live
# por su cuenta (Kubernetes, ECS), puedes borrar tanto el apt-get como el HEALTHCHECK.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Metadatos del artefacto: los expone GET /_info para saber qué commit está desplegado.
ARG COMMIT_SHA=""
ARG BUILD_DATE=""
ENV COMMIT_SHA=${COMMIT_SHA} \
    BUILD_DATE=${BUILD_DATE} \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# APP_UID viene definido en la imagen base (usuario sin privilegios, uid 1654).
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Camilo.OrderFlow.WebApi.dll"]
