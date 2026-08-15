# Architecture Decisions Log

- 2026-08-15: Solución estructurada en capas desacopladas (ATLAS.Core, ATLAS.Storage, ATLAS.UI, ATLAS.Core.Tests) usando .NET 10, Windows App SDK (WinUI 3), Microsoft.Data.Sqlite y CommunityToolkit.Mvvm.
- 2026-08-15: Infraestructura de Command System implementada en ATLAS.Core (ICommand, ICommandRegistry, CommandRegistry, CommandResult, CommandParameterDescriptor) con ejecución asíncrona segura.
- 2026-08-15: Implementado ATLAS.Storage con SQLite local (%LocalAppData%\ATLAS\atlas.db), tabla única notes (id, content, created_at, source), DatabaseInitializer y NotesRepository.
- 2026-08-15: Implementado primer comando real CaptureNoteCommand ('capture.note') en ATLAS.Core, registrado en CommandRegistry y probado a través del CommandRegistry contra SQLite.
- 2026-08-15: Implementado Global Launcher en ATLAS.UI: hotkey global Ctrl+Space (Win32 RegisterHotKey + SetWindowSubclass), ventana flotante sin bordes ni barra de título en tercio superior, ejecución de capture.note con Enter y cierre con Escape/blur.
- 2026-08-15: Etapa 2a: Extendido modelo y tabla notes con title (opcional), type (default 'note'), tags y source mediante migración no destructiva e idempotente en DatabaseInitializer.
- 2026-08-15: Etapa 2a: Implementado comando KnowledgeSearchCommand ('knowledge.search') con búsqueda simple LIKE (case-insensitive) sobre title, content y tags con orden cronológico descendente y límite configurable.
- 2026-08-15: Etapa 2a: Launcher dual en ATLAS.UI con Live Search (debounce 160ms), lista interactiva con navegación por teclado (flechas), vista expandida de notas y captura automática con Enter cuando no hay selección.
- 2026-08-15: Etapa 2a: Implementada ventana secundaria ActivityWindow en ATLAS.UI (lista cronológica de solo lectura, lector de detalle dividido y hotkey global Ctrl+Shift+Space).
- 2026-08-15: Etapa 2b: Almacenamiento seguro de secretos mediante abstracción ISecretVault e implementación WindowsPasswordVault (Windows Credential Locker). Interfaz IAiProvider con implementación GeminiProvider (Google Gemini gemini-1.5-flash) y ventana de configuración SettingsWindow en WinUI 3.
- 2026-08-15: Etapa 2b: Implementados comandos AiSummarizeCommand ('ai.summarize') y AiAskCommand ('ai.ask') en ATLAS.Core desacoplados del proveedor concreto e integrados al CommandRegistry.
- 2026-08-15: Etapa 2b: Launcher integrado con IA: acción 'Resumir con IA' en notas expandidas, modo de consulta directa mediante prefijo '?' (ej. '? explicar Rust') e indicador visual de procesamiento con Gemini en segundo plano.
- 2026-08-15: Etapa 3a: Implementadas tablas SQLite (goals, habits, habit_events) en DatabaseInitializer con foreign keys activadas, entidades de dominio (Goal, Habit, HabitEvent) y repositorios (GoalsRepository, HabitsRepository).
- 2026-08-15: Etapa 3a: Implementados comandos GoalCreateCommand ('goal.create') y GoalUpdateProgressCommand ('goal.update_progress') en ATLAS.Core, con auto-completado de status al alcanzar 100% de progreso.
- 2026-08-15: Etapa 3a: Implementados comandos HabitCreateCommand ('habit.create') y HabitCompleteCommand ('habit.complete') en ATLAS.Core. Se determinó que múltiples registros de un hábito en el mismo día son totalmente válidos como eventos inmutables en habit_events (ej. hidratación o repeticiones múltiples).
- 2026-08-15: Etapa 3b: Extendido Global Launcher para soportar creación de metas (/goal), creación de hábitos (/habit) y completado de hábitos (/done, !done, hecho) mediante búsqueda en vivo reactiva sin formularios.
