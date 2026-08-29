#!/usr/bin/env bash
# Every asset Unity sees needs a committed .meta. Without one the file is dropped
# from the UPM branch (immutable folder — Unity can't generate the .meta there)
# and from the .unitypackage (built from a `find -name *.meta` list), so the SDK
# ships half-compiled. See issue #270.
#
# Usage: scripts/check-meta.sh [git-ref]   (default: the index)
set -euo pipefail

ref="${1:-}"
if [ -n "$ref" ]; then
    list() { git ls-tree -r --name-only "$ref" -- Assets/; }
else
    list() { git ls-files -- Assets/; }
fi

paths=$(mktemp) metas=$(mktemp) required=$(mktemp)
trap 'rm -f "$paths" "$metas" "$required"' EXIT

# Unity skips `~`-suffixed folders entirely and never generates a .meta for
# dot-prefixed names — but a .gitkeep still means its folder exists.
while IFS= read -r path; do
    case "$path" in *~/*) continue ;; esac
    if [ "${path%.meta}" != "$path" ]; then
        printf '%s\n' "$path" >> "$metas"
    else
        printf '%s\n' "$path" >> "$paths"
    fi
done < <(list)

hidden() { case "$1" in .*|*/.*) return 0 ;; *) return 1 ;; esac; }

while IFS= read -r path; do
    hidden "$path" || printf '%s.meta\n' "$path" >> "$required"
    dir=${path%/*}
    while [ "$dir" != "Assets" ] && [ "$dir" != "$path" ]; do
        hidden "$dir" || printf '%s.meta\n' "$dir" >> "$required"
        dir=${dir%/*}
    done
done < "$paths"

sort -u -o "$required" "$required"
sort -u -o "$metas" "$metas"

status=0

missing=$(comm -23 "$required" "$metas")
if [ -n "$missing" ]; then
    echo "Assets missing a committed .meta — Unity generated these locally, git add them:" >&2
    printf '%s\n' "$missing" | sed 's/^/  /' >&2
    status=1
fi

# A .meta over a gitignored path is deliberate: it pins the GUID of a folder
# fetched at build time (Runtime/WebSocket) or otherwise kept out of the repo.
# The trailing slash matters — a `dir/` pattern matches only what git can see is
# a directory, and on a fresh checkout the directory isn't there yet.
orphans=$(mktemp)
trap 'rm -f "$paths" "$metas" "$required" "$orphans"' EXIT
while IFS= read -r meta; do
    [ -n "$meta" ] || continue
    git check-ignore -q "${meta%.meta}" && continue
    git check-ignore -q "${meta%.meta}/" && continue
    printf '%s\n' "$meta" >> "$orphans"
done < <(comm -13 "$required" "$metas")
if [ -s "$orphans" ]; then
    echo ".meta files whose asset is gone — delete them:" >&2
    sed 's/^/  /' "$orphans" >&2
    status=1
fi

[ $status -eq 0 ] && echo "check-meta: every Assets/ entry has a committed .meta."
exit $status
