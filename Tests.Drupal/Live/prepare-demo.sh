#!/usr/bin/env bash

set -euo pipefail

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
demo_dir="${DEMO_DRUPAL_DIR:-$repo_dir/../demo-drupal}"

cd "$demo_dir"

versions="${DRUPAL_VERSIONS:-8 9 10 11}"

for version in $versions; do
  service="drupal${version}"
  if ! docker compose ps --status running --services | grep -qx "$service"; then
    echo "Required demo service is not running: $service" >&2
    exit 1
  fi
done

for version in $versions; do
  service="drupal${version}"
  docker compose exec -T -e TEST_VERSION="$version" "$service" php <<'PHP'
<?php

chdir('/opt/drupal/web');
$autoloader = require 'autoload.php';
$request = Symfony\Component\HttpFoundation\Request::create('/');
$kernel = Drupal\Core\DrupalKernel::createFromRequest($request, $autoloader, 'prod');
$response = $kernel->handle($request);

try {
  $version = getenv('TEST_VERSION');
  $translator_id = 'blackbird_connector_tests_drupal_' . $version;
  $api_key = 'blackbird-live-key-drupal-' . $version;
  $translator = Drupal\tmgmt\Entity\Translator::load($translator_id);
  if (!$translator) {
    $translator = Drupal\tmgmt\Entity\Translator::create([
      'name' => $translator_id,
      'label' => 'Blackbird connector tests Drupal ' . $version,
      'plugin' => 'blackbird',
      'settings' => ['api_key' => $api_key],
      'auto_accept' => FALSE,
      'weight' => 0,
    ]);
  }
  else {
    $translator->setSetting('api_key', $api_key);
  }
  $translator->save();

  if (!Drupal\language\Entity\ConfigurableLanguage::load('fr')) {
    Drupal\language\Entity\ConfigurableLanguage::createFromLangcode('fr')->save();
  }

  $labels = [
    'Blackbird connector capture read Drupal ' . $version,
    'Blackbird connector capture upload Drupal ' . $version,
    'Blackbird connector status transition Drupal ' . $version,
  ];
  $job_storage = Drupal::entityTypeManager()->getStorage('tmgmt_job');
  foreach ($labels as $label) {
    foreach ($job_storage->loadByProperties(['label' => $label, 'translator' => $translator_id]) as $old_job) {
      $old_job_id = $old_job->id();
      try {
        $old_job->delete();
      }
      catch (Throwable $exception) {
        // Drupal 11 demo can retain tmgmt_local configuration while its task
        // table is absent. Keep fallback strictly scoped to connector-owned ID.
        $database = Drupal::database();
        foreach (['tmgmt_message', 'tmgmt_remote', 'tmgmt_job_item', 'tmgmt_job'] as $table) {
          if ($database->schema()->tableExists($table)) {
            $database->delete($table)->condition('tjid', $old_job_id)->execute();
          }
        }
        $job_storage->resetCache([$old_job_id]);
        print 'Drupal ' . $version . ': direct cleanup for connector-owned job ' . $old_job_id . PHP_EOL;
      }
    }
  }

  $nodes = Drupal::entityTypeManager()->getStorage('node')->loadMultiple();
  $node = reset($nodes);
  if (!$node) {
    throw new RuntimeException('Standard Drupal source node was not found.');
  }

  foreach ($labels as $label) {
    $job = tmgmt_job_create('en', 'fr', 1, ['label' => $label]);
    $job->translator = $translator_id;
    $job->save();
    $job->addItem('content', 'node', $node->id());
    $job->requestTranslation();
    print 'Drupal ' . $version . ': ' . $label . ' job=' . $job->id() . PHP_EOL;
  }
}
finally {
  $kernel->terminate($request, $response);
}
PHP
done
