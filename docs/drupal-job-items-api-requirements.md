# Drupal TMGMT job-items API requirements

## Purpose

Blackbird downloads translation jobs as TMGMT File HTML. Each `<div class="asset" id="…">` represents one TMGMT job item, but exported HTML exposes only job-item ID. It does not identify source page, content entity, label, or URL.

Add read-only API that maps each exported asset to its Drupal/TMGMT source. Blackbird will use mapping to annotate interoperable HTML without changing TMGMT import structure.

## Endpoint

```http
GET /api/tmgmt/blackbird/job/{job_id}/items
x-api-key: {translator_api_key}
Accept: application/json
```

### Authentication and authorization

- Reuse existing `x-api-key` authentication.
- Resolve Blackbird translator from API key using constant-time comparison.
- Return only job owned by resolved translator.
- Reject missing, invalid, or unauthorized API key/job combinations with same status and error format as existing job endpoint.
- Disable page caching because authentication varies by request header.
- Return `Cache-Control: no-store, private`.

## Response contract

Return JSON array containing all job items. IDs must be strings.

```json
[
  {
    "job_item_id": "2",
    "source_plugin": "content",
    "item_type": "node",
    "item_id": "42",
    "label": "About us",
    "source_url": "https://drupal.example.com/node/42",
    "admin_url": "https://drupal.example.com/admin/tmgmt/items/2"
  }
]
```

### Fields

| Field | Required | Meaning |
| --- | --- | --- |
| `job_item_id` | Yes | TMGMT job-item ID. Must equal corresponding HTML `asset` ID. |
| `source_plugin` | Yes | TMGMT source plugin, for example `content`, `config`, or `locale`. |
| `item_type` | Yes | Source-specific item type. For content sources this is normally Drupal entity type such as `node`. |
| `item_id` | Yes | Source-specific item ID. For content sources this is normally entity ID. |
| `label` | Yes | Human-readable TMGMT job-item label. Empty string allowed when source cannot provide label. |
| `source_url` | No | Absolute public/canonical source URL. Return `null` when source plugin cannot provide one. |
| `admin_url` | Yes | Absolute Drupal URL for reviewing TMGMT job item. |

### General behavior

- Return every job item in one response; no pagination.
- Preserve deterministic order, ascending by numeric job-item ID.
- Return `[]`, never `null`, when valid job has no items.
- Use TMGMT APIs (`getPlugin()`, `getItemType()`, `getItemId()`, `label()`, and source URL methods); do not infer identity from encoded atom IDs.
- Support non-content TMGMT source plugins. `item_type` and `item_id` remain source-specific and must not be assumed to be Drupal entity identifiers.
- Generate absolute URLs using current request/site base URL.
- Avoid loading full source entities unless TMGMT requires it for label or URL.

## Error behavior

| Scenario | Required result |
| --- | --- |
| Missing or invalid API key | `403` with actionable JSON/Drupal error response. |
| Job does not exist | `404`. |
| Job belongs to different translator | `403`. |
| Invalid non-numeric job ID | Route does not match or returns `404`. |
| One item cannot produce source URL | Return item with `source_url: null`; do not fail entire response. |
| Unexpected item serialization failure | `500`; log job ID and job-item ID without logging API key. |

## Drupal module implementation

- Add route `tmgmt_blackbird.api.job_items` for `GET /api/tmgmt/blackbird/job/{tmgmt_job}/items`.
- Add `jobItems(JobInterface $tmgmt_job)` controller method to existing Blackbird API service.
- Reuse existing translator authorization check used by job HTML endpoint.
- Keep endpoint compatible with Drupal 8.9, 9.5, 10.x, and 11.x supported by module.
- Do not alter existing languages, jobs, job-download, or job-upload response formats.
- Do not alter TMGMT File HTML exporter/importer.

## Blackbird app behavior

`Download job content` will call job-items endpoint only for interoperable HTML output. Original format remains untouched.

For each HTML asset, match `asset/@id` to `job_item_id` and add:

```html
<div
  class="asset"
  id="2"
  data-drupal-source-plugin="content"
  data-drupal-item-type="node"
  data-drupal-item-id="42"
  data-drupal-label="About us"
  data-drupal-source-url="https://drupal.example.com/node/42"
  data-drupal-admin-url="https://drupal.example.com/admin/tmgmt/items/2">
</div>
```

- Keep attributes as metadata only; TMGMT import continues using asset/atom IDs.
- HTML-encode attribute values through DOM API.
- Omit `data-drupal-source-url` when API value is `null`.
- Throw actionable application error when HTML contains asset absent from API response or API contains item absent from HTML. Partial mapping risks misleading content provenance.
- Continue building `data-blackbird-key` from job ID, job-item ID, and atom ID.
- Do not treat individual job items as separate blueprint content units. TMGMT job remains UCID/content unit.

## Acceptance tests

Run against local Drupal 8, 9, 10, and 11 instances.

1. Single-node job returns one item with matching job-item ID, `content`, `node`, node ID, label, and absolute URLs.
2. Multi-node job returns every item once and in deterministic order.
3. Job using non-content source returns generic source plugin/type/ID without failure.
4. Empty job returns `[]`.
5. Invalid API key returns `403`.
6. API key for different translator cannot access job.
7. Unknown job returns `404`.
8. Response has `no-store, private` cache policy.
9. Downloaded interoperable HTML contains correct metadata on every asset.
10. Original HTML output remains byte-for-byte unchanged.
11. Annotated HTML successfully uploads through existing job endpoint.
12. HTML and XLIFF 1/2 roundtrips retain job-item metadata and existing Blackbird metadata.

## Out of scope

- Creating or modifying TMGMT jobs.
- Returning complete source entity payloads or field values.
- Changing job-level UCID strategy.
- Adding content-created/content-updated Drupal events.
- Changing TMGMT File HTML import semantics.
