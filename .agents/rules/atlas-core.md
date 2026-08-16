# Reglas de Desarrollo — ATLAS Core & Storage

Al trabajar en `ATLAS.Core`, `ATLAS.Storage`, integraciones externas o tests unitarios, respetar obligatoriamente las siguientes directivas:

1. **Core Desacoplado de UI:** `ATLAS.Core` y `ATLAS.Storage` no deben tener ninguna dependencia hacia `ATLAS.UI`, Microsoft.Maui, Blazor o componentes visuales.
2. **Commands como Entrada de Acciones:** Toda acción de negocio (crear nota, completar hábito, agregar transacción, sincronizar) debe implementarse como un `ICommand` registrado en `ICommandRegistry`.
3. **No Duplicar Lógica:** Las validaciones y cálculos de dominio deben residir en Core o Repositorios, nunca en los code-behind de Razor o ViewModels.
4. **SQLite Local-First:** SQLite es la fuente primaria de verdad. Toda operación de lectura y escritura debe funcionar sin conexión a internet.
5. **Secretos mediante Secure Store:** Toda API key, token o credencial se gestiona a través de la abstracción `ISecretVault` (DPAPI / Windows Credential Locker), nunca en texto plano o variables de entorno desprotegidas.
6. **Integraciones Aisladas:** Servicios externos (Telegram, Mercado Pago, Gmail, Supabase) deben estar encapsulados, con manejo robusto de excepciones de red y sin bloquear la inicialización de la app.
7. **Pruebas Unitarias:** Toda lógica nueva o no trivial debe contar con pruebas unitarias en `ATLAS.Core.Tests`.
8. **Cero Dependencias Innecesarias:** Evitar incorporar paquetes NuGet externos si la funcionalidad puede resolverse con APIs estándar de .NET 10 (ej. `HttpClient`, `System.Text.Json`).
