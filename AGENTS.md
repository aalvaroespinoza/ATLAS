# AGENTS.md — ATLAS Personal OS

Este archivo se lee al inicio de cada sesión de Antigravity. Es el único lugar
donde persiste contexto entre sesiones — no asumas que el agente recuerda nada
de conversaciones anteriores salvo lo que esté acá.

## Qué es este proyecto

ATLAS es un sistema personal **local-first** para Windows, construido en
**C# + WinUI 3 + Windows App SDK**. Centraliza captura, conocimiento y
automatización mediante un Core desacoplado de la UI.

La especificación completa de producto vive en `/docs/ATLAS_PERSONAL_OS.md`.
Leela antes de proponer cualquier arquitectura o feature nueva. Este
`AGENTS.md` es un resumen operativo, no la reemplaza.

## Reglas no negociables

1. **No implementar todo el documento de una vez.** Se trabaja por etapas,
   una feature autocontenida por vez. Ver "Alcance actual" abajo.
2. **No agregar funciones, pantallas ni módulos no solicitados.** Si te
   parece que algo "estaría bueno agregar", proponelo en el plan, no lo
   implementes directamente.
3. **Separación estricta de capas**: UI → Core → Modules / Integrations.
   La UI nunca accede directo a SQLite ni a integraciones externas. Todo pasa
   por Commands del Core.
4. **Todo se implementa como Command.** Forma: `id`, `name`, `description`,
   `input schema`, `execute()`, `result`. Se implementa una sola vez y lo
   reutilizan UI, launcher, integraciones futuras (Telegram, etc.) y
   automatizaciones. No dupliques lógica entre UI y Core.
5. **WinUI 3 nativo. Nada de Electron ni web envuelta en ventana.**
6. **SQLite local desde el día 1.** No se introduce ninguna dependencia
   (paquete NuGet, servicio cloud, librería externa) sin justificarla
   explícitamente en el plan antes de tocar código.
7. **No empezar por el dashboard ni por pantallas "lindas".** El producto
   nace del Core y de las acciones (Commands), no de la UI.
8. **Nunca exponer credenciales, tokens o secretos en el cliente.**
9. **El repo debe compilar y quedar funcional después de cada bloque de
   trabajo.** No encadenar cambios grandes sin verificar que compila y
   corre entre medio.
10. Si una tarea requiere tocar más de 2-3 archivos nuevos o parece que se
    está saliendo del alcance pedido, parar y preguntar antes de seguir.

## Stack técnico

- C# / .NET (net10.0+), WinUI 3, Windows App SDK
- SQLite vía `Microsoft.Data.Sqlite` (o `sqlite-net-pcl` si simplifica el ORM)
- MVVM con `CommunityToolkit.Mvvm`
- Sin frameworks de UI adicionales, sin ORMs pesados, sin DI containers
  externos salvo que se justifique (el `Microsoft.Extensions.DependencyInjection`
  que ya trae la plantilla WinUI alcanza para esta etapa)

## Estructura del repo

```
ATLAS.sln
/src
  /ATLAS.UI          (WinUI 3, ventanas, launcher flotante)
  /ATLAS.Core        (Command system, Event bus, servicios de contexto)
  /ATLAS.Storage     (SQLite, migraciones, repositorios)
/docs
  ATLAS_PERSONAL_OS.md   (spec completa, no tocar salvo pedido explícito)
  decisions.md           (decisiones de arquitectura, una línea por decisión)
/tests
  /ATLAS.Core.Tests
```

No crear más carpetas/proyectos que estos sin plan previo.

## Definition of Done (por feature)

- Compila sin warnings nuevos
- Manejo de errores explícito (no `catch` vacíos ni excepciones silenciadas)
- Logs mínimos donde corresponda (no logging masivo)
- No rompe funcionalidad existente
- Tests para Commands con lógica no trivial (no hace falta testear getters/setters)
- Si modifica algo arquitectónico, se agrega una línea en `/docs/decisions.md`

## Proveedor de IA (definido)

Para todo lo que requiera IA cloud: **Google Gemini API** (free tier, vía
Google AI Studio). La key vive en el almacenamiento seguro del usuario
(Windows Credential Locker / DPAPI), nunca en texto plano ni commiteada al
repo. La integración se hace atrás de una interfaz `IAiProvider` para poder
cambiar de proveedor a futuro sin tocar el resto del Core (regla 12 del
doc de producto).

## Etapa 4 — COMPLETADA (hasta el bloque 4a; 4b pendiente de retomar)

Telegram (long polling, mapeo de mensajes a Commands) andando. Finanzas
(4b) queda pausado — se retoma después de Etapa 5, ya con UI para
mostrar el balance en algo mejor que texto.

## Alcance actual — Etapa 5: UI real / Dashboard interactivo (no avanzar sin confirmación)

Hasta acá ATLAS solo tenía el Launcher (Ctrl+Space) como interfaz
visual. Esta etapa le pone una interfaz gráfica de verdad encima del
Core que ya existe — **sin romper el principio anti-saturación de la
sección 3 del doc de producto**. Nada de dashboards con 15 items de
sidebar ni 8 tarjetas simultáneas — eso es justo lo que el doc pide
evitar.

Se divide en dos bloques secuenciales:

**5a — Shell de navegación real:**
- Reemplazar la ventana simple actual por un layout con `NavigationView`
  de WinUI 3.
- Sidebar curada, máximo 6 items: Inicio, Capturar, Buscar (Second
  Brain), Hábitos y Goals, Finanzas, Configuración. Nada de secciones
  "NÚCLEO / HERRAMIENTAS / INTEGRACIONES" separadas ni items para cada
  integración (Gmail, Telegram, MercadoPago no tienen sidebar item
  propio — viven dentro de Configuración como conexiones).
- Cada item navega a una Page que consume los Commands/ViewModels que
  YA existen (habit.complete, goal.*, knowledge.search, finance.*) — no
  se duplica lógica de negocio en la UI.
- El Launcher (Ctrl+Space) se mantiene intacto y sigue siendo el modo
  rápido — la navegación completa es un complemento, no un reemplazo.

**5b — Dashboard interactivo en Inicio (recién después de 5a estable):**
- Reemplaza el texto plano de Inicio de la Etapa 3 por tarjetas
  visuales reales, con datos reales de los repositorios ya existentes
  (nunca mock data): racha de hábitos con barra de progreso, goals
  activos con progreso, balance financiero reciente, notas capturadas
  recientes. Máximo 4-6 tarjetas, elegidas por relevancia real de uso,
  no "porque quedan lindas".
- Un gráfico simple (ej. balance de los últimos 30 días) usando una
  librería de charts justificada en el plan antes de agregarla como
  dependencia (ej. LiveCharts2, que soporta WinUI).
- Todo interactivo: click en una tarjeta navega a la sección
  correspondiente, no es solo decorativo.

Explícitamente **fuera de alcance** todavía: personalización de qué
tarjetas se muestran (eso es Etapa 7+, cuando haya uso real para saber
qué se ignora), Roadmaps, Gmail, WhatsApp, IA local.

## Pendiente — Etapa 4b (Finanzas, retomar después de Etapa 5)

No se toca hasta cerrar Etapa 5. Cuando se retome:

- Tabla `transactions`: fecha, monto, tipo, origen, descripción, moneda,
  categoría, subcategoría, id_externo, estado, metadata.
- Command `finance.add_transaction` (carga manual) — invocable desde el
  Launcher **y** desde Telegram vía `/expense <monto> <descripción>`
  (reusando el mismo Command, no duplicando lógica).
- Mercado Pago vía **personal access token** (no OAuth de app pública),
  guardado en el secure store generalizado de la Etapa 4a. Command
  `finance.sync_mercadopago`, sync manual (a demanda), sin polling
  automático.
- Categorización manual por ahora; automática con IA queda para después.

## Flujo de trabajo esperado

```
Explorar → Plan (mostrar antes de tocar código) → Implementar en bloques
chicos → Verificar que compila y corre → Commit con mensaje claro
```

No asumas luz verde para pasar de "Plan" a "Implementar": esperá
confirmación explícita del tipo "dale, implementá" o "corregí esto y
recién ahí implementá".

## Convenciones de commit

Mensajes en español, formato: `tipo: descripción corta`
(`feat:`, `fix:`, `refactor:`, `docs:`, `test:`). Un commit por bloque
funcional, no un commit gigante al final del día.