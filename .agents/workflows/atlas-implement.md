# Workflow: ATLAS Implement

Este workflow debe ejecutarse una vez aprobado el plan de implementación.

---

## Pasos del Workflow

1. **Implementar Solo el Bloque Aprobado:** Realizar estrictamente los cambios autorizados en el plan.
2. **No Agregar Funcionalidades Adicionales:** Respetar la regla anti-scope-creep; no añadir código no solicitado.
3. **Compilar la Solución:** Ejecutar `dotnet build ATLAS.sln` y verificar 0 errores y 0 advertencias nuevas.
4. **Ejecutar Tests Unitarios:** Ejecutar `dotnet test` y validar que el 100% de las pruebas pasen satisfactoriamente.
5. **Revisar Cambios:** Comprobar con `git status` y `git diff` que no existan modificaciones no deseadas.
6. **Preparar Commit:** Realizar commit descriptivo con convención estándar (ej. `feat:`, `fix:`, `perf:`, `style:`) y pushear a `origin/main`.
