# Diseño de base de datos

## Estado y motor

Esquema de Fase 5. La base `NexusPay` de SQL Server 2022 contiene las tablas operacionales (`Customers`, `FinancialOperations`, `OperationStatusHistory`, `PaymentProviders`, `PaymentAttempts`, `IntegrationLogs`, `AuditLogs`) y las siete tablas estándar de ASP.NET Core Identity. Cuatro migraciones están aplicadas (`InitialOperationalSchema`, `AddProvidersAuditAndIntegrationLogs`, `AddStatusHistoryActorAndCorrelation`, `AddIdentitySchema`); al 2026-11-06 EF Core no reporta cambios pendientes. La instancia está aislada de `MSSQLSERVER04` (SQL Server 2016 SP2), que permanece intacta. El contenedor es `nexuspay-sql2022` y el volumen dedicado `nexuspay-sql2022-data`.

La base se creó con `Latin1_General_100_CI_AS` para comparar texto sin distinguir mayúsculas/minúsculas y distinguiendo acentos. SQL se publica solo en `127.0.0.1:1433`. La conexión de desarrollo usa `TrustServerCertificate=True` por el certificado local del contenedor; esto no se debe copiar a producción. Las migraciones se aplicaron mediante EF Core CLI 10.0.14 y la conexión configurada en User Secrets.

## Entidades y relaciones

| Tabla | Propósito y campos principales |
| --- | --- |
| `AspNetUsers` | **Implementada.** Usuarios Identity; email normalizado único, estado activo y timestamps. |
| `AspNetRoles`, `AspNetUserRoles` | **Implementadas.** Roles y asignaciones (`Administrator`, `Operator`, `Auditor`). |
| `Customers` | **Implementada.** `Id`, código y email normalizados, nombre/apellido, teléfono ficticio, tipo/número de documento, estado y timestamps. No guarda creador. |
| `FinancialOperations` | **Implementada.** `Id`, código normalizado, cliente, tipo, importe, moneda, estado, descripción, clave/hash de idempotencia, timestamps y `rowversion`. |
| `OperationStatusHistory` | **Implementada.** Estado anterior/nuevo, actor opcional con FK a Identity, fecha, motivo breve y correlation ID. |
| `PaymentProviders` | **Implementada.** Código en mayúsculas único, nombre, tipo simulado, habilitado/disponible, configuración no secreta y timestamps. Alpha está sembrado; se pueden registrar metadatos de otros simuladores. |
| `PaymentAttempts` | **Implementada.** Operación/proveedor, número de intento, clave idempotente de procesamiento, resultado y fechas. No contiene datos de tarjeta/cuenta. |
| `IntegrationLogs` | **Implementada.** Intento opcional, proveedor, correlation ID, resultado, código de error, duración y detalles técnicos redactados. |
| `AuditLogs` | **Implementada.** Actor opcional, acción, entidad/ID, fecha UTC, correlation ID, resultado y resumen redactado; dispone de ruta de consulta. |

Identity se mantiene en sus tablas estándar para aprovechar sus índices y convenciones, en lugar de crear tablas duplicadas `Users`/`Roles`.

El diagrama ERD completo, alineado con el esquema aplicado, está en [erd.md](erd.md).

## Tipos, constraints e índices

- Claves de dominio: `uniqueidentifier`; IDs de Identity seguirán el tipo string estándar de Identity. Historiales/logs: `bigint` identity.
- Fechas UTC como `datetimeoffset(3)`; montos en `decimal(18,2)`; moneda `char(3)`; `rowversion` en operaciones.
- `CHECK (Amount > 0)` y moneda restringida a `PEN`, `USD` o `EUR`; los estados válidos se controlan en dominio/aplicación, no mediante un `CHECK` SQL.
- `Customers`: índices únicos sobre `CustomerCodeNormalized`, `EmailNormalized` y `(DocumentType, DocumentNumberNormalized)` cuando el documento exista. En SQL Server, los índices únicos permiten un único `NULL` para una columna nullable; por ello el índice documental filtrará filas completas con documento no nulo.
- `FinancialOperations`: índice único sobre `OperationCodeNormalized`; índice único sobre `IdempotencyKey`; índices de lectura por `(CreatedAtUtc DESC, Id)`, `(Status, CreatedAtUtc DESC)` y `(CustomerId, CreatedAtUtc DESC)`.
- `PaymentProviders`: índice único sobre `ProviderCode`; `PaymentAttempts` único por `(OperationId, AttemptNumber)` y por clave de procesamiento.
- Historial y auditoría: índices por `(OperationId, ChangedAtUtc)`, `(OccurredAtUtc DESC)`, `(ActorUserId, OccurredAtUtc DESC)` y correlation ID según el volumen medido.
- Las claves foráneas operacionales usan borrado restrictivo para preservar trazabilidad; clientes y usuarios se desactivan. Logs/auditoría no tienen endpoint de edición/eliminación.

## IgnoreCase y unicidad

Se almacenarán campos de identidad normalizados de forma determinista con normalización invariante en la aplicación (correo y códigos normalizados; documentos normalizados conservando reglas del tipo documental). Los índices únicos se aplicarán al valor normalizado. La collation CI aporta consistencia en SQL, pero la aplicación no dependerá de la cultura del sistema ni de comparar valores sin normalizar. En C# se usará `StringComparison.OrdinalIgnoreCase` solo para comparaciones en memoria donde corresponda.

La collation observada en `master` es `Modern_Spanish_CI_AS`; la base NexusPay se creó con la collation explícita indicada arriba, sin alterar la collation del servidor ni de otras bases.

## Idempotencia y concurrencia

Al crear una operación se conserva la clave de idempotencia y un hash SHA-256 del payload canónico (sin secretos). Misma clave y mismo hash devuelve el recurso original; misma clave y payload distinto devuelve `409 Conflict`. El índice único resuelve carreras concurrentes. Al procesar, se usa una clave de procesamiento única y un identificador estable enviado al simulador. `rowversion` permite detectar actualizaciones concurrentes; una colisión vuelve como conflicto controlado, no como sobrescritura silenciosa.

## Borrado, retención y protección de datos

Clientes se desactivan; usuarios se deshabilitan. No se borran operaciones con historial. Datos de cliente serán ficticios. No se guardan credenciales del proveedor ni medios de pago. Logs de integración limitan tamaño y redactan campos antes de persistir. Se definirá retención de logs antes de un despliegue; la primera versión local no purga auditoría automáticamente.

