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

## Etapa 3 — COMPLETADA

Goals + Habits (tablas, commands, launcher, pantalla Inicio). Ya andando,
no se toca salvo bugs.

## Alcance actual — Etapa 4: Telegram + Finanzas (no avanzar sin confirmación)

Se divide en dos bloques secuenciales, no simultáneos:

**4a — Integración con Telegram (long polling, no webhook):**
- Bot creado externamente vía @BotFather (fuera del repo, a mano).
- El token del bot se guarda con el mismo mecanismo seguro que ya existe
  para la key de Gemini (reusar/generalizar el secure store existente,
  no crear uno nuevo desde cero).
- `TelegramListenerService`: long polling (`getUpdates`) corriendo en
  background mientras la app está abierta. Sin webhook, sin servidor
  propio expuesto.
- Mapeo mínimo de mensajes a Commands existentes: texto libre →
  `capture.note` (con `source = "telegram"`), y un prefijo simple para
  completar un habit (ej. `/habit <nombre>` → `habit.complete`). Nada
  de `/expense` todavía — depende de que exista el módulo de Finanzas
  (bloque 4b).
- El bot responde confirmando la acción ejecutada (texto simple, no
  botones ni menús interactivos todavía).

**4b — Finanzas (recién después de que 4a esté estable):**
- Tabla `transactions`: fecha, monto, tipo, origen, descripción, moneda,
  categoría, subcategoría, id_externo, estado, metadata.
- Command `finance.add_transaction` (carga manual) — debe poder
  invocarse desde el Launcher **y** desde Telegram vía el prefijo
  `/expense <monto> <descripción>` (reusando el mismo Command, no
  duplicando lógica).
- Integración con Mercado Pago vía **personal access token** (no OAuth
  de app pública — es uso personal, alcanza con el token que da el panel
  de developers de MP), guardado con el mismo secure store. Command
  `finance.sync_mercadopago` que trae movimientos recientes y los
  normaliza a `transactions`. Sync manual (a demanda o al abrir la app),
  no polling automático en esta etapa.
- Categorización: manual en esta etapa. La categorización automática con
  IA (ya tenés `ai.ask` de la Etapa 2) queda para una iteración
  posterior, no de este bloque.

Explícitamente **fuera de alcance** todavía: WhatsApp, Gmail, Roadmaps,
IA local, categorización automática de gastos, webhooks de ningún tipo,
sincronización automática en background de Mercado Pago.

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