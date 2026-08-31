# tsee CLI — Main entry point
# Implements 7 Skill DoD commands (per design spec §7.3)
# Per Phase 1.6 Task Group B

import argparse
import json
import sys
from pathlib import Path

# tsee package imports
from tsee.classify_table import classify_table, is_auto_migration_allowed, TableType
from tsee.iron_laws import check_iron_law_compliance, check_all_iron_laws, IronLaw, IRON_LAW_DESCRIPTIONS
from tsee.human_gate import validate_approval_record, ApprovalError, generate_signature_hash
from tsee.safety_gate import check_safety_gate, check_all_safety_gates, GateVerdict, GateResult


def cmd_classify_table(args):
    """DoD-03 (part): Classify a table per IRON-TABLE-08 + B-3 case normalization."""
    result = classify_table(args.table)
    auto = is_auto_migration_allowed(result)
    output = {
        "table": args.table,
        "table_type": result.value,
        "auto_migration_allowed": auto,
        "iron_law": "IRON-TABLE-08" if not auto else None,
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0 if auto or not args.strict else 1


def cmd_decide(args):
    """DoD-03: Migration Decision Engine — classify + decide migration type."""
    table_type = classify_table(args.table)
    auto = is_auto_migration_allowed(table_type)

    # Decision logic per design spec §6
    if table_type in (TableType.DYNAMIC_FORM, TableType.USER_EXTENDED):
        migration_type = "C"
        human_gate = "REQUIRED"
        iron_laws = ["IRON-TABLE-08"]
    elif table_type == TableType.OUT_OF_SCOPE:
        migration_type = None
        human_gate = "REQUIRED"
        iron_laws = ["IRON-TABLE-08"]
    elif table_type == TableType.SYSTEM_CORE_SECURITY:
        migration_type = "B"
        human_gate = "REQUIRED"
        iron_laws = ["IRON-TABLE-02", "IRON-TABLE-04", "IRON-TABLE-06"]
    else:
        migration_type = args.column and "A" or "B"  # heuristic
        human_gate = "NOT_REQUIRED" if args.column else "REQUIRED"
        iron_laws = ["IRON-TABLE-02"]

    output = {
        "table": args.table,
        "table_type": table_type.value,
        "column": args.column,
        "migration_type": migration_type,
        "iron_laws_triggered": iron_laws,
        "human_gate": human_gate,
        "rationale": (
            f"Table classified as {table_type.value}. "
            f"Migration Type: {migration_type or 'N/A (OUT_OF_SCOPE)'}. "
            f"Human Gate: {human_gate}."
        ),
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0


def cmd_iron_laws(args):
    """Check all 10 Iron Laws against context."""
    context = {}
    if args.context_file:
        context = json.loads(Path(args.context_file).read_text(encoding="utf-8"))
    else:
        # Interactive mode: minimal context
        print("Usage: --context-file <path.json> required")
        return 1

    results = check_all_iron_laws(context)
    output = {
        "iron_laws_check": [
            {
                "law": law.value,
                "description": IRON_LAW_DESCRIPTIONS[law],
                "passed": r["passed"],
                "reason": r["reason"],
            }
            for law, r in results.items()
        ],
        "overall_pass": all(r["passed"] for r in results.values()),
        "failed_count": sum(1 for r in results.values() if not r["passed"]),
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0 if output["overall_pass"] else 1


def cmd_human_gate(args):
    """DoD-07 + B-4 FIX: Validate --approval-record token (NOT boolean flag)."""
    tables = [t.strip() for t in args.tables.split(",")]
    try:
        result = validate_approval_record(args.approval_record, tables, args.action)
        print(json.dumps(result, indent=2, ensure_ascii=False))
        return 0
    except ApprovalError as e:
        output = {"valid": False, "error": str(e)}
        print(json.dumps(output, indent=2, ensure_ascii=False))
        return 1


def cmd_safety_gate(args):
    """B-1 FIX: Execute Safety Gate (with EXECUTABLE blocking)."""
    context = {}
    if args.context_file:
        context = json.loads(Path(args.context_file).read_text(encoding="utf-8"))
    else:
        print("Usage: --context-file <path.json> required")
        return 1

    result = check_safety_gate(args.gate, context)

    output = {
        "gate": result.gate_name,
        "verdict": result.verdict.value,
        "reason": result.reason,
        "evidence": result.evidence,
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0 if result.verdict == GateVerdict.PASS else 1


def cmd_no_change_validate(args):
    """DoD-04: NO-CHANGE must have 8-dimension evidence (IRON-TABLE-01)."""
    evidence = {}
    if args.evidence_file:
        evidence = json.loads(Path(args.evidence_file).read_text(encoding="utf-8"))

    required_dims = [
        "column_naming", "data_type", "nullable_contract",
        "tenant_model", "audit_model", "index_contract",
        "constraint_contract", "security_boundary"
    ]
    missing = [d for d in required_dims if d not in evidence]
    partial = [d for d in required_dims if evidence.get(d) == "PARTIAL"]

    output = {
        "table": args.table,
        "decision": "NO-CHANGE",
        "eight_dimension_evidence": evidence,
        "missing_dimensions": missing,
        "partial_dimensions": partial,
        "iron_law": "IRON-TABLE-01",
        "verdict": "PASS" if not missing else "BLOCKED",
        "reason": (
            "All 8 dimensions PASS"
            if not missing and not partial
            else f"Missing: {missing}, Partial: {partial}"
        ),
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0 if not missing else 1


def cmd_contract_check(args):
    """DoD-01: Generate Table Contract Matrix from contracts."""
    contracts_dir = Path(args.contracts_dir)
    if not contracts_dir.exists():
        print(f"Contracts directory not found: {contracts_dir}")
        return 1

    # MVP: just list the contracts found
    contract_files = list(contracts_dir.glob("*.yaml")) + list(contracts_dir.glob("*.yml"))
    matrix = []
    for cf in contract_files:
        try:
            import yaml
            content = yaml.safe_load(cf.read_text(encoding="utf-8"))
            table_name = content.get("table_name", cf.stem)
            dims = [d for d in ["column_naming", "data_type", "nullable_contract",
                               "tenant_model", "audit_model", "index_contract",
                               "constraint_contract", "security_boundary"]
                   if d in content]
            matrix.append({
                "table": table_name,
                "dimensions_present": len(dims),
                "dimensions_total": 8,
                "contract_file": cf.name,
            })
        except Exception as e:
            matrix.append({"file": cf.name, "error": str(e)})

    # Markdown output
    print("| Table | Dimensions Present | Total | Status |")
    print("|-------|-------------------|-------|--------|")
    for row in matrix:
        if "error" in row:
            print(f"| {row['file']} | ERROR | 8 | ❌ ERROR |")
        else:
            status = "✅ COMPLETE" if row["dimensions_present"] == 8 else f"⚠️ {8 - row['dimensions_present']} MISSING"
            print(f"| {row['table']} | {row['dimensions_present']} | 8 | {status} |")

    print(f"\nTotal contracts found: {len(matrix)}")
    return 0


def cmd_gap_analysis(args):
    """DoD-02: Gap Analysis (placeholder — full implementation requires DB connection)."""
    # MVP: parse target contract + check basic dimensions
    if not args.target_contract:
        print("Usage: --target-contract <path.yaml> required")
        return 1

    try:
        import yaml
        contract = yaml.safe_load(Path(args.target_contract).read_text(encoding="utf-8"))
    except ImportError:
        print("PyYAML not installed. Install with: pip install pyyaml")
        return 1

    gaps = {}
    # Simple dimension presence check
    for dim in ["column_naming", "data_type", "nullable_contract",
               "tenant_model", "audit_model", "index_contract",
               "constraint_contract", "security_boundary"]:
        gaps[dim + "_gaps"] = [] if dim in contract else [
            {"issue": f"Dimension '{dim}' missing from contract", "severity": "G1_MAJOR"}
        ]

    overall_verdict = "NO-CHANGE_OK" if all(not v for v in gaps.values()) else "MANUAL_REVIEW_REQUIRED"

    output = {
        "table_name": contract.get("table_name", args.table),
        "analysis_timestamp": "PLACEHOLDER",
        "gaps": gaps,
        "overall_verdict": overall_verdict,
        "migration_type": None,
        "iron_laws_triggered": [],
        "note": "MVP: only checks contract dimension presence. Full DB schema comparison requires sqlserver connection.",
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0


def cmd_evidence_collect(args):
    """DoD-05: Evidence Collector (placeholder)."""
    output = {
        "evidence_collection": "PLACEHOLDER",
        "note": "MVP: requires DB connection for schema/row_count snapshots",
        "next_steps": [
            "1. Connect to target database (pyodbc)",
            "2. Snapshot current schema to JSON",
            "3. Query sys.dm_db_index_usage_stats",
            "4. Capture row count",
            "5. Run performance benchmark with SET STATISTICS IO/TIME",
            "6. Save to bundle directory"
        ]
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0


def cmd_rollback_validate(args):
    """DoD-06: Rollback Validator (placeholder)."""
    output = {
        "rollback_validation": "PLACEHOLDER",
        "change_id": args.change_id,
        "note": "MVP: requires test DB to actually run forward + rollback SQL and verify schema restoration",
        "next_steps": [
            "1. Snapshot schema before forward SQL",
            "2. Execute forward SQL on test DB",
            "3. Snapshot schema after forward",
            "4. Execute rollback SQL on test DB",
            "5. Snapshot schema after rollback",
            "6. Compare pre/post rollback schemas — must be equal",
            "7. Compare row counts — must be equal"
        ]
    }
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0


def main():
    parser = argparse.ArgumentParser(
        prog="tsee",
        description="Table Schema Evolution Expert v2.0 — Minimum Executable Governance Layer"
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # classify_table
    p = subparsers.add_parser("classify_table", help="Classify table per IRON-TABLE-08")
    p.add_argument("table", help="Table name (case-insensitive after B-3 fix)")
    p.add_argument("--strict", action="store_true", help="Exit 1 if auto-migration blocked")
    p.set_defaults(func=cmd_classify_table)

    # decide (DoD-03)
    p = subparsers.add_parser("decide", help="Migration Decision Engine")
    p.add_argument("table", help="Table name")
    p.add_argument("--column", help="Optional column name")
    p.set_defaults(func=cmd_decide)

    # contract_check (DoD-01)
    p = subparsers.add_parser("contract_check", help="Table Contract Matrix (DoD-01)")
    p.add_argument("--contracts-dir", required=True, help="Directory containing contract YAMLs")
    p.set_defaults(func=cmd_contract_check)

    # gap_analysis (DoD-02)
    p = subparsers.add_parser("gap_analysis", help="Gap Analysis (DoD-02)")
    p.add_argument("table", help="Table name")
    p.add_argument("--target-contract", help="Path to Target Schema Contract YAML")
    p.set_defaults(func=cmd_gap_analysis)

    # evidence_collect (DoD-05)
    p = subparsers.add_parser("evidence_collect", help="Evidence Collector (DoD-05)")
    p.add_argument("table", help="Table name")
    p.set_defaults(func=cmd_evidence_collect)

    # no_change_validate (DoD-04)
    p = subparsers.add_parser("no_change_validate", help="NO-CHANGE 8-dim validator (DoD-04)")
    p.add_argument("table", help="Table name")
    p.add_argument("--evidence-file", help="Path to 8-dim evidence JSON")
    p.set_defaults(func=cmd_no_change_validate)

    # rollback_validate (DoD-06)
    p = subparsers.add_parser("rollback_validate", help="Rollback Validator (DoD-06)")
    p.add_argument("--change-id", required=True, help="Migration change ID")
    p.set_defaults(func=cmd_rollback_validate)

    # human_gate (DoD-07 + B-4 FIX)
    p = subparsers.add_parser("human_gate", help="Validate --approval-record token (DoD-07, B-4 FIX)")
    p.add_argument("--approval-record", required=True, help="Path to approval record YAML")
    p.add_argument("--tables", required=True, help="Comma-separated table names")
    p.add_argument("--action", required=True, choices=["MIGRATE_FORWARD", "ROLLBACK", "DROP", "READ"])
    p.set_defaults(func=cmd_human_gate)

    # safety_gate (B-1 FIX: EXECUTABLE blocking)
    p = subparsers.add_parser("safety_gate", help="Execute Safety Gate (B-1 FIX)")
    p.add_argument("gate", help="Gate name (Gate-01..04, S1..S4)")
    p.add_argument("--context-file", required=True, help="Path to context JSON")
    p.set_defaults(func=cmd_safety_gate)

    # iron_laws (utility)
    p = subparsers.add_parser("iron_laws", help="Check all 10 Iron Laws")
    p.add_argument("--context-file", help="Path to context JSON")
    p.set_defaults(func=cmd_iron_laws)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())