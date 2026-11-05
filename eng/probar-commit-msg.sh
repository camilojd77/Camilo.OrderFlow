#!/usr/bin/env bash
# Pruebas del hook .githooks/commit-msg: cada caso es un mensaje y el código de salida esperado.
# Uso (desde la raíz del repo): bash eng/probar-commit-msg.sh
# Sale con 1 si algún caso no da el código esperado.

set -u
HOOK="$(cd "$(dirname "$0")/.." && pwd)/.githooks/commit-msg"
TMP="$(mktemp)"
trap 'rm -f "$TMP"' EXIT
fallos=0
total=0

probar() {
  local esperado=$1 etiqueta=$2 mensaje=$3 obtenido
  printf '%s' "$mensaje" > "$TMP"
  sh "$HOOK" "$TMP" >/dev/null 2>&1
  obtenido=$?
  total=$((total + 1))
  if [ "$obtenido" -eq "$esperado" ]; then
    printf 'OK    esperado=%s obtenido=%s  %s\n' "$esperado" "$obtenido" "$etiqueta"
  else
    printf 'FALLO esperado=%s obtenido=%s  %s\n' "$esperado" "$obtenido" "$etiqueta"
    fallos=$((fallos + 1))
  fi
}

# Aceptados (0)
probar 0 'feat sin scope'            'feat: add order totals'
probar 0 'fix con scope'             'fix(identity): reject duplicate usernames on registration'
probar 0 'scope claude y ruta'       $'chore(claude): track .claude and add rules\n\nVersion .claude/rules and CLAUDE.local.md.'
probar 0 'breaking change'           'feat(api)!: drop version 1.0'
probar 0 'cuerpo y comentarios'      $'docs: update readme\n\nWhy it changed.\n# Please enter the commit message\n# Co-Authored-By: ignored in comments'
probar 0 'merge generado por git'    "Merge branch 'build/git-hooks' into develop"
probar 0 'revert generado por git'   $'Revert "feat: add order totals"\n\nThis reverts commit abc123.'
probar 0 'asunto de 72 caracteres'   "docs: $(printf 'x%.0s' $(seq 1 66))"
probar 0 'fin de linea CRLF'         $'fix: handle windows editors\r\n\r\nBody.\r\n'

# Rechazados (1)
probar 1 'sin tipo'                  'Add order totals'
probar 1 'tipo desconocido'          'update: order totals'
probar 1 'tipo en mayusculas'        'Feat: order totals'
probar 1 'scope en mayusculas'       'fix(Identity): order totals'
probar 1 'sin espacio tras dos puntos' 'fix:order totals'
probar 1 'asunto de 73 caracteres'   "docs: $(printf 'x%.0s' $(seq 1 67))"
probar 1 'mensaje vacio'             ''
probar 1 'solo comentarios'          $'# nothing here\n# at all'
probar 1 'co-authored-by'            $'feat: add totals\n\nCo-Authored-By: Someone <noreply@anthropic.com>'
probar 1 'co-authored-by minusculas' $'feat: add totals\n\nco-authored-by: Someone <x@y.z>'
probar 1 'generated with'            $'feat: add totals\n\nGenerated with [some tool](https://example.com)'
probar 1 'claude code'               $'feat: add totals\n\nWritten with Claude Code.'
probar 1 'modelo'                    $'feat: add totals\n\nDrafted by Claude Opus.'
probar 1 'chatgpt'                   $'feat: add totals\n\nIdea from ChatGPT.'
probar 1 'copilot'                   'feat: copilot suggestion'

echo "----"
echo "Casos: $total  Fallos: $fallos"
[ "$fallos" -eq 0 ]
