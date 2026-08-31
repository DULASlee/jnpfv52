# Iron Laws Library — 10 Iron Laws (v2.0)
# Per design spec §4 + master-spec-v2.md §4

from enum import Enum


class IronLaw(Enum):
    IRON_TABLE_01 = "IRON-TABLE-01"  # No Change ≠ No Action
    IRON_TABLE_02 = "IRON-TABLE-02"  # Mapping Is Not Migration
    IRON_TABLE_03 = "IRON-TABLE-03"  # Every Table Needs Target Contract
    IRON_TABLE_04 = "IRON-TABLE-04"  # Security Boundary First
    IRON_TABLE_05 = "IRON-TABLE-05"  # Performance Claim Requires Measurement
    IRON_TABLE_06 = "IRON-TABLE-06"  # Migration First-Class
    IRON_TABLE_07 = "IRON-TABLE-07"  # Runtime Compatibility First
    IRON_TABLE_08 = "IRON-TABLE-08"  # Dynamic Platform Exception
    IRON_TABLE_09 = "IRON-TABLE-09"  # Evidence Over Declaration
    IRON_TABLE_10 = "IRON-TABLE-10"  # Batch Completion Requires Representative Proof


# Human-readable descriptions
IRON_LAW_DESCRIPTIONS = {
    IronLaw.IRON_TABLE_01: "NO-CHANGE must prove 8-dimension compliance",
    IronLaw.IRON_TABLE_02: "Mapping (column alias) ≠ real migration",
    IronLaw.IRON_TABLE_03: "Every table needs Target Schema Contract (8 dims)",
    IronLaw.IRON_TABLE_04: "Identity/Tenant/Permission audit first",
    IronLaw.IRON_TABLE_05: "Performance claim requires Before/After measurement",
    IronLaw.IRON_TABLE_06: "Migration First-Class: 4-file bundle mandatory",
    IronLaw.IRON_TABLE_07: "7-layer runtime chain must be verified",
    IronLaw.IRON_TABLE_08: "Low-code dynamic tables skipped",
    IronLaw.IRON_TABLE_09: "Completion claims bind to evidence files",
    IronLaw.IRON_TABLE_10: "Batch needs 1 complex + 1 normal + 1 dynamic",
}


def check_iron_law_compliance(
    iron_law: IronLaw,
    context: dict,
) -> tuple[bool, str]:
    """
    Check if a specific Iron Law is satisfied in the given context.

    Returns:
        (passed: bool, reason: str)
    """
    if iron_law == IronLaw.IRON_TABLE_01:
        # No Change must have 8-dim evidence
        if context.get("decision") == "NO-CHANGE":
            evidence = context.get("eight_dimension_evidence", {})
            if not evidence or len(evidence) < 8:
                return False, "NO-CHANGE missing 8-dimension evidence"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_02:
        # No mapping bypass
        if context.get("uses_mapping_bypass"):
            return False, "Mapping Bypass detected (e.g., 'F_X AS F_Y')"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_03:
        # Target Contract must have 8 dimensions
        contract = context.get("target_contract", {})
        required_dims = ["column_naming", "data_type", "nullable_contract",
                        "tenant_model", "audit_model", "index_contract",
                        "constraint_contract", "security_boundary"]
        missing = [d for d in required_dims if d not in contract]
        if missing:
            return False, f"Target Contract missing dimensions: {missing}"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_04:
        # P0-Security table must have security audit
        if context.get("is_p0_security"):
            audit = context.get("security_boundary_audit", {})
            if not audit:
                return False, "P0-Security table missing security_boundary_audit"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_05:
        # Performance claim requires Before/After
        if context.get("performance_claimed"):
            if not (context.get("before_measurement") and context.get("after_measurement")):
                return False, "Performance claim lacks Before/After measurement"
            # Verify reduction >= 50%
            before = context["before_measurement"]
            after = context["after_measurement"]
            if "logical_reads" in before and "logical_reads" in after:
                reduction = (before["logical_reads"] - after["logical_reads"]) / max(before["logical_reads"], 1)
                if reduction < 0.5:
                    return False, f"Performance improvement < 50% ({reduction:.1%})"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_06:
        # Migration must have 4-file bundle
        if context.get("migration_planned"):
            files = context.get("migration_files", {})
            required = ["forward", "rollback", "verify", "evidence"]
            missing = [k for k in required if k not in files or not files[k]]
            if missing:
                return False, f"Migration bundle missing: {missing}"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_07:
        # 7-layer runtime check must be done
        if context.get("migration_executed"):
            layers = context.get("runtime_layers", {})
            required = ["db", "orm", "repository", "dynamic_sql", "form", "workflow", "permission"]
            failed = [l for l in required if layers.get(l) == "FAIL"]
            if failed:
                return False, f"Runtime layer check FAILED: {failed}"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_08:
        # Type C tables must be skipped (no auto-migration)
        table_type = context.get("table_type")
        if table_type in ("DYNAMIC_FORM", "USER_EXTENDED", "OUT_OF_SCOPE"):
            if context.get("migration_planned"):
                return False, f"Type C table {context.get('table_name')} cannot auto-migrate"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_09:
        # Claims must bind to evidence
        claims = context.get("claims", [])
        for claim in claims:
            if not claim.get("evidence_file"):
                return False, f"Claim '{claim.get('item')}' lacks evidence_file"
        return True, "OK"

    elif iron_law == IronLaw.IRON_TABLE_10:
        # Batch needs representative proof
        batch = context.get("batch", {})
        rep = batch.get("representative_coverage", {})
        required = ["complex_table", "normal_table", "dynamic_table"]
        missing = [k for k in required if k not in rep]
        if missing:
            return False, f"Batch missing representative coverage: {missing}"
        return True, "OK"

    return True, "Unknown Iron Law"


def check_all_iron_laws(context: dict) -> dict:
    """
    Check ALL 10 Iron Laws against the context.

    Returns:
        Dict of {iron_law: (passed, reason)}
    """
    results = {}
    for law in IronLaw:
        passed, reason = check_iron_law_compliance(law, context)
        results[law] = {"passed": passed, "reason": reason}
    return results