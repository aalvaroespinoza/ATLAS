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
   **Cualquier dependencia cloud (Supabase incluido) es una excepción explícita, no la norma — ver sección "Sync cloud" abajo.**
7. **Nunca exponer credenciales, tokens o secretos en el cliente.** (Windows Credential Locker / DPAPI).
8. **El repo debe compilar y quedar funcional después de cada bloque de trabajo.**
9. **Aislamiento de Superficies de Interacción:** Cada bloque de trabajo modifica como máximo una experiencia principal (Dock ≠ Home, Home ≠ Launcher, Launcher ≠ Search, Search ≠ Context Actions).
10. **Personalidad y Calidez:** La interfaz debe sentirse moderna, personal, expresiva y fluida ("una herramienta hecha para mí").
11. **Un pedido abierto no es luz verde para alcance abierto.** Si Álvaro pide algo amplio y sin acotar ("mejorá el diseño", "hacé que se vea mejor", "lo que haga falta"), el primer paso es SIEMPRE proponer un plan concreto y acotado (qué se toca, qué no) y esperar confirmación — nunca interpretarlo como autorización para rediseñar módulos enteros, agregar integraciones nuevas (cloud, terceros) o inventar una numeración de etapas propia. Esto es lo que falló entre la Etapa 6 y la 13: un pedido abierto de diseño terminó en 7 etapas no confirmadas y una integración cloud no pedida (Supabase). No se repite.

## Sync cloud — Supabase (formalizado)

**Propósito real, confirmado con el usuario:** poder ver los datos de ATLAS
desde el celular/navegador en el futuro (no hay companion app todavía,
pero se diseña la base de datos pensando en eso desde ahora).

**Estado de seguridad: en hardening (ver Etapa 14 abajo).** El esquema
original (`docs/supabase_schema.sql`) usaba políticas RLS
`USING (true) WITH CHECK (true)` — es decir, RLS activado pero sin
restricción real: cualquiera con la anon key lee y escribe todo. Esto
se está corrigiendo a Supabase Auth real (un solo usuario) + RLS scoped
a `auth.uid()`, ver Etapa 14.

Reglas específicas para este módulo:
- La anon key y las credenciales de sesión van al mismo `ISecretVault`
  que todo lo demás. Nunca en `wwwroot`, nunca en un `.razor` como
  string literal, nunca commiteadas.
- Ninguna tabla se expone sin políticas RLS scoped a `auth.uid()`.
  "Allow all" no es una política válida en este proyecto.
- El sync sigue siendo a demanda (o disparado por el usuario), no un
  polling constante en background salvo que se decida explícitamente.

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
- **Etapas 7 a 12 (Reset Visual & Funcional, no planeado con el usuario
  de antemano — pedido abierto de diseño que se expandió sin control):**
  Design System Índigo/Obsidiana, Personal Dock, Command Launcher,
  Universal Search, Context Actions, microinteracciones. Completado a
  nivel código, **adoptado retroactivamente como lenguaje visual
  oficial del proyecto** tras revisión.
- **Etapa 13:** Blindaje del Lenguaje Visual (Completada).
- **Sync cloud (Supabase):** implementado con seguridad insuficiente
  (RLS abierto). **Pendiente de hardening — ver Etapa 14.**
- **Pendiente sin hacer:** limpieza de código muerto (`NavMenu.razor`,
  `DesignSystemDemo.razor`, `DockPrototype.razor`), y
  `finance.categorize` (categorización automática de gastos con IA,
  planeada originalmente para el bloque 7b y nunca implementada porque
  el número de etapa se usó para otra cosa).

## Alcance actual — Etapa 14: Hardening de Supabase + limpieza (no avanzar sin confirmación)

**14a — Seguridad de Supabase:**
- Agregar Supabase Auth (email/password, un solo usuario: Álvaro).
- Agregar columna `user_id` a las 7 tablas sincronizadas, poblada con
  el `auth.uid()` del usuario autenticado.
- Reemplazar las políticas `USING (true)` por políticas scoped:
  `USING (auth.uid() = user_id)` en cada tabla, para SELECT/INSERT/
  UPDATE/DELETE.
- El login se hace una sola vez desde Configuración; el refresh token
  de la sesión va a `ISecretVault`, igual que el resto de credenciales.
- Rotar la anon key actual del proyecto de Supabase (buena práctica
  dado que el schema estuvo público con políticas abiertas, aunque la
  key en sí nunca se filtró).

**14b — Limpieza de código muerto (después de 14a):**
- Eliminar `NavMenu.razor` (reemplazado por `AtlasPersonalDock`, sin
  referencias activas).
- Eliminar o mover fuera de `Components/Pages` los prototipos
  `DesignSystemDemo.razor` y `DockPrototype.razor` si no se usan en
  producción.

**14c — Retomar 7b: categorización automática de gastos con IA:**
- Command `finance.categorize` (transaction_id → sugiere categoría vía
  `ai.ask`/Gemini sobre la descripción, sugerencia editable, nunca
  sobreescribe una categoría cargada a mano sin confirmación).