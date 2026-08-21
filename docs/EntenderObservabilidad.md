# Ruta de Estudio — Observabilidad Moderna con OpenTelemetry, Grafana, Prometheus, Loki y Tempo

---

# Por qué leer este documento

Este documento no es solo una lista de temas. Es una guía que explica el *qué*, el *por qué* y el *para qué* de cada concepto. Está pensado para que puedas entender observabilidad a fondo y comunicarlo con claridad a tu equipo y a la organización.

Al terminar deberías poder responder preguntas como:

- ¿Por qué nuestra API fue lenta ayer a las 3pm?
- ¿Cuántos errores tuvimos en las últimas 24 horas?
- ¿Qué servicio causó que esta transacción tardara 8 segundos?
- ¿Cuándo empezó a degradarse el sistema antes del incidente?
- ¿Qué porcentaje de nuestros usuarios experimenta latencia mayor a 2 segundos?

Sin observabilidad, estas preguntas se responden con suposiciones. Con observabilidad, se responden con datos.

---

# El Problema que Resuelve la Observabilidad

## Monitoreo tradicional vs Observabilidad

Durante años, las empresas usaron **monitoreo**: chequear si un servidor está vivo, si el disco tiene espacio, si la CPU está al 100%. Eso funcionaba cuando los sistemas eran simples: un servidor, una base de datos, una aplicación.

El problema moderno es diferente. Un sistema empresarial moderno puede tener:

- 10 o 50 microservicios
- Bases de datos, caches, colas de mensajes
- APIs externas de terceros
- Balanceadores de carga
- Contenedores que se crean y destruyen en segundos

En ese entorno, saber que "el servidor está vivo" no sirve de nada si la transacción de compra tarda 12 segundos porque el servicio de pagos está esperando a una API externa que responde lento.

**Monitoreo** te dice si algo está roto.
**Observabilidad** te dice *por qué* está roto y *dónde exactamente*.

La diferencia conceptual es importante:

- El monitoreo es reactivo: configuras alertas para condiciones que ya conoces.
- La observabilidad es proactiva: puedes hacer preguntas sobre el sistema que nunca hiciste antes, sin necesidad de haber instrumentado exactamente esa pregunta de antemano.

## Los Tres Pilares

La observabilidad moderna se construye sobre tres pilares complementarios:

```
┌─────────────────────────────────────────────────────────┐
│                   OBSERVABILIDAD                        │
│                                                         │
│   LOGS          MÉTRICAS          TRAZAS                │
│   ─────         ────────          ──────                │
│   Qué pasó      Cuánto/cuándo     Cómo fluye           │
│   Eventos       Tendencias        Una request           │
│   Texto         Números           Árbol de llamadas     │
└─────────────────────────────────────────────────────────┘
```

Cada pilar responde preguntas distintas. Solos son útiles. Juntos y correlacionados son poderosos.

---

# Objetivo Final

Al terminar esta ruta deberías poder:

- Diseñar una plataforma enterprise de observabilidad.
- Instrumentar aplicaciones .NET correctamente.
- Centralizar logs, métricas y trazas.
- Entender problemas de rendimiento reales.
- Diagnosticar cuellos de botella.
- Crear dashboards profesionales.
- Implementar tracing distribuido.
- Configurar pipelines OpenTelemetry.
- Explicar a la organización por qué observabilidad es una inversión, no un costo.

---

# El Valor para la Empresa

Antes de entrar en tecnología, es importante entender el argumento de negocio. Si necesitas explicar esto a directivos o a un equipo que no es técnico, estos son los puntos clave:

## Reducción del tiempo de resolución de incidentes (MTTR)

MTTR (Mean Time To Resolve) es el tiempo promedio que tarda tu equipo en detectar un problema y resolverlo. Sin observabilidad, un equipo puede tardar horas o días buscando qué falló. Con observabilidad, el mismo problema se diagnostica en minutos porque puedes ver exactamente qué span fue lento, qué log lanzó el error y en qué servicio.

Una reducción de MTTR de 4 horas a 20 minutos en un sistema crítico tiene un impacto financiero directo: menos downtime, menos impacto a clientes, menos costo de ingeniería dedicado a apagar incendios.

## Confianza para deployar

Uno de los problemas más comunes en equipos sin observabilidad es el miedo a hacer deploys. Si no tienes visibilidad de qué pasa después de un deploy, cada release es un riesgo. Con observabilidad puedes ver en tiempo real si después del deploy aumentaron los errores, si la latencia subió, si hay spans que fallaron. Esto habilita deployar con confianza y hacer rollbacks informados.

## Entender el comportamiento real de usuarios

Las métricas de negocio (cuántas transacciones, cuántos errores le afectaron al usuario, qué porcentaje de requests terminaron bien) salen del mismo sistema de observabilidad. No es solo una herramienta técnica: es inteligencia de negocio.

## Cumplimiento y auditoría

En muchos sectores (financiero, salud, gobierno) hay requisitos de trazabilidad: poder demostrar qué procesó el sistema, cuándo, y qué resultado tuvo. Los logs estructurados y el tracing distribuido proveen exactamente esa evidencia de auditoría.

---

# ETAPA 0 — Fundamentos Base

Antes de entrar a observabilidad moderna debes entender los sistemas que vas a observar. Si ya manejas bien estos temas, puedes avanzar directamente a la Etapa 1.

---

## Qué estudiar

### Backend y HTTP

- HTTP: protocolo de comunicación que usan las APIs
- APIs REST: convención para diseñar APIs sobre HTTP
- Headers: metadatos que viajan con cada request (aquí viajan los TraceIds de observabilidad)
- Status Codes: 200 OK, 400 Bad Request, 500 Internal Server Error — la base de las métricas de error
- Request/Response lifecycle: qué ocurre desde que el cliente manda una request hasta que recibe respuesta

### Concurrencia y rendimiento

- CPU: procesamiento, indica saturación cuando está alto
- RAM: memoria, indica memory leaks cuando crece sin bajar
- IO: lectura/escritura de disco y red, la fuente más común de latencia
- Threading: cómo el servidor atiende múltiples requests a la vez
- Async/Await: modelo moderno de C# para IO no bloqueante

### Docker

- Containers: unidades aisladas donde corren los servicios
- Volumes: persistencia de datos entre reinicios
- Networks: cómo los contenedores se comunican entre sí
- Docker Compose: orquestación local de múltiples servicios (Grafana, Prometheus, tu API)

### Arquitectura

- Microservicios: sistema dividido en servicios pequeños e independientes
- APIs distribuidas: múltiples servicios que se llaman entre sí
- Load balancers: distribuyen el tráfico entre instancias
- Gateways: punto de entrada único que enruta a los servicios

---

## Conceptos que debes dominar

- Qué ocurre cuando una request entra a una API: cliente → red → gateway → servicio → lógica → base de datos → respuesta
- Qué significa latencia: tiempo total desde que el cliente manda la request hasta que recibe respuesta
- Diferencia entre CPU-bound e IO-bound: CPU-bound significa que el cuello de botella es procesamiento (cálculos). IO-bound significa que el cuello de botella es esperar algo externo (base de datos, red, disco).
- Qué es una dependencia externa: cualquier cosa que tu servicio llama y no controla (otro servicio, base de datos, API de tercero).
- Qué es un cuello de botella: el punto del sistema que limita la velocidad del resto. Optimizar cualquier otra cosa no mejora el sistema si el cuello de botella sigue ahí.
- Cómo se comunican servicios entre sí: HTTP/REST, gRPC, colas de mensajes (Kafka, RabbitMQ).

---

## Videos recomendados

Para esta etapa busca en YouTube:

```
Canal: Fireship — "HTTP Crash Course"
Canal: TechWorld with Nana — "Docker Tutorial for Beginners"
Canal: TechWorld with Nana — "Microservices explained"
Canal: Hussein Nasser — "HTTP/1.1 vs HTTP/2"
Búsqueda: "REST API explained for beginners"
Búsqueda: "Async await C# explained"
```

---

# ETAPA 1 — Logging Profesional

---

## Objetivo

Entender logging moderno enterprise: por qué importa, cómo hacerlo bien y qué diferencia hace en producción.

---

## Explicación Teórica

### El problema con Console.WriteLine

En desarrollo, `Console.WriteLine("algo pasó")` parece suficiente. En producción falla por varias razones:

1. **No tiene estructura**: es texto libre. Para buscarlo tienes que hacer grep de texto plano, lo cual no escala.
2. **No tiene contexto**: no sabes de qué request viene, de qué usuario, de qué servidor.
3. **No tiene niveles de severidad**: no puedes filtrar errores críticos de mensajes informativos.
4. **No persiste**: en contenedores, si el contenedor muere, los logs desaparecen.
5. **No es procesable por máquinas**: no puedes hacer alertas, dashboards ni análisis sobre texto libre.

### Structured Logging: logs que las máquinas pueden leer

Structured logging es la práctica de escribir logs como objetos con propiedades en vez de strings libres.

**Log tradicional (malo para producción):**
```
[2026-05-11 14:23:01] Usuario 42 hizo login exitoso desde 192.168.1.10
```

**Structured log (correcto para producción):**
```json
{
  "timestamp": "2026-05-11T14:23:01Z",
  "level": "Information",
  "message": "User login successful",
  "userId": 42,
  "ipAddress": "192.168.1.10",
  "correlationId": "abc-123-xyz",
  "service": "auth-service",
  "duration_ms": 45
}
```

El segundo log se puede:
- Filtrar por `userId = 42` en una herramienta como Loki
- Contar cuántos logins fallidos hubo en la última hora
- Correlacionar con una traza para ver todo el flujo
- Crear alertas si `level = "Error"`

### CorrelationId: seguir una request entre servicios

Imagina que una compra pasa por 5 servicios: gateway → pedidos → inventario → pagos → notificaciones. Si algo falla en pagos, ¿cómo sabes qué request específica falló? ¿Cómo ves los logs de los 5 servicios que pertenecen a esa misma operación?

El CorrelationId (también llamado TraceId) es un identificador único que se genera cuando llega la primera request y se propaga a todos los servicios. Cada servicio lo incluye en sus logs. Así puedes buscar en Loki `correlationId = "abc-123-xyz"` y ver todos los logs de todos los servicios que participaron en esa operación.

```
Request llega → se genera ID: "abc-123"
  ↓
Servicio A → log: {correlationId: "abc-123", ...}
  ↓ llama a →
Servicio B → log: {correlationId: "abc-123", ...}
  ↓ llama a →
Servicio C → log: {correlationId: "abc-123", ...}

Búsqueda en Loki: correlationId="abc-123" → ves TODO el flujo
```

### Serilog en .NET

Serilog es la librería estándar de logging estructurado en el ecosistema .NET. Sus conceptos clave:

- **Sinks**: destinos donde Serilog escribe los logs. Puede escribir a consola, a archivo, a Loki, a Elasticsearch, a varios al mismo tiempo.
- **Enrichers**: componentes que agregan contexto automáticamente a cada log. Por ejemplo, el enricher de CorrelationId agrega el ID de request a cada log sin que tengas que hacerlo manualmente.
- **Log levels**: Verbose → Debug → Information → Warning → Error → Fatal. Puedes configurar qué nivel mínimo quieres ver en cada ambiente.

---

## Qué estudiar

### Logs tradicionales

- INFO: eventos normales del sistema
- WARNING: algo inusual pero no crítico
- ERROR: algo falló, necesita atención
- DEBUG: detalle para desarrollo, no para producción

### Structured Logging

- JSON logs: formato estándar legible por máquinas
- Propiedades: campos nombrados en el log
- Metadata: contexto adicional (servicio, version, ambiente)

### CorrelationId

- Propagación entre servicios via HTTP headers
- Request tracking: seguir una operación de punta a punta

### Serilog

- Sinks: Console, File, Loki, Seq, Elasticsearch
- Enrichers: CorrelationId, Machine Name, Environment
- Structured logging con templates `{PropertyName}`

---

## Conceptos que debes dominar

- Por qué Console.WriteLine no sirve en producción: no tiene estructura, no persiste, no es filtrable.
- Qué es structured logging: logs con propiedades en vez de texto libre.
- Por qué los logs JSON son superiores: son procesables por máquinas, filtrables, agregables.
- Qué es correlationId: identificador único que permite seguir una request entre servicios.
- Cómo seguir una request entre servicios: propagando el correlationId en los headers HTTP y escribiéndolo en cada log.

---

## Qué deberías poder construir

Una API .NET que:

- Genere logs estructurados en JSON
- Tenga correlationId propagado automáticamente
- Registre excepciones con stack trace completo
- Registre duración de cada request

---

## Videos recomendados

```
Canal: IAmTimCorey — "Logging in C# .NET Modern and Structured"
Canal: Nick Chapsas — "Structured Logging in .NET"
Búsqueda: "Serilog ASP.NET Core tutorial 2024"
Búsqueda: "Structured logging explained .NET"
Búsqueda: "CorrelationId middleware ASP.NET Core"
```

---

# ETAPA 2 — Métricas

---

## Objetivo

Entender monitoreo basado en métricas: qué son, cómo se clasifican y por qué son la base del monitoreo enterprise.

---

## Explicación Teórica

### Qué son las métricas y por qué son distintas a los logs

Los logs registran eventos: "el usuario 42 hizo login", "la query tardó 340ms", "hubo un error en el pago".

Las métricas registran estado y tendencias numéricas: "en este momento hay 1.247 requests por segundo", "el percentil P95 de latencia es 320ms", "hay 3 errores por minuto".

La diferencia clave es el costo y la granularidad. Los logs son detallados pero costosos de almacenar y buscar. Las métricas son ligeras, ocupan muy poco espacio y permiten guardar meses de historia de forma eficiente. Prometheus puede guardar el historial completo de latencia de tu API de los últimos 6 meses en pocos GB.

### Los cuatro tipos de métricas

**Counter (Contador):**
Solo puede subir. Nunca baja. Se usa para contar cosas que ocurren: requests totales, errores totales, bytes transferidos. La query útil no es el valor absoluto sino la tasa de cambio: "cuántos errores por segundo" se calcula con `rate(errores[5m])`.

```
Ejemplo: http_requests_total{method="POST", status="200"} = 15432
```

**Gauge (Indicador):**
Puede subir y bajar. Representa un estado instantáneo: cantidad de conexiones activas, uso de memoria RAM en este momento, temperatura del servidor, número de items en una cola.

```
Ejemplo: process_memory_bytes = 245678912
Ejemplo: active_connections = 47
```

**Histogram (Histograma):**
Mide la distribución de valores. Es el tipo más poderoso para medir latencia. Un histogram divide las observaciones en "buckets" (cubetas): cuántas requests tardaron menos de 100ms, cuántas entre 100ms y 500ms, cuántas más de 500ms. Esto permite calcular percentiles.

**Summary:**
Similar al histogram pero calcula percentiles del lado del cliente. Menos flexible que el histogram para consultas posteriores.

### Por qué los percentiles importan más que el promedio

El promedio miente. Supón que tienes 100 requests: 99 responden en 10ms y 1 responde en 10 segundos. El promedio es ~110ms, lo cual suena razonable. Pero 1 de cada 100 usuarios espera 10 segundos.

Los percentiles revelan la verdad:
- **P50 (mediana)**: el 50% de requests responde en X ms o menos.
- **P95**: el 95% de requests responde en X ms o menos. El 5% más lento puede ser 10x más lento que la mediana.
- **P99**: el 99% de requests responde en X ms o menos. Indica la experiencia del 1% más afectado.

En sistemas financieros o de salud, ese 1% que tiene mala experiencia puede ser crítico. Las empresas fijan SLOs (Service Level Objectives) típicamente sobre P95 o P99: "el P99 de latencia debe ser menor a 2 segundos".

### Las 4 Golden Signals de Google SRE

Google SRE (Site Reliability Engineering) definió 4 métricas que cualquier servicio debe monitorear:

```
┌─────────────────────────────────────────────────────────┐
│              4 GOLDEN SIGNALS                           │
│                                                         │
│  LATENCY     Cuánto tarda una request exitosa           │
│  TRAFFIC     Cuánta demanda tiene el sistema            │
│  ERRORS      Tasa de requests que fallan                │
│  SATURATION  Qué tan "lleno" está el sistema            │
└─────────────────────────────────────────────────────────┘
```

Si solo puedes monitorear 4 cosas, que sean estas.

---

## Qué estudiar

### Tipos de métricas

- Counter: solo sube, se usa con rate()
- Gauge: sube y baja, estado instantáneo
- Histogram: distribución, base para percentiles
- Summary: percentiles pre-calculados

### Conceptos

- Throughput: requests por segundo que procesa el sistema
- Latency: tiempo de respuesta, medido con percentiles P50/P95/P99
- Error rate: porcentaje de requests que terminan con error
- Saturation: qué tan cerca está el sistema de su límite (CPU, conexiones, memoria)

### Métricas .NET automáticas

- GC (Garbage Collector): frecuencia y duración de garbage collection
- Memory: heap size, objetos en memoria
- HTTP requests: duración, conteo por status code
- SQL duration: tiempo de queries a base de datos

---

## Conceptos que debes dominar

- Diferencia entre logs y métricas: logs son eventos detallados, métricas son tendencias numéricas ligeras.
- Qué es un histogram: estructura que cuenta observaciones por rangos para calcular percentiles.
- Qué es percentil P95/P99: el valor por debajo del cual cae el 95%/99% de las observaciones.
- Qué es throughput: demanda que procesa el sistema, medida en requests/segundo.
- Cómo medir rendimiento real: con histogramas de latencia y tasas de error, no con promedios.

---

## Qué deberías poder construir

Una API que exponga `/metrics` y puedas ver:

- Requests por segundo
- Tasa de errores
- Distribución de latencia (P50, P95, P99)
- Uso de memoria
- CPU

---

## Videos recomendados

```
Canal: IBM Technology — "Prometheus Explained"
Canal: TechWorld with Nana — "Prometheus Tutorial"
Búsqueda: "Prometheus metrics types explained counter gauge histogram"
Búsqueda: "P95 P99 latency explained"
Búsqueda: "4 golden signals SRE Google"
Búsqueda: ".NET metrics OpenTelemetry tutorial"
```

---

# ETAPA 3 — Tracing Distribuido

---

## Objetivo

Entender cómo seguir una request completa a través de múltiples servicios y detectar exactamente dónde ocurre un problema.

---

## Explicación Teórica

### El problema que resuelve el tracing

Los logs y las métricas te dicen *qué* pasó y *cuánto*, pero no te dicen *cómo fluyó una operación específica*. En un sistema con múltiples servicios, una transacción puede pasar por 6 servicios, hacer 3 queries a base de datos y llamar a 2 APIs externas. Si esa transacción tarda 8 segundos, ¿en cuál de esos 6 servicios está el 80% del tiempo?

El tracing distribuido responde exactamente esa pregunta.

### Trace y Span: los conceptos fundamentales

Un **Trace** representa el viaje completo de una request, desde que entra al sistema hasta que sale. Tiene un **TraceId** único (un UUID) que identifica esa operación a través de todos los servicios.

Un **Span** representa una unidad de trabajo dentro del trace. Cada llamada a un servicio, cada query a base de datos, cada llamada HTTP, se convierte en un span. Un span tiene:

- SpanId: identificador único de este span
- TraceId: el trace al que pertenece
- ParentSpanId: el span que lo llamó (si existe)
- Nombre: qué operación representa
- Timestamps de inicio y fin: para calcular duración
- Atributos: metadata adicional (query SQL ejecutada, URL llamada, etc.)
- Status: si tuvo éxito o error

### El árbol de spans (Waterfall)

Los spans se organizan en un árbol. El span raíz es la request inicial. Cada vez que ese servicio llama a otro, crea un span hijo. La visualización de este árbol en Grafana Tempo se llama "waterfall" o "flame graph" y es extremadamente poderosa:

```
TraceId: abc-123
│
├── [0ms - 850ms] POST /api/orders (orden-service)
│   ├── [2ms - 45ms] SELECT * FROM users WHERE id=42 (SQL)
│   ├── [50ms - 400ms] GET /inventory/check (inventario-service)
│   │   └── [55ms - 395ms] SELECT * FROM stock WHERE sku='ABC' (SQL)
│   ├── [410ms - 800ms] POST /payments/process (pagos-service)
│   │   ├── [415ms - 420ms] Redis GET session:42
│   │   └── [425ms - 798ms] POST https://stripe.com/v1/charges (HTTP externo)
│   └── [805ms - 845ms] POST /notifications/send (notif-service)
```

De este waterfall se ve inmediatamente que:
- La llamada a Stripe tarda 373ms (el cuello de botella)
- La query de inventario tarda 340ms (segundo cuello)
- El resto es rápido

Sin tracing distribuido, encontrar esto requeriría revisar logs de 4 servicios diferentes, correlacionarlos manualmente y hacer cálculos.

### Propagación de contexto

Para que el tracing funcione entre servicios, el TraceId y el SpanId deben viajar en los HTTP headers. El estándar se llama **W3C Trace Context** y define los headers:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
              │  │                                │                │
              │  TraceId (128 bits, hex)           SpanId          flags
              version
```

Cuando el servicio B recibe una llamada del servicio A, lee ese header, lo entiende como "soy un hijo del trace abc-123" y crea sus spans bajo ese trace. Así el árbol se construye automáticamente.

OpenTelemetry y las librerías de instrumentación hacen esto automáticamente. No tienes que manejar los headers manualmente.

---

## Qué estudiar

### Trace

Representación completa del viaje de una request. Tiene un TraceId único.

### Span

Unidad de trabajo dentro de un trace. Tiene inicio, fin, nombre, atributos y status.

### TraceId

Identificador único de un trace. Se propaga entre servicios.

### SpanId

Identificador único de un span dentro del trace.

### Parent span

El span que originó la llamada. Establece la relación jerárquica del árbol.

### Child span

Span creado por una llamada desde otro span.

---

## Conceptos que debes dominar

- Qué es distributed tracing: seguimiento del camino completo de una request a través de múltiples servicios.
- Qué es contexto distribuido: la información (TraceId, SpanId) que viaja entre servicios para construir el árbol de spans.
- Cómo una request pasa entre servicios: vía HTTP con headers de contexto que propagan la identidad del trace.
- Cómo detectar cuellos de botella: mirando el waterfall y encontrando el span con mayor duración.
- Cómo detectar queries lentas: los spans de SQL tienen la query ejecutada como atributo, puedes ver exactamente cuál query tardó.

---

## Qué deberías poder construir

Un sistema donde:

- Una request atraviese varios servicios
- Puedas ver el waterfall completo en Grafana Tempo
- Puedas detectar exactamente en qué servicio y qué operación ocurrió el retraso

---

## Videos recomendados

```
Canal: IBM Technology — "Distributed Tracing Explained"
Canal: ByteByteGo — "Distributed Tracing"
Búsqueda: "distributed tracing explained microservices"
Búsqueda: "OpenTelemetry tracing tutorial .NET"
Búsqueda: "TraceId SpanId W3C trace context"
Búsqueda: "Jaeger Tempo tracing tutorial"
```

---

# ETAPA 4 — OpenTelemetry

---

## Objetivo

Entender el estándar moderno y vendor-neutral de observabilidad, que unifica los tres pilares.

---

## Explicación Teórica

### El problema antes de OpenTelemetry

Antes de OpenTelemetry, cada proveedor tenía su propio SDK y formato:

- Si usabas Datadog, instrumentabas con el SDK de Datadog. Si luego querías migrar a Grafana, reescribías toda la instrumentación.
- Si usabas Jaeger para trazas y Prometheus para métricas, cada uno tenía su forma de instrumentar.
- Los vendors tenían "vendor lock-in": una vez que instrumentabas con su SDK, cambiarlo era muy costoso.

### Qué es OpenTelemetry

OpenTelemetry (OTEL) es un estándar abierto y vendor-neutral, mantenido por la CNCF (Cloud Native Computing Foundation, la misma organización detrás de Kubernetes). Define:

1. **Una API**: cómo instrumentas tu código (crear spans, registrar métricas, escribir logs).
2. **Un SDK**: implementación de la API para cada lenguaje (.NET, Java, Python, Go, etc.).
3. **Un protocolo**: OTLP (OpenTelemetry Protocol), el formato estándar para transportar telemetría.
4. **Un Collector**: componente para recibir, procesar y exportar telemetría a distintos backends.

La idea central es: instrumentas tu código una sola vez con OpenTelemetry y luego puedes enviar esa telemetría a cualquier backend (Prometheus, Loki, Tempo, Jaeger, Datadog, New Relic, etc.) simplemente cambiando la configuración del Collector, sin tocar el código de tu aplicación.

```
Tu API (.NET + OTEL SDK)
        │
        │ OTLP
        ▼
OTEL Collector
        │
        ├──► Prometheus (métricas)
        ├──► Loki (logs)
        └──► Tempo (trazas)
```

### Auto-instrumentación vs instrumentación manual

OpenTelemetry ofrece dos modos:

**Auto-instrumentación**: paquetes NuGet que, al agregarlos, instrumentan automáticamente sin cambiar tu código:

- `OpenTelemetry.Instrumentation.AspNetCore`: crea un span por cada HTTP request
- `OpenTelemetry.Instrumentation.HttpClient`: crea un span por cada llamada HTTP saliente
- `OpenTelemetry.Instrumentation.SqlClient`: crea un span por cada query SQL
- `OpenTelemetry.Instrumentation.Runtime`: métricas del runtime .NET (GC, memoria)

**Instrumentación manual**: cuando quieres crear spans propios para operaciones que no son automáticamente instrumentadas. Por ejemplo, si tienes una operación de negocio crítica que quieres medir específicamente.

Para la mayoría de los casos, la auto-instrumentación cubre el 80% de lo necesario con cero cambios en el código existente.

### OTLP: el protocolo

OTLP (OpenTelemetry Protocol) es el formato de transporte estándar. Es un protocolo binario eficiente basado en Protocol Buffers (el mismo formato que usa gRPC). Las señales (logs, métricas, trazas) se serializan en OTLP y se envían al Collector via gRPC o HTTP.

### Resource attributes

Un "resource" en OpenTelemetry es la descripción del proceso que genera la telemetría. Los resource attributes son metadatos que se adjuntan a toda la telemetría de ese proceso:

```
service.name: "orders-service"
service.version: "1.2.3"
deployment.environment: "production"
host.name: "server-01"
container.id: "a1b2c3d4"
```

Esto permite filtrar en Grafana "muéstrame solo las métricas del servicio orders-service en producción versión 1.2.3".

---

## Qué estudiar

### OpenTelemetry SDK

El SDK que se integra en tu aplicación. En .NET son paquetes NuGet.

### Instrumentation

Proceso de agregar telemetría a tu código, automática o manual.

### Exporters

Componentes del SDK que envían la telemetría al destino (Collector, Prometheus, etc.).

### OTLP

OpenTelemetry Protocol: formato binario estándar para transportar telemetría.

### Resource attributes

Metadata del proceso que genera telemetría (nombre del servicio, versión, ambiente).

---

## Instrumentaciones automáticas importantes en .NET

- ASP.NET Core: spans por request HTTP entrante
- HttpClient: spans por llamadas HTTP salientes
- SQLClient: spans por queries SQL
- Runtime: métricas del runtime (.NET GC, memoria)
- Exceptions: registro automático de excepciones en spans

---

## Conceptos que debes dominar

- Qué es OpenTelemetry: estándar abierto vendor-neutral para logs, métricas y trazas.
- Qué es OTLP: el protocolo de transporte estándar de OpenTelemetry.
- Diferencia entre SDK y Collector: el SDK vive en tu aplicación, el Collector es un proceso separado que recibe y enruta la telemetría.
- Qué es instrumentation: el acto de agregar capacidad de observabilidad al código.
- Qué significa vendor-neutral: instrumentas una vez, cambias el backend sin tocar el código.

---

## Qué deberías poder construir

Una API instrumentada con OpenTelemetry que exporte automáticamente:

- Métricas HTTP (requests/segundo, latencia, errores)
- Trazas por cada request con spans de SQL y HTTP
- Logs correlacionados con el TraceId

---

## Videos recomendados

```
Canal: OpenTelemetry (canal oficial YouTube) — Getting Started
Canal: Grafana — "OpenTelemetry for .NET Developers"
Búsqueda: "OpenTelemetry .NET tutorial 2024"
Búsqueda: "OpenTelemetry ASP.NET Core getting started"
Búsqueda: "OTLP explained OpenTelemetry protocol"
Búsqueda: "OpenTelemetry vs Prometheus vs Jaeger"
```

---

# ETAPA 5 — OpenTelemetry Collector

---

## Objetivo

Entender el corazón de la plataforma de observabilidad: el componente que recibe, procesa y enruta toda la telemetría.

---

## Explicación Teórica

### Por qué un Collector y no exportar directamente

Podrías hacer que tu aplicación envíe métricas directamente a Prometheus, logs directamente a Loki y trazas directamente a Tempo. ¿Por qué agregar el Collector en el medio?

**Desacoplamiento**: tu aplicación no necesita saber qué backends existen. Solo habla OTLP con el Collector. Si mañana decides agregar Elasticsearch además de Loki, cambias la config del Collector, no el código de todas tus aplicaciones.

**Procesamiento**: el Collector puede transformar, filtrar, samplear y enriquecer la telemetría antes de enviarla. Tu aplicación no tiene que hacer eso.

**Eficiencia**: el Collector agrupa (batch) la telemetría antes de enviarla, reduciendo la cantidad de conexiones y mejorando el throughput.

**Reducción de costos**: con sampling puedes decidir que solo el 10% de las trazas exitosas se guarden, pero el 100% de las trazas con errores. Esto reduce drásticamente el costo de almacenamiento.

**Resiliencia**: si un backend está caído, el Collector puede hacer retry, guardar en buffer y no perder la telemetría.

### La arquitectura del Collector: Pipelines

El Collector organiza el flujo de telemetría en pipelines. Un pipeline tiene tres etapas:

```
Receivers → Processors → Exporters

OTLP gRPC ─┐
            ├─► [batch] → [memory_limiter] ─┬─► Prometheus
OTLP HTTP ─┘                                ├─► Loki
                                            └─► Tempo
```

**Receivers** (receptores): puntos de entrada. El más común es `otlp` que acepta conexiones en gRPC (puerto 4317) y HTTP (puerto 4318).

**Processors** (procesadores): transforman la telemetría en tránsito:

- `batch`: agrupa telemetría y la envía en lotes para eficiencia
- `memory_limiter`: limita el uso de memoria del Collector para evitar OOM
- `filter`: descarta telemetría que no necesitas (reduce costos)
- `attributes`: agrega, modifica o elimina atributos
- `probabilistic_sampler`: muestrea trazas (guarda solo X% de las trazas)
- `tail_sampling`: sampling inteligente basado en el resultado (guardar 100% de errores, 10% de éxitos)
- `redaction`: elimina datos sensibles (PII, contraseñas) de la telemetría

**Exporters** (exportadores): destinos de salida. Envían a Prometheus, Loki, Tempo, Jaeger, Datadog, o cualquier otro backend compatible.

### Separación de señales en pipelines

El Collector puede tener pipelines separados para cada tipo de señal:

```yaml
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/tempo]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [loki]
```

Esto da control granular: puedes samplear trazas sin afectar logs, puedes filtrar métricas sin afectar trazas.

---

## Qué estudiar

### Receivers

Puntos de entrada del Collector. OTLP es el más importante.

### Exporters

Destinos de salida: Prometheus, Loki, Tempo, y otros.

### Processors

Transformadores de telemetría: batch, memory limiter, filtering, sampling, redaction.

### Pipelines

Configuración que conecta receivers → processors → exporters para cada tipo de señal.

---

## Processors importantes

- `batch`: agrupa telemetría para envío eficiente, reduce carga en backends
- `retry`: reintenta envíos fallidos automáticamente
- `memory_limiter`: protege el Collector de usar demasiada RAM
- `filter`: descarta telemetría innecesaria (reduce costos de almacenamiento)
- `probabilistic_sampler`: guarda solo X% de las trazas para controlar costos
- `tail_sampling`: sampling inteligente: 100% de errores, muestra de éxitos
- `redaction`: elimina PII y datos sensibles (compliance, GDPR)

---

## Conceptos que debes dominar

- Por qué usar Collector: desacoplamiento, procesamiento, eficiencia, resiliencia.
- Cómo enruta telemetría: via pipelines configurables por tipo de señal.
- Qué es batching: agrupar telemetría para enviar en lotes eficientes.
- Qué es sampling: guardar solo una fracción de las trazas para controlar costos.
- Cómo reducir costos: filtering de telemetría innecesaria + tail sampling.

---

## Qué deberías poder construir

Un Collector configurado que:

- Reciba OTLP de múltiples servicios
- Exporte logs a Loki
- Exporte trazas a Tempo
- Exporte métricas a Prometheus

---

## Videos recomendados

```
Canal: OpenTelemetry (canal oficial) — "OpenTelemetry Collector Tutorial"
Canal: Grafana — "OpenTelemetry Collector Deep Dive"
Búsqueda: "OpenTelemetry Collector tutorial beginners"
Búsqueda: "OTEL Collector pipelines configuration"
Búsqueda: "OpenTelemetry Collector docker compose"
Búsqueda: "tail sampling OpenTelemetry"
```

---

# ETAPA 6 — Prometheus

---

## Objetivo

Entender el sistema de métricas estándar de la industria y su lenguaje de consultas PromQL.

---

## Explicación Teórica

### Cómo Prometheus almacena datos: Time Series

Prometheus es una base de datos de series de tiempo (time series database). Una serie de tiempo es una secuencia de pares (timestamp, valor) identificada por un nombre y un conjunto de etiquetas (labels):

```
http_requests_total{service="orders", method="POST", status="200"} = 15432 @ 2026-05-11T14:00:00
http_requests_total{service="orders", method="POST", status="200"} = 15435 @ 2026-05-11T14:00:15
http_requests_total{service="orders", method="POST", status="200"} = 15441 @ 2026-05-11T14:00:30
```

Cada combinación única de nombre + labels es una serie de tiempo separada. Esto permite queries muy precisas: "dame solo las requests POST exitosas del servicio de pedidos".

### Scraping: cómo Prometheus recolecta métricas

A diferencia de otros sistemas donde la aplicación *envía* métricas (push), Prometheus *descarga* (pull/scrape) las métricas de los servicios periódicamente. Tu API expone un endpoint `/metrics` con un formato texto específico, y Prometheus visita ese endpoint cada 15 segundos (configurable) y guarda los valores.

```
Prometheus ──scrape cada 15s──► tu-api:8080/metrics
```

El formato del endpoint es texto plano:

```
# HELP http_requests_total Total number of HTTP requests
# TYPE http_requests_total counter
http_requests_total{method="POST",status="200"} 15432
http_requests_total{method="POST",status="500"} 23
```

OpenTelemetry y librerías como prometheus-net generan este formato automáticamente.

### Labels y cardinalidad

Los labels son lo que hace a Prometheus tan poderoso, pero también su principal riesgo. Cada combinación única de label values crea una nueva serie de tiempo. Si agregas un label `user_id` con millones de valores distintos, creas millones de series de tiempo, lo que consume memoria y CPU exponencialmente.

Regla: los labels deben tener **baja cardinalidad** (pocos valores posibles). Ejemplos buenos: `method`, `status`, `service`, `environment`. Ejemplos malos: `user_id`, `transaction_id`, `ip_address`.

### PromQL: el lenguaje de consultas

PromQL (Prometheus Query Language) es el lenguaje para consultar las series de tiempo. Ejemplos:

```promql
# Tasa de requests por segundo en los últimos 5 minutos
rate(http_requests_total[5m])

# Porcentaje de errores
rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m]) * 100

# Latencia P95 calculada desde un histogram
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Memoria usada en MB
process_memory_bytes / 1024 / 1024
```

Estas queries son las que alimentan los paneles de Grafana.

---

## Qué estudiar

### Scraping

Cómo Prometheus recolecta métricas haciendo pull del endpoint /metrics.

### Time Series

Estructura de datos de Prometheus: nombre + labels + (timestamp, valor).

### Labels

Dimensiones de una métrica. Permiten filtrar. Deben tener baja cardinalidad.

### PromQL

Lenguaje de consultas para métricas en Prometheus.

---

## Qué estudiar técnicamente

### Queries PromQL esenciales

- `rate()`: tasa de cambio por segundo de un counter
- `increase()`: incremento total de un counter en un período
- `histogram_quantile()`: calcula percentiles desde un histogram

### Alerting

Definición de alertas basadas en queries PromQL. AlertManager gestiona el envío de notificaciones.

### Recording Rules

Pre-cálculo de queries complejas para mejorar performance de dashboards.

---

## Conceptos que debes dominar

- Cómo Prometheus almacena métricas: time series identificadas por nombre + labels.
- Qué es cardinality: número de series de tiempo únicas. Alta cardinality = más memoria y CPU.
- Cómo funciona PromQL: lenguaje de consultas funcional sobre time series.
- Cómo funcionan alertas: reglas que evalúan queries PromQL periódicamente y disparan notificaciones.

---

## Qué deberías poder construir

Dashboards y alertas para:

- Tasa de errores por servicio
- Latencia P95/P99 por endpoint
- CPU y memoria por contenedor
- Requests por segundo
- Alertas cuando error rate > 1%

---

## Videos recomendados

```
Canal: TechWorld with Nana — "Prometheus Tutorial | Monitoring with Prometheus and Grafana"
Canal: IBM Technology — "Prometheus Explained"
Canal: Grafana (oficial) — "Getting started with Prometheus"
Búsqueda: "PromQL tutorial for beginners"
Búsqueda: "Prometheus alerting AlertManager tutorial"
Búsqueda: "histogram_quantile PromQL explained"
Búsqueda: "Prometheus cardinality explained"
```

---

# ETAPA 7 — Loki

---

## Objetivo

Centralizar logs profesionalmente con Grafana Loki, el sistema de logs diseñado para ser eficiente y correlacionado con métricas y trazas.

---

## Explicación Teórica

### Qué es Loki y por qué no es Elasticsearch

El sistema tradicional de centralización de logs es el stack ELK (Elasticsearch, Logstash, Kibana). Elasticsearch indexa todos los campos de los logs, lo que lo hace muy potente para búsqueda pero extremadamente costoso en almacenamiento y CPU.

Loki, creado por Grafana Labs, tiene un enfoque diferente: **solo indexa los labels, no el contenido del log**. El contenido del log se comprime y se guarda sin indexar. Esto lo hace mucho más eficiente y económico, a cambio de que las búsquedas full-text son más lentas (pero siguen siendo usables).

La filosofía de Loki es "como Prometheus pero para logs": usa el mismo sistema de labels, se integra nativamente con Grafana, y está diseñado para correlacionar con métricas del mismo sistema.

### LogQL: el lenguaje de consultas de Loki

Loki tiene su propio lenguaje de consultas llamado LogQL, inspirado en PromQL:

```logql
# Todos los logs de error del servicio de órdenes
{service="orders-service"} |= "ERROR"

# Logs de un TraceId específico
{service="orders-service"} | json | traceId="abc-123-xyz"

# Tasa de líneas de log de error por minuto
rate({service="orders-service"} |= "ERROR" [1m])

# Extraer el campo userId del JSON del log
{service="orders-service"} | json | userId="42"
```

### Labels en Loki

Al igual que Prometheus, Loki usa labels para identificar streams de logs. Los streams son secuencias de logs del mismo origen. Un label típico en Loki sería:

```
{service="orders-service", environment="production", host="server-01"}
```

La regla de baja cardinalidad aplica igual que en Prometheus: no uses campos como `user_id` o `request_id` como labels, esos van dentro del contenido del log como propiedades JSON.

### Correlación: el superpoder de Grafana + Loki + Tempo

La razón más poderosa para usar Loki junto a Tempo y Prometheus es la correlación nativa en Grafana:

1. Estás mirando un dashboard de Prometheus y ves un spike de errores a las 2pm.
2. Haces clic en el spike y Grafana te lleva a los logs de Loki de ese mismo momento.
3. En los logs ves el TraceId del error.
4. Haces clic en el TraceId y Grafana te lleva al trace en Tempo.
5. En Tempo ves el waterfall completo y el span exacto que falló con el error.

Este flujo de investigación de incidentes que antes tomaba horas ahora toma minutos.

---

## Qué estudiar

### Labels

Identificadores de streams de logs en Loki. Deben tener baja cardinalidad.

### LogQL

Lenguaje de consultas de Loki para filtrar y agregar logs.

### Structured logs en Loki

Cómo Loki parsea logs JSON para permitir filtros por campos específicos.

### Correlation

Cómo enlazar logs con trazas (via TraceId) y métricas (via timestamps).

---

## Qué debes dominar

- Buscar logs por TraceId en Loki
- Correlacionar logs con trazas en Grafana
- Filtrar errores rápidamente con LogQL
- Entender la diferencia de arquitectura entre Loki y Elasticsearch

---

## Qué deberías poder construir

Un sistema donde:

- Los logs de todos los servicios están centralizados en Loki
- Puedes buscar errores por servicio, por TraceId, por usuario
- Tienes correlación directa entre logs y trazas en Grafana

---

## Videos recomendados

```
Canal: Grafana (oficial) — "Getting started with Grafana Loki"
Canal: TechWorld with Nana — "Loki Tutorial"
Búsqueda: "Grafana Loki tutorial Docker"
Búsqueda: "LogQL tutorial Loki"
Búsqueda: "Serilog Loki sink .NET"
Búsqueda: "Loki vs Elasticsearch comparison"
Búsqueda: "Grafana correlate logs traces metrics"
```

---

# ETAPA 8 — Tempo

---

## Objetivo

Implementar tracing distribuido enterprise con Grafana Tempo como backend de almacenamiento de trazas.

---

## Explicación Teórica

### Qué es Tempo

Grafana Tempo es un backend de almacenamiento para trazas distribuidas. Recibe trazas en formato OTLP (o Jaeger, Zipkin) y las almacena de forma eficiente para consulta posterior.

A diferencia de Jaeger (el backend de trazas más antiguo), Tempo está diseñado para:
- Escalar masivamente: almacena trazas en object storage (S3, GCS, Azure Blob)
- Ser económico: no indexa el contenido de los spans, solo el TraceId
- Integrarse nativamente con Grafana para la experiencia de correlación completa

### Trace storage y búsqueda

Tempo almacena trazas por TraceId. Para encontrar una traza puedes:

1. Buscar directamente por TraceId (si lo tienes de un log o de un error)
2. Usar TraceQL (el lenguaje de consultas de Tempo) para buscar trazas por atributos:
   - Trazas con duración > 2 segundos
   - Trazas que terminaron en error
   - Trazas del servicio X que llamaron al endpoint Y
3. Desde Grafana, hacer clic en un TraceId que aparece en un log de Loki

### Context propagation en detalle

Cuando el OTEL SDK instrumenta tu código, el proceso de propagación funciona así:

1. El primer servicio recibe una request sin headers de tracing → genera un nuevo TraceId
2. El OTEL SDK intercepta automáticamente cada llamada HTTP saliente y agrega los headers `traceparent`
3. El siguiente servicio recibe la llamada, lee el `traceparent`, crea un span hijo bajo ese TraceId
4. Todos los spans se envían al Collector que los reenvía a Tempo
5. Tempo los almacena y puede reconstruir el árbol completo porque todos los spans tienen el mismo TraceId

### Sampling en Tempo

Para sistemas de alto tráfico, guardar el 100% de las trazas es muy costoso. El sampling decide qué trazas guardar:

**Head sampling**: decisión al inicio de la request. Rápido, pero puede descartar trazas de errores que no sabíamos que iban a fallar.

**Tail sampling**: decisión al final del trace, cuando ya se conoce el resultado. Más costoso pero permite: guardar 100% de errores, guardar 100% de trazas lentas (>2s), guardar solo 5% de trazas exitosas rápidas.

El tail sampling se configura en el OTEL Collector con el processor `tail_sampling`.

---

## Qué estudiar

### Trace storage

Cómo Tempo almacena y indexa trazas por TraceId.

### Span visualization

El waterfall view en Grafana: visualización jerárquica de spans con tiempos.

### Context propagation

Propagación de TraceId/SpanId entre servicios via HTTP headers (W3C Trace Context).

### Sampling

Estrategias para reducir volumen de trazas almacenadas manteniendo utilidad.

---

## Qué debes dominar

- Seguir requests entre servicios usando el waterfall de Tempo
- Detectar cuellos de botella analizando qué span tiene mayor duración
- Encontrar spans lentos y ver sus atributos (query SQL, URL, etc.)
- Entender el waterfall view: cada barra es un span, el ancho es la duración

---

## Qué deberías poder construir

Visualización completa en Grafana de una request que pase por:

- Tu API principal (span raíz)
- Queries SQL (spans de base de datos)
- Llamadas a Redis (spans de cache)
- Llamadas a APIs externas (spans HTTP saliente)

---

## Videos recomendados

```
Canal: Grafana (oficial) — "Getting started with Grafana Tempo"
Canal: Grafana (oficial) — "Distributed tracing with Grafana"
Búsqueda: "Grafana Tempo tutorial OpenTelemetry"
Búsqueda: "distributed tracing visualization Grafana"
Búsqueda: "tail sampling OpenTelemetry Collector"
Búsqueda: "Tempo TraceQL tutorial"
```

---

# ETAPA 9 — Grafana

---

## Objetivo

Visualización y observabilidad unificada: el panel de control que une logs, métricas y trazas en una sola interfaz.

---

## Explicación Teórica

### Qué es Grafana y por qué es el estándar

Grafana es una plataforma de visualización y observabilidad open source. No almacena datos propios (en su versión básica): se conecta a data sources (Prometheus, Loki, Tempo, y decenas más) y los visualiza en dashboards.

Lo que hace a Grafana tan poderosa para observabilidad enterprise:

1. **Multi-datasource**: un solo dashboard puede tener un panel de Prometheus, otro de Loki y otro de Tempo al mismo tiempo.
2. **Correlación nativa**: puedes hacer clic en un punto de una métrica de Prometheus y que Grafana te lleve a los logs de Loki de ese momento exacto.
3. **Alerting**: Grafana puede evaluar queries de cualquier datasource y enviar alertas.
4. **Variables de template**: un mismo dashboard puede servir para cualquier servicio o ambiente usando variables dinámicas.
5. **Explore mode**: modo de investigación libre donde puedes escribir queries sin necesidad de un dashboard configurado.

### Data Sources

Un data source es la configuración de la conexión a un backend de datos. En Grafana, configuras los data sources una vez y luego los usas en cualquier panel:

```
Data Sources en Grafana:
├── Prometheus → http://prometheus:9090
├── Loki       → http://loki:3100
└── Tempo      → http://tempo:3200
```

### Panels y Dashboards

Un **panel** es una visualización individual: un gráfico de series de tiempo, un gauge, una tabla, un histograma, un log viewer.

Un **dashboard** es una colección de paneles organizados en una cuadrícula. Un dashboard bien diseñado para una API podría tener:

- Gráfico de requests/segundo
- Gráfico de tasa de errores
- Gráfico de latencia P50/P95/P99
- Gauge de CPU y memoria
- Panel de logs recientes de error
- Tabla de endpoints más lentos

### Variables de template

Las variables hacen los dashboards reutilizables. En vez de hacer un dashboard por servicio, haces uno con una variable `$service` que se puede seleccionar desde un dropdown:

```promql
# Sin variable (solo para orders-service):
rate(http_requests_total{service="orders-service"}[5m])

# Con variable (funciona para cualquier servicio):
rate(http_requests_total{service="$service"}[5m])
```

### Alerting en Grafana

Grafana tiene su propio sistema de alertas (Grafana Alerting). Funciona así:

1. Defines una **alert rule**: una query que se evalúa periódicamente (ej: cada minuto).
2. Si la query supera un threshold, la alerta pasa a estado "Firing".
3. Grafana envía la notificación via un **contact point**: email, Slack, PagerDuty, Teams, etc.
4. Puedes configurar **notification policies**: quién recibe qué tipo de alerta.

---

## Qué estudiar

### Dashboards

Colecciones de paneles para visualizar el estado del sistema.

### Panels

Tipos: Time series, Stat, Gauge, Bar chart, Table, Logs, Traces.

### Variables

Templates que hacen los dashboards reutilizables para cualquier servicio o ambiente.

### Datasources

Conexiones a Prometheus, Loki, Tempo y otros backends.

### Alerting

Reglas de alerta basadas en queries, con notificaciones a múltiples canales.

---

## Qué debes dominar

- Crear dashboards con los 4 Golden Signals
- Construir paneles de logs con Loki
- Navegar entre métricas, logs y trazas en Grafana
- Configurar alertas con umbrales y contact points
- Usar variables de template para dashboards reutilizables

---

## Qué deberías poder construir

Un dashboard enterprise con:

- Métricas de tráfico, errores y latencia
- Panel de logs recientes con filtro de errores
- Link directo a trazas de Tempo desde los logs
- Alertas de error rate y latencia
- Variables para filtrar por servicio y ambiente

---

## Videos recomendados

```
Canal: Grafana (oficial) — "Getting started with Grafana"
Canal: TechWorld with Nana — "Grafana Tutorial for Beginners"
Canal: Grafana (oficial) — "Grafana Alerting Tutorial"
Búsqueda: "Grafana dashboard tutorial 2024"
Búsqueda: "Grafana template variables tutorial"
Búsqueda: "Grafana Loki Tempo correlation"
Búsqueda: "Grafana alerting Slack notification"
```

---

# ETAPA 10 — Observabilidad Enterprise Avanzada

---

## Objetivo

Entrar al nivel enterprise: diseñar estrategias de observabilidad para sistemas complejos, controlar costos y medir calidad de servicio.

---

## Explicación Teórica

### RED Method y USE Method: frameworks de métricas

En lugar de monitorear métricas al azar, estas metodologías proveen un framework estructurado.

**RED Method** (para servicios/APIs):

- **R**ate: ¿cuántas requests por segundo procesa este servicio?
- **E**rrors: ¿qué porcentaje de esas requests termina en error?
- **D**uration: ¿cuánto tarda cada request? (distribución, no promedio)

RED responde: "¿está este servicio funcionando bien para sus usuarios?"

**USE Method** (para recursos de infraestructura):

- **U**tilization: ¿qué porcentaje del recurso está siendo usado? (CPU al 70%)
- **S**aturation: ¿hay trabajo esperando porque el recurso está lleno? (queue de requests)
- **E**rrors: ¿hay errores en el recurso? (disk errors, network drops)

USE responde: "¿está este recurso siendo un cuello de botella?"

En la práctica, para observabilidad completa aplicas RED a cada servicio y USE a cada recurso de infraestructura.

### SLI, SLO y SLA

Estos tres conceptos son la base de la ingeniería de confiabilidad moderna (SRE):

**SLI (Service Level Indicator)**: una métrica específica que mide la calidad del servicio. Ejemplo: "el porcentaje de requests HTTP que responden en menos de 500ms".

**SLO (Service Level Objective)**: el objetivo que te fijas para un SLI. Ejemplo: "el SLI de latencia debe ser >= 99.5%". Es decir, el 99.5% de las requests deben responder en menos de 500ms.

**SLA (Service Level Agreement)**: el contrato formal con un cliente que define consecuencias (económicas, contractuales) si el SLO no se cumple. Ejemplo: "si el SLO de disponibilidad baja del 99.9%, el cliente recibe un crédito".

```
┌──────────────────────────────────────────────────────────┐
│  SLI: porcentaje requests < 500ms = 99.7%                │
│  SLO: ese porcentaje debe ser >= 99.5% (CUMPLIDO)        │
│  SLA: si baja de 99.0%, el cliente recibe crédito        │
│                                                          │
│  Error Budget = SLO - 1 = 0.5%                          │
│  (puedes "gastar" 0.5% de requests lentas sin violar SLO)│
└──────────────────────────────────────────────────────────┘
```

El **Error Budget** es la cantidad de "fallas permitidas" antes de violar el SLO. Es un concepto poderoso: si tu equipo gastó el error budget del mes en deploys riesgosos, el SRE framework dice que deben pausar los deploys y enfocarse en confiabilidad.

### High Cardinality

La cardinalidad en sistemas de observabilidad es el número de valores únicos que puede tener una dimensión. En Prometheus, alta cardinalidad significa demasiadas series de tiempo. En Loki, demasiados label values.

El problema práctico: si agregas `user_id` como label en Prometheus con 1 millón de usuarios, Prometheus necesita mantener 1 millón de series de tiempo en memoria. Esto puede consumir cientos de GB de RAM.

La solución para high cardinality es usar **sistemas especializados** como Grafana Tempo (que es exactamente un sistema diseñado para high cardinality, porque cada TraceId es único) o herramientas como Honeycomb o ClickHouse para casos extremos.

### Retention y Cost Optimization

En producción, guardar todos los datos para siempre no es económico ni necesario. La estrategia estándar:

- **Métricas (Prometheus)**: retención de 15 días a 3 meses en storage rápido. Para histórico largo, usar Thanos o Grafana Mimir que archivan a object storage (S3).
- **Logs (Loki)**: retención de 30-90 días. Logs de debug con retención más corta (7 días). Logs de errores críticos con retención más larga (6 meses).
- **Trazas (Tempo)**: retención de 7-30 días. Las trazas son las más costosas de almacenar; el sampling agresivo es esencial.

### Kubernetes Observability

En entornos Kubernetes, la observabilidad agrega complejidad porque:

- Los pods se crean y destruyen dinámicamente
- Las IPs cambian constantemente
- Hay múltiples capas: nodo, pod, contenedor, aplicación

Herramientas clave para Kubernetes observability:

- **kube-state-metrics**: expone el estado de los recursos de Kubernetes como métricas Prometheus
- **node-exporter**: expone métricas del nodo (CPU, memoria, disco, red)
- **OpenTelemetry Operator**: gestiona el Collector en Kubernetes via Custom Resources

---

## Qué estudiar

### Sampling

Head sampling vs tail sampling. Estrategias para controlar costos de trazas.

### High Cardinality

Qué es, por qué es un problema en Prometheus/Loki, cómo manejarlo.

### Retention

Políticas de retención por tipo de señal y nivel de importancia.

### Cost Optimization

Filtering, sampling, compresión, tiered storage para reducir costos.

### Multi-tenancy

Separar observabilidad de múltiples equipos/ambientes en la misma plataforma.

### Kubernetes Observability

Monitoreo de pods, nodos, deployments, y aplicaciones en K8s.

---

## Conceptos avanzados

### RED Method

- Rate: requests por segundo
- Errors: tasa de errores
- Duration: distribución de latencia

### USE Method

- Utilization: porcentaje de uso del recurso
- Saturation: trabajo esperando en cola
- Errors: errores del recurso

### SLI/SLO/SLA

- SLI: la métrica específica que mides
- SLO: el objetivo que te fijas
- SLA: el contrato formal con consecuencias
- Error Budget: la cantidad de fallas permitidas antes de violar el SLO

### Incident Response

Proceso para detectar, diagnosticar y resolver incidentes usando la plataforma de observabilidad.

---

## Qué debes dominar

- Diseñar una estrategia de observabilidad con RED y USE
- Definir SLIs y SLOs para servicios críticos
- Configurar retention y sampling para controlar costos
- Construir alertas útiles que reduzcan ruido
- Diagnosticar incidentes usando el flujo: métricas → logs → trazas

---

## Videos recomendados

```
Canal: Google Cloud Tech — "SLIs, SLOs, SLAs explained"
Canal: IBM Technology — "SRE explained"
Búsqueda: "SLI SLO SLA tutorial Google SRE"
Búsqueda: "RED method USE method observability"
Búsqueda: "high cardinality Prometheus problem"
Búsqueda: "Kubernetes observability Prometheus Grafana"
Búsqueda: "OpenTelemetry production best practices"
Canal: ByteByteGo — "System Design observability"
```

---

# Ruta práctica recomendada

---

## Proyecto 1 — Básico: Structured Logging

Construye:

- API .NET con Serilog
- Logs estructurados en JSON
- CorrelationId middleware
- Logs de duración de requests

**Lo que aprenderás**: diferencia entre logging básico y profesional.

---

## Proyecto 2 — Métricas con Prometheus y Grafana

Agrega:

- OpenTelemetry SDK a la API
- Endpoint /metrics con métricas HTTP
- Prometheus scrapeando la API en Docker Compose
- Grafana conectado a Prometheus
- Dashboard básico con requests/segundo, errores y latencia

**Lo que aprenderás**: el ciclo completo de métricas desde la app hasta el dashboard.

---

## Proyecto 3 — Tracing Distribuido

Agrega:

- OpenTelemetry Collector en Docker Compose
- Grafana Tempo como backend de trazas
- Auto-instrumentación de HttpClient y SqlClient
- Visualización del waterfall en Grafana

**Lo que aprenderás**: cómo seguir una request completa con todos sus spans.

---

## Proyecto 4 — Full Observability Stack

Agrega:

- Loki para centralización de logs
- Serilog con sink a OTEL Collector
- Grafana con los tres datasources (Prometheus, Loki, Tempo)
- Correlación entre logs y trazas via TraceId

**Lo que aprenderás**: la plataforma completa funcionando con correlación nativa.

---

## Proyecto 5 — Enterprise

Agrega:

- Múltiples microservicios con tracing distribuido
- Queries SQL instrumentadas (spans de base de datos)
- Cache Redis instrumentado
- Llamadas a APIs externas con spans HTTP
- Alertas de error rate y latencia en Grafana
- Tail sampling en el Collector
- Dashboards avanzados con variables de template
- SLOs definidos y medidos

**Lo que aprenderás**: cómo funciona observabilidad en un sistema real con múltiples dependencias.

---

# Qué deberías entender al final

Si completas todo esto deberías poder explicar técnicamente:

- Cómo fluye una request entre microservicios y cómo se rastrea
- Cómo se detecta un cuello de botella usando el waterfall de Tempo
- Cómo funciona OpenTelemetry internamente: SDK → Collector → Backends
- Cómo funciona tracing distribuido con W3C Trace Context
- Cómo Grafana correlaciona logs de Loki, trazas de Tempo y métricas de Prometheus
- Cómo Prometheus almacena time series y por qué la cardinalidad importa
- Cómo diseñar una estrategia de observabilidad enterprise con SLOs
- Cómo instrumentar correctamente una API .NET con OpenTelemetry
- Cómo diagnosticar un incidente de producción: métrica → log → traza
- Cómo controlar costos con sampling, filtering y retention policies

---

# Para comunicar esto en tu empresa

## El argumento para directivos

> "Observabilidad es la capacidad de entender el estado interno de nuestros sistemas a partir de sus salidas externas. Sin ella, cuando hay un problema en producción tardamos horas en diagnosticarlo. Con ella, tardamos minutos. Esto reduce directamente el costo de incidentes, aumenta la confianza para hacer cambios y nos da visibilidad del comportamiento real de los usuarios."

## El argumento para equipos técnicos

> "Con OpenTelemetry instrumentamos una sola vez y podemos enviar logs, métricas y trazas a cualquier backend sin reescribir código. Con Grafana, Prometheus, Loki y Tempo tenemos una plataforma open source que hace lo mismo que Datadog o New Relic a una fracción del costo y sin vendor lock-in."

## El argumento para cumplimiento y auditoría

> "Los logs estructurados y el tracing distribuido nos dan trazabilidad completa de cada operación: qué procesó el sistema, cuándo, con qué resultado, y en qué secuencia. Esto es evidencia de auditoría nativa del sistema."

---

# Canal de referencia principal

Para esta ruta completa, el canal más completo y actualizado en español/inglés:

```
Canal YouTube: TechWorld with Nana
  → Tiene tutoriales de Docker, Kubernetes, Prometheus, Grafana
  → Explicaciones claras desde cero con demos prácticas

Canal YouTube: Grafana (oficial)
  → Tutoriales de todas las herramientas del stack (Loki, Tempo, Grafana, Mimir)
  → Webinars técnicos profundos

Canal YouTube: IBM Technology
  → Explicaciones conceptuales de distributed tracing, observability, SRE
  → Muy bueno para entender el "por qué" antes del "cómo"

Canal YouTube: ByteByteGo
  → System design con diagramas claros
  → Observability, distributed systems, architecture

Playlist recomendada para empezar:
  Busca: "IBM Technology observability explained" → ver la serie completa
  Luego: "TechWorld with Nana Prometheus Grafana tutorial"
  Luego: "Grafana getting started with Loki Tempo"
```
