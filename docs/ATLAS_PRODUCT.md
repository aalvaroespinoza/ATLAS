# ATLAS — Definición de Producto

> **Fuente de Verdad del Producto**  
> Este documento define el propósito, alcance, principios y módulos de ATLAS. Ninguna funcionalidad se construye si no está alineada con este documento.

---

## 1. Visión y Propósito
**ATLAS** es un **Personal Command Center** (Centro de Comando Personal) y **Personal OS**, diseñado para centralizar la vida digital, operativa y cognitiva de un único usuario.

No es un software empresarial, ni un ERP corporativo, ni un tablero analítico con métricas de vanidad. Es una superficie de trabajo íntima, silenciosa y de alta velocidad.

---

## 2. Principios Fundamentales

### Local-First y Soberanía de Datos
- La base de datos primaria de la verdad es local (**SQLite** en el dispositivo del usuario).
- Funciona 100% offline sin depender de servidores externos para lectura, captura o visualización.
- La sincronización en la nube (Supabase) actúa como réplica y backup secundario, nunca como bloqueo de ejecución local.

### Memoria, Contexto y Acciones
- **Memoria:** Registro duradero y estructurado de pensamientos, hitos, hábitos y finanzas.
- **Contexto:** La interfaz presenta primero lo que requiere atención inmediata en este momento (hábitos pendientes de hoy, hitos activos, actividad reciente).
- **Acciones:** Cada dato es un punto de partida para ejecutar comandos directos (`Command-first`).

### Command-First & Context-First
- Acceso global e instantáneo mediante teclado (<kbd>Ctrl+Space</kbd> / <kbd>Ctrl+K</kbd>).
- Búsqueda universal rápida a través de todos los dominios sin navegar por menús complejos.
- Acciones contextuales que entienden el tipo de objeto en foco (resumir nota, completar hábito, categorizar gasto).

### Filosofía Anti-Saturación y Anti-Dashboard
- Cero gráficos de pastel, cero widgets decorativos sin datos, cero métricas sin utilidad real.
- Menos, pero mejor: cada píxel y cada línea de texto debe ganarse su lugar.
- Tipografía clara, balances netos en tipografía mono, estados de vacío elegantes y sin ruido visual.

### Lenguaje Visual Personal, Moderno y Expresivo
- Paleta unificada Índigo/Obsidiana con superficies satinadas y profundidad sutil.
- Resplandores semánticos funcionales (esmeralda para hábitos e ingresos, violeta para metas y roadmaps, rosa para gastos).
- Microinteracciones táctiles con respuesta física elástica (`scale: 0.98`, `cubic-bezier(0.16, 1, 0.3, 1)`).

---

## 3. Módulos y Capacidades Actuales

### 1. Second Brain (Captura y Conocimiento)
- **Captura rápida:** Atajo global y canvas limpio sin distracciones con soporte Markdown.
- **Búsqueda Universal:** Explorador Split-View (40/60) con filtrado instantáneo por chips (Notas, Roadmaps, Hábitos, Finanzas).
- **Asistencia IA:** Resúmenes y extracción estructurada con Google Gemini.

### 2. Hábitos (Habits)
- Registro y seguimiento de hábitos con frecuencias configurables.
- Marcado rápido en un clic con auto-silenciamiento cuando se completan en el día.
- Cálculo de rachas activas en días consecutivos (`🔥 X días`).

### 3. Metas y Rutas Secuenciales (Goals & Roadmaps)
- Definición de metas de alto nivel vinculadas a roadmaps accionables.
- Hitos ordenados secuencialmente (*milestones*) con cálculo porcentual automático de progreso.
- Visualización de hilos activos directamente en la pantalla de inicio.

### 4. Finanzas Personales (Finance)
- Registro rápido de transacciones (ingresos y gastos) desde PC y Telegram (`/expense`, `/gasto`).
- Balance real acumulado sin estimaciones ficticias.
- **Categorización asistida por IA:** Sugerencias inteligentes basadas en la descripción, con confirmación manual obligatoria y editable (nunca sobreescribe sin confirmación).
- Sincronización a demanda de movimientos de Mercado Pago.

### 5. Inteligencia Artificial Transversal
- Integrada de forma silenciosa mediante Google Gemini (`IAiProvider`).
- Disponible mediante prefijo `?` en el launcher, en botones de acción contextual o para asistencia en tareas.

### 6. Integraciones Aisladas
- **Telegram Bot:** Captura remota de notas, marcado de hábitos y registro de gastos vía long polling.
- **Gmail:** Lectura de correos recientes de solo lectura mediante OAuth 2.0.
- **Supabase Cloud:** Sincronización REST con Row Level Security (RLS) basado en `auth.uid()`.
- **Windows Credential Locker (DPAPI) & Bóveda PIN:** Almacenamiento seguro y encriptado de API keys y tokens.

---

## 4. Entornos de Uso

- **Entorno Principal Actual:** Aplicación de escritorio para Windows (PC), optimizada para teclado, atajos y flujos de trabajo rápidos.
- **Interacción Externa / Móvil:** 
  - Actual: Vía bot de Telegram para captura rápida desde el celular.
  - Futura: Interacción móvil dedicada respetando la misma arquitectura de backend/sincronización y principios de diseño.
