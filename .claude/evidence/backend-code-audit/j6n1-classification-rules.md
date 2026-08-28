# J6/N1 Finding Classification Audit

## Classification Rules

| Class | Definition | Evidence Required |
|-------|-----------|-------------------|
| A-TenantData | Query targets tenant-specific business data | Entity has TenantId + Query lacks filter |
| B-GlobalData | Query targets platform-wide shared data | Entity definition + Business semantics |
| C-Permission | Query targets auth/identity data | Tenant boundary + Permission model |
| D-FilterCovered | ITenantFilter reliably covers this query | Config + Entity + SQL/Run-time test |
| E-CrossTenant | Explicit cross-tenant operation | Authorization + Scope + Audit |
| F-FalsePositive | Scanner false positive | Finding cause |
| G-Undetermined | Cannot determine safety | Keep as P0, investigate further |

## Entity Classification Reference

### High-Tenant Entities (Likely A)
- UserEntity, UserRelationEntity, OrganizeEntity, PositionEntity
- FlowTemplateJsonEntity, FlowTaskEntity, FlowLaunchEntity
- FormEntity, FormDataEntity
- ScreenEntity, ScreenComponentEntity
- VisualDevModelEntity

### High-Global Entities (Likely B)
- ModuleEntity, ModuleButtonEntity, ModuleColumnEntity, ModuleFormEntity
- PortalEntity, PortalManageEntity
- DictionaryTypeEntity, DictionaryDataEntity
- SystemEntity, AuthorizeEntity
- ConfigEntity, SysConfigEntity

### Permission Entities (Likely C)
- ColumnsPurviewEntity, AuthorizeEntity
- RoleEntity, PermissionGroupEntity
- OrganizeAdministratorEntity

## Audit Process

1. For each Finding:
   - Check Entity definition (has TenantId?)
   - Check Query context (service method purpose)
   - Check if ITenantFilter covers this entity
   - Classify or mark G-Undetermined

2. For B/D/E exemptions:
   - Sample run-time SQL verification
   - Test with Tenant A and Tenant B

3. Output:
   - Classification per Finding
   - Evidence per classification
   - Summary statistics