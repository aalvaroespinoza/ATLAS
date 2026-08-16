# AGENTS.md — ATLAS Personal OS

Este archivo se lee al inicio de cada sesión de Antigravity. Es el único lugar
donde persiste contexto entre sesiones — no asumas que el agente recuerda nada
de conversaciones anteriores salvo lo que esté acá.

## Qué es este proyecto

ATLAS es un sistema personal **local-first** para Windows, construido en
**C# + .NET MAUI Blazor Hybrid (WebView2)**. Centraliza captura, conocimiento y
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
5. **UI en .NET MAUI Blazor Hybrid (Razor + HTML/CSS/Tailwind sobre WebView2). No Electron, no Chromium empaquetado — WebView2 es el motor nativo de Windows.** Decisión tomada en Etapa 5, después de comparar con WinUI 3 nativo (ver `/docs/decisions.md`).
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
11. **Regla de producto — Anti Dashboard:** ATLAS no debe mostrar una
    capacidad únicamente porque exista. Las capacidades aparecen mediante
    contexto, búsqueda, comandos o navegación cuando realmente aportan valor.
    No crear una pantalla, tarjeta, widget o elemento de navegación
    simplemente para representar un módulo existente.
    **Prioridad estricta de la interfaz:**
    1. Contexto
    2. Acciones
    3. Información relevante
    4. Navegación
    5. Configuración
    *(Nunca invertir ese orden sin justificación explícita).*

## Stack técnico

- C# / .NET (net10.0+)
- **UI: .NET MAUI Blazor Hybrid** (Razor components + HTML/CSS/Tailwind renderizados vía
  WebView2, target Windows únicamente — sin Android/iOS/MacCatalyst).
- SQLite vía `Microsoft.Data.Sqlite`
- Core/Storage siguen siendo C# puro, sin ninguna referencia a UI.
- Sin ORMs pesados, sin DI containers externos salvo que se justifique

## Estructura del repo

```
ATLAS.sln
/src
  /ATLAS.UI          (.NET MAUI Blazor Hybrid Windows-only, Shell, Launcher flotante)
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

## Identidad visual y experiencia — "Menos interfaz, más capacidad"

ATLAS se concibe como un **Personal Command Center** premium, minimalista y
profundamente integrado al sistema operativo, inspirado en Linear (jerarquía
fuerte, navegación silenciosa), Craft (acceso rápido y split-view) y Raycast
(command-first interaction):
- **Superficies oscuras neutras** (#090d14 / #111620) con bordes hairline (1px white/0.07).
- **Cero colecciones de tarjetas de colores ni gradientes llamativos**.
- **Cero color-coding permanente por módulo**: un único acento primario sobrio y colores semánticos solo para estados (éxito, error, pendiente).
- **Estructura basada en listas, filas, texto y separadores limpios**.
- **Sidebar visualmente secundaria** y silenciosa (monocromo).
- **Command Launcher (Ctrl+Space)** como punto de interacción hero y central.
- **IA contextual** en el flujo de trabajo (no una sección gigante promocional).

## Etapa 4 — COMPLETADA (hasta el bloque 4a; 4b pendiente de retomar)

Telegram (long polling, mapeo de mensajes a Commands) andando. Finanzas
(4b) queda pausado — se retoma después de Etapa 5, ya con UI para
mostrar el balance en algo mejor que texto.

## Etapa 5 — COMPLETADA

UI construida en **.NET MAUI Blazor Hybrid** (target Windows únicamente, Tailwind vía Play CDN):
- **5a (Setup MAUI Blazor):** Proyecto `ATLAS.UI` exclusivo para Windows, integración de Tailwind Play CDN, inyección de dependencias (`DatabaseInitializer`, `CommandRegistry`, `WindowsPasswordVault`, `TelegramListenerService`). Core, Storage y Tests 100% desacoplados e intactos.
- **5b (Shell de navegación):** Sidebar curada de 6 items exactos (Inicio, Capturar, Buscar, Hábitos y Goals, Finanzas, Configuración) con estética glassmorphism (Opción B) y tipografía Inter.
- **5c (Dashboard de Inicio y Páginas):** Dashboard en Inicio con las 5 tarjetas curadas del mockup (Hábitos de hoy con racha, Metas en foco, Finanzas con datos reales de SQLite, Segundo Cerebro con notas recientes, y AI Toolbox conectada a Gemini con `ai.ask`). Cero mock data.

## Alcance actual — Etapa 6: Roadmaps + Gmail (o retomar Etapa 4b Finanzas en profundidad)

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