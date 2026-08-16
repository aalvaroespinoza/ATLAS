---
name: atlas-architecture
description: Guía de arquitectura en capas, flujo de datos y separación de responsabilidades en ATLAS.
---

# Skill: ATLAS Architecture

## Cuándo Utilizar esta Skill
Utilizar esta skill al crear o refactorizar comandos, repositorios, entidades de dominio, servicios de integración o esquemas de base de datos.

## Referencias Principales
- **Arquitectura del Sistema:** `@docs/ATLAS_ARCHITECTURE.md`
- **Referencia de Arquitectura:** `/reference/atlas-architecture-reference.png`
- **Reglas de Core:** `@.agents/rules/atlas-core.md`

## Separación de Capas y Responsabilidades
1. **Core (`ATLAS.Core`):** Contiene contratos (`ICommand`, `ICommandRegistry`, `IAiProvider`, `ISecretVault`), entidades de dominio puras y comandos. Totalmente agnóstico de UI.
2. **Storage (`ATLAS.Storage`):** Contiene `DatabaseInitializer`, migraciones SQLite (WAL mode) e implementaciones de repositorios.
3. **UI (`ATLAS.UI`):** Shell MAUI Blazor Hybrid, Personal Dock, Design System y páginas Razor. Consume Core y Storage mediante inyección de dependencias.
4. **Integraciones:** Servicios de terceros (Telegram, Supabase, Mercado Pago, Gmail) aislados tras interfaces en Core.
