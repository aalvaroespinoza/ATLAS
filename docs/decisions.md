# Architecture Decisions Log

- 2026-08-15: Solución estructurada en capas desacopladas (ATLAS.Core, ATLAS.Storage, ATLAS.UI, ATLAS.Core.Tests) usando .NET 10, Windows App SDK (WinUI 3), Microsoft.Data.Sqlite y CommunityToolkit.Mvvm.
- 2026-08-15: Infraestructura de Command System implementada en ATLAS.Core (ICommand, ICommandRegistry, CommandRegistry, CommandResult, CommandParameterDescriptor) con ejecución asíncrona segura.
- 2026-08-15: Implementado ATLAS.Storage con SQLite local (%LocalAppData%\ATLAS\atlas.db), tabla única notes (id, content, created_at, source), DatabaseInitializer y NotesRepository.
- 2026-08-15: Implementado primer comando real CaptureNoteCommand ('capture.note') en ATLAS.Core, registrado en CommandRegistry y probado a través del CommandRegistry contra SQLite.
- 2026-08-15: Implementado Global Launcher en ATLAS.UI: hotkey global Ctrl+Space (Win32 RegisterHotKey + SetWindowSubclass), ventana flotante sin bordes ni barra de título en tercio superior, ejecución de capture.note con Enter y cierre con Escape/blur.
