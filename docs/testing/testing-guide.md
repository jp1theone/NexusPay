# Guía de pruebas

## Estado verificado

En la última ejecución local (`dotnet test NexusPay.sln -c Release --no-restore`) pasaron **21 pruebas unitarias y 7 pruebas de integración**, sin fallos. En `frontend/nexuspay-web`, `npm run lint`, `npm test` (**3 pruebas**) y `npm run build` pasaron. Las integraciones usan el contenedor SQL Server 2022 y su base aislada `NexusPay_IntegrationTests`.

Tras los cambios más recientes de UX, `npm run lint` y `npm run build` se ejecutaron de nuevo y pasaron. Las suites automatizadas no se volvieron a ejecutar en esa actualización; los conteos anteriores corresponden a la verificación previa.

La suite cubre reglas de clientes/importes/monedas, transiciones e idempotencia, resultados del simulador, redacción de auditoría/logs, login, autorización de rutas, cliente/operación persistidos, repetición idempotente, procesamiento, dashboard, consultas, administración de proveedor, reportes CSV/JSON y restricciones del rol Auditor. Aún no se prueban carreras concurrentes reales, lockout completo, cada caso negativo de Identity ni visualmente el navegador.

## Requisitos de integración

- Docker Desktop Linux activo y contenedor `nexuspay-sql2022` iniciado, o SQL Server 2022 compatible.
- Base aislada `NexusPay_IntegrationTests`; la fixture fuerza `ConnectionStrings:NexusPay` a esa base antes de iniciar la API y aplica EF migrations allí. La fixture rechaza otra base. No apunta a `NexusPay` ni `MSSQLSERVER04`.
- User Secrets/entorno para el proyecto API:

```powershell
dotnet user-secrets set "ConnectionStrings:NexusPayIntegrationTests" "Server=localhost,1433;Database=NexusPay_IntegrationTests;User Id=nexuspay_app;Password=<contraseña-local>;Encrypt=False;TrustServerCertificate=True" --project src/NexusPay.Api
dotnet user-secrets set "BootstrapAdmin:Email" "admin@nexuspay.local" --project src/NexusPay.Api
dotnet user-secrets set "BootstrapAdmin:Password" "<contraseña-local-fuerte>" --project src/NexusPay.Api
```

El usuario SQL debe poder crear/actualizar esquema dentro de la base exclusiva de pruebas. También se pueden pasar directamente las variables `NEXUSPAY_TEST_ADMIN_EMAIL` y `NEXUSPAY_TEST_ADMIN_PASSWORD` al proceso de pruebas. En CI se usa SQL Server Developer efímero y contraseñas generadas en el runner.

## Ejecutar backend

```powershell
dotnet test NexusPay.sln -c Release
dotnet test tests/NexusPay.UnitTests/NexusPay.UnitTests.csproj -c Release
dotnet test tests/NexusPay.IntegrationTests/NexusPay.IntegrationTests.csproj -c Release
```

Las unitarias no requieren SQL. Las integraciones requieren configuración y conexión SQL anterior. No borres esa base para “limpiar” pruebas: los casos usan identificadores aleatorios y el esquema se mantiene como fixture persistente del equipo.

## Ejecutar frontend

```powershell
cd frontend/nexuspay-web
npm ci
npm run lint
npm test
npm run build
```

Vitest usa jsdom y React Testing Library. Los tests de login cubren renderizado, validación de correo antes de enviar y mensaje cuando el backend rechaza credenciales. `npm run build` verifica tipos TypeScript y empaquetado de producción.

## CI

`.github/workflows/ci.yml` tiene jobs separados. El backend instala .NET, restaura/compila, genera credenciales efímeras, inicia un contenedor SQL Server 2022 para el job, ejecuta las dos suites y publica `TestResults/**/*.trx` como artefacto. El job frontend instala Node.js 24, ejecuta `npm ci`, lint, Vitest y build. El workflow está configurado, pero su ejecución en GitHub queda por verificar cuando exista un remoto y un push autorizado.
