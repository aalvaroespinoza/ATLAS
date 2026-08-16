# Workflow: ATLAS Plan

Este workflow debe ejecutarse antes de realizar cambios de código o modificaciones estructurales.

---

## Pasos del Workflow

1. **Leer Contexto Relevante:** Consultar `docs/ATLAS_PRODUCT.md`, `docs/ATLAS_ARCHITECTURE.md` y las reglas correspondientes en `.agents/rules/`.
2. **Inspeccionar Código Afectado:** Usar herramientas de lectura (`view_file`, `grep_search`) para examinar con precisión los archivos y clases involucradas.
3. **Proponer Plan Estructurado:** Redactar un plan claro y conciso divido en pasos funcionales concretos.
4. **Indicar Archivos a Modificar:** Listar explícitamente las rutas completas de archivos que se crearán o modificarán.
5. **Indicar Qué NO Tocar:** Declarar explícitamente los módulos, tablas o archivos que quedan fuera de alcance.
6. **Esperar Aprobación:** Detener la ejecución y aguardar la confirmación del usuario antes de proceder a la implementación.
