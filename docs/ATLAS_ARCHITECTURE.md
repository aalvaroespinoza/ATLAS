# ATLAS — Arquitectura Técnica

> **Fuente de Verdad de Arquitectura**  
> Este documento describe la arquitectura real implementada en el repositorio y delimita con precisión los componentes existentes frente a objetivos futuros.

---

## 1. Flujo Conceptual de Datos

```
External Sources (Telegram, Mercado Pago, Gmail, Supabase)
       ↓
Input / Capture (Global Hotkeys, Telegram Webhook/Polling, Command Launcher, Quick Canvas)
       ↓
ATLAS Core (Commands, CommandRegistry, Parsing, Security Abstractions)
       ↓
Modules (Second Brain, Habits, Goals, Roadmaps, Finance)
       ↓
Storage (SQLite Local-First, Wal Mode, Schema Migrations)
       ↓
AI (IAiProvider, GeminiProvider, Context Actions)
       ↓
UI / Experience (MAUI Blazor Hybrid, Design System, Personal Dock, Pages)
       ↓
Outputs (Local Display, Action Feedback, Notifications, Cloud Sync)
```

---

## 2. Mapa de Componentes Reales por Capa

### A. External Sources & Integrations
Componentes aislados del Core que interactúan con servicios de terceros:
- **Telegram:** `TelegramListenerService`, `TelegramMessageProcessor` (Long polling vía HttpClient nativo sin SDKs externos).
- **Mercado Pago:** `FinanceSyncMercadoPagoCommand` (Consumo REST con Personal Access Token).
- **Gmail:** `GmailClient`, `GmailListRecentCommand` (OAuth 2.0 en modo solo lectura).
- **Supabase:** `SupabaseAuthService`, `SupabaseSyncService`, `SupabaseSyncCommand` (PostgREST REST API con JWT Bearer token y RLS `auth.uid()`).

### B. Input / Capture
Puntos de entrada de información y comandos:
- **Global Hotkey:** `RegisterHotKey` Win32 (<kbd>Ctrl+Space</kbd>, <kbd>Ctrl+N</kbd>, <kbd>Alt+D</kbd>).
- **Command Launcher:** `CommandLauncher.razor` (Búsqueda multi-dominio y ejecución por teclado).
- **Quick Canvas:** `Capture.razor` (Captura sin distracciones hacia SQLite).
- **Parser de Gastos:** `ExpenseTextParser` (Extracción de montos y conceptos en lenguaje natural).

### C. ATLAS Core (`ATLAS.Core`)
Capa central desacoplada de la UI:
- **Command System:** `ICommand`, `ICommandRegistry`, `CommandRegistry`, `CommandResult`, `CommandParameterDescriptor`.
- **Comandos Implementados:**
  - `CaptureNoteCommand` (`capture.note`)
  - `KnowledgeSearchCommand` (`knowledge.search`)
  - `AiSummarizeCommand` (`ai.summarize`)
  - `AiAskCommand` (`ai.ask`)
  - `GoalCreateCommand` (`goal.create`)
  - `GoalUpdateProgressCommand` (`goal.update_progress`)
  - `HabitCreateCommand` (`habit.create`)
  - `HabitCompleteCommand` (`habit.complete`)
  - `FinanceAddTransactionCommand` (`finance.add_transaction`)
  - `FinanceCategorizeCommand` (`finance.categorize`)
  - `FinanceSyncMercadoPagoCommand` (`finance.sync_mercadopago`)
  - `GmailListRecentCommand` (`gmail.list_recent`)
  - `RoadmapCreateCommand` (`roadmap.create`)
  - `RoadmapAddMilestoneCommand` (`roadmap.add_milestone`)
  - `RoadmapCompleteMilestoneCommand` (`roadmap.complete_milestone`)
  - `SupabaseSyncCommand` (`supabase.sync`)
- **Seguridad & Bóveda:** `ISecretVault`, `SecretKeys`, `PasswordVaultHelper`.

### D. Dominio & Módulos
Entidades de negocio en `ATLAS.Core.Entities`:
- `Note`: Identificador, título, contenido, tipo, tags, fecha de creación, fuente.
- `Goal`: Título, descripción, progreso porcentual, fecha objetivo, estado.
- `Habit` & `HabitEvent`: Nombre, descripción, frecuencia, eventos inmutables de completado.
- `Roadmap` & `RoadmapMilestone`: Rutas vinculadas a metas, hitos secuenciales y progreso.
- `Transaction`: Fecha, monto, tipo (income/expense), origen, descripción, moneda, categoría sugerida/confirmada.

### E. Storage (`ATLAS.Storage`)
Persistencia local-first:
- **Base de Datos:** SQLite local en `%LocalAppData%\ATLAS\atlas.db`.
- **Configuración de Alto Rendimiento:** `PRAGMA journal_mode = WAL;`, `PRAGMA synchronous = NORMAL;`, `PRAGMA temp_store = MEMORY;`, `PRAGMA cache_size = -8000;`.
- **Inicialización y Migraciones:** `DatabaseInitializer` (DDL idempotente con claves foráneas activas).
- **Repositorios:**
  - `NotesRepository` (`INoteRepository`)
  - `GoalsRepository` (`IGoalRepository`)
  - `HabitsRepository` (`IHabitsRepository`)
  - `RoadmapRepository` (`IRoadmapRepository`)
  - `TransactionsRepository` (`ITransactionRepository`)

### F. AI (`ATLAS.Core.Ai`)
- **Abstracción:** `IAiProvider`.
- **Implementación:** `GeminiProvider` (Google AI Studio, endpoint `gemini-1.5-flash-latest:generateContent`).
- **Servicio Contextual:** `IContextActionService` / `ContextActionService`.

### G. UI & Experiencia (`ATLAS.UI`)
- **Host:** .NET 10 MAUI Blazor Hybrid restringido a Windows (`net10.0-windows10.0.19041.0`, unpackaged).
- **Motor CSS:** Tailwind CSS 3.4 compilado localmente en build-time (`app.css` ~41 KB) sin dependencias CDN de red.
- **Design System:** `AtlasButton`, `AtlasIconButton`, `AtlasInput`, `AtlasSearchInput`, `AtlasCommandInput`, `AtlasListItem`, `AtlasBadge`, `AtlasProgress`, `AtlasDivider`, `AtlasContextMenu`, `AtlasFloatingPanel`, `AtlasEmptyState`, `AtlasLoadingState`, `AtlasErrorState`, `AtlasPersonalDock`, `AtlasContextActionBar`.
- **Páginas Principales:**
  - `Home.razor` (Now, Quick Input, Hábitos de Hoy, Hilos Activos, Feed Unificado).
  - `Capture.razor` (Borrador de notas sin distracciones).
  - `Search.razor` (Explorador Universal Split-View 40/60).
  - `HabitsGoals.razor` (Gestión segmentada de Hábitos y Metas/Roadmaps).
  - `Finance.razor` (Balance real, transacciones y sugerencias IA confirmables).
  - `Settings.razor` (Bóveda con PIN de 4 dígitos, claves de APIs y Supabase Auth).

---

## 3. Objetivos Futuros (Fuera del Alcance Actual)

> [!NOTE]
> Los siguientes ítems representan objetivos previstos para fases posteriores y **NO** deben implementarse en las etapas inmediatas:

1. **Cliente Móvil Dedicado:** Aplicación móvil nativa/híbrida que consuma la réplica de Supabase.
2. **Sincronización Bidireccional en Background:** Sincronización continua de dos vías en tiempo real con resolución automática de conflictos.
3. **Modelos Locales de IA (On-Device LLM):** Soporte para Small Language Models locales (ej. vía ONNX Runtime / Phi-3) sin conexión a internet.
4. **Bandeja de Entrada Unificada (Inbox Zero):** Procesamiento interactivo de correos de Gmail con etiquetado y archivo.
