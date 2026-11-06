# Contribuir a NexusPay

NexusPay es un proyecto de portafolio. Las contribuciones deben respetar el alcance de simulación: no agregar pagos reales, datos personales reales ni credenciales de proveedores.

## Requisitos locales

- .NET SDK 10.
- Node.js 24 o una versión compatible con Vite.
- npm.
- Docker Desktop con WSL 2 y SQL Server 2022 para desarrollo local.
- Git.

## Configuración

Consulta [README.md](README.md) para preparar SQL Server, User Secrets y los proyectos. No agregues contraseñas, tokens ni archivos `.env` locales al repositorio. El único archivo de ejemplo permitido es `.env.example`.

## Convenciones

- Mantén las dependencias apuntando hacia `Domain`.
- Expón DTOs desde la API; no retornes entidades EF.
- Normaliza valores de identidad de manera determinista y aplica unicidad en SQL.
- Sigue `.editorconfig` y usa nombres descriptivos.
- Evita nuevas bibliotecas si el framework o el código explícito resuelven el caso con claridad.

## Cambios

1. Crea una rama descriptiva desde `main`.
2. Realiza cambios pequeños y enfocados.
3. Ejecuta las verificaciones correspondientes y registra cualquier requisito externo.
4. Abre un Pull Request con el problema, la solución, comandos ejecutados y limitaciones.

## Commits

Usa mensajes breves en imperativo, por ejemplo `Add customer search endpoint` o `Document local SQL setup`.

## Pull Requests

- Explica el comportamiento que cambia.
- Incluye capturas solo si cambia una pantalla.
- Actualiza documentación y diagramas cuando cambie el contrato o el esquema.
- No incluyas secretos ni datos reales.
- No publiques despliegues ni crees recursos Azure desde CI.

