# Guía de la API

## Estado

Esta guía corresponde a las rutas disponibles en `src/NexusPay.Api/Controllers`. Todo pago y proveedor es simulado. Swagger está habilitado solo en `Development`.

## Convenciones

- Prefijo `/api`; JSON y enums con nombres de texto; fechas UTC ISO 8601.
- JWT `Bearer` en todos los endpoints salvo login y `GET /health`.
- Respuestas de lista usan `items`, `totalCount`, `page`, `pageSize`; `page` empieza en 1 y `pageSize` admite 1–100 (por defecto 20).
- `X-Correlation-ID` se valida/crea y se devuelve en la respuesta.
- Errores globales usan `ProblemDetails` con `code` y `correlationId`; las validaciones incluyen `errors`.
- El backend aplica los roles; el frontend no es la barrera de seguridad.

## Endpoints

| Método y ruta | Acceso | Uso |
| --- | --- | --- |
| `POST /api/auth/login` | Anónimo, 5/minuto | Emite JWT para cuenta activa. |
| `GET /api/auth/me` | Autenticado | Perfil y roles. |
| `POST /api/auth/change-password` | Autenticado | Cambia la contraseña validando la actual. |
| `GET /api/users` | Administrator | Lista/busca usuarios y roles, paginado. |
| `POST /api/users` | Administrator | Crea usuario con roles permitidos; la contraseña no se devuelve ni registra. |
| `PATCH /api/users/{id}/status` | Administrator | Activa/desactiva usuario. |
| `PUT /api/users/{id}/roles` | Administrator | Reemplaza los roles; cambios y auditoría van en transacción. |
| `GET /api/customers`, `GET /api/customers/{id}` | Administrator, Operator, Auditor | Busca/pagina o consulta clientes. |
| `POST /api/customers`, `PUT /api/customers/{id}` | Administrator, Operator | Crea o actualiza cliente. |
| `PATCH /api/customers/{id}/status` | Administrator | Activa/desactiva cliente. |
| `GET /api/operations`, `GET /api/operations/{id}` | Administrator, Operator, Auditor | Busca operaciones; el detalle incluye historial e intentos. |
| `POST /api/operations` | Administrator, Operator | Crea una operación idempotente. |
| `POST /api/operations/{id}/process`, `POST /api/operations/{id}/cancel` | Administrator, Operator | Procesa mediante simulador o cancela una pendiente. |
| `GET /api/providers` | Cualquier autenticado | Lista proveedores, filtro opcional `enabled`. |
| `POST /api/providers`, `PUT /api/providers/{id}` | Administrator | Registra proveedor simulado o actualiza nombre/estado. |
| `GET /api/audit-logs`, `GET /api/integration-logs` | Administrator, Auditor | Consulta registros paginados con filtros. |
| `GET /api/dashboard/summary` | Administrator, Operator, Auditor | Métricas persistidas por rango de fecha. |
| `GET /api/reports/operations`, `/customers`, `/providers` | Administrator, Auditor | Reportes JSON paginados. |
| `GET /api/reports/operations.csv`, `/customers.csv`, `/providers.csv` | Administrator, Auditor | Exporta CSV, máximo 10.000 filas. |
| `GET /health` | Anónimo | Estado de API y conexión SQL. |

El cambio de contraseña está disponible; la recuperación por correo no está configurada. El registro de proveedor guarda metadatos y habilitación; el procesamiento sigue usando el simulador elegido en configuración, sin adaptadores dinámicos para registros nuevos.

## Filtros y límites

- `GET /api/customers`: `search`, `isActive`, `page`, `pageSize`, `sortBy` (`createdAt`, `customerCode`, `lastName`), `sortDirection` (`asc`, `desc`).
- `GET /api/operations`: `customerId`, `status`, `currency`, `from`, `to`, `page`, `pageSize`, `sortBy` (`createdAt`, `amount`, `status`), `sortDirection`.
- `GET /api/users`: `search`, `page`, `pageSize`.
- Proveedores, auditoría y logs: paginación; filtros según controller (`enabled`, actor/acción/entidad/correlation ID/fecha; provider/outcome/correlation ID/fecha).
- Dashboard y los tres reportes requieren `from` y `to`; el intervalo no debe superar 366 días.
- El reporte por cliente produce una fila por cliente y moneda para no sumar divisas distintas. Los volúmenes del dashboard también se separan por moneda.
- Cada CSV limita a 10.000 filas (operaciones, filas cliente/moneda o proveedores) y responde `413` si el filtro produce más.

## Ejemplos

Crear una operación requiere cliente activo, moneda `PEN`, `USD` o `EUR`, monto positivo y `Idempotency-Key` no vacío de hasta 128 caracteres:

```http
POST /api/operations
Authorization: Bearer <token>
Idempotency-Key: 2f41b1b7-b7f7-4a61-bffa-26d8c2f93113
Content-Type: application/json

{
  "customerId": "84f52ec4-f8bd-4a5c-a16d-c245d49c014e",
  "type": "Payment",
  "amount": 125.50,
  "currency": "PEN",
  "description": "Pago ficticio"
}
```

Primera solicitud: `201 Created`; la misma clave y la misma carga: `200 OK` con el mismo recurso; la misma clave con una carga diferente: `409 Conflict`.

Procesar una operación requiere una clave idempotente propia para el intento:

```http
POST /api/operations/2d3f2fb7-11e7-49cc-86d8-13dd7e087ab7/process
Authorization: Bearer <token>
Idempotency-Key: 9b79aeec-8757-4bde-970c-5c7227b462a7
```

La respuesta incluye el estado final, el proveedor, referencia/resumen y códigos de fallo cuando corresponda; no expone trazas ni configuración privada.

Ejemplo de reportes por fecha:

```http
GET /api/reports/providers?from=2026-11-01T00:00:00Z&to=2026-11-30T23:59:59Z&page=1&pageSize=20
Authorization: Bearer <token>
```

## Códigos de error

| HTTP | Situación |
| --- | --- |
| `400` | Request o validación de entrada inválida. |
| `401` | Token ausente/expirado o credenciales no válidas. |
| `403` | Rol insuficiente. |
| `404` | Recurso inexistente. |
| `409` | Duplicado, idempotencia o concurrencia. |
| `413` | CSV supera el límite de filas. |
| `422` | Regla de negocio incumplida. |
| `429` | Límite de login excedido. |
| `500` | Error inesperado; revisar `correlationId` en logs locales. |
