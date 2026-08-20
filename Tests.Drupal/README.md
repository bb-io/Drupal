# Drupal connector tests

Tests run production connector flows against checked-in captures and local Drupal 8–11 instances without category separation. Seed demo jobs and run complete suite:

```bash
Tests.Drupal/Live/run-demo-tests.sh
```

Set `DRUPAL_VERSIONS=11` to prepare only Drupal 11 before a focused live test.

`appsettings.json.example` defines local `demo-drupal` live contexts. Copy it to ignored `appsettings.json` only when local URLs or API keys differ.

Refresh captures explicitly. Script verifies containers, recreates only connector-owned read/upload/status-transition jobs, validates authenticated responses, and atomically replaces each fixture file:

```bash
Tests.Drupal/Live/capture-fixtures.sh
```

Run complete suite directly when demo jobs already exist:

```bash
dotnet test Drupal.sln
```

Generated action files go under ignored `TestFiles/Output/`.
