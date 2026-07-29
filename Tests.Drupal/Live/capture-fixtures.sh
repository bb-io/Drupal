#!/usr/bin/env bash

set -euo pipefail

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
input_root="$repo_dir/Tests.Drupal/TestFiles/Input"

"$repo_dir/Tests.Drupal/Live/prepare-demo.sh"
mkdir -p "$input_root"

for version in 8 9 10 11; do
  case "$version" in
    8) base_url="http://localhost:8088" ;;
    9) base_url="http://localhost:8099" ;;
    10) base_url="http://localhost:8010" ;;
    11) base_url="http://localhost:8011" ;;
  esac
  api_key="blackbird-live-key-drupal-$version"
  stage="$(mktemp -d "$input_root/.Drupal${version}.capture.XXXXXX")"

  curl --fail --silent --show-error \
    --header "x-api-key: $api_key" \
    "$base_url/api/tmgmt/blackbird/jobs?state=active" \
    --output "$stage/jobs.json"
  curl --fail --silent --show-error \
    --header "x-api-key: $api_key" \
    "$base_url/api/tmgmt/blackbird/languages" \
    --output "$stage/languages.json"

  read_job_id="$(python3 - "$stage/jobs.json" "$version" <<'PY'
import json
import sys

jobs = json.load(open(sys.argv[1], encoding="utf-8"))
label = f"Blackbird connector capture read Drupal {sys.argv[2]}"
matches = [job for job in jobs if job.get("name") == label]
if len(matches) != 1:
    raise SystemExit(f"Expected one read job named {label!r}, found {len(matches)}")
print(matches[0]["id"])
PY
)"

  curl --fail --silent --show-error \
    --header "x-api-key: $api_key" \
    "$base_url/api/tmgmt/blackbird/job/$read_job_id" \
    --output "$stage/job.html"

  python3 - "$stage" "$version" "$api_key" <<'PY'
import datetime
import json
import pathlib
import re
import sys

stage = pathlib.Path(sys.argv[1])
version = int(sys.argv[2])
api_key = sys.argv[3]
jobs = json.loads((stage / "jobs.json").read_text(encoding="utf-8"))
languages = json.loads((stage / "languages.json").read_text(encoding="utf-8"))
html = (stage / "job.html").read_text(encoding="utf-8")

if not isinstance(jobs, list) or not jobs:
    raise SystemExit("jobs.json must contain a non-empty JSON array")
if not {"en", "fr"}.issubset({language.get("id") for language in languages}):
    raise SystemExit("languages.json must contain en and fr")

def one_job(kind):
    label = f"Blackbird connector capture {kind} Drupal {version}"
    matches = [job for job in jobs if job.get("name") == label]
    if len(matches) != 1:
        raise SystemExit(f"Expected one {kind} job, found {len(matches)}")
    job = matches[0]
    required = {"id", "name", "source", "target", "created"}
    if not required.issubset(job):
        raise SystemExit(f"{kind} job misses fields: {sorted(required - set(job))}")
    int(job["created"])
    return job

read_job = one_job("read")
upload_job = one_job("upload")
job_id_match = re.search(r'<meta\s+name=["\']JobID["\']\s+content=["\']([^"\']+)', html, re.I)
if not job_id_match or job_id_match.group(1) != read_job["id"]:
    raise SystemExit("job.html JobID does not match captured read job")

manifest = {
    "Version": version,
    "CapturedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00", "Z"),
    "ReadJob": {key: read_job[source] for key, source in {
        "Id": "id", "Name": "name", "Source": "source", "Target": "target", "Created": "created"
    }.items()},
    "UploadJob": {key: upload_job[source] for key, source in {
        "Id": "id", "Name": "name", "Source": "source", "Target": "target", "Created": "created"
    }.items()},
}
manifest["ReadJob"]["Created"] = int(manifest["ReadJob"]["Created"])
manifest["UploadJob"]["Created"] = int(manifest["UploadJob"]["Created"])
(stage / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

for path in stage.iterdir():
    if api_key in path.read_text(encoding="utf-8"):
        raise SystemExit(f"API key leaked into capture: {path.name}")
PY

  destination="$input_root/Drupal$version"
  mkdir -p "$destination"
  for file in jobs.json languages.json job.html manifest.json; do
    mv -f "$stage/$file" "$destination/$file"
  done
  rmdir "$stage"
  echo "Captured Drupal $version fixtures."
done
