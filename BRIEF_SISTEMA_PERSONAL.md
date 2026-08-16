# Sistema Personal de Álvaro — Brief Estratégico

Este documento es la fuente de verdad del proyecto. Reemplaza a los
`AGENTS.md` anteriores de ATLAS. Cualquier IA (Antigravity, Claude,
lo que sea) que ayude a construir esto debe leer este archivo primero,
completo, antes de tocar código.

## 0. Por qué existe este documento

Patrón identificado en conversaciones anteriores: cada proyecto personal
(ConstruSeco, AppHorarios, ListaCompra, ATLAS) terminó con el mismo
problema — se le siguen agregando cosas hasta saturarlo, o se pierde el
foco entre sesiones y el desarrollo se va para un lado que nadie pidió
(pasó literalmente con ATLAS: un pedido de "mejorá el diseño" terminó en
7 etapas no planeadas y una integración cloud no autorizada).

Este documento existe para cortar ese patrón de raíz, con dos reglas:

1. **No se construye nada que no esté en este documento.** Si aparece una
   idea nueva en el camino, se anota en la sección 7 (Backlog / Ideas
   futuras) y no se toca hasta que se decida moverla acá arriba.
2. **Se usa cada cosa dos semanas antes de sumar la siguiente.** Si a las
   dos semanas no se usó, no se sigue construyendo esa parte — se
   revisa por qué, no se le agrega más funcionalidad para "arreglarla".

## 1. Decisión de arquitectura: componer, no reinventar

La versión anterior (ATLAS) intentaba construir desde cero, en C#, todo
lo que ya existe hecho, probado y mantenido por comunidades grandes:
un segundo cerebro, un sistema de hábitos, un motor de automatizaciones
para conectar Telegram/Gmail/MercadoPago, y un asistente de IA local.
Eso es mucho código propio para mantener solo, con mucha superficie
para que se rompa o se sature.

**Nueva base: tres herramientas gratuitas, maduras, con comunidad
grande detrás, compuestas entre sí — más una capa fina propia encima.**

| Pieza | Herramienta | Por qué | Costo |
|---|---|---|---|
| Second Brain + Hábitos | **Obsidian** (+ plugins) | Notas en Markdown local, plugin de Hábitos maduro (streaks, metas, heatmap, recordatorios), Dataview para armar dashboards propios, app de iPhone real y gratuita | Gratis (core + plugins). Sync entre PC e iPhone: gratis con Remotely Save (más setup) o $4/mes con Obsidian Sync (cero fricción) |
| Automatizaciones / integraciones | **n8n** (self-hosted, en tu propia PC vía Docker) | +1200 integraciones nativas, +500 nodos de comunidad, soporta Telegram y Gmail nativamente, conecta cualquier cosa con cualquier cosa sin que vos escribas el cliente de cada API | Gratis 100% self-hosted, corriendo en tu propia PC (no hace falta VPS) |
| IA local | **Ollama** | Estándar de facto para correr modelos livianos en Windows, aceleración nativa por GPU (tu RTX 2050 corre modelos chicos tipo Phi-4-mini o Qwen 3B sin drama), API compatible con OpenAI que cualquier cosa puede consumir | Gratis, 100% local |
| Capa personal (lo único que programás vos) | Launcher liviano (Ctrl+Space) + algún glue puntual | Conecta el teclado con Obsidian/n8n/Ollama. No reimplementa nada que ya resuelvan las tres de arriba | Gratis |

**Regla de oro:** antes de escribir una línea de código propia para
algo, primero se pregunta "¿esto ya lo resuelve Obsidian, n8n u Ollama
con un plugin/nodo existente?". Solo se programa lo que de verdad no
existe.

## 2. Módulo por módulo (con tu feedback ya incorporado)

### Second Brain — PRIORIDAD ALTA
Obsidian directo, sin capa propia encima. Vault local, sincronizado a
iPhone. Templater + QuickAdd para captura rápida (el "Ctrl+Space" que
te gustaba, pero ya resuelto por un plugin maduro en vez de C# a mano).

### Hábitos — PRIORIDAD MEDIA, con foco en encontrar el sistema
Vos mismo dijiste que no encontraste un sistema de hábitos que te
resulte útil en el día a día — ese es el problema real, no la
herramienta. Se arranca con el plugin **Habits** de Obsidian (soporta
done/not-done, contados, con tiempo, streaks, metas, y hasta
notificaciones), sin gamificación (XP/niveles) al principio. Se prueba
2 semanas en uso real. Si a las 2 semanas lo estás usando, ahí se
conversa si hace falta un sistema de niveles encima (lo más simple
posible) o si con streaks alcanza.

### Finanzas — PRIORIDAD CONDICIONAL a que MercadoPago se automatice
Vos lo dijiste clarísimo: sin auto-import de MercadoPago no le ves
utilidad. Entonces no se construye nada de finanzas hasta que el flujo
n8n → API de MercadoPago → nota/entrada en Obsidian (o una hoja simple)
esté andando solo, sin carga manual. Si esa automatización no sale
liviana y confiable, este módulo se descarta directamente en vez de
construirse a medias.

### Roadmaps — PRIORIDAD BAJA, apartado
Una sola nota en Obsidian tipo "Roadmaps.md" con lo que vos quieras
planear a mediano plazo. Sin base de datos, sin UI dedicada, sin
comandos. Es texto que revisás cada tanto, no un módulo del sistema.

### Local AI Toolbox — PRIORIDAD ALTA, con cuidado técnico
Ollama corriendo en background con un modelo chico (Phi-4-mini o Qwen
3B, livianos para tu RTX 2050), expuesto como API local que:
- El launcher usa para consultas rápidas.
- n8n usa como nodo de IA para lo que corresponda mantener local.
- Gemini (cloud, gratis, ya lo tenías andando) queda como fallback para
  lo que el modelo local no resuelva bien — exactamente el patrón que
  usa la mayoría de la gente hoy: local para lo simple, cloud para lo
  difícil, así nunca sentís que "no funciona".

### "Project ATLAS" completo — DESCARTADO como objetivo en sí mismo
No se construye un sistema operativo personal monolítico. Lo que
sobrevive de ATLAS es el **patrón de Commands** (una acción, un solo
lugar, reusable desde teclado/Telegram/lo que sea) aplicado como
capa fina sobre Obsidian/n8n/Ollama, no como una app de escritorio
gigante que hay que mantener sola.

## 3. Mobile — integraciones, no PWA

Confirmado: nada de PWA. En el celular, la interacción es a través de
apps que ya usás:
- **Obsidian iOS** (notas, hábitos) — app real, offline-first.
- **Telegram** — ya tenés el bot; con n8n de por medio, el mismo bot
  puede escribir directo al vault de Obsidian o disparar automatizaciones,
  sin que vos abras ninguna app rara.
- **Gmail / WhatsApp** — via nodos de n8n (Gmail nativo; WhatsApp según
  disponibilidad de nodo comunitario u oficial al momento de implementar,
  revisar antes de prometerlo).

## 4. Costo total: $0, con una sola excepción opcional

Todo lo de arriba es gratis self-hosted. La única excepción real es
sync de Obsidian a iPhone: gratis pero con más fricción de setup
(Remotely Save + un bucket S3-compatible gratuito), o $4/mes por
Obsidian Sync sin fricción ninguna. Se decide cuando llegue ese punto,
no antes.

## 5. Fases (chicas, con "parar y usar" entre cada una)

**Fase 0 — Second Brain solo.** Instalar Obsidian, armar el vault,
Templater/QuickAdd para captura rápida. Usar 2 semanas antes de seguir.

**Fase 1 — Hábitos.** Plugin Habits sobre el mismo vault. Usar 2 semanas.

**Fase 2 — n8n + Telegram.** Levantar n8n en Docker local, conectar el
bot de Telegram existente, primer flujo: mensaje de Telegram → nota
nueva en Obsidian. Usar 2 semanas.

**Fase 3 — Ollama.** Instalar, elegir modelo chico, exponerlo al
launcher y a n8n. Usar 2 semanas.

**Fase 4 — Finanzas (solo si Fase 2 demostró que n8n es confiable).**
Flujo MercadoPago → n8n → Obsidian/hoja simple, sin carga manual.

**Fase 5 — Gmail/WhatsApp (opcional, evaluar si hace falta después de
todo lo anterior).**

Roadmaps no tiene fase — es una nota, se crea cuando se necesita.

## 6. Qué hacer con el repo ATLAS actual

No se borra: queda como referencia de código (el patrón de Commands,
la integración de Gmail OAuth, el parser de gastos de Telegram sirven
de ejemplo para los flujos de n8n). Se pausa el desarrollo activo sobre
él. No se le agrega nada nuevo salvo que una fase de arriba
específicamente decida reusar una pieza concreta de su código.

## 7. Backlog / Ideas futuras (no tocar hasta moverlas arriba)

*(vacío por ahora — acá van las ideas que surjan en el camino, para no
perderlas pero tampoco construirlas antes de tiempo)*

## 8. Cómo usar este documento

Pegale este archivo completo a la IA que te ayude a implementar cada
fase, junto con la instrucción: "Trabajamos una fase a la vez, la que
está marcada como actual. Antes de tocar código, mostrame el plan y
esperá mi confirmación." Cuando cierres una fase, marcala como
completada acá mismo, en este archivo, antes de pasar a la próxima —
así el contexto no depende de que vos o yo nos acordemos entre sesiones.

**Fase actual: Fase 0 — Second Brain.**
