# JNPF Expert Traps & Survival Guide

> **⚠️ 架构级陷阱已迁移至** `.claude/rules/architecture-redlines.md`（架构铁律单一信源）。
> 本文档保留的陷阱侧重于**实际编码中的"出乎意料"行为**（非红线违反，而是直觉陷阱）。
>
> **架构红线映射：**
> - Traps 1, 6, 9 → R1 (API Generation)
> - Traps 4, 5 → R2 (Unified Response)
> - Traps 7, 8, 13 → R4 (Multi-Tenant Isolation)
> - Trap 12 → R3 (Codegen Boundary)
> - Traps 2, 3, 10, 11, 14 → 本文档保留（实操陷阱，非架构铁律）

These are real traps in the JNPF framework that violate standard .NET intuition. Each one is a "without being told, you'd never figure out why it breaks" lesson learned the hard way.

---

## Trap 1: DynamicApiController Route Binding — Renaming = Production Incident

- **What you'd do**: Rename a Service method (e.g., GetUser → GetUserDetail), thinking it's a harmless refactor
- **What actually happens**: JNPF API routes are auto-generated from `{ClassName}/{MethodName}`. Rename = URL changed = all frontend API calls return 404 = workflow node bindings break = production incident
- **Survival rule**: NEVER rename published Service classes or public methods. MUST rename? Grep all frontend api files and workflow definitions for the URL first, then update them all in sync

---

## Trap 2: Mapster Adapt() Audit Field Overwrite — One Line Corrupts All Audit Data

- **What you'd do**: `input.Adapt<Entity>()` for clean mapping, then save directly
- **What actually happens**: Adapt() copies all same-name fields from InputDto to Entity, including CreateTime, CreateUserId, and TenantId. On update operations, original record's creation time and creator are silently overwritten
- **Survival rule**: For updates, ALWAYS query the original entity first: `var entity = await db.Queryable<T>().InSingleAsync(id)`, then `input.Adapt(entity)` or use `.Ignore(dest => dest.CreateTime)` to exclude audit fields. NEVER do `db.Updateable(input.Adapt<T>())` blind full overwrite

---

## Trap 3: SqlSugar Navigation Property Serialization — N+1 Without Any Loop

- **What you'd do**: Query a list and return it directly, assuming no loop means no N+1
- **What actually happens**: SqlSugar navigation properties are lazy-loaded by default. When the JSON serializer accesses each entity's navigation properties, it triggers one DB query per property per entity. 100 records × 3 navigation properties = 300 queries
- **Survival rule**: For list queries, ALWAYS use `.Includes(o => o.User, o => o.Department)` for eager loading, or use `.Select(x => new ListOutput { ... })` to project to a DTO (DTOs have no navigation properties). NEVER return raw entity lists with navigation properties

---

## Trap 4: Oops.Bah vs Oops.Oh — Wrong Choice = User Sees "Internal Server Error"

- **What you'd do**: Throw any exception when business validation fails
- **What actually happens**: Oops.Bah("Username exists") → HTTP 200 + business error code + frontend shows the message. Oops.Oh("Username exists") → HTTP 500 + error log + frontend shows "Internal Server Error". Wrong choice = user never sees the actual error reason
- **Survival rule**: User-caused business errors (validation failure, rule conflict, insufficient permissions, data not found) → Oops.Bah. System-internal failures (DB connection failure, external service timeout) → Oops.Oh. NEVER mix them up

---

## Trap 5: RESTfulResult Double Wrapping — Data Nested Two Layers, Frontend Parsing Fails

- **What you'd do**: Manually return `new RESTfulResult<T> { code = 200, data = result }`, thinking it's more reliable
- **What actually happens**: JNPF framework automatically wraps all return values as RESTfulResult<T>. Your manual wrap causes the framework to wrap again. Frontend gets `{ code: 200, data: { code: 200, data: actual_data } }` and parses the wrong layer
- **Survival rule**: Just `return result` or `return PageResult<T>`. NEVER manually create RESTfulResult. The framework handles it automatically

---

## Trap 6: Async Suffix Breaks Routes — GetListAsync ≠ GetList

- **What you'd do**: Follow standard C# async naming convention, name it GetPageListAsync
- **What actually happens**: DynamicApiController generates routes from full method name. `GetPageListAsync` → `/api/xxx/GetPageListAsync`. Frontend is bound to `/api/xxx/GetPageList` — instant 404
- **Survival rule**: IDynamicApiController interface methods NEVER have an Async suffix. Even if the implementation uses async/await, interface method names stay `GetPageList`, `GetInfo`, `Add`, `Update`, `Delete`

---

## Trap 7: ITenantFilter Fails in Subqueries — Data Leak in Silence

- **What you'd do**: Write subqueries or Joins assuming ITenantFilter is automatically active
- **What actually happens**: ITenantFilter only auto-applies on the root Queryable. Subqueries, Joins, Unions, and Ado.SqlQuery do NOT automatically include tenant filtering. Data leak produces no errors — it just silently returns other tenants' data
- **Survival rule**: For subqueries or raw SQL, ALWAYS manually add `.Where(x => x.TenantId == tenantId)`. After writing, verify the generated SQL contains the TenantId condition

---

## Trap 8: Updateable/Deleteable Don't Auto-Filter Tenants — One Update Changes All Tenants' Data

- **What you'd do**: `db.Updateable(entity).ExecuteCommand()`, assuming it works like Queryable with automatic tenant conditions
- **What actually happens**: SqlSugar's ITenantFilter only auto-applies to Queryable. Updateable/Deleteable's WHERE clause does NOT automatically include TenantId. Without explicit specification, you may update all tenants' data
- **Survival rule**: For write operations, ALWAYS specify tenant condition explicitly: `db.Updateable(entity).Where(x => x.TenantId == tenantId).ExecuteCommand()`

---

## Trap 9: Service Class Public Methods = API Endpoints — Helper Methods Get Exposed Too

- **What you'd do**: Write a public helper method in a Service class for internal use by other methods
- **What actually happens**: DynamicApiController maps ALL public methods on the implementation class to API endpoints. Your "internal helper" becomes a publicly accessible API
- **Survival rule**: Internal helper methods MUST be declared as private or protected. NEVER expose public methods on IDynamicApiController implementation classes that aren't meant to be API endpoints

---

## Trap 10: EventBus At-Least-Once Delivery — Same Order Triggers Workflow Twice

- **What you'd do**: Publish an event assuming the handler executes exactly once
- **What actually happens**: RabbitMQ guarantees at-least-once delivery. Channel in-process mode may re-execute on retry. A customer places one order but two workflow instances are triggered
- **Survival rule**: Event handlers MUST be idempotent — query target state or check processed EventId before executing business logic. NEVER assume an event fires only once

---

## Trap 11: Mapster Circular Reference Stack Overflow — Adapt Entity with Navigation Properties Explodes

- **What you'd do**: `entity.Adapt<Dto>()` for all mapping scenarios
- **What actually happens**: JNPF entities commonly have circular references (User → Department → Users → Department...). Mapster's default config recurses infinitely until stack overflow
- **Survival rule**: When mapping complex entities with navigation properties, ALWAYS use `.Select()` to project to DTO, or configure Mapster's `MaxDepth` to limit recursion. NEVER directly Adapt entity graphs with circular references

---

## Trap 12: .vm Templates Use Velocity Syntax — Writing C# in Templates Generates Invalid Code

- **What you'd do**: Use C# syntax in .vm templates like `if (x == null)` or `foreach (var item in list)`
- **What actually happens**: Apache Velocity uses completely different syntax. C# syntax is treated as plain text in templates, producing invalid C# that fails to compile
- **Survival rule**: Before modifying .vm templates, ALWAYS first Read an existing working template to understand its structure. Velocity syntax: `#if($entity.Name)` / `#foreach($field in $fields)` / `#set($x = "value")` / `$entity.Name`

---

## Trap 13: SqlSugar Filter Priority Override — Tenant Filtering Silently Fails

- **What you'd do**: Add custom global filters with `db.QueryFilter.Add()`, assuming ITenantFilter still works normally
- **What actually happens**: SqlSugar executes multiple global filters in the order they are added. A subsequently added filter using `OR` conditions or conflicting `AND` priority with the tenant filter may cause the tenant filter to be bypassed. No errors — silently returns other tenants' data
- **Survival rule**: After adding custom global filters, ALWAYS verify with `db.Queryable<T>().ToSql()` that the final SQL includes the TenantId condition. MUST confirm tenant filtering is in the correct position in the AND chain

---

## Trap 14: SqlSugar Query Without Pagination — 10 Records Fine, 10000 Records OOM

- **What you'd do**: `db.Queryable<Entity>().ToListAsync()` to return full table to frontend. Works fine in development with little data
- **What actually happens**: In production as data grows, full table queries cause out-of-memory or timeouts. SqlSugar and Dapper have no default pagination — returns however many rows exist in the table
- **Survival rule**: List endpoints ALWAYS use `.ToPageListAsync(currentPage, pageSize)` for pagination. Default pageSize = 20. NEVER use `.ToListAsync()` for business data that may exceed 100 records. Export scenarios MUST use streaming queries or batch reads
