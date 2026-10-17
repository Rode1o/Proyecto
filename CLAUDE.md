# Proyecto (Unity 6000.6, URP)

## Controlar el Editor: Unity CLI

Usa la CLI `unity` (paquete `com.unity.pipeline`) en lugar de Unity MCP, que está obsoleto. El Editor tiene que estar abierto con este proyecto.

- `unity command --tags`: lista las categorías de comandos.
- `unity command --tag <tag>` o `unity command --query <texto>`: busca comandos.
- `unity command --result-only <comando> [--arg valor]`: ejecuta un comando y devuelve solo el JSON.
- Ejemplos: `console_status`, `console --tail 20 --level error`, `clear_console`.

Ejecuta los comandos desde la raíz del proyecto o pasa `--project-path D:\Unity\Proyecto`.

## Git

- No añadas líneas `Co-Authored-By` ni de atribución a Claude en los commits ni en los PRs.
- Los binarios (imágenes, audio, modelos, etc.) van por Git LFS, según `.gitattributes`.
