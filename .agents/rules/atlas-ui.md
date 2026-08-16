# Reglas de Desarrollo — ATLAS UI

Al trabajar en la interfaz de usuario (`ATLAS.UI`, Razor components, Tailwind, CSS), respetar obligatoriamente las siguientes directivas:

1. **Usar el Design System:** Emplear exclusivamente los componentes reutilizables de `ATLAS.UI.Components.Common` (`AtlasButton`, `AtlasInput`, `AtlasListItem`, `AtlasEmptyState`, etc.).
2. **No Duplicar Estilos:** Utilizar los tokens semánticos definidos (`atlas-canvas`, `atlas-dock`, `atlas-surface`, `atlas-elevated`) y las clases de Tailwind compiladas localmente. No agregar CSS ad-hoc redundante.
3. **UI sin Lógica de Negocio:** La capa visual solo despacha comandos (`ICommandRegistry.ExecuteAsync`) y lee repositorios inyectados para renderizar estado.
4. **Command-First:** Priorizar flujos accesibles por teclado, atajos globales (<kbd>Ctrl+Space</kbd>, <kbd>Ctrl+N</kbd>, <kbd>Alt+D</kbd>) y acciones directas.
5. **Context-First:** Presentar en primer plano lo que requiere atención inmediata del usuario, auto-silenciando elementos completados.
6. **Interfaz Personal, Moderna y Expresiva:** Diseñar con profundidad satinada, paleta Índigo/Obsidiana y microinteracciones elásticas (`scale: 0.98`, `cubic-bezier(0.16, 1, 0.3, 1)`).
7. **Evitar Apariencia Corporativa:** Prohibido el uso de tarjetas genéricas de estilo SaaS empresarial o tablas densas sin jerarquía.
8. **Evitar Dashboards Saturados:** Prohibido crear métricas ficticias, gráficos decorativos o tarjetas de vanidad sin datos reales.
9. **Evitar Navegación Excesiva:** Favorecer superficies de enfoque unificadas y split-views frente a árboles profundos de subpáginas.
