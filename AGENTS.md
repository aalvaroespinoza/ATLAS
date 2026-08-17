# ATLAS — Reglas Operativas

## 1. Qué es ATLAS
**ATLAS** es un **Personal Command Center** y **Personal OS** diseñado para centralizar la memoria, el contexto y las acciones operativas de un único usuario. No es un ERP, ni un software corporativo, ni un dashboard con métricas decorativas.

## 2. Stack Actual
- **Framework:** .NET 10 (C# 13)
- **Host de UI:** MAUI Blazor Hybrid (restringido a Windows `net10.0-windows10.0.19041.0`, unpackaged)
- **Estilos:** Tailwind CSS 3.4 (build estático local en `wwwroot/app.css`, sin dependencias CDN)
- **Persistencia:** SQLite local-first (`Microsoft.Data.Sqlite`, WAL mode, `%LocalAppData%\ATLAS\atlas.db`)
- **Seguridad:** Windows Credential Locker (DPAPI) + Bóveda PIN de 4 dígitos (SHA-256)
- **IA:** Google Gemini API (`gemini-1.5-flash-latest`, `IAiProvider`)

## 3. Arquitectura y Flujo (Personal OS)

**EXPERIENCIA**
↓
Dock / Home / Command Center / Search / Context

**INPUT**
↓
Windows / Telegram / Gmail / Mercado Pago / archivos

**CORE**
↓
Commands / Context / Events / AI orchestration

**MODULES**
↓
Knowledge / Habits / Goals / Roadmaps / Finance

**STORAGE**
↓
SQLite / Secure Store / Supabase Sync

**AI**
↓
Gemini / Local provider preparado

**INTEGRATIONS**
↓
Gmail / Telegram / Mercado Pago / Supabase / Windows

*Reglas Fundamentales:*
- **Core desacoplado de UI:** `ATLAS.Core` y `ATLAS.Storage` no conocen a `ATLAS.UI` ni a Blazor.
- **Commands como punto de entrada:** Toda acción de negocio se ejecuta mediante `ICommand` registrado en `ICommandRegistry`.
- **SQLite local-first:** Todas las operaciones primarias funcionan 100% offline.
- **Integraciones aisladas:** Terceros (Telegram, Mercado Pago, Gmail, Supabase) encapsulados tras abstracciones en Core.
- **Secretos protegidos:** Ninguna clave en texto plano; todo se custodia en `ISecretVault`.

## 4. Reglas de Desarrollo
- Trabajar **un bloque por vez**.
- **Primero plan, después implementación** (seguir `.agents/workflows/atlas-plan.md`).
- **Compilar y testear** después de cada bloque (`dotnet build`, `dotnet test`).
- **Un commit por bloque funcional**, siempre pusheando al remoto (`git push origin main`).
- Mantener compatibilidad estricta con la funcionalidad existente.

## 5. Regla Anti-Scope-Creep (Mecanismo de Protección)
- **Prohibido agregar funcionalidades no solicitadas explícitamente.**
- No crear entidades, comandos, tablas o pantallas no contempladas en el plan aprobado.
- No inventar dependencias ni servicios futuros antes de su fase correspondiente.

**Evaluación Obligatoria:**
ANTES de agregar una feature nueva debe responderse:
1. ¿Resuelve un problema cotidiano real?
2. ¿Se usa al menos semanalmente?
3. ¿Puede existir como capability sin una nueva pantalla?
4. ¿Puede reutilizar un Command existente?
5. ¿Puede reutilizar el Context system?
6. ¿Duplica una herramienta existente?
7. ¿Aumenta la complejidad visual?
8. ¿Aumenta la complejidad arquitectónica?
9. ¿Puede esperar dos semanas de uso real?

Si una feature no pasa esta evaluación:
→ backlog.
No implementación.
No se crea una nueva etapa automáticamente.
No se inventa una numeración de etapas.

## 6. Fuentes de Verdad
- **Producto:** `@docs/ATLAS_PRODUCT.md`
- **Arquitectura:** `@docs/ATLAS_ARCHITECTURE.md`
- **Roadmap & Fases:** `@docs/ATLAS_ROADMAP.md`
- **Registro de Decisiones:** `@docs/decisions.md`

## 7. Imágenes de Referencia
Las imágenes oficiales de referencia de interfaz y arquitectura residen en `/reference/`:
- `/reference/atlas-home-reference.png`
- `/reference/atlas-architecture-reference.png`

## 8. Modificación de Base de Datos y Entidades Core
**REGLA ESTRICTA:** Cada vez que se modifique una entidad del Core (ej. Goals, Habits, Transactions), es obligatorio:
1. Revisar `DatabaseInitializer.cs` y los repositorios correspondientes en `ATLAS.Storage` usando tools de lectura.
2. NO asumir el esquema de la base de datos; consultarlo leyendo los archivos `.sql` o `.cs` correspondientes.
3. Si hay un cambio en el modelo, actualizar el esquema y el inicializador de la base de datos local (SQLite) ANTES de tocar la UI.