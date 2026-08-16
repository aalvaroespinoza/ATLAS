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

## Design Philosophy

ATLAS se concibe como un **Personal Command Center** moderno, minimalista,
táctil y con identidad propia, inspirado conceptualmente en el ritmo de Linear,
la velocidad de Raycast y la pureza de lectura de Craft, pero con alma personal:

1. **Personal OS, no ERP:** ATLAS no es un software empresarial, ni un gestor administrativo, ni un panel SaaS de analíticas. Debe sentirse como una herramienta personal que el usuario construyó para sí mismo y que realmente quiere usar todos los días.
2. **Personal Dock, no sidebar administrativa:** Navegación en isla flotante, silenciosa, compacta (56px) con foco en herramientas hero (⚡ Capturar, ⌘ Comandos) y espacios esenciales.
3. **Command-First (`Ctrl+Space`):** El Command Launcher es la puerta de entrada neurálgica a todo el sistema. Toda acción o búsqueda debe poder ejecutarse en menos de 2 segundos desde el teclado.
4. **Context-First & Anti-Dashboard:** La pantalla no muestra cosas simplemente porque existen. La jerarquía estricta es:
   1. Contexto actual (Now)
   2. Acciones inmediatas
   3. Información relevante
   4. Navegación
   5. Configuración
5. **Menos cards, más tipografía y listas:** Cero card grids decorativas. La estructura se basa en listas (`AtlasListItem`), filas limpias, separadores sutiles y tipografía con buen contraste (Inter + JetBrains Mono).
6. **Menos navegación permanente:** Mantener la interfaz limpia de botones innecesarios; las acciones aparecen donde y cuando el contexto las demanda.
7. **IA Transversal y Silenciosa:** Google Gemini (`IAiProvider`) se integra naturalmente en el flujo de trabajo (`ai.ask`, `ai.summarize`, extracción de tareas) a través de `IContextActionService` y el Launcher, no como una sección promocional o un chatbot gigante aislado.
8. **Un solo lenguaje visual y paleta unificada:** 
   - Base: Obsidiana Profunda (`#090b10` / `#10141e` / `#121724` / `#171e2e`).
   - Accent Principal: Índigo / Violeta Frío (`#6366f1` / `#818cf8`).
   - Semánticos: Solo para estados reales (`emerald` para éxito, `rose` para error/peligro, `amber` para hitos en curso).
   - Cero color-coding permanente por módulo.
9. **Microinteracciones Snappy y Naturales:** Movimiento elástico de 120ms a 140ms (`cubic-bezier(0.16, 1, 0.3, 1)`), respuesta táctil en clics (`scale: 0.98`) y halos de foco precisos. Cero esperas lentas ni rebotes exagerados.

### Lo que NO debe hacerse (Anti-patrones Prohibidos)

- ❌ **NO crear Dashboards llenos de widgets ni métricas de vanidad** (evitar grids de cards cuadradas, contadores gigantes innecesarios, balances descontextualizados).
- ❌ **NO agregar 10 items permanentes de navegación** (el Dock mantiene únicamente los 4-5 espacios principales).
- ❌ **NO crear tarjetas (cards) por cada feature** (usar listas, texto estructurado y filas satinadas).
- ❌ **NO asignar un color diferente a cada módulo** (todos los módulos comparten la misma base neutra e índigo).
- ❌ **NO poner botones para absolutamente todo** (priorizar <kbd>Enter</kbd>, <kbd>Ctrl+Enter</kbd>, <kbd>Tab</kbd> y atajos).
- ❌ **NO agregar animaciones decorativas, partículas, brillos excesivos ni transiciones lentas.**
- ❌ **NO duplicar lógica entre UI y Core** (todo pasa por Commands existentes e `IContextActionService`).
- ❌ **NO agregar pantallas, componentes o elementos visuales simplemente porque una funcionalidad técnica existe en el backend.**

## Reglas no negociables

1. **No implementar todo el documento de una vez.** Se trabaja por etapas,
   una feature autocontenida por vez.
2. **No agregar funciones, pantallas ni módulos no solicitados.** Proponer en el plan antes de tocar código.
3. **Separación estricta de capas**: UI → Core → Modules / Integrations.
   La UI nunca accede directo a SQLite ni a integraciones externas. Todo pasa
   por Commands del Core.
4. **Todo se implementa como Command.** Forma: `id`, `name`, `description`,
   `input schema`, `execute()`, `result`. Se implementa una sola vez y lo
   reutilizan UI, launcher, Telegram y automatizaciones.
5. **UI en .NET MAUI Blazor Hybrid (Razor + HTML/CSS/Tailwind sobre WebView2).** Windows target únicamente.
6. **SQLite local desde el día 1.** No se introduce ninguna dependencia sin justificarla en el plan.
7. **Nunca exponer credenciales, tokens o secretos en el cliente.** (Windows Credential Locker / DPAPI).
8. **El repo debe compilar y quedar funcional después de cada bloque de trabajo.**
9. **Aislamiento de Superficies de Interacción:** Cada bloque de trabajo modifica como máximo una experiencia principal (Dock ≠ Home, Home ≠ Launcher, Launcher ≠ Search, Search ≠ Context Actions).
10. **Personalidad y Calidez:** La interfaz debe sentirse moderna, personal, expresiva y fluida ("una herramienta hecha para mí").

## Stack técnico

- C# / .NET (net10.0+)
- **UI: .NET MAUI Blazor Hybrid** (Razor components + HTML/CSS/Tailwind renderizados vía
  WebView2, target Windows únicamente).
- SQLite vía `Microsoft.Data.Sqlite`
- Core/Storage en C# puro, sin ninguna referencia a UI.
- DI estándar de .NET (`Microsoft.Extensions.DependencyInjection`).

## Estructura del repo

```
ATLAS.sln
/src
  /ATLAS.UI          (.NET MAUI Blazor Hybrid Windows-only, Shell, Dock, Launcher, Design System)
  /ATLAS.Core        (Command system, Event bus, IA, Integraciones, Context services)
  /ATLAS.Storage     (SQLite, migraciones, repositorios)
/docs
  ATLAS_PERSONAL_OS.md   (spec completa)
  decisions.md           (registro de decisiones arquitectónicas)
/tests
  /ATLAS.Core.Tests
```

## Definition of Done (por feature)

- Compila sin warnings nuevos (`dotnet build ATLAS.sln`).
- Pasan el 100% de las pruebas unitarias (`dotnet test`).
- Manejo de errores explícito (cero excepciones silenciadas).
- No rompe funcionalidad existente.
- Se documenta la decisión en `/docs/decisions.md`.

## Proveedor de IA

**Google Gemini API** (vía Google AI Studio, gemini-1.5-flash). Key custodiada en Windows Credential Locker tras la interfaz `IAiProvider`.

## Estado de Etapas del Proyecto

- **Etapa 1:** Core Command System + SQLite Local (Completada).
- **Etapa 2:** Second Brain + Búsqueda de Notas + IA Gemini (Completada).
- **Etapa 3:** Metas & Hábitos + Registro Diario (Completada).
- **Etapa 4a:** Integración Telegram Long Polling (Completada).
- **Etapa 4b:** Módulo de Finanzas y Transacciones (Completada en Core y UI).
- **Etapa 5:** UI MAUI Blazor Hybrid + WebView2 (Completada).
- **Etapa 6:** Integración Gmail OAuth + Roadmaps Secuenciales (Completada).
- **Etapas 7 a 12 (Reset Visual & Funcional):**
  - Design System unificado (Índigo/Obsidiana, suite de 17 componentes `Atlas*`).
  - Personal Dock flotante en isla (56px compacto / 208px expandido).
  - Shell de doble isla con Header minimalista.
  - Home Contextual (Now / Quick Input / Atención de Hábitos / Hitos en Progreso / Feed).
  - Command Launcher Raycast-style (`Ctrl+Space`, sub-menús contextuales con `Tab`).
  - Universal Search Split-View (40/60) con supresión de categorías vacías.
  - Sistema Universal de Context Actions (`IContextActionService` / `AtlasContextActionBar`).
  - Pulido de Microinteracciones y tactilidad (120ms-140ms snappy easing).
  - Auditoría y refactorización final Anti-Dashboard (Finanzas en mono, Captura draft limpio, Hábitos segmentados, Configuración silenciosa).
- **Etapa 13:** Blindaje del Lenguaje Visual & Filosofía de Producto (Completada).