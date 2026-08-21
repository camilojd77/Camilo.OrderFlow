# syntax=docker/dockerfile:1
#
# Imagen de la API. Multi-stage: el SDK solo existe durante la compilación; la imagen
# final lleva únicamente el runtime de ASP.NET y los binarios publicados.
#
#   docker build -t company-product-api .
#   docker run --rm -p 8080:8080 company-product-api
#
# Etapas:
#   build     → restore + publish
#   migrator  → aplica migraciones EF (la usa deploy/app/docker-compose.yml)
#   final     → imagen de ejecución (por defecto)

ARG DOTNET_VERSION=10.0

# ──────────────────────────────────────────────────────────────────────────────
# build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Los manifiestos primero: mientras no cambien, Docker reutiliza la capa de restore
# aunque cambie el código. Central Package Management exige Directory.Packages.props.
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Company.Product.BuildingBlocks.SharedKernel/*.csproj      Company.Product.BuildingBlocks.SharedKernel/
COPY Company.Product.BuildingBlocks.Infrastructure/*.csproj    Company.Product.BuildingBlocks.Infrastructure/
COPY Company.Product.Modules.Identity.Domain/*.csproj          Company.Product.Modules.Identity.Domain/
COPY Company.Product.Modules.Identity.Application/*.csproj     Company.Product.Modules.Identity.Application/
COPY Company.Product.Modules.Identity.Infrastructure/*.csproj  Company.Product.Modules.Identity.Infrastructure/
COPY Company.Product.WebApi/*.csproj                           Company.Product.WebApi/
RUN dotnet restore Company.Product.WebApi/Company.Product.WebApi.csproj

COPY . .
RUN dotnet publish Company.Product.WebApi/Company.Product.WebApi.csproj \
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
            "--project", "Company.Product.Modules.Identity.Infrastructure", \
            "--startup-project", "Company.Product.WebApi"]

# ──────────────────────────────────────────────────────────────────────────────
# final — imagen de ejecución
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final

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

ENTRYPOINT ["dotnet", "Company.Product.WebApi.dll"]
