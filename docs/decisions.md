# Architecture Decisions Log

- 2026-08-15: Solución estructurada en capas desacopladas (ATLAS.Core, ATLAS.Storage, ATLAS.UI, ATLAS.Core.Tests) usando .NET 10, Windows App SDK (WinUI 3), Microsoft.Data.Sqlite y CommunityToolkit.Mvvm.
- 2026-08-15: Infraestructura de Command System implementada en ATLAS.Core (ICommand, ICommandRegistry, CommandRegistry, CommandResult, CommandParameterDescriptor) con ejecución asíncrona segura.
- 2026-08-15: Implementado ATLAS.Storage con SQLite local (%LocalAppData%\ATLAS\atlas.db), tabla única notes (id, content, created_at, source), DatabaseInitializer y NotesRepository.
