#!/usr/bin/env bash
# Prueba el hook git-solo-lectura.cs: cada caso se envía como JSON por stdin y se compara el código de salida.
# Uso (desde la raíz del repo, en Git Bash o Linux):  bash .claude/hooks/probar-git-solo-lectura.sh
# Sale con 1 si algún caso no da el código esperado.

set -u
HOOK="$(cd "$(dirname "$0")" && pwd)/git-solo-lectura.cs"
fallos=0
total=0

json_escape() {
  local s=$1
  s=${s//\\/\\\\}
  s=${s//\"/\\\"}
  s=${s//$'\n'/\\n}
  s=${s//$'\r'/\\r}
  s=${s//$'\t'/\\t}
  printf '%s' "$s"
}

probar() {
  local esperado=$1 payload=$2 etiqueta=$3
  local salida obtenido
  salida=$(printf '%s' "$payload" | dotnet run "$HOOK" 2>&1 >/dev/null)
  obtenido=$?
  total=$((total + 1))
  if [ "$obtenido" -eq "$esperado" ]; then
    printf 'OK    esperado=%s obtenido=%s  %s\n' "$esperado" "$obtenido" "$etiqueta"
  else
    printf 'FALLO esperado=%s obtenido=%s  %s  %s\n' "$esperado" "$obtenido" "$etiqueta" "$salida"
    fallos=$((fallos + 1))
  fi
}

comando() {
  local esperado=$1 cmd=$2
  probar "$esperado" "{\"tool_name\":\"Bash\",\"tool_input\":{\"command\":\"$(json_escape "$cmd")\"}}" "$cmd"
}

# Permitidos (0)
comando 0 'git status'
comando 0 'git status --short'
comando 0 'git diff develop...HEAD --stat'
comando 0 'git log --oneline -20'
comando 0 'git show HEAD:docs/architecture.md'
comando 0 'git merge-base develop HEAD'
comando 0 'git rev-parse --abbrev-ref HEAD'
comando 0 'git ls-files .claude'
comando 0 'git blame -L 10,20 Camilo.OrderFlow.WebApi/Program.cs'
comando 0 'git branch --show-current'
comando 0 '  git diff --name-only  '

# Bloqueados (2)
comando 2 'git push'
comando 2 'git push origin develop'
comando 2 'git commit -m "x"'
comando 2 'git checkout main'
comando 2 'git branch -D feature/x'
comando 2 'git branch'
comando 2 'git reset --hard HEAD'
comando 2 'git status; rm -rf .'
comando 2 'git status && git push'
comando 2 'git log | head -5'
comando 2 'git diff > salida.txt'
comando 2 'git show $(git rev-parse HEAD)'
comando 2 'git log `whoami`'
comando 2 'git -c core.pager=sh log'
comando 2 'git log -c core.pager=less'
comando 2 'git diff --output=/tmp/x'
comando 2 'git diff --ext-diff'
comando 2 'git log --textconv'
comando 2 'git diff --open-files-in-pager=sh'
comando 2 'dotnet build'
comando 2 'rm -rf bin'
comando 2 'gitk'
comando 2 'git statusx'
comando 2 $'git status\nrm -rf .'
comando 2 'git diff {a,b}'

# Entradas inválidas (2)
probar 2 '' 'stdin vacío'
probar 2 'esto no es json' 'JSON roto'
probar 2 '{"tool_name":"Bash","tool_input":{}}' 'sin command'
probar 2 '{"tool_name":"Bash","tool_input":{"command":"   "}}' 'command en blanco'

echo "----"
echo "Casos: $total  Fallos: $fallos"
[ "$fallos" -eq 0 ]
