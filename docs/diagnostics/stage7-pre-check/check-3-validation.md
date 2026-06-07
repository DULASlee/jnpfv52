# Check 3: Existing Validation Mechanisms

## 3.1 DataAnnotation Usage Statistics

| Attribute | Usage Count | Typical Files |
|---|---|---|
| `[Required]` | 3 | `framework/JNPF/CorsAccessor/Options/CorsAccessorSettingsOptions.cs:15`, `framework/JNPF/DynamicApiController/Options/DynamicApiControllerSettingsOptions.cs:20,56` |
| `[StringLength]` | 0 | — |
| `[MaxLength]` | 0 | — |
| `[MinLength]` | 0 | — |
| `[Range]` | 0 | — |
| `[RegularExpression]` | 0 | — |
| `[EmailAddress]` | 0 | — |
| `[Phone]` | 0 | — |
| `[Compare]` | 0 | — |
| `[CustomValidation]` | 0 | — |

**Summary:** DataAnnotation usage is essentially absent from the codebase. Only 3 occurrences of `[Required]` exist, all in framework-internal option classes, not in any business DTOs. The business layer (~90+ `*Input.cs` files across modules) uses zero DataAnnotation attributes.

## 3.2 Custom Validation

| Method | File | Description |
|---|---|---|
| DataValidationAttribute (custom) | `framework/JNPF/DataValidation/Attributes/DataValidationAttribute.cs` | Custom `ValidationAttribute`. Configurable `ValidationPattern` (AllOfThem, AtLeastOne, etc.). Overrides `IsValid(object, ValidationContext)`. |
| SensitiveDetectionAttribute (custom) | `framework/JNPF/SensitiveDetection/Attributes/SensitiveDetectionAttribute.cs` | Custom `ValidationAttribute`. Checks for sensitive/prohibited words. Also supports auto-replacement. |
| NonValidationAttribute (opt-out) | `framework/JNPF/DataValidation/Attributes/NonValidationAttribute.cs` | Opt-out of automatic validation pipeline. |
| DataValidator static class | `framework/JNPF/DataValidation/Validators/DataValidator.cs` | Wraps `Validator.TryValidateObject()` and `Validator.TryValidateValue()`. |
| DataValidationFilter | `framework/JNPF/DataValidation/Filters/DataValidationFilter.cs` | Action filter auto-validating parameters with `ValidationAttribute`. |
| IValidatableObject | **Not found** | No class implements `IValidatableObject`. |
| Manual ModelState.IsValid | **Not found** | No manual model-state validation. |
| FluentValidation | **Not found** | Zero references in `.csproj` or `.cs` files. |

### Impact on Stage 7.3 (FluentValidation)

- **FluentValidation does NOT already exist.** Must be introduced from scratch.
- **DataAnnotation should be retained** but can be soft-replaced. Migration cost is low (only 3 `[Required]` on framework options).
- **Validator count estimate:** ~80-100 validators needed to cover existing `*Input.cs` / `*CrInput.cs` / `*UpInput.cs` files. Recommend ~20 high-priority validators initially.

## Summary for Stage 7.3

1. DataAnnotation usage is negligible — only 3 `[Required]` on framework option classes. No business DTOs use validation attributes.
2. FluentValidation is completely absent.
3. Existing validation strategy is framework-driven: `DataValidationAttribute` (type-based) + `SensitiveDetectionAttribute` (sensitive-word). DTOs carry no per-property validation.
4. **Recommendation:** Introduce FluentValidation as new layer. Start with critical DTOs (LoginInput, UserCrInput, RoleCrInput). Retain `DataValidationAttribute` pipeline for existing behavior. Target ~20 high-priority validators initially.
