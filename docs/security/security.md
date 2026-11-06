# Diseño de seguridad

## Alcance

Controles implementados para una aplicación de portafolio y datos ficticios. No representa una certificación ni una garantía de seguridad para producción.

## Identidad y acceso

- ASP.NET Core Identity administra usuarios, roles, hashing de contraseñas y cambio de contraseña autenticado (`POST /api/auth/change-password`). Recuperación por correo no está implementada.
- Login emite JWT de vida limitada con sujeto, roles y expiración; la API valida firma, emisor, audiencia y expiración.
- Roles iniciales: `Administrator`, `Operator`, `Auditor`. Políticas de autorización en backend para cada endpoint.
- No hay registro público. El administrador inicial se crea en bootstrap local usando User Secrets. El alta administrativa acepta una contraseña inicial y la devuelve una sola vez; el operador debe compartirla por un canal seguro y el usuario debe cambiarla manualmente.
- Cuentas inactivas no pueden iniciar sesión ni usar tokens vigentes (la autorización consulta estado actual o mecanismo equivalente de revocación).
- Las claves de firma y contraseñas de bootstrap nunca se guardan en `appsettings.json`, commits, logs ni respuestas persistidas.
- El JWT dura 15 minutos y se mantiene en `sessionStorage` en esta versión local. La UI muestra un mensaje accionable al recibir `401`, pero este almacenamiento sigue siendo vulnerable a JavaScript ejecutado en el mismo origen; no se considera una solución final de producción.

## API y entrada

- FluentValidation (o validación explícita equivalente) en los límites de Application.
- EF Core usa parámetros; no se construye SQL mediante concatenación de entrada.
- CORS con orígenes explícitos, rate limit en login, límites de paginación y exportación.
- `ProblemDetails` uniforme; stack traces solo en logs locales protegidos, nunca en respuestas públicas.
- Correlation ID validado/generado, propagado en registros y errores.

## Datos, auditoría y logs

- Solo datos ficticios; prohibido introducir PAN, cuentas o datos personales reales.
- Nunca registrar contraseñas, JWT, secretos, claves de firma ni configuraciones privadas.
- `AuditLogs` guarda acción y resumen de cambios redactado; operaciones de escritura y sus auditorías se confirman en la misma transacción cuando sea viable.
- Borrado físico de clientes/usuarios no forma parte del flujo normal; se desactivan para preservar referencias históricas.
- Las conexiones de desarrollo pueden confiar en el certificado de SQL local de forma explícita; despliegues requieren certificado validado.
- Contraseñas de conexión solo en .NET User Secrets. La contraseña `sa` se usa únicamente para inicializar el contenedor y no aparece en archivos versionados; Docker guarda variables de entorno dentro de metadatos locales del contenedor, por lo que el daemon y el perfil Windows deben protegerse.

## Pendientes para implementación

- Implementar recuperación de contraseña solo si se configura un canal de entrega apropiado.
- Definir formalmente políticas de renovación/revocación del JWT.
- Ampliar pruebas de autorización por rol, errores y límites de abuso.
- Revisar configuración de producción antes de cualquier despliegue.
- Sustituir el almacenamiento de JWT en Web Storage por un diseño de sesión aprobado (BFF/cookie `HttpOnly`, `Secure`, `SameSite` o proveedor OIDC adecuado), con protección CSRF si se usan cookies.
- Definir revocación, renovación, expiración y comportamiento de sesión expirada, incluidos flujos sin pérdida silenciosa de formularios.

