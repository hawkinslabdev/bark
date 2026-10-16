#!/usr/bin/env bash
# Static export of the current docs plus every older entry in Bark/docs/config.json "versions".
# An older entry's link is its git tag, e.g. "/v0.0.29/". Older tags render with today's
# binary and today's "versions" list, so every version's switcher stays in sync.
#
# Usage: scripts/export-versions.sh <publish-dir> <out-dir> <base-url> <base-path>
# Local:  dotnet publish Bark/src/Bark -c Release -o /tmp/bark-publish
#         scripts/export-versions.sh /tmp/bark-publish /tmp/site/bark http://localhost:8000 /bark
#         python3 -m http.server -d /tmp/site 8000   # open http://localhost:8000/bark/
set -euo pipefail

publish=$(realpath "$1")
out=$(realpath -m "$2")
base_url=$3
base_path=$4
repo=$(git rev-parse --show-toplevel)
config="$repo/Bark/docs/config.json"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

export_docs() {
    (cd "$publish" && Docs__RootPath="$1" ./Bark --export "$2" --base-url "$base_url" --base-path "$3")
}

export_docs "$repo/Bark/docs" "$out" "$base_path"

jq -r 'select(.versions.enabled != false) | (.versions.items // [])[1:][].link | select(startswith("/"))' "$config" | while read -r link; do
    tag=$(basename "$link")
    docs="$tmp/$tag"
    mkdir -p "$docs"
    git -C "$repo" archive "$tag" Bark/docs | tar -x -C "$docs" --strip-components=2
    [ -f "$docs/config.json" ] || echo '{}' > "$docs/config.json"
    jq --slurpfile current "$config" '.versions = $current[0].versions' "$docs/config.json" > "$docs/config.tmp"
    mv "$docs/config.tmp" "$docs/config.json"
    export_docs "$docs" "$out/$tag" "$base_path/$tag"
done
