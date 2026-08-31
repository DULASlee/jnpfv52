"""Batch 29 Schema Gap Analysis (Group B)
Per Group B of Batch 29 Execution Package.
Analyzes evidence against JNPF Project Contract.
Outputs: batch-29-gap-analysis.json
"""
import json
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')


# JNPF Audit field requirements (per JNPF Project Extension)
REQUIRED_AUDIT_FIELDS = ["created_time_field", "created_by_field",
                         "modified_time_field", "modified_by_field"]
REQUIRED_INDEXES_ON = ["tenant_field", "fk_columns"]


def analyze_gap(table_evidence):
    """Produce gap analysis for a single table."""
    gaps = []

    # Gap 1: Primary Key
    if not table_evidence.get("has_primary_key"):
        gaps.append({
            "gap_type": "missing_primary_key",
            "severity": "G1_MAJOR",
            "detail": "Table has no PRIMARY KEY constraint",
            "iron_law": "schema integrity",
        })

    # Gap 2: Tenant field
    tenant = table_evidence.get("tenant_field")
    if not tenant:
        gaps.append({
            "gap_type": "missing_tenant_field",
            "severity": "G1_MAJOR",
            "detail": "Table lacks tenant isolation column (f_tenant_id)",
            "iron_law": "multi-tenant safety",
        })

    # Gap 3: Audit fields
    missing_audit = []
    for field_key in REQUIRED_AUDIT_FIELDS:
        if not table_evidence.get(field_key):
            missing_audit.append(field_key)
    if missing_audit:
        gaps.append({
            "gap_type": "missing_audit_fields",
            "severity": "G2_MINOR",
            "detail": f"Missing audit fields: {missing_audit}",
            "iron_law": "audit completeness",
        })

    # Gap 4: Soft delete (JNPF convention, but not strictly required)
    if not table_evidence.get("soft_delete_field"):
        gaps.append({
            "gap_type": "missing_soft_delete",
            "severity": "G3_OK",
            "detail": "No soft-delete column (acceptable for log/reference tables)",
            "iron_law": "JNPF convention",
        })

    # Gap 5: Index coverage analysis
    tenant = table_evidence.get("tenant_field")
    index_columns = set()
    for idx in table_evidence.get("indexes", []):
        if idx.get("columns"):
            for c in idx["columns"].split(","):
                index_columns.add(c.strip())
    if tenant and tenant not in index_columns:
        gaps.append({
            "gap_type": "missing_tenant_index",
            "severity": "G1_MAJOR",
            "detail": f"Tenant field '{tenant}' not covered by any index (potential multi-tenant query slowness)",
            "iron_law": "multi-tenant safety",
        })

    # Gap 6: Row count awareness
    row_count = table_evidence.get("row_count", 0)
    if row_count == 0:
        gaps.append({
            "gap_type": "empty_table",
            "severity": "G3_OK",
            "detail": "Table is empty (no production data) - low priority for optimization",
            "iron_law": "JNPF convention",
        })

    return gaps


def classify_no_change(table_evidence, gaps):
    """Determine decision for Batch 29 (Baseline Confirmation).

    Batch 29 is explicitly baseline-only per Chief Architect directive:
    - Establishes that these 15 tables have been professionally analyzed
    - NO production DDL will be executed in this batch
    - Gaps are recorded as observations for future batches (Batch 30+)

    All decisions are NO_CHANGE in this batch.
    """
    row_count = table_evidence.get("row_count", 0)
    has_pk = table_evidence.get("has_primary_key", False)
    fk_count = table_evidence.get("fk_count", 0)
    tenant_field = table_evidence.get("tenant_field")
    index_count = table_evidence.get("index_count", 0)
    g0_gaps = [g for g in gaps if g["severity"] == "G0_CRITICAL"]
    g1_gaps = [g for g in gaps if g["severity"] == "G1_MAJOR"]
    g2_gaps = [g for g in gaps if g["severity"] == "G2_MINOR"]

    rationale = []
    rationale.append("Batch 29 is Baseline Confirmation batch (NO production DDL per directive)")

    if row_count < 100:
        rationale.append(f"Row count = {row_count} (< 100) - index optimization not justified")
    if has_pk:
        rationale.append("Primary Key exists - data integrity baseline met")
    if not has_pk:
        rationale.append(f"Primary Key MISSING - deferred to Batch 30+ (NOT fixed in this batch)")
    if tenant_field:
        rationale.append(f"Tenant field '{tenant_field}' present - multi-tenant baseline met")
    if index_count == 0 and row_count < 100:
        rationale.append("0 non-PK indexes (acceptable for empty/tiny tables)")
    if fk_count == 0:
        rationale.append("0 foreign keys (isolated tables - no referential integrity gaps)")
    if g0_gaps:
        rationale.append(f"⚠ {len(g0_gaps)} G0_CRITICAL gap(s) detected - escalated to Batch 30+")
    if g1_gaps:
        rationale.append(f"⚠ {len(g1_gaps)} G1_MAJOR gap(s) detected - deferred to Batch 30+")
    if g2_gaps:
        rationale.append(f"ℹ {len(g2_gaps)} G2_MINOR gap(s) recorded as observations")

    return {
        "decision": "NO_CHANGE",
        "verdict": "BASELINE_CONFIRMED" if not g0_gaps else "BASELINE_WITH_G0_ESCALATION",
        "rationale": rationale,
        "gap_summary": {
            "G0_CRITICAL": len(g0_gaps),
            "G1_MAJOR": len(g1_gaps),
            "G2_MINOR": len(g2_gaps),
            "G3_OK": len([g for g in gaps if g["severity"] == "G3_OK"]),
        },
    }


def analyze_batch29():
    evidence_file = Path("batch-29-evidence.json")
    evidence = json.loads(evidence_file.read_text(encoding="utf-8"))

    analysis = {
        "metadata": {
            "batch": "Batch 29",
            "purpose": "Schema Gap Analysis (Baseline Confirmation)",
            "analyzed_at": evidence["metadata"]["collected_at"],
            "analyzer": "tsee.batch29.gap_analysis (v0.1)",
            "total_tables": len(evidence["tables"]),
        },
        "tables": [],
    }

    for table in evidence["tables"]:
        if "error" in table:
            analysis["tables"].append({
                "table_name": table["table_name"],
                "error": table["error"],
                "gaps": [],
                "decision": "ERROR",
            })
            continue

        gaps = analyze_gap(table)
        decision = classify_no_change(table, gaps)

        analysis["tables"].append({
            "table_name": table["table_name"],
            "row_count": table.get("row_count", 0),
            "column_count": table.get("column_count", 0),
            "index_count": table.get("index_count", 0),
            "has_pk": table.get("has_primary_key"),
            "fk_count": table.get("fk_count", 0),
            "tenant_field": table.get("tenant_field"),
            "soft_delete_field": table.get("soft_delete_field"),
            "gaps": gaps,
            "decision": decision,
        })

    return analysis


if __name__ == "__main__":
    analysis = analyze_batch29()

    output_path = Path("batch-29-gap-analysis.json")
    output_path.write_text(
        json.dumps(analysis, indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Gap analysis for {len(analysis['tables'])} tables")
    print(f"[OK] Output: {output_path}")

    # Summary
    no_change = sum(1 for t in analysis["tables"] if t.get("decision", {}).get("decision") == "NO_CHANGE")
    baseline_confirmed = sum(1 for t in analysis["tables"] if t.get("decision", {}).get("verdict") == "BASELINE_CONFIRMED")
    baseline_escalation = sum(1 for t in analysis["tables"] if t.get("decision", {}).get("verdict") == "BASELINE_WITH_G0_ESCALATION")
    errors = sum(1 for t in analysis["tables"] if t.get("decision") == "ERROR")
    total_gaps = sum(len(t.get("gaps", [])) for t in analysis["tables"])

    print(f"[OK] NO_CHANGE (Batch 29 baseline): {no_change}")
    print(f"[OK]   BASELINE_CONFIRMED: {baseline_confirmed}")
    print(f"[OK]   BASELINE_WITH_G0_ESCALATION: {baseline_escalation}")
    print(f"[OK] Errors: {errors}")
    print(f"[OK] Total gaps recorded: {total_gaps}")
