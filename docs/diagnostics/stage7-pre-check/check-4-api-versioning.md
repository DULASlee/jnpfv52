# Check 4: DynamicApiController Routing Mechanism

## 4.1 DynamicApiController Configuration

- **Registration:** `services.AddDynamicApiControllers()` in `AddInject()` pipeline
  - Entry: `framework/JNPF/App/Extensions/AppServiceCollectionExtensions.cs:43`
  - Implementation: `framework/JNPF/DynamicApiController/Extensions/DynamicApiControllerServiceCollectionExtensions.cs`
  - Registers: `DynamicApiControllerFeatureProvider`, `DynamicApiControllerApplicationModelConvention`, `MvcActionDescriptorChangeProvider`

- **Default route template:** `api/{module}/[controller]/{action}`
  - `DefaultRoutePrefix` = "api" (configurable, see `DynamicApiControllerSettingsOptions.cs:111`)
  - Controllers implement `IDynamicApiController` marker interface
  - Methods auto-discovered and registered based on naming conventions (Get/Post/Put/Delete prefixes)
  - **Version infrastructure already exists:** `VersionSeparator` = "v", `VersionInFront` = true, `ApiDescriptionSettings.Version` property supported

- **Swagger integration:** Via `SpecificationDocumentBuilder`
  - Groups auto-discovered from `[ApiDescriptionSettings]` attributes
  - `TagActionsBy` grouping; `DocInclusionPredicate` checks group membership
  - Caching via `SwaggerServiceExtensions.AddCachingSwaggerProvider()`

- **Key framework files:**
  - `DynamicApiControllerApplicationModelConvention.cs` (925 lines)
  - `DynamicApiControllerSettingsOptions.cs`
  - `ApiDescriptionSettingsAttribute.cs`
  - `SpecificationDocumentBuilder.cs`

## 4.2 Route Template Analysis

- **Uniform format?** Not uniform. Two patterns coexist:
  1. **Explicit `[Route]`:** Most services use `[Route("api/{area}/[controller]")]` — e.g., `[Route("api/system/[controller]")]`, `[Route("api/permission/[controller]")]`
  2. **No `[Route]`:** Some rely entirely on DynamicApiController auto-routing (TechnicalLogService, LogHealthCheckService)

- **Example routes:**
  - `api/system/System/{id}` (GET)
  - `api/permission/Users/Selector` (GET)
  - `api/permission/Users/ImUser/Selector/{organizeId}` (POST)
  - `api/extend/Order/getList`
  - `api/message/SendMessageConfig/{action}`

- **115 files** contain at least one `[HttpGet]`/`[HttpPost]`/`[HttpPut]`/`[HttpDelete]` attribute.

- **Existing version segments:** **No.** No routes use `/v1/` or `/v2/` version segments. The version infrastructure exists in the framework but is not actively used by any current service class.

- **Custom route attributes:** `[ApiDescriptionSettings]` on ~80+ service classes (Tag, Name, Order). No `[Area("")]` attributes. No `[ApiController]` (DynamicApiController replaces it).

## 4.3 Swagger Configuration

- **Grouping:** By `Tag` from `[ApiDescriptionSettings]` attribute. Groups auto-discovered from DefaultGroupName config.
- **Security:** JWT Bearer via `framework/JNPF.Extras.Authentication.JwtBearer/`. Swagger security definitions via `SpecificationDocumentSettingsOptions.SecurityDefinitions`. `ConfigureSecurities` adds them when `EnableAuthorized=true`.
- **Custom schema IDs:** Yes. `DefaultSchemaIdSelector` supports generic type resolution and `[SchemaId]` attribute overrides.

### Impact on Stage 7.4 (API Versioning)

- **Can DynamicApiController routes include version segments?** Yes. Framework natively supports it:
  - `VersionSeparator` defaults to "v", `VersionInFront` defaults to true
  - `[ApiDescriptionSettings(Version="1")]` inserts version segments
  - `ResolveNameVersion` supports convention-based versioning (e.g., `GetUserV1` auto-extracts "1")
  - However, this only affects the **action name** portion, not controller route prefix

- **Does route template need modification?** Possibly:
  - URL path versioning: convention's `DefaultRoutePrefix` + version support can inject version between prefix and controller
  - Custom `[ApiDescriptionSettings(Module="v1")]` could prepend version module to routes

- **Can Swagger groups integrate with API versions?** Yes. `SpecificationDocumentBuilder` creates separate Swagger docs per group. API versions map to Swagger document groups. `GroupOpenApiInfos` supports per-group title, version, description.

- **Recommended approach:**
  1. URL path versioning: `api/v1/system/System/{id}`
  2. Leverage existing `[ApiDescriptionSettings(Version="1")]` for coarse-grained
  3. For fine-grained, use `[ApiDescriptionSettings(Module="v1")]`
  4. Configure separate Swagger docs per API version via `GroupOpenApiInfos`
  5. Default un-versioned controllers to v1 for backward compatibility

## Summary for Stage 7.4

The JNPF framework already has built-in versioning support (VersionSeparator, VersionInFront, ApiDescriptionSettings.Version) that is **not yet used** in production code. All 100+ service classes use non-versioned `api/{module}/[controller]/{action}`. Stage 7.4 should activate existing versioning and potentially extend for explicit path-based version segments (like `api/v1/` prefixes).
