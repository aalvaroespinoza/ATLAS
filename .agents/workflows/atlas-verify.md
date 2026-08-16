# Workflow: ATLAS Verify

Este workflow se ejecuta para validar la integridad del sistema tras una modificación o antes de finalizar una tarea.

---

## Pasos del Workflow

1. **Compilar la Solución:** Ejecutar `dotnet build ATLAS.sln`.
2. **Ejecutar Pruebas Unitarias:** Ejecutar `dotnet test tests/ATLAS.Core.Tests/ATLAS.Core.Tests.csproj`.
3. **Revisar Warnings:** Asegurar 0 errores y 0 advertencias de compilación en todos los proyectos.
4. **Revisar Funcionalidad Afectada:** Verificar que las funciones modificadas respondan según lo especificado sin regresiones.
5. **Revisar Alcance:** Inspeccionar que no se hayan tocado archivos fuera del alcance definido.
6. **Informar Resultado:** Presentar un resumen conciso del estado de compilación, tests pasados y estado de git.
