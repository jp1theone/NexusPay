# ADR-002: SQL Server 2022 aislado en Docker

- **Estado:** Implementado y verificado localmente.
- **Fecha:** 2026-11-05 (verificación de continuidad 2026-11-06).

## Contexto

La instancia `MSSQLSERVER04` es SQL Server 2016 SP2 y debe permanecer intacta. El proyecto apunta a .NET 10 y EF Core 10; la guía actual de Microsoft para el proveedor EF Core SQL Server incluye SQL Server 2019 en adelante como motores soportados.

## Decisión

Usar una instancia independiente de SQL Server 2022 en Docker Desktop para NexusPay. El contenedor es `nexuspay-sql2022`, la base `NexusPay` y el volumen persistente exclusivo `nexuspay-sql2022-data`. No se conectará ni escribirá en `MSSQLSERVER04`.

El contenedor `nexuspay-sql2022` publica el puerto interno 1433 solo en `127.0.0.1:1433`; el puerto estaba libre. Usa el volumen nuevo y exclusivo `nexuspay-sql2022-data`, 4 CPU y un límite de 4 GiB de RAM (SQL Server configurado para hasta 3072 MiB). No comparte recursos con `MSSQLSERVER04`.

En la primera inspección se encontraron 7 contenedores antiguos detenidos/creados y 4 imágenes asociadas. El usuario autorizó retirar exactamente esos contenedores e imágenes. No se eliminaron volúmenes ni caché de build; esta última se conservó. No se modificó ningún otro contenedor o volumen.

Las credenciales no irán en archivos versionados, `docker-compose.yml`, historial de shell ni logs. La conexión de la aplicación se configurará en .NET User Secrets; la contraseña del usuario de aplicación será distinta a `sa` y tendrá permisos limitados a `NexusPay`. La contraseña de inicialización de `sa` se generará para el contenedor y no se imprimirá. Docker almacena las variables de entorno de un contenedor en sus metadatos locales; el acceso al daemon y a la cuenta local debe considerarse sensible.

## Compatibilidad y verificación

SQL Server 2022 está dentro de la versión mínima indicada por la guía actual del proveedor EF Core SQL Server (2019+). El contenedor ejecuta SQL Server 2022 Developer `16.0.4305.5` en Linux. Se verificó conexión SQL con el usuario `nexuspay_app` a la base `NexusPay` y su collation `Latin1_General_100_CI_AS`. La API usa EF Core SQL Server `10.0.14`. Cuatro migraciones están aplicadas; las pruebas usan la base aislada `NexusPay_IntegrationTests`.

Docker Desktop inicialmente falló al abrir el socket local `dockerInference`; en el siguiente intento WSL 2 arrancó y el daemon Docker respondió. No se repitió el Factory Reset. La instancia `MSSQLSERVER04` fue comprobada por consulta de solo lectura y permanece intacta.

## Recursos y costes locales

El manifiesto de `mcr.microsoft.com/mssql/server:2022-latest` reportó tres capas comprimidas de aproximadamente 598 MiB; la imagen descargada aparece con tamaño Docker de 2.35 GB. Microsoft publica 2 GB como RAM mínima de SQL Server en Linux y 6 GB como requisito mínimo de disco. La máquina reportó 23.71 GB de RAM física y Docker tiene 11.54 GiB disponibles. Después de la configuración quedaban 19.8 GB libres en C:. El volumen persistente crecerá con la base y los logs.

El contenedor consumirá RAM y CPU mientras esté en ejecución; el volumen crecerá con la base y logs. No hay coste de servicio cloud asociado a esta ejecución local; pueden aplicar los términos/licencia de SQL Server Developer para uso de desarrollo/pruebas.

## Referencias

- [Proveedor SQL Server para EF Core](https://learn.microsoft.com/ef/core/providers/sql-server/)
- [Lista de proveedores de bases de datos EF Core](https://learn.microsoft.com/ef/core/providers/)

