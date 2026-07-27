#!/usr/bin/env bash

set -euo pipefail

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

"$repo_dir/Tests.Drupal/Live/prepare-demo.sh"

cd "$repo_dir"
dotnet test Drupal.sln
