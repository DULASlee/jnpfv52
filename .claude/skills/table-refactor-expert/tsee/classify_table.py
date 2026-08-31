# Table Classification (per IRON-TABLE-08)
# B-3 FIX: case normalization BEFORE prefix check

from enum import Enum


class TableType(Enum):
    SYSTEM_CORE_SECURITY = "SYSTEM_CORE_SECURITY"  # base_user etc.
    BUSINESS_ENTITY = "BUSINESS_ENTITY"               # flow_*, normal ext_*
    DYNAMIC_FORM = "DYNAMIC_FORM"                    # wform_*, lowcode_*
    USER_EXTENDED = "USER_EXTENDED"                  # runtime ext_*
    LEGACY_WAREHOUSE = "LEGACY_WAREHOUSE"            # WH_*, WM_*
    OUT_OF_SCOPE = "OUT_OF_SCOPE"                    # explicit OUT_OF_SCOPE
    UNKNOWN = "UNKNOWN"


# P0-Security tables that must be audited first (IRON-TABLE-04)
P0_SECURITY_TABLES = frozenset({
    "base_user", "base_organize", "base_role", "base_authorize",
    "base_module", "base_module_button", "base_module_form", "base_module_column",
    "base_permission_group", "base_module_authorize",
})


# User-extended tables (runtime-created, forbid auto-rename)
# Per design spec §6.4 + IRON-TABLE-08
USER_EXTENDED_TABLES = frozenset({
    "ext_table_example",  # SVR-001 (Phase 8)
})


def classify_table(table_name: str) -> TableType:
    """
    Classify a table per JNPF Type classification (IRON-TABLE-08).

    B-3 FIX: case normalization MUST happen BEFORE prefix check
    to prevent bypass via mixed case (e.g., WFORM_contractapproval).

    Args:
        table_name: raw table name (any case)

    Returns:
        TableType enum value

    Examples:
        >>> classify_table("base_user")
        TableType.SYSTEM_CORE_SECURITY
        >>> classify_table("WFORM_contractapproval")  # mixed case
        TableType.DYNAMIC_FORM
        >>> classify_table("WH_Bill")
        TableType.LEGACY_WAREHOUSE
    """
    # B-3 FIX: case normalization FIRST
    if not table_name:
        return TableType.UNKNOWN
    normalized = table_name.lower().strip()

    # Explicit OUT_OF_SCOPE
    if USER_EXTENDED_TABLES and normalized in USER_EXTENDED_TABLES:
        return TableType.OUT_OF_SCOPE

    # Type C: Low-Code Dynamic
    if normalized.startswith(("wform_", "lowcode_")):
        return TableType.DYNAMIC_FORM

    # Legacy warehouse (R3+ default NO-CHANGE)
    if normalized.startswith(("wh_", "wm_")):
        return TableType.LEGACY_WAREHOUSE

    # P0-Security system core
    if normalized in P0_SECURITY_TABLES:
        return TableType.SYSTEM_CORE_SECURITY

    # System core base_* (non-P0-Security) → treat as business entity
    # to avoid over-classification
    if normalized.startswith("base_"):
        return TableType.BUSINESS_ENTITY

    # User-extended ext_* (heuristic: small table < 100 rows = user ext)
    # Note: in production, this requires row count check
    if normalized.startswith("ext_"):
        # Conservative: treat as business entity unless explicit USER_EXTENDED
        return TableType.BUSINESS_ENTITY

    # Business entities
    if normalized.startswith(("flow_", "sa_", "ai_", "kg_", "blade_", "ext_product")):
        return TableType.BUSINESS_ENTITY

    return TableType.UNKNOWN


def is_auto_migration_allowed(table_type: TableType) -> bool:
    """
    IRON-TABLE-08: Type C tables MUST NOT auto-migrate.
    """
    return table_type not in (TableType.DYNAMIC_FORM, TableType.USER_EXTENDED, TableType.OUT_OF_SCOPE)


if __name__ == "__main__":
    import sys
    if len(sys.argv) < 2:
        print("Usage: python -m tsee.classify_table <table_name>")
        sys.exit(1)
    result = classify_table(sys.argv[1])
    print(f"{sys.argv[1]} -> {result.value}")
    print(f"Auto-migration allowed: {is_auto_migration_allowed(result)}")