# Test patterns across `bb-*` connector repositories

## Scope and method

Static review performed on 2026-07-16 against local repositories under `/home/terales/coding`.

Included:

- `bb-aem`
- `bb-bitbucket`
- `bb-github`
- `bb-gitlab`
- `bb-google-vertex-ai`
- `bb-phrase-strings`
- `bb-phrase-tms`
- `bb-taus`
- `bb-xtm`

Excluded by scope: `bb-drupal`, `bb-filters`, and `bb-utilities`.

Review covered test project files, test fixtures, shared test infrastructure, test data, configuration examples, and in-repository CI workflows. Tests were not executed. Most suites require credentials and live vendor data; several tests create, update, upload, delete, subscribe, or poll external resources.

Terms used below describe observed behavior, not framework categories:

- **Unit**: pure logic or model behavior with no network or filesystem dependency.
- **Local integration**: production actions/converters exercised with local fixture files and a test file-management client.
- **Live integration**: production actions, handlers, validators, polling lists, or webhook subscription handlers exercised against vendor APIs.
- **Flow**: one test spans several operations, such as upload, poll, download, and parse.

Counts are active source-level test attributes, not discovered runtime cases. Custom credential data sources and MSTest data rows can expand one source method into several cases. `bb-phrase-tms` count excludes ten commented-out webhook tests.

## Common organization

All nine repositories broadly use this shape; AEM and some others add specialized base fixtures:

```text
<solution>.sln
Apps.<Connector>/
Tests.<Connector>/
├── Base/
│   ├── TestBase.cs
│   ├── FileManager.cs
│   ├── TestBaseMultipleConnections.cs       # when multiple auth modes exist
│   └── ContextDataSourceAttribute.cs         # name varies
├── <Surface>Tests.cs
├── GlobalUsings.cs
├── TestFiles/
│   ├── Input/
│   └── Output/
├── appsettings.json.example
└── Tests.<Connector>.csproj
```

`bb-github` keeps `MSTestSettings.cs` at project root. `bb-phrase-tms` also keeps webhook JSON payloads in `Webhooks/`. Most test classes remain in one flat directory, even in suites with more than 100 tests.

### Project setup

Shared project pattern:

- Target framework: `net8.0` in all nine repositories.
- Test framework: MSTest in all nine repositories.
- Direct `ProjectReference` from test project to connector project.
- `Microsoft.NET.Test.Sdk`, MSTest adapter/framework, and `coverlet.collector` in every test project.
- MSTest and test SDK versions vary by repository; no central package version file governs them.
- `Microsoft.Extensions.Configuration.Json` loads test configuration from `appsettings.json`.
- Every repository tracks `appsettings.json.example`; local `appsettings.json` supplies credentials and fixture paths.
- Included connector workflows contain no in-repository `dotnet test` command. External CI behavior cannot be inferred from these repositories.

### Shared fixtures

`TestBase` usually performs all environment setup in its constructor:

1. Load `appsettings.json`.
2. Convert `ConnectionDefinition` entries into `AuthenticationCredentialsProvider` objects.
3. Build one or more `InvocationContext` objects.
4. Instantiate a local `IFileManagementClient` implementation.

Two credential shapes recur:

- **Single context**: GitHub, Google Vertex AI, and TAUS read one flat credential set and expose one `InvocationContext`.
- **Multiple contexts**: AEM, Bitbucket, GitLab, Phrase Strings, Phrase TMS, and XTM read groups and execute selected tests once per auth mode.

Multiple-context mechanisms differ:

- AEM uses MSTest `DynamicData` and `AllInvocationContexts`.
- Bitbucket uses custom `TargetConnectionsAttribute`.
- GitLab, Phrase Strings, Phrase TMS, and XTM use near-identical `ContextDataSourceAttribute` implementations.
- Attribute order varies between `[TestMethod, ContextDataSource]` and `[ContextDataSource, TestMethod]`.

Phrase TMS has heavier implicit setup: `TestBase` performs a live login during construction for an OAuth credential group, then appends generated authorization credentials. Test construction itself can therefore fail before test arrange code runs.

### File handling

Most suites replace Blackbird file management with a small local adapter:

- `DownloadAsync` reads from `TestFiles/Input`.
- `UploadAsync` writes production output to `TestFiles/Output` and returns a `FileReference`.
- Some adapters can reopen outputs or create input references.
- Input fixtures cover JSON, HTML, XLIFF, documents, images, and other connector-specific content.
- Output assertions range from checking returned filenames to reparsing XML/XLIFF/TMX and asserting exact structure.

Path resolution is inconsistent. Some adapters walk three build-directory parents, some walk four, and GitLab/Phrase Strings-style adapters use a configured `TestFolder`. Output folders are commonly created or modified during a run.

## Style and granularity patterns

### Test construction

Production classes are normally constructed directly inside each test:

```csharp
var actions = new SomeActions(invocationContext, FileManager);
var result = await actions.SomeAction(identifier, request);
Assert.IsNotNull(result);
```

No dependency-injection container or shared application host is used. Network behavior is normally real, not stubbed. Local webhook tests construct handlers and feed captured or hand-built payloads directly.

### Naming

Three naming styles coexist:

- `Method_Scenario_ExpectedResult` — strongest in newer AEM, Bitbucket, GitLab, and focused helper tests.
- `Action_IsSuccess` or `Action_ReturnsValue` — common live integration smoke style.
- Lowercase or mixed snake case such as `Search_jobs_works` and `Edit_xliff` — frequent in Phrase TMS and older AI/action tests.

Typos exist in active names (`IssSuccess`, `Succerss`, `Intergrated`, `Threshhold`, `Hanlder`). Class names usually map to one production action group or runtime surface.

### Arrange/act/assert structure

- AEM, Bitbucket, GitLab, Phrase TMS, and XTM commonly use explicit `// Arrange`, `// Act`, `// Assert` comments.
- Phrase Strings, GitHub, TAUS, and many older tests use compact code without section comments.
- Async connector methods use `async Task`; pure helper/model tests use synchronous `void`.
- Results are often serialized to `TestContext` or `Console` for manual diagnosis.

### Assertion depth

Observed levels:

1. **Existence smoke**: call succeeds; response or file is non-null.
2. **Contract smoke**: response collection is non-empty, validator success flag matches, or filename/type is correct.
3. **Behavior assertion**: filters include/exclude exact inputs, model fields map correctly, or exception type/message is verified.
4. **Artifact assertion**: output file is reparsed and exact units, languages, states, tags, counts, or metadata are checked.
5. **Flow assertion**: multi-operation roundtrip preserves or transforms expected content.

Live action tests lean toward levels 1–2. Unit and local conversion tests lean toward levels 3–4. A smaller number of interoperability tests reach level 5.

Exception style also varies: newer tests use `Assert.ThrowsExceptionAsync`/`Assert.ThrowsExactlyAsync`; some Bitbucket tests use explicit `try`/`catch` plus `Assert.Fail`.

### State and determinism

Live tests commonly contain hard-coded repository, project, job, file, model, user, branch, or language identifiers. Tests can depend on resource existence and account state. Mutation and cleanup strategy varies:

- Some flows create and delete their own object in one test.
- Some action tests mutate shared remote objects without restoring prior state.
- Some deletion tests assume a pre-existing disposable object.
- Polling tests use real time and vendor job completion.
- Google Vertex AI includes polling loops and fixed delays for batch jobs.
- XTM includes an active stress-style test that uploads 100 files with a four-second delay per iteration.
- AEM, Phrase Strings, and Phrase TMS sometimes call `Assert.Inconclusive` when required environment data is unavailable.

## Repository matrix

| Repository | Active test methods | Credential execution | Main test character | Granularity |
|---|---:|---|---|---|
| `bb-aem` | 67 | Multiple contexts; 59 `DynamicData` uses | Hybrid live + local conversion | Fine converter/content-fragment cases; medium action smoke/flows |
| `bb-bitbucket` | 55 | Multiple contexts through `TargetConnections` | Hybrid live + unit webhook logic | Fine webhook filters/mapping; medium action smoke |
| `bb-github` | 18 | Single context | Hybrid live + local webhook/model tests | Fine filter/model cases; coarse file and subscription flows |
| `bb-gitlab` | 19 | Multiple contexts through `ContextDataSource` | Mostly live | One endpoint/filter scenario per test; mostly contract smoke |
| `bb-google-vertex-ai` | 20 | Single context with inferred auth type | Live AI/file flows plus local artifact parsing | Mixed: precise XLIFF/report assertions and broad AI smoke |
| `bb-phrase-strings` | 74 | Multiple contexts through `ContextDataSource` | Broad live action suite plus local webhook logic | Mostly one endpoint per test; several roundtrip/data-row flows |
| `bb-phrase-tms` | 122 | Multiple contexts through `ContextDataSource`; constructor login | Broadest live suite plus a few units | Many one-handler/one-endpoint smoke tests; selected exact payload/error tests |
| `bb-taus` | 21 | Single context | Hybrid live AI flows + unit parsers/helpers | Fine parser/helper cases; medium background action flows |
| `bb-xtm` | 59 | Multiple contexts through `ContextDataSource` | Broad live suite + local TMX generation | Exact artifact tests; action smoke; large webhook/stress flows |

## Per-repository findings

### `bb-aem`

Organization:

- `Tests.AEM/Base` contains `TestBase`, local `FileManager`, and abstract `BaseDataHandlerTests`.
- Abstract data-handler tests are inherited by page, tag, and content-fragment handler fixtures.
- Invocation contexts are parameterized through `DynamicData`, with connection type included in test display names.

Tested surfaces:

- Asset search, download, metadata/property updates.
- Content search/download/upload and tag changes.
- Content-fragment search, permissions, references, nesting, exclusions, tag changes, and uploads.
- AEM Guides DITA search/download/upload variants.
- Page/tag/content-fragment data handlers and content picker navigation.
- Page/tag/content-fragment polling behavior.
- Connection validation and cloud token service.
- JSON/HTML/original content converters and roundtrips.

Granularity:

- Strongest connector suite for content-fragment edge cases: one test per exclusion, reference depth, input format, or tag operation.
- Converter tests use local fixtures and exact DOM/entity/roundtrip assertions.
- Live action tests usually verify success, returned values, and selected fields; some environment-dependent branches become inconclusive.

### `bb-bitbucket`

Organization:

- Flat action test files plus `Base` infrastructure.
- `TargetConnectionsAttribute` selects all or named connection types and produces readable case names.
- Unit webhook fixtures do not inherit `TestBase`, separating pure logic from credential-dependent tests.

Tested surfaces:

- Branch, file, pull request, repository, and user actions.
- Connection validation and dynamic data handlers.
- Webhook subscription/payload handling.
- File webhook branch filters, regex extraction, commit aggregation, and error cases.
- Pull request title, description, branch filters, payload mapping, and changed-file filtering.

Granularity:

- Webhook helper tests are fine-grained and deterministic, with separate positive, negative, combined-filter, mapping, and exception cases.
- Live action tests usually cover one connector method and assert response shape or Boolean outcome.
- Upload/delete/merge/create operations use remote state and can mutate shared test repositories.

### `bb-github`

Organization:

- Small flat suite with one shared context and local file manager.
- Captured push payload is embedded directly in `SubscriptionTests` rather than stored as a fixture file.

Tested surfaces:

- Connection validation.
- Repository, workflow, file-path, and folder-path data handlers.
- Create/update and download/translate/upload file flows.
- Live webhook subscribe/unsubscribe lifecycle.
- Local push webhook folder/path filtering.
- Workflow dispatch/run JSON model deserialization.

Granularity:

- Model and push-filter tests assert exact fields and preflight/default behavior.
- File tests are multi-step local/live flows with metadata checks.
- Subscription lifecycle is one stateful live test covering get, subscribe, get, unsubscribe, and get.
- Branch, pull-request, repository, user, and workflow action classes have no direct corresponding action fixture in this suite.

### `bb-gitlab`

Organization:

- All 19 tests use `ContextDataSource`; every configured auth context can execute each applicable method.
- `TestBaseWithContext` adds JSON/data-handler logging.
- File picker tests use a configured local file-management client where needed.

Tested surfaces:

- Get branch.
- Get/create repository and get/push file.
- Commit search filters: author include/exclude, maximum results, date range, message, and file path.
- Find-first commit and exact time-boundary behavior.
- OAuth, personal access token, and invalid connection validation.
- File picker root mapping.

Granularity:

- Commit tests use one filter concern per method and include a useful exact boundary case.
- Tests still call live GitLab, so filter precision depends on known repository history.
- Pull request and user actions plus most dynamic handlers have no direct fixture mapping.

### `bb-google-vertex-ai`

Organization:

- One context supports Gemini API key or service-account credentials; connection type can be inferred from configured keys.
- Service-account JSON can be read from a configured local file and inserted into credentials.
- Test files are written to output and reparsed as Blackbird transformations.

Tested surfaces:

- Text and file-based generation.
- Edit and translate for text, HTML, XLIFF, locale-sensitive text, and batch flows.
- Content shortening with character restrictions and segment-state filtering.
- MQM/report generation and score thresholds.
- File-search store end-to-end flow.
- Model data handler and connection validation.

Granularity:

- Content-shortening tests are detailed: unit counts, restricted/matched/updated counts, grapheme limits, segment states, token usage, and error lists.
- Reporting tests inspect generated XLIFF/report behavior.
- General AI generation/localization tests often assert non-empty output, not exact wording, matching nondeterministic model output.
- Batch tests start real jobs and poll with fixed delays; flow duration depends on vendor completion.

### `bb-phrase-strings`

Organization:

- Broad flat suite: one fixture per action group, one aggregated data-handler fixture, plus polling/webhook fixtures.
- `ContextDataSource` runs action tests across configured connection types.
- Interoperable XLIFF tests use data rows and local input/output files.

Tested surfaces:

- Projects, jobs, keys, translations, repositories, orders, comments, screenshots, Figma links, users, and teams.
- Fourteen dynamic data handlers.
- Connection validation.
- Repository-sync polling.
- Phrase job-status webhook event/project/job ID/name filters.
- Download/upload interoperability roundtrip and quality-performance metadata.
- Project variables, including create/set/get/delete flows.

Granularity:

- Most action methods get one live success-path test with a response existence or selected-field assertion.
- Webhook tests are deterministic and scenario-specific, including case-insensitive include/exclude filters and preflight behavior.
- Interoperability fixture is coarse and deep: a single parameterized roundtrip performs several transformations and validates final content.
- Several action tests create, update, complete, reopen, or delete remote resources using hard-coded identifiers.

### `bb-phrase-tms`

Organization:

- Largest connector suite: 122 active methods across action, data-source, polling, webhook, client, and helper fixtures.
- Thirty-nine data-source tests are grouped in one `DataSources` class.
- Webhook payload samples also exist as JSON files.
- Ten substantial webhook tests are commented out rather than marked as active tests.

Tested surfaces:

- Jobs, projects/templates, analyses, automation, clients, conversations/comments, custom fields, glossaries, interoperability, QA/LQA, translation memories, and users.
- Broad dynamic data-source coverage for business units, clients, domains, jobs, languages, LQA, net rates, price lists, projects/templates, custom fields, references, subdomains, term bases, TMs, users, vendors, workflows, conversations, comments, and segments.
- Connection validation and user polling.
- Webhook success and explicit misconfiguration-to-bad-request mapping.
- Automated project settings payload transformation.

Granularity:

- Data-source tests commonly assert only non-null and print returned values: broad surface smoke, shallow item semantics.
- Action tests mostly use one endpoint per method and hard-coded remote identifiers.
- Custom-field tests cover type-specific get/set behavior separately.
- Payload builder tests are pure, precise units asserting field preservation, override, removal, and nested-object-to-UID conversion.
- Validation tests assert specific exception paths for mismatched arrays and range constraints.
- Commented webhook block represents unexecuted scenarios for job/status/source/workflow/language filters.

### `bb-taus`

Organization:

- Small suite with one flat credential context and shared local file manager.
- Pure parser/helper fixtures avoid `TestBase`; live action fixtures inherit it.

Tested surfaces:

- Edit and review actions for text, XLIFF 1.2, XLIFF 2/contentful/XTM variants, and background processing.
- Batch-finished polling and background result download.
- Connection validation.
- XLIFF batch score/APE metadata parsing.
- Segment state/exclusion decision helper.
- Default background request behavior.

Granularity:

- Parser/helper tests are narrow and exact, asserting every mapped score, remark, billing, state, and default field.
- Edit/review tests are live medium-grained flows that assert returned files/text and selected response properties.
- Background flow crosses submit, poll, and download steps and includes real waiting.

### `bb-xtm`

Organization:

- Multiple credential contexts use the shared `ContextDataSource` pattern.
- Tests are grouped by files, projects, workflows, polling, webhooks, glossary, TM, generated TMX, and data sources.
- Webhook fixture exceeds 600 lines because payloads and request construction are repeated inline.

Tested surfaces:

- File generation/upload/download, translated/reference/project files, and interoperable translation upload options.
- Project get/list/details/completion/status/estimates/create/update/download/analysis.
- Workflow advancement.
- Glossary export, TM import, edit-based TM tagging dry run, and local XLIFF-to-TMX generation.
- Eleven dynamic data handlers.
- Connection validation.
- Project and workflow polling plus detailed automatic/manual webhook transition filters.

Granularity:

- Local TMX generation has exact artifact-level assertions for segment counts, XML structure, source/target languages, and custom tags.
- Webhook tests verify flight/preflight behavior across event status, project name, and form-encoded payloads, but each test carries large setup blocks.
- Many live action/data-handler tests are response smoke tests.
- Active 100-file upload test behaves as stress/manual verification inside normal suite and can run for more than six minutes before API time.
- Customer, user, template, LQA, system, and several other action groups have no direct action fixture mapping.

## Closest cross-repository consensus for Drupal

Observed connector consensus, without imposing one repository's exceptions:

- Keep `Tests.Drupal` beside `Apps.Drupal` and reference production project directly.
- Use MSTest on `net8.0` with test SDK and coverlet collector.
- Put credential/config construction and `InvocationContext` in `Base/TestBase.cs`.
- Put local Blackbird file handling in `Base/FileManager.cs`; use `TestFiles/Input` and `TestFiles/Output`.
- Track `appsettings.json.example`, not credentials.
- Group tests by runtime surface: action group, data handler family, polling list, connection validator, webhook behavior, and pure helper/converter.
- Construct production classes directly.
- Use `Method_Scenario_ExpectedResult` where a scenario has meaningful branches; use action-oriented names for simple endpoint smoke tests.
- Keep one behavior branch per unit/helper test.
- For files and interoperability, parse output and assert content/metadata/state rather than only `FileReference` existence.
- For live connector actions, assert stable response contracts and selected identifiers while avoiding assertions on vendor-generated or AI-generated text.
- If Drupal supports multiple auth modes, parameterize contexts through one shared data-source attribute instead of duplicating fixtures.
- Keep pure logic and captured webhook payload tests independent from live credentials when production constructors allow it.

This consensus still permits both fine unit tests and live connector tests. Existing repositories do not enforce one universal depth; granularity follows tested surface and determinism.
