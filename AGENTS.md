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

## Stack técnico

- C# / .NET (net10.0+)
- **UI: .NET MAUI Blazor Hybrid** (Razor components + HTML/CSS renderizados vía
  WebView2, target Windows únicamente — sin Android/iOS/MacCatalyst). Estilos
  con Tailwind. Reemplaza al proyecto WinUI 3 planteado originalmente en
  ATLAS.UI (decisión de Etapa 5, ver `/docs/decisions.md`).
- SQLite vía `Microsoft.Data.Sqlite` (o `sqlite-net-pcl` si simplifica el ORM)
- Core/Storage siguen siendo C# puro, sin ninguna referencia a UI — por eso
  el cambio de WinUI a Blazor Hybrid no los toca.
- Sin ORMs pesados, sin DI containers externos salvo que se justifique

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

## Identidad visual (definida)

ATLAS usa una estética inspirada en iOS: superficies con Mica/Acrylic
(vidrio esmerilado), esquinas bien redondeadas, animaciones tipo spring
en transiciones y hover states, tipografía con jerarquía clara (no todo
el mismo tamaño/peso). Mismo lenguaje visual que ya usás en tu setup de
Hyprland — no se inventa uno nuevo para ATLAS. Se define UNA vez en un
`ResourceDictionary`/tema compartido antes de construir pantallas, no se
improvisa pantalla por pantalla.

## Etapa 4 — COMPLETADA (hasta el bloque 4a; 4b pendiente de retomar)

Telegram (long polling, mapeo de mensajes a Commands) andando. Finanzas
(4b) queda pausado — se retoma después de Etapa 5, ya con UI para
mostrar el balance en algo mejor que texto.

## Alcance actual — Etapa 5: UI real en .NET MAUI Blazor Hybrid (no avanzar sin confirmación)

Decisión tomada: la UI se construye en **.NET MAUI Blazor Hybrid**, no WinUI 3
puro (comparado con mockups, se eligió la estética web/Tailwind). El Core,
Storage y Commands no se tocan — siguen siendo C# puro, consumidos igual
que antes, ahora desde componentes Razor en vez de XAML.

Sigue vigente el principio anti-saturación de la sección 3 del doc de
producto: sidebar curada máximo 6 items, dashboard máximo 4-6 tarjetas.

Se divide en tres bloques secuenciales:

**5a — Setup del proyecto MAUI Blazor Hybrid:**
- Nuevo proyecto `ATLAS.UI` (reemplaza al de WinUI 3, que nunca se llegó a
  implementar con contenido real — solo existía el esqueleto).
- Target únicamente Windows (sin Android/iOS/MacCatalyst) para no cargar
  workloads ni complejidad innecesaria.
- Tailwind vía CDN para esta etapa (Play CDN) — suficiente para iterar
  rápido. Migrar a un build pipeline de Tailwind real queda para más
  adelante, solo si el CDN empieza a molestar (FOUC, tamaño).
- El Launcher (Ctrl+Space) sigue siendo una ventana flotante nativa
  aparte — no hace falta que sea Blazor también, puede seguir en XAML
  si eso resulta más simple para una ventana chica y rápida. Definir en
  el plan, no asumir.

**5b — Shell de navegación (Razor):**
- Layout con sidebar de 6 items (Inicio, Capturar, Buscar, Hábitos y
  Goals, Finanzas, Configuración), cada uno una página Razor que
  consume los Commands/ViewModels existentes.

**5c — Dashboard interactivo en Inicio:**
- Tarjetas con datos reales (nunca mock data), reusando el mismo
  contenido curado validado en el mockup: racha de hábitos, goals
  activos, Segundo Cerebro, AI Toolbox, y Finanzas mostrando el estado
  real de "pausado" si 4b sigue sin retomarse.

Explícitamente **fuera de alcance** todavía: build pipeline de Tailwind,
personalización de tarjetas, Roadmaps, Gmail, WhatsApp, IA local,
retomar el bloque 4b de Mercado Pago.

## Etapa 6 — Roadmaps + Gmail (después de cerrar Etapa 5)

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