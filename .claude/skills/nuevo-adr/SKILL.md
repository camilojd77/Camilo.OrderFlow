---
name: nuevo-adr
description: "Registra una decisión de arquitectura nueva o el reemplazo de una existente en docs/adr de Camilo.OrderFlow (número, plantilla, índice y línea en CLAUDE.md §7). Úsala cuando se decida algo con alternativas reales o cuando un cambio contradiga un ADR vigente."
argument-hint: "<título de la decisión>"
---

# Nuevo ADR

1. Lee [`docs/adr/README.md`](../../../docs/adr/README.md) y busca si ya hay un ADR sobre el tema. Si lo hay y la
   decisión cambia, el nuevo **reemplaza** al antiguo (paso 5); no edites el antiguo salvo su estado.
2. Número = el mayor existente + 1, con cuatro dígitos. Archivo `docs/adr/NNNN-<decision-en-minusculas-con-guiones>.md`.
3. Copia [`docs/adr/plantilla.md`](../../../docs/adr/plantilla.md). Título en positivo (la decisión, no el problema).
   Estado `Propuesta` salvo que la persona diga que está aceptada; fecha de hoy; «Decide» = quien decide (pregunta si
   no lo sabes); «Revisar cuando» = un evento concreto.
4. Contexto con las **alternativas reales**; Decisión en frases verificables; Consecuencias (qué obliga a hacer);
   Referencias a archivos reales del repo (comprueba que existen).
5. Si reemplaza a otro: en el nuevo, «Reemplaza a ADR-XXXX» en Contexto; en el antiguo, cambia **solo** la línea de
   estado a `Reemplazada por [ADR-NNNN](NNNN-....md)`.
6. En el mismo cambio: fila en la tabla de `docs/adr/README.md` y línea en `CLAUDE.md` §7 («Decisiones deliberadas»).
   Si afecta a reglas, skills o `docs/architecture.md`, actualízalos también.
7. Verifica: los números del índice, de los archivos y de `CLAUDE.md` §7 coinciden, y los enlaces resuelven.
