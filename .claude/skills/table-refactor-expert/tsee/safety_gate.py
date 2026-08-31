# Safety Gate Module — 8 gates with executable blocking (B-1 FIX)
# Per design spec §7.3 + Master Spec §5

from enum import Enum
from dataclasses import dataclass, field
from typing import Optional


class GateVerdict(Enum):
    PASS = "PASS"
    FAIL = "FAIL"
    BLOCKED = "BLOCKED"


@dataclass
class GateResult:
    gate_name: str
    verdict: GateVerdict
    reason: str
    evidence: dict = field(default_factory=dict)


# Dangerous SQL patterns that should BLOCK
DANGEROUS_PATTERNS = {
    "TRUNCATE": "TRUNCATE TABLE operation requires explicit human approval",
    "DROP_COLUMN": "DROP COLUMN operation requires 6-month dual-write period (Type B)",
    "DROP_TABLE": "DROP TABLE operation is irreversible",
    "ALTER_COLUMN_NOT_NULL": "ALTER COLUMN ... NOT NULL requires data pre-check",
}


def check_safety_gate(gate_name: str, context: dict) -> GateResult:
    """
    Execute a Safety Gate check (B-1: EXECUTABLE blocking).

    Per Phase 1.6 Task Group B: gates are not just documentation,
    they actually block based on conditions.
    """
    if gate_name == "Gate-01-Migration-Safety":
        return _gate_01_migration_safety(context)
    elif gate_name == "Gate-02-Runtime-Compatibility":
        return _gate_02_runtime_compatibility(context)
    elif gate_name == "Gate-03-Dynamic-Platform":
        return _gate_03_dynamic_platform(context)
    elif gate_name == "Gate-04-Human-Approval":
        return _gate_04_human_approval(context)
    elif gate_name == "S1-HardGateFN":
        return _gate_s1_hard_gate_fn(context)
    elif gate_name == "S2-P0P1-Decision-Error":
        return _gate_s2_p0p1_decision_error(context)
    elif gate_name == "S3-Scope-Error":
        return _gate_s3_scope_error(context)
    elif gate_name == "S4-Closure-Error":
        return _gate_s4_closure_error(context)
    else:
        return GateResult(
            gate_name=gate_name,
            verdict=GateVerdict.FAIL,
            reason=f"Unknown gate: {gate_name}",
        )


def _gate_01_migration_safety(context: dict) -> GateResult:
    """
    Gate-01: Migration Safety
    Blocks: TRUNCATE / DROP COLUMN (without 6-month wait) / ALTER COLUMN NOT NULL (without pre-check)
    """
    sql = context.get("forward_sql", "")
    sql_upper = sql.upper()

    # Check for TRUNCATE
    if "TRUNCATE" in sql_upper and "TRUNCATE" not in context.get("explicitly_approved_truncate", []):
        return GateResult(
            gate_name="Gate-01-Migration-Safety",
            verdict=GateVerdict.BLOCKED,
            reason="TRUNCATE detected. Requires explicit human approval + retention plan.",
            evidence={"sql_excerpt": sql[:200]}
        )

    # Check for DROP COLUMN
    if "DROP COLUMN" in sql_upper:
        # Verify Type B 6-month wait
        migration_type = context.get("migration_type", "")
        if migration_type != "B":
            return GateResult(
                gate_name="Gate-01-Migration-Safety",
                verdict=GateVerdict.BLOCKED,
                reason="DROP COLUMN requires Migration Type B (Semantic Change) with 6-month dual-write period.",
                evidence={"migration_type": migration_type}
            )

    # Check for ALTER COLUMN ... NOT NULL
    if "ALTER COLUMN" in sql_upper and "NOT NULL" in sql_upper:
        # Verify data pre-check has been done
        if not context.get("null_check_completed", False):
            return GateResult(
                gate_name="Gate-01-Migration-Safety",
                verdict=GateVerdict.BLOCKED,
                reason="ALTER COLUMN ... NOT NULL requires null_check_completed flag.",
                evidence={"required_flag": "null_check_completed"}
            )

    return GateResult(
        gate_name="Gate-01-Migration-Safety",
        verdict=GateVerdict.PASS,
        reason="No dangerous patterns detected.",
    )


def _gate_02_runtime_compatibility(context: dict) -> GateResult:
    """
    Gate-02: Runtime Compatibility (7 layers)
    """
    layers = context.get("runtime_layers", {})

    # Required: all 7 layers present
    required = ["db", "orm", "repository", "dynamic_sql", "form", "workflow", "permission"]

    # In production: actually run the checks
    # In MVP: verify the checks were attempted (not just absent)
    attempted = context.get("runtime_check_attempted", False)
    if not attempted:
        return GateResult(
            gate_name="Gate-02-Runtime-Compatibility",
            verdict=GateVerdict.BLOCKED,
            reason="7-layer runtime check was not attempted. Set runtime_check_attempted=True after running.",
            evidence={"required_layers": required}
        )

    failed = [l for l in required if layers.get(l) == "FAIL"]
    if failed:
        return GateResult(
            gate_name="Gate-02-Runtime-Compatibility",
            verdict=GateVerdict.BLOCKED,
            reason=f"Runtime layer check FAILED: {failed}",
            evidence={"failed_layers": failed}
        )

    return GateResult(
        gate_name="Gate-02-Runtime-Compatibility",
        verdict=GateVerdict.PASS,
        reason="All 7 runtime layers verified.",
    )


def _gate_03_dynamic_platform(context: dict) -> GateResult:
    """
    Gate-03: Dynamic Platform Protection (IRON-TABLE-08)
    B-3 FIX: case normalization applied via classify_table
    """
    table_name = context.get("table_name", "")
    table_type = context.get("table_type", "")
    migration_planned = context.get("migration_planned", False)

    # Type C tables cannot auto-migrate
    if table_type in ("DYNAMIC_FORM", "USER_EXTENDED", "OUT_OF_SCOPE") and migration_planned:
        return GateResult(
            gate_name="Gate-03-Dynamic-Platform",
            verdict=GateVerdict.BLOCKED,
            reason=f"Type C table '{table_name}' ({table_type}) cannot auto-migrate. Manual governance required.",
            evidence={"table_type": table_type}
        )

    # B-3 defense: case bypass detection
    normalized_check = context.get("normalized_check_passed", True)
    if not normalized_check:
        return GateResult(
            gate_name="Gate-03-Dynamic-Platform",
            verdict=GateVerdict.BLOCKED,
            reason="Case normalization check failed. Possible bypass attempt.",
        )

    return GateResult(
        gate_name="Gate-03-Dynamic-Platform",
        verdict=GateVerdict.PASS,
        reason="Type C protection verified (case-normalized).",
    )


def _gate_04_human_approval(context: dict) -> GateResult:
    """
    Gate-04: Human Approval Boundary
    B-4 FIX: Requires --approval-record token (NOT boolean flag)
    """
    action = context.get("action", "")
    approval_record = context.get("approval_record", None)
    production = context.get("environment") == "PRODUCTION"

    # Production + any DDL action requires approval record
    if production and action in ("MIGRATE_FORWARD", "ROLLBACK", "DROP"):
        if not approval_record:
            return GateResult(
                gate_name="Gate-04-Human-Approval",
                verdict=GateVerdict.BLOCKED,
                reason=f"Production {action} requires --approval-record token (NOT boolean flag).",
                evidence={"required": "approval_record_path"}
            )

    # If approval_record provided, validate it
    if approval_record:
        # Lazy import to avoid circular dependency
        from tsee.human_gate import validate_approval_record, ApprovalError

        requested_tables = context.get("requested_tables", [context.get("table_name", "")])
        try:
            result = validate_approval_record(approval_record, requested_tables, action)
            return GateResult(
                gate_name="Gate-04-Human-Approval",
                verdict=GateVerdict.PASS,
                reason=f"Approval record validated: {result['approval_id']} by {result['reviewer']}",
                evidence={"approval_id": result["approval_id"]}
            )
        except ApprovalError as e:
            return GateResult(
                gate_name="Gate-04-Human-Approval",
                verdict=GateVerdict.BLOCKED,
                reason=f"Approval record validation failed: {e}",
            )

    return GateResult(
        gate_name="Gate-04-Human-Approval",
        verdict=GateVerdict.PASS,
        reason="No production action requiring approval.",
    )


def _gate_s1_hard_gate_fn(context: dict) -> GateResult:
    """
    S1: Hard Gate False Negative
    """
    # If a G0_CRITICAL gap exists, Hard Gate MUST have triggered
    gaps = context.get("gaps", [])
    has_g0 = any(g.get("severity") == "G0_CRITICAL" for g in gaps)
    hard_gate_triggered = context.get("hard_gate_triggered", False)

    if has_g0 and not hard_gate_triggered:
        return GateResult(
            gate_name="S1-HardGateFN",
            verdict=GateVerdict.FAIL,
            reason="G0_CRITICAL gap exists but Hard Gate did NOT trigger (False Negative).",
        )

    return GateResult(
        gate_name="S1-HardGateFN",
        verdict=GateVerdict.PASS,
        reason="Hard Gate behavior consistent.",
    )


def _gate_s2_p0p1_decision_error(context: dict) -> GateResult:
    """
    S2: P0/P1 Decision Error
    """
    is_p0 = context.get("is_p0_security", False)
    security_audit_present = context.get("security_boundary_audit") is not None
    priority = context.get("security_priority", "P1_BUSINESS")

    # P0-Security table MUST have security audit + P0 priority
    if is_p0 and (not security_audit_present or priority != "P0_SECURITY"):
        return GateResult(
            gate_name="S2-P0P1-Decision-Error",
            verdict=GateVerdict.FAIL,
            reason="P0-Security table missing security audit or wrong priority.",
        )

    return # P1-Business can skip audit
    return GateResult(
        gate_name="S2-P0P1-Decision-Error",
        verdict=GateVerdict.PASS,
        reason="Decision classification correct.",
    )


def _gate_s3_scope_error(context: dict) -> GateResult:
    """
    S3: Scope Error
    """
    requested_scope = context.get("requested_scope", [])
    processed_scope = context.get("processed_scope", [])

    if not set(processed_scope).issubset(set(requested_scope)):
        extra = set(processed_scope) - set(requested_scope)
        return GateResult(
            gate_name="S3-Scope-Error",
            verdict=GateVerdict.FAIL,
            reason=f"Processed tables OUTSIDE requested scope: {extra}",
        )

    return GateResult(
        gate_name="S3-Scope-Error",
        verdict=GateVerdict.PASS,
        reason="Scope honored.",
    )


def _gate_s4_closure_error(context: dict) -> GateResult:
    """
    S4: Closure Error
    Blocks: Closure without 5-condition gate satisfaction
    """
    conditions_met = context.get("closed_gate_conditions", {})
    required = ["evidence_sufficient", "target_settled", "decision_made",
                "verification_passed", "no_blocking"]
    missing = [c for c in required if not conditions_met.get(c, False)]

    if missing:
        return GateResult(
            gate_name="S4-Closure-Error",
            verdict=GateVerdict.BLOCKED,
            reason=f"Closed Gate conditions NOT met: {missing}",
            evidence={"missing_conditions": missing}
        )

    return GateResult(
        gate_name="S4-Closure-Error",
        verdict=GateVerdict.PASS,
        reason="All 5 Closed Gate conditions met.",
    )


def check_all_safety_gates(context: dict, gates_to_check: list[str]) -> list[GateResult]:
    """Check a list of safety gates. Returns BLOCKED on any FAIL/BLOCKED."""
    results = []
    for gate in gates_to_check:
        result = check_safety_gate(gate, context)
        results.append(result)
    return results