# ATLAS — Personal Operating System

## Documento maestro para desarrollo asistido por IA / vibe coding

**Estado:** Diseño inicial
**Objetivo:** construir un sistema personal para Windows, ligero, modular, profundamente personalizado y escalable sin convertirse en una superapp saturada.
**Prioridad:** utilidad cotidiana > velocidad > simplicidad > integración > amplitud de funciones.

---

# 1. La idea central

ATLAS no debe ser una aplicación que intente hacer todo.

Debe ser un **núcleo personal** que centraliza contexto, acciones y automatizaciones, mientras que las funciones especializadas viven como módulos independientes.

La regla principal es:

> **ATLAS administra el contexto. No intenta reemplazar todas las aplicaciones que ya utilizo.**

Por eso:

- Windows será el centro principal.
- Las funciones pesadas y locales estarán en la PC.
- El teléfono no necesita tener una copia completa de ATLAS.
- Gmail, Telegram, WhatsApp, calendario, archivos y otras aplicaciones seguirán siendo herramientas externas.
- ATLAS se conecta a ellas solamente cuando aporta valor.
- La IA será una capa transversal, no una sección gigante llamada “IA”.

Esto evita terminar con una interfaz llena de botones, dashboards y módulos que casi nunca se usan.

---

# 2. Problema que debe resolver

El sistema debe reducir fricción en cuatro situaciones cotidianas:

1. **Capturar** algo rápidamente.
2. **Recordar** algo sin tener que organizarlo manualmente.
3. **Entender** información dispersa.
4. **Ejecutar** acciones repetitivas mediante automatización.

Ejemplos reales:

- Guardar una idea mientras trabajo.
- Copiar un texto y pedir una transformación sin abrir otra aplicación.
- Guardar un PDF y obtener un resumen/búsqueda posteriormente.
- Registrar un gasto.
- Consultar cuánto gasté.
- Revisar una rutina o hábito.
- Convertir una meta en un roadmap.
- Preguntarle a ATLAS qué tengo pendiente.
- Mandarle algo a ATLAS desde Telegram.
- Recibir una notificación importante.

ATLAS debe tratar de convertir estas acciones en operaciones de pocos pasos.

---

# 3. Principio anti-saturación

Este es el requisito de producto más importante.

## 3.1 No tener una barra lateral con 15 módulos

La navegación principal debe ser mínima.

Propuesta:

- **Inicio**
- **Capturar**
- **Buscar**
- **Actividad**
- **Configuración**

El resto aparece bajo contexto, búsqueda o comandos.

Por ejemplo:

- Finanzas no necesita estar siempre visible.
- Roadmaps no necesita ocupar un lugar permanente si se utiliza pocas veces.
- Herramientas de IA deben estar disponibles desde cualquier lugar.
- Hábitos deben aparecer cuando corresponde al momento del día.

## 3.2 El usuario no debe administrar módulos

ATLAS debe descubrir capacidades automáticamente.

Ejemplo:

> “Quiero aprender redes desde cero.”

ATLAS puede sugerir crear un roadmap.

Pero no es necesario que exista un botón gigante “ROADMAPS”.

## 3.3 Las funciones no deben competir entre sí

Cada módulo tiene una responsabilidad clara.

### Second Brain
Guardar, recuperar y relacionar conocimiento.

### Hábitos / Rutinas
Convertir comportamientos repetidos en acciones observables.

### Finanzas
Entender movimientos y flujo de dinero.

### Roadmaps
Convertir objetivos complejos en planes temporales.

### Local AI Toolbox
Ejecutar acciones rápidas sobre texto, archivos y sistema operativo.

### Integraciones
Conectar ATLAS con aplicaciones externas.

No crear un módulo nuevo si una función puede vivir razonablemente dentro de uno existente.

---

# 4. Arquitectura conceptual

ATLAS se estructura formalmente en las siguientes capas desacopladas, asegurando un diseño mantenible y local-first:

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

La clave es que los módulos no dependan de la interfaz. La UI, la IA y las integraciones consumen exclusivamente servicios del Core (`ICommandRegistry`, `IContextActionService`). Esto permite cambiar UI, modelo de IA o proveedor sin reescribir la lógica subyacente. Módulos complejos no implementados explícitamente se consideran capacidades **futuras**.

---

# 5. Aplicación principal de Windows

## Recomendación

Construir la aplicación principal como **Windows nativo con C# + WinUI 3 + Windows App SDK**.

Motivos:

- Excelente integración con Windows.
- Notificaciones nativas.
- Tray / startup / ventanas secundarias.
- Atajos globales.
- Integración con portapapeles.
- Interacción con archivos.
- Buen rendimiento para una aplicación local.
- Adecuado para una herramienta altamente personalizada.
- Menor fricción que intentar convertir una web en una aplicación de sistema operativo.

Windows App SDK es actualmente la plataforma recomendada por Microsoft para nuevas aplicaciones de escritorio Windows y ofrece APIs de ventana, ciclo de vida, notificaciones y otras capacidades modernas. La versión estable más reciente disponible al momento de este diseño es la 2.3.1. 

Fuente oficial:
https://learn.microsoft.com/windows/apps/windows-app-sdk

## Regla importante

No utilizar Electron salvo que exista una razón técnica concreta.

Tampoco convertir ATLAS en una web envuelta dentro de una ventana únicamente porque el vibe coding sea más cómodo.

La comodidad del desarrollo no debe destruir el objetivo principal: **sentirse como parte de Windows**.

---

# 6. Arquitectura física de la aplicación

Separar claramente:

```text
ATLAS.exe
│
├── UI
├── Core
├── Modules
│   ├── Knowledge
│   ├── Habits
│   ├── Finance
│   ├── Roadmaps
│   └── AI
│
├── Integrations
│   ├── Gmail
│   ├── Telegram
│   ├── WhatsApp
│   └── FileSystem
│
├── LocalServices
│   ├── Search
│   ├── Embeddings
│   ├── OCR
│   └── AI
│
└── Storage
    ├── SQLite
    ├── Vector index
    └── encrypted secrets
```

La aplicación debe funcionar aunque Internet esté desconectado, salvo aquellas funciones que requieran servicios externos.

---

# 7. Base de datos

## Primera opción

SQLite local.

No utilizar una base de datos externa desde el primer día.

La información principal de ATLAS debe pertenecer al usuario y estar disponible localmente.

## Datos principales

- notes
- documents
- tasks
- habits
- habit_events
- goals
- roadmaps
- roadmap_steps
- transactions
- integrations
- conversations
- attachments
- events
- settings
- ai_actions

## Vector search

Añadir búsqueda semántica solamente cuando la base de conocimiento ya exista.

No comenzar con RAG por moda.

Primero:

1. almacenar correctamente,
2. indexar correctamente,
3. buscar correctamente,
4. después añadir semántica.

---

# 8. Módulo 1 — Second Brain

Este será uno de los pilares de ATLAS.

## Objetivo

Que cualquier información útil pueda entrar al sistema sin tener que decidir inmediatamente dónde guardarla.

## Captura

Fuentes posibles:

- texto escrito
- portapapeles
- archivos
- PDFs
- imágenes
- enlaces
- notas rápidas
- mensajes recibidos por Telegram
- posteriormente mensajes o correos seleccionados

## Organización

ATLAS debe evitar depender de carpetas rígidas.

Cada elemento puede tener:

- título
- contenido
- tipo
- etiquetas
- fecha
- origen
- relaciones
- proyectos
- embeddings

## IA

La IA puede:

- resumir
- clasificar
- extraer conceptos
- detectar relaciones
- generar etiquetas
- crear preguntas
- transformar contenido
- responder preguntas usando la información guardada

## Regla

Nunca crear 30 categorías manuales.

Debe existir una búsqueda global potente y organización automática.

---

# 9. Módulo 2 — Hábitos / Rutinas

No construir un tracker de hábitos tradicional.

El objetivo es crear un sistema que sea útil diariamente y no se convierta en una lista de casilleros para marcar.

## Concepto

Separar:

### Hábitos
Comportamientos recurrentes.

### Rutinas
Secuencias de acciones.

### Objetivos
Resultados que se buscan conseguir.

Ejemplo:

Objetivo: estudiar ciberseguridad.

Rutina: bloque de estudio.

Hábitos asociados:

- empezar a determinada hora
- completar una sesión
- registrar aprendizaje

## Gamificación

Usar XP y niveles, pero nunca convertirlo en una obligación.

La experiencia debe ser:

```text
acción → evidencia → progreso → recompensa
```

No:

```text
acción → puntos arbitrarios
```

## Sistema propuesto

Cada actividad puede aportar progreso a varias dimensiones.

Por ejemplo:

```text
Estudiar 60 minutos
↓
+ progreso en objetivo
+ racha de rutina
+ XP global
```

Pero la puntuación debe ser explicable y configurable.

## IA

ATLAS puede detectar:

- hábitos abandonados
- horarios en los que realmente se cumplen las cosas
- objetivos estancados
- exceso de tareas
- rutinas poco realistas

Y sugerir cambios.

No debe inventar presión artificial.

---

# 10. Módulo 3 — Finanzas

Este módulo solo tiene sentido si la carga manual se reduce muchísimo.

Por eso la prioridad es:

## Mercado Pago primero

Mercado Pago ofrece APIs y reportes de movimientos financieros, incluyendo información sobre transacciones y movimientos que afectan el saldo. La documentación oficial incluye APIs para generar reportes y consultar información financiera. 

Fuentes:

https://www.mercadopago.com.ar/developers/es/docs/reports/introduction
https://www.mercadopago.com.ar/developers/es/docs/reports/account-money/api

## Flujo ideal

```text
Mercado Pago
     ↓
Integration Service
     ↓
Normalización
     ↓
ATLAS Finance
     ↓
Categorías / análisis / IA
```

## Importante

No almacenar tokens de Mercado Pago en el cliente.

La documentación oficial indica que las credenciales privadas deben permanecer en servidor/entorno seguro y no exponerse en código client-side. 

## Datos normalizados

Cada movimiento debería terminar con:

- fecha
- monto
- tipo
- origen
- descripción
- moneda
- categoría
- subcategoría
- identificador externo
- estado
- metadatos

## IA

La IA puede:

- categorizar gastos
- detectar gastos atípicos
- explicar variaciones
- resumir el mes
- detectar suscripciones
- detectar ingresos recurrentes
- producir una visión simple del flujo de dinero

## Manual

Debe seguir existiendo carga manual para gastos que no provengan de Mercado Pago.

Pero nunca convertir la app en una planilla contable compleja.

---

# 11. Módulo 4 — Roadmaps

Este módulo no debe vivir en el centro de la interfaz.

Se utiliza cuando aparece un objetivo complejo.

Ejemplo:

> Quiero aprender ciberseguridad desde cero.

ATLAS puede generar:

```text
Objetivo
  ↓
Roadmap
  ↓
Etapas
  ↓
Acciones
  ↓
Evidencia
  ↓
Progreso
```

Los pasos del roadmap deberían poder convertirse automáticamente en tareas o hábitos.

## Integración con Knowledge

Cada etapa puede tener:

- documentos
- notas
- recursos
- enlaces
- sesiones realizadas
- preguntas
- resultados

Esto evita que Roadmaps sea una herramienta aislada.

---

# 12. Módulo 5 — Local AI Toolbox

Este es uno de los elementos diferenciales más importantes.

## Objetivo

Tener pequeñas herramientas de IA accesibles desde cualquier lugar de Windows, sin abrir una aplicación pesada ni subir automáticamente los datos a Internet.

## Experiencia ideal

Selecciono un texto → atajo global → aparece ATLAS.

Selecciono un archivo → ATLAS puede procesarlo.

Copio algo → ATLAS puede resumirlo.

No quiero navegar por 10 pantallas.

## Herramientas iniciales

- resumir texto
- reescribir texto
- traducir
- explicar
- extraer datos
- convertir a formato estructurado
- OCR
- resumir PDF
- consultar PDF
- renombrar archivos mediante IA
- clasificar archivos
- generar nombres/descripciones
- comparar textos

## Interacción

ATLAS debe tener una interfaz flotante tipo launcher/command bar.

Ejemplo:

```text
        ┌─────────────────────────────┐
        │ ¿Qué querés hacer?          │
        │ > resumir selección         │
        └─────────────────────────────┘
```

No abrir todo el dashboard para una acción pequeña.

## IA local

La arquitectura debe permitir utilizar modelos locales cuando sea razonable.

Ejemplos de funciones candidatas a local:

- clasificación
- OCR
- resumen de documentos pequeños
- extracción estructurada
- transformación de texto
- búsqueda semántica

Funciones candidatas a cloud:

- tareas complejas
- razonamiento pesado
- generación avanzada
- tareas donde la calidad supere claramente al modelo local

## Regla de privacidad

Por defecto:

> datos locales → procesamiento local

Solo usar proveedores cloud cuando:

1. el usuario lo permita,
2. la función lo requiera,
3. el beneficio sea claro.

---

# 13. Integraciones y uso desde iPhone

La estrategia recomendada NO es construir inmediatamente una aplicación iOS completa.

Eso duplicaría trabajo y aumentaría muchísimo la superficie de bugs.

Primero construir ATLAS para Windows y usar integraciones como interfaz móvil.

## Telegram

Debe ser una de las primeras integraciones.

Ejemplos:

> /addidea Aprender Rust

> /expense 4500 comida

> ¿Qué tengo pendiente hoy?

> Guardá esto como nota

Telegram dispone de APIs oficiales para desarrollar integraciones y clientes. 

Fuente:
https://core.telegram.org/api

## Gmail

Gmail puede utilizarse para:

- buscar correos relevantes
- convertir correos en tareas
- guardar correos en Knowledge
- resumir cadenas largas
- detectar información importante

La API de Gmail tiene cuotas oficiales y su uso estándar no tiene costo adicional dentro de los límites establecidos. Google actualizó sus cuotas el 1 de mayo de 2026. 

Fuente:
https://developers.google.com/workspace/gmail/api/reference/quota

## WhatsApp

No debe ser una dependencia del núcleo.

Se debe tratar como una integración opcional y desacoplada.

La prioridad debería ser:

1. Telegram
2. Gmail
3. notificaciones Windows
4. archivos / portapapeles
5. WhatsApp cuando exista una vía oficial conveniente para el caso de uso concreto

Nunca diseñar el Core dependiendo de WhatsApp.

## iPhone

El iPhone puede utilizar ATLAS mediante:

- Telegram
- Gmail
- enlaces/deep links
- automatizaciones de iOS
- notificaciones
- eventualmente una PWA mínima de captura

La PWA, si llega, debe ser un **companion**, no otra versión completa del producto.

---

# 14. Qué NO construir

Estas restricciones son obligatorias.

## No construir:

- gestor de calendario completo
- cliente de correo completo
- chat propio tipo WhatsApp
- gestor de tareas hipercompleto
- gestor financiero contable
- editor de documentos
- red social
- navegador
- CRM
- sistema de notas con 500 opciones
- marketplace de agentes en la primera etapa
- sistema de plugins de usuario demasiado pronto

La aplicación debe integrarse con herramientas existentes en lugar de reemplazarlas.

## Regla Anti-Scope-Creep (Evaluación Obligatoria)

ATLAS ha sufrido problemas de scope creep anteriormente. Para proteger el producto, **ANTES** de agregar una feature nueva debe responderse:

1. ¿Resuelve un problema cotidiano real?
2. ¿Se usa al menos semanalmente?
3. ¿Puede existir como capability sin una nueva pantalla?
4. ¿Puede reutilizar un Command existente?
5. ¿Puede reutilizar el Context system?
6. ¿Duplica una herramienta existente?
7. ¿Aumenta la complejidad visual?
8. ¿Aumenta la complejidad arquitectónica?
9. ¿Puede esperar dos semanas de uso real?

**Si una feature no pasa esta evaluación:**
→ Se envía al backlog.
→ NO se implementa.
→ NO se crea una nueva etapa automáticamente.
→ NO se inventa una numeración de etapas.

---

# 15. Experiencia de usuario

## Inicio

La pantalla principal debe responder una pregunta:

> ¿Qué necesito saber o hacer ahora?

No debe mostrar 40 tarjetas.

Ejemplo:

```text
Buenos días, Álvaro

Ahora
────────────────────────
2 cosas importantes
1 hábito en curso
1 tarea pendiente

Acciones rápidas
[Capturar] [Buscar] [IA]

Actividad reciente
────────────────────────
...
```

## Dashboard adaptable

El usuario debe poder ocultar cualquier bloque.

ATLAS debe aprender qué información se utiliza realmente.

Si una tarjeta se ignora durante mucho tiempo, puede sugerir ocultarla.

---

# 16. Comando universal

Debe existir una acción global como:

```text
Ctrl + Space
```

que abra el launcher de ATLAS.

Desde allí:

- buscar
- capturar
- consultar IA
- lanzar herramientas
- ejecutar acciones
- abrir módulos

Ejemplos:

```text
> buscar redes TCP
> resumir selección
> registrar gasto 4500 comida
> guardar esta idea
> abrir mi roadmap de ciberseguridad
> ¿qué tengo hoy?
```

Este launcher es más importante que tener decenas de botones en pantalla.

---

# 17. Sistema de comandos

ATLAS debe tener un sistema interno de acciones.

Conceptualmente:

```text
Command
├── id
├── name
├── description
├── permissions
├── input schema
├── execute()
└── result
```

Ejemplos:

```text
capture.note
finance.add_transaction
finance.sync_mercadopago
knowledge.search
knowledge.summarize
habit.complete
roadmap.create
ai.ask
file.summarize
```

La UI simplemente invoca Commands.

Esto hará que:

- el launcher pueda utilizarlos,
- Telegram pueda utilizarlos,
- automatizaciones puedan utilizarlos,
- la IA pueda sugerirlos.

Una acción debe implementarse una sola vez.

---

# 18. Event Bus

ATLAS debe utilizar eventos internos.

Ejemplos:

```text
NoteCreated
DocumentImported
TransactionCreated
HabitCompleted
TaskCompleted
EmailImported
AIActionExecuted
```

Esto permite automatizaciones sin acoplar módulos.

Ejemplo:

```text
EmailImported
    ↓
AI categorizes email
    ↓
Important information detected
    ↓
Knowledge item created
```

---

# 19. IA como capa transversal

No crear una única pantalla gigante de “Chat con IA”.

La IA debe existir en contexto.

Ejemplo:

En una nota:

> resumir
> convertir en tareas
> explicar
> relacionar

En Finanzas:

> explicar este mes

En Hábitos:

> analizar por qué estoy fallando

En Roadmaps:

> reorganizar plan

En archivos:

> resumir PDF

Además debe existir un chat general para preguntas abiertas.

---

# 20. Seguridad

El sistema debe tratarse como una bóveda personal.

## Nunca:

- guardar secretos en código
- subir tokens al frontend
- versionar credenciales
- guardar Access Tokens sin protección
- enviar documentos privados a servicios externos por defecto

## Secret management

Separar:

```text
User Data
Integration Credentials
AI Providers
Application Settings
```

Credenciales deben utilizar almacenamiento seguro del sistema cuando sea posible.

---

# 21. Offline-first realista

ATLAS no debe depender de Internet para:

- abrirse
- buscar información local
- consultar notas
- ejecutar herramientas locales
- usar el launcher
- administrar hábitos
- administrar objetivos

Internet será necesario para:

- sincronización externa
- Gmail
- Mercado Pago
- servicios cloud de IA
- integraciones remotas

Esto evita que una caída de Internet convierta la aplicación en inútil.

---

# 22. Sincronización

No implementar sincronización entre dispositivos en la primera versión.

Primero:

```text
Windows local
```

Después:

```text
Windows
   ↓
Encrypted Sync Layer
   ↓
iPhone / other devices
```

Cuando llegue el momento de sincronizar, evaluar Supabase u otro backend ligero.

Pero el sistema local debe seguir funcionando si la nube falla.

---

# 23. Estrategia de costos

Objetivo: costo recurrente casi cero mientras el sistema sea personal.

## Preferencias

- SQLite local
- modelos locales para tareas simples
- Gmail API dentro de sus cuotas
- Telegram para comandos/mensajería
- APIs externas solo cuando realmente sean necesarias
- hosting mínimo
- evitar servicios SaaS innecesarios

## IA

Modelo híbrido:

```text
Local AI
   ↓
¿Puede resolverlo bien?
   ├── Sí → ejecutar localmente
   └── No → Cloud AI
```

La arquitectura debe permitir cambiar de proveedor.

No acoplar ATLAS a un único modelo.

---

# 24. Stack recomendado

## Desktop

- C#
- .NET
- WinUI 3
- Windows App SDK

## Database

- SQLite

## ORM / data access

Una solución simple y estable; evitar abstracciones innecesarias.

## Search

- SQLite FTS para búsqueda textual inicial
- vector search posteriormente

## AI local

Capa abstracta para proveedores locales.

## Cloud AI

Capa abstracta:

```text
IA Provider
├── LocalProvider
├── OpenAIProvider
├── AnthropicProvider
└── GeminiProvider
```

No asumir que todos estarán habilitados.

## Integraciones

- Gmail API
- Telegram Bot API
- Mercado Pago API / Reports
- sistema de archivos Windows
- Windows notifications
- Clipboard

## Automatización futura

n8n puede utilizarse como sistema de automatización externo cuando una tarea requiera varios servicios, pero el Core no debe depender de n8n para funcionar.

---

# 25. Estructura de proyecto sugerida

```text
src/
├── Atlas.App/
│   ├── UI/
│   ├── Commands/
│   └── App.xaml
│
├── Atlas.Core/
│   ├── Entities/
│   ├── Services/
│   ├── Events/
│   └── Interfaces/
│
├── Atlas.Modules.Knowledge/
├── Atlas.Modules.Habits/
├── Atlas.Modules.Finance/
├── Atlas.Modules.Roadmaps/
├── Atlas.Modules.AI/
│
├── Atlas.Integrations.Gmail/
├── Atlas.Integrations.Telegram/
├── Atlas.Integrations.MercadoPago/
├── Atlas.Integrations.Windows/
│
├── Atlas.Infrastructure/
│   ├── SQLite/
│   ├── Search/
│   ├── Secrets/
│   └── Files/
│
└── Atlas.Tests/
```

Cada módulo debe depender de `Atlas.Core`, no de la UI de otro módulo.

---

# 26. MVP real

NO empezar intentando construir todo.

## MVP 0 — Shell

Objetivo: tener una aplicación Windows nativa ligera.

Debe incluir:

- ventana principal
- tray
- startup opcional
- atajo global
- launcher
- configuración básica
- SQLite
- logging

No IA todavía.

## MVP 1 — Capture + Knowledge

Agregar:

- notas
- captura rápida
- búsqueda
- archivos
- historial
- tags automáticos simples

La aplicación ya debe ser útil sin IA.

## MVP 2 — Local AI Toolbox

Agregar:

- resumir
- reescribir
- explicar
- OCR
- procesar PDF
- acciones sobre selección

Este probablemente sea el primer punto donde ATLAS empiece a sentirse realmente especial.

## MVP 3 — Habits + Goals

Agregar:

- objetivos
- hábitos
- rutinas
- progreso
- XP
- niveles

No intentar definir el sistema de gamificación perfecto desde el principio.

Medir uso real y ajustar.

## MVP 4 — Telegram

Agregar:

- captura desde Telegram
- consultar pendientes
- crear notas
- registrar gastos
- disparar acciones

## MVP 5 — Finanzas

Agregar:

- movimientos manuales
- categorías
- Mercado Pago
- dashboard mínimo
- análisis IA

## MVP 6 — Roadmaps

Agregar:

- crear objetivo
- generar roadmap
- convertir pasos en tareas
- enlazar conocimiento

## MVP 7 — Gmail

Agregar solamente operaciones realmente útiles.

Por ejemplo:

- buscar
- resumir
- convertir en tarea
- guardar en Knowledge

## MVP 8 — Sincronización / companion móvil

Solo después de comprobar que ATLAS ya se usa diariamente.

---

# 27. Criterio para agregar nuevas funciones

Antes de construir una feature, la IA debe responder:

### Pregunta 1
¿Resuelve una acción que realizo frecuentemente?

### Pregunta 2
¿Reduce pasos o fricción?

### Pregunta 3
¿Puede vivir dentro de un módulo existente?

### Pregunta 4
¿Necesita estar visible permanentemente?

### Pregunta 5
¿Puede agregarse sin aumentar significativamente la complejidad del Core?

Si una feature falla varias de estas preguntas, debe quedar fuera.

---

# 28. Regla de las 3 capas

Cada feature nueva debe clasificarse como una de estas:

## Core
Necesaria para que ATLAS exista.

## Module
Una capacidad especializada.

## Integration
Una conexión externa.

Nunca permitir que una integración modifique directamente el Core.

Ejemplo correcto:

```text
Mercado Pago
    ↓
Integration
    ↓
Transaction Service
    ↓
Finance Module
```

Ejemplo incorrecto:

```text
Mercado Pago
    ↓
UI
    ↓
SQLite
    ↓
IA
```

---

# 29. Regla anti-dependencias

Cada dependencia externa nueva debe justificarse.

Antes de instalar una librería:

1. ¿Realmente hace falta?
2. ¿La plataforma ya lo resuelve?
3. ¿Aporta una parte importante del producto?
4. ¿Tiene mantenimiento razonable?
5. ¿Qué ocurre si desaparece?

El objetivo no es tener pocas líneas de código.

El objetivo es tener pocas cosas que puedan romper el sistema.

---

# 30. Rendimiento

Requisitos:

- arranque rápido
- bajo consumo de RAM en idle
- procesamiento pesado fuera del hilo de UI
- jobs en background
- cache local
- lazy loading de módulos
- no cargar modelos de IA pesados al arrancar

La IA local pesada debe iniciarse bajo demanda.

ATLAS debe sentirse como una utilidad del sistema, no como un navegador con 30 pestañas.

---

# 31. Observabilidad

Debe existir logging desde el día 1.

Cada operación importante debe poder responder:

- qué ocurrió
- cuándo
- cuánto tardó
- qué componente falló
- si hubo fallback

Pero los logs nunca deben contener:

- tokens
- contraseñas
- contenido privado innecesario

---

# 32. Testing

Vibe coding no significa no testear.

La IA puede escribir código, pero ATLAS debe tener pruebas en las partes importantes.

Prioridad:

1. Core
2. Database
3. Commands
4. Integraciones
5. UI

Especialmente probar:

- sincronización
- duplicados
- importación
- movimientos financieros
- comandos
- recuperación ante errores

---

# 33. Desarrollo con IA

El proyecto debe desarrollarse mediante prompts pequeños y autosuficientes.

Nunca pedir:

> “Construí ATLAS completo.”

Eso genera deuda técnica rápidamente.

Preferir:

> “Implementá únicamente el Command System del Core. No modifiques UI, base de datos ni módulos. Agregá tests y documentación.”

Después:

> “Implementá el launcher utilizando el Command System existente. No cambies la arquitectura del Core.”

## Regla

Cada prompt debería producir una feature funcional y verificable.

---

# 34. Flujo de trabajo con IA

```text
REQUISITO
   ↓
DISEÑO
   ↓
IMPLEMENTACIÓN
   ↓
TEST
   ↓
REVISIÓN
   ↓
COMMIT
```

No encadenar 12 cambios antes de verificar.

El repositorio debe permanecer funcional después de cada bloque importante.

---

# 35. Roadmap estratégico

## Etapa 1
ATLAS como utilidad de Windows.

## Etapa 2
Knowledge + AI Toolbox.

## Etapa 3
Goals + Habits.

## Etapa 4
Telegram + Finance.

## Etapa 5
Roadmaps + Gmail.

## Etapa 6
Automatización avanzada.

## Etapa 7
Sincronización y companion móvil.

No avanzar de etapa solamente porque sea técnicamente posible.

Avanzar cuando la etapa anterior ya sea útil.

---

# 36. Qué debería sentirse al usarlo

ATLAS no debería sentirse como:

- Notion
- Trello
- un ERP
- una suite de oficina
- otro chatbot

Debe sentirse como:

> **“Una capa personal encima de Windows que me ayuda a capturar, entender y ejecutar cosas.”**

Esa es la identidad del producto.

---

# 37. Arquitectura de una acción completa

Ejemplo:

El usuario selecciona un texto en Windows.

```text
Usuario
  ↓
Ctrl + Space
  ↓
ATLAS Launcher
  ↓
“Resumir”
  ↓
Command: ai.summarize
  ↓
AI Router
  ├── Local model disponible → usar local
  └── no disponible → cloud provider
  ↓
Resultado
  ↓
Copiar / Guardar / Reemplazar / Compartir
```

Otro ejemplo:

```text
Telegram
  ↓
“Registré $4500 de comida”
  ↓
Telegram Integration
  ↓
Command: finance.add_transaction
  ↓
Finance Module
  ↓
IA categoriza
  ↓
SQLite
  ↓
Respuesta al Telegram
```

Esto demuestra por qué el Command System es una pieza arquitectónica tan importante.

---

# 38. Integración con el sistema operativo

Prioridades para Windows:

- global hotkey
- tray icon
- toast notifications
- clipboard
- drag & drop
- file associations
- context menu cuando sea viable
- startup opcional
- búsqueda rápida
- ventana flotante
- shortcuts

No intentar implementar todas al inicio.

La sensación de integración debe crecer gradualmente.

---

# 39. Personalización

La personalización debe estar basada en configuración, no forks del código.

El usuario debería poder cambiar:

- tema
- atajos
- módulos visibles
- comportamiento del launcher
- proveedor de IA
- modelo local
- categorías
- reglas de automatización
- notificaciones
- privacidad

Pero la configuración avanzada no debe aparecer en la interfaz principal.

---

# 40. Automatizaciones futuras

Cuando el Core esté maduro, se podrán crear automatizaciones como:

```text
Evento
 ↓
Condición
 ↓
Acción
```

Ejemplos:

```text
Email importante
 ↓
Guardar resumen
```

```text
Nuevo movimiento Mercado Pago
 ↓
Categorizar
 ↓
Actualizar resumen financiero
```

```text
Documento agregado
 ↓
Indexar
 ↓
Crear resumen
```

```text
Telegram message
 ↓
Interpretar
 ↓
Ejecutar Command
```

No construir un editor visual de automatizaciones hasta tener suficientes casos reales.

---

# 41. Definition of Done

Una feature no está terminada porque “funciona en mi máquina”.

Debe:

- compilar
- tener manejo de errores
- tener tests donde corresponda
- tener logs adecuados
- no introducir secretos
- no romper módulos existentes
- tener una UX coherente
- ser documentada si modifica arquitectura
- tener una ruta clara de rollback

---

# 42. Regla máxima del proyecto

> **ATLAS debe crecer hacia afuera, no hacia adentro.**

Crecer hacia afuera significa:

- nuevos módulos
- nuevas integraciones
- nuevos comandos
- nuevos proveedores de IA

No significa:

- una pantalla cada vez más grande
- más menús
- más opciones visibles
- más configuraciones obligatorias
- más dependencias dentro del Core

---

# 43. Primera versión que realmente quiero usar

La primera versión útil debería ser sorprendentemente pequeña.

Debe permitir:

1. abrir ATLAS con un atajo global,
2. escribir o pegar algo rápidamente,
3. guardar una nota,
4. buscarla después,
5. seleccionar texto y ejecutar una herramienta de IA,
6. ver una lista pequeña de acciones pendientes,
7. registrar una meta y progreso,
8. utilizar Telegram para capturas rápidas.

Nada más es obligatorio para considerar que el producto ya tiene valor.

---

# 44. Decisión final de producto

La visión aprobada para este proyecto es:

## ATLAS

**Un sistema personal local-first para Windows que centraliza contexto, conocimiento y acciones, y utiliza IA e integraciones externas para reducir fricción cotidiana.**

### Núcleo

- Capture
- Search
- Knowledge
- Commands
- Context
- Local storage

### Capacidades

- Local AI Toolbox
- Goals / Habits
- Finance
- Roadmaps

### Integraciones

- Telegram
- Gmail
- Mercado Pago
- Windows
- posteriormente WhatsApp

### Dispositivos

- Windows = producto principal
- iPhone = interfaz secundaria mediante integraciones
- PWA = únicamente si demuestra una necesidad real

---

# 45. Fuentes técnicas verificadas

Las siguientes fuentes fueron consultadas para validar decisiones que dependen de APIs/plataformas actuales:

- Microsoft Windows App SDK:
  https://learn.microsoft.com/windows/apps/windows-app-sdk

- Windows App SDK downloads / versiones:
  https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads

- Gmail API quotas:
  https://developers.google.com/workspace/gmail/api/reference/quota

- Mercado Pago Reports:
  https://www.mercadopago.com.ar/developers/es/docs/reports/introduction

- Mercado Pago Account Money API:
  https://www.mercadopago.com.ar/developers/es/docs/reports/account-money/api

- Mercado Pago API reference:
  https://www.mercadopago.com.ar/developers/es/reference

- Telegram API:
  https://core.telegram.org/api

---

# 46. Instrucción para la IA que tome este proyecto

Este archivo es la especificación conceptual y de producto de ATLAS.

Al comenzar el desarrollo:

1. No implementes todo el documento de una vez.
2. No agregues funciones no solicitadas.
3. No conviertas ATLAS en una superapp visualmente saturada.
4. Respeta la separación Core / Module / Integration.
5. No introduzcas dependencias sin justificar.
6. Prioriza rendimiento y estabilidad sobre cantidad de features.
7. Mantén la aplicación funcional después de cada cambio.
8. Utiliza prompts/tareas pequeños y autocontenidos.
9. Cada feature debe incluir validación y tests razonables.
10. Pregúntate siempre si una nueva función realmente necesita una nueva pantalla.
11. Cuando exista una solución del sistema operativo, preferirla antes que instalar una biblioteca adicional.
12. Mantener abierta la posibilidad de cambiar proveedor de IA.
13. Mantener datos principales locales.
14. Nunca exponer credenciales en el cliente.
15. No iniciar sincronización multidispositivo hasta que el producto local sea estable.

## Primer objetivo técnico recomendado

Construir únicamente:

```text
Windows App
+
SQLite
+
Core
+
Command System
+
Global Launcher
+
Capture
```

Cuando eso sea estable, construir Knowledge.

Después Local AI Toolbox.

Después continuar por etapas.

**No comenzar por el dashboard.**

El producto debe nacer desde sus acciones y su núcleo, no desde una pantalla bonita.
