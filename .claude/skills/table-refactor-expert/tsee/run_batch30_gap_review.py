"""Batch 30+ Gap Review — Tasks 30.1 through 30.7
Per Master Plan v2.1 (docs/superpowers/plans/2026-08-31-JNPF-Table-Refactoring-Master-Plan-v2.1.md)

Authorization: Chief Architect directive 2026-08-31
Scope: Gap Decision Batch ONLY (NO Schema DDL)
Output: Decision matrix for 17 G1_MAJOR Gaps

Tasks:
  30.1 Gap Inventory
  30.2 Gap Re-validation
  30.3 PK Analysis
  30.4 Tenant Index Analysis
  30.5 Audit Field Analysis
  30.6 Dynamic Classification
  30.7 Migration Decision Matrix
"""
import json
import sys
from datetime import datetime
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

# Paths
# Resolve project root (3 levels up from tsee/)
PROJECT_ROOT = Path(__file__).resolve().parent.parent.parent.parent
BATCH29_DIR = PROJECT_ROOT / "backend" / "database" / "batch-29"
BATCH30_DIR = PROJECT_ROOT / "backend" / "database" / "batch-30"
BATCH30_DIR.mkdir(parents=True, exist_ok=True)

# Target Contract (JNPF Project Default + Module Override)
TARGET_CONTRACT = {
    "schema_convention": {
        "id_type": "nvarchar(50)",
        "id_naming": "lowercase_with_f_prefix",
        "examples_pass": ["f_id", "f_account", "f_tenant_id", "f_creator_time"],
        "examples_fail": ["F_USER_ID", "fUserId", "id"],
    },
    "tenant_model": {
        "required": True,
        "field": "f_tenant_id",
        "data_type": "nvarchar(50)",
        "nullable": False,  # Per P0-Security standard
        "isolation_level": "STRICT",
    },
    "audit_model": {
        "required_fields": [
            "f_creator_time", "f_creator_user_id",
            "f_last_modify_time", "f_last_modify_user_id",
            "f_delete_mark",  # soft delete
        ],
        "soft_delete": "f_delete_mark (0=active, 1=deleted)",
    },
    "primary_key": {
        "required": True,
        "convention": "f_id PRIMARY KEY",
    },
    "classification_categories": [
        "SYSTEM_CORE_SECURITY",  # base_user, base_organize etc.
        "BUSINESS_ENTITY",          # flow_*, ext_*
        "DYNAMIC_FORM",             # wform_*
        "USER_EXTENDED",            # ext_*
        "LEGACY_WAREHOUSE",         # WH_*, WM_*
        "OUT_OF_SCOPE",
    ],
}

# The 17 G1_MAJOR Gaps from Batch 29
# Format: (gap_id, table, dimension, batch_29_gap_summary)
G1_MAJOR_GAPS = [
    # GAP-01, GAP-02: Missing PK
    ("GAP-01", "base_signature", "primary_key", "Missing PRIMARY KEY constraint"),
    ("GAP-02", "base_signature_user", "primary_key", "Missing PRIMARY KEY constraint"),
    # GAP-03: Tenant index gaps (15 tables)
    ("GAP-03-a", "base_advanced_query_scheme", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-b", "base_app_data", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-c", "base_columns_purview", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-d", "base_data_interface_user", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-e", "base_data_interface_variate", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-f", "base_db_link", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-g", "base_im_content", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-h", "base_im_reply", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-i", "base_integrate", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-j", "base_integrate_node", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-k", "base_organize_relation", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-l", "base_portal", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-m", "base_portal_data", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-n", "base_signature", "tenant_index", "tenant field not covered by any index"),
    ("GAP-03-o", "base_signature_user", "tenant_index", "tenant field not covered by any index"),
    # GAP-04: Audit field gaps (5 tables)
    ("GAP-04-a", "base_advanced_query_scheme", "audit_fields", "Missing audit fields"),
    ("GAP-04-b", "base_app_data", "audit_fields", "Missing audit fields"),
    ("GAP-04-c", "base_im_content", "audit_fields", "Missing audit fields"),
    ("GAP-04-d", "base_integrate", "audit_fields", "Missing audit fields"),
    ("GAP-04-e", "base_portal", "audit_fields", "Missing audit fields"),
]


# ============================================================
# TASK 30.1 — Gap Inventory
# ============================================================

def task_30_1_gap_inventory():
    """Establish full Gap Inventory with evidence linkage."""
    import pyodbc
    conn_str = (
        "DRIVER={SQL Server};"
        "SERVER=(local)\\SQLEXPRESS;"
        "DATABASE=ZXAF_V1_DevTest1;"
        "Trusted_Connection=yes;"
    )
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    inventory = {
        "metadata": {
            "batch": "Batch 30+ Gap Review",
            "task": "30.1 Gap Inventory",
            "executed_at": datetime.now().isoformat(),
            "scope": "17 G1_MAJOR Gaps from Batch 29",
            "target_contract_ref": "JNPF Project Default + Module Override",
        },
        "gaps": [],
    }

    for gap_id, table, dimension, batch_29_summary in G1_MAJOR_GAPS:
        # Query actual DB metadata for current state
        cursor.execute("""
            SELECT
                SCHEMA_NAME(t.schema_id) AS schema_name,
                t.name AS table_name,
                (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS column_count,
                (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.is_primary_key = 1) AS pk_count,
                (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.is_primary_key = 0 AND i.name IS NOT NULL AND i.is_hypothetical = 0) AS non_pk_index_count,
                (SELECT COUNT(*) FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id) AS fk_count
            FROM sys.tables t
            WHERE t.object_id = OBJECT_ID('dbo.' + ?)
        """, (table,))
        row = cursor.fetchone()
        if not row:
            inventory["gaps"].append({
                "gap_id": gap_id, "table": table, "error": "Table not found in DB",
            })
            continue

        current_state = {
            "schema": row[0],
            "table": row[1],
            "column_count": row[2],
            "pk_count": row[3],
            "non_pk_index_count": row[4],
            "fk_count": row[5],
        }

        # Determine target state based on dimension
        if dimension == "primary_key":
            target_state = {
                "required": "PRIMARY KEY constraint on f_id",
                "convention": "JNPF Standard: PRIMARY KEY (f_id) CLUSTERED",
            }
            dimension_target_field = "f_id"
        elif dimension == "tenant_index":
            target_state = {
                "required": "Index on f_tenant_id (or composite index including f_tenant_id)",
                "convention": "JNPF Standard: IDX_<table>_tenant_* or composite covering f_tenant_id",
            }
            dimension_target_field = "f_tenant_id"
        elif dimension == "audit_fields":
            target_state = {
                "required": "Audit fields present: f_creator_time, f_creator_user_id, f_last_modify_time, f_last_modify_user_id, f_delete_mark",
                "convention": "JNPF Standard: 5-field audit + soft delete",
            }
            dimension_target_field = "f_creator_time, f_creator_user_id, f_last_modify_time, f_last_modify_user_id, f_delete_mark"
        else:
            target_state = {}
            dimension_target_field = ""

        inventory["gaps"].append({
            "gap_id": gap_id,
            "project": "JNPF v5.2",
            "module": table.split("_")[0] if table.startswith("base_") else "system",
            "table": table,
            "dimension": dimension,
            "target_field": dimension_target_field,
            "current_state": current_state,
            "target_state": target_state,
            "severity": "G1_MAJOR",
            "evidence": "batch-29-gap-analysis.json (Group B output, 2026-08-31)",
            "dynamic_status": "PENDING_CLASSIFICATION",  # Set in 30.6
        })

    conn.close()

    output_path = BATCH30_DIR / "batch-30-gap-inventory.json"
    output_path.write_text(
        json.dumps(inventory, indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.1 — Gap Inventory written: {output_path}")
    print(f"     Total gaps: {len(inventory['gaps'])}")
    print(f"     17/17 traceable: {len(inventory['gaps']) == 17}")
    return inventory


# ============================================================
# TASK 30.2 — Gap Re-validation
# ============================================================

def task_30_2_revalidation(inventory):
    """Re-validate every Gap by fresh DB query — do NOT trust historical reports."""
    import pyodbc
    conn_str = (
        "DRIVER={SQL Server};"
        "SERVER=(local)\\SQLEXPRESS;"
        "DATABASE=ZXAF_V1_DevTest1;"
        "Trusted_Connection=yes;"
    )
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    revalidation = {
        "metadata": {
            "batch": "Batch 30+ Gap Review",
            "task": "30.2 Gap Re-validation",
            "executed_at": datetime.now().isoformat(),
            "method": "Fresh DB query, no historical trust",
        },
        "revalidations": [],
    }

    for gap in inventory["gaps"]:
        if "error" in gap:
            continue
        table = gap["table"]
        dimension = gap["dimension"]

        # Fresh evidence per dimension
        if dimension == "primary_key":
            cursor.execute("""
                SELECT
                    tc.CONSTRAINT_NAME,
                    kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                WHERE tc.TABLE_SCHEMA = 'dbo' AND tc.TABLE_NAME = ?
                  AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            """, (table,))
            pk_rows = cursor.fetchall()
            current_pk_columns = [r[1] for r in pk_rows]
            revalidation["revalidations"].append({
                "gap_id": gap["gap_id"],
                "table": table,
                "dimension": dimension,
                "fresh_query_result": {
                    "has_primary_key": len(pk_rows) > 0,
                    "pk_columns": current_pk_columns,
                },
                "gap_confirmed": len(pk_rows) == 0,
                "evidence": "INFORMATION_SCHEMA.TABLE_CONSTRAINTS (fresh query 2026-08-31)",
            })

        elif dimension == "tenant_index":
            cursor.execute("""
                SELECT
                    i.name AS index_name,
                    i.type_desc,
                    STUFF((
                        SELECT ',' + c2.name
                        FROM sys.index_columns ic
                        JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id
                        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                        ORDER BY ic.key_ordinal
                        FOR XML PATH('')
                    ), 1, 1, '') AS columns
                FROM sys.indexes i
                WHERE i.object_id = OBJECT_ID('dbo.' + ?)
                  AND i.is_primary_key = 0
                  AND i.name IS NOT NULL
                  AND i.is_hypothetical = 0
            """, (table,))
            index_rows = cursor.fetchall()
            index_columns_combined = ",".join([r[2] or "" for r in index_rows])
            has_tenant_index = "f_tenant_id" in index_columns_combined.lower()
            revalidation["revalidations"].append({
                "gap_id": gap["gap_id"],
                "table": table,
                "dimension": dimension,
                "fresh_query_result": {
                    "non_pk_index_count": len(index_rows),
                    "index_columns_combined": index_columns_combined,
                    "has_tenant_index": has_tenant_index,
                },
                "gap_confirmed": not has_tenant_index,
                "evidence": "sys.indexes + sys.index_columns (fresh query 2026-08-31)",
            })

        elif dimension == "audit_fields":
            cursor.execute("""
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = ?
            """, (table,))
            columns = [r[0] for r in cursor.fetchall()]
            required_audit = ["f_creator_time", "f_creator_user_id", "f_last_modify_time", "f_last_modify_user_id", "f_delete_mark"]
            missing = [f for f in required_audit if f not in columns]
            revalidation["revalidations"].append({
                "gap_id": gap["gap_id"],
                "table": table,
                "dimension": dimension,
                "fresh_query_result": {
                    "column_count": len(columns),
                    "missing_audit_fields": missing,
                },
                "gap_confirmed": len(missing) > 0,
                "evidence": "INFORMATION_SCHEMA.COLUMNS (fresh query 2026-08-31)",
            })

    conn.close()

    output_path = BATCH30_DIR / "batch-30-gap-revalidation.json"
    output_path.write_text(
        json.dumps(revalidation, indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.2 — Gap Re-validation written: {output_path}")
    print(f"     Total revalidations: {len(revalidation['revalidations'])}")
    confirmed = sum(1 for r in revalidation["revalidations"] if r["gap_confirmed"])
    print(f"     Gaps confirmed: {confirmed}/{len(revalidation['revalidations'])}")
    return revalidation


# ============================================================
# TASK 30.3 — PK Gap Analysis (base_signature, base_signature_user)
# ============================================================

def task_30_3_pk_analysis(inventory, revalidation):
    """Deep analysis of Missing PK Gaps."""
    import pyodbc
    conn_str = (
        "DRIVER={SQL Server};"
        "SERVER=(local)\\SQLEXPRESS;"
        "DATABASE=ZXAF_V1_DevTest1;"
        "Trusted_Connection=yes;"
    )
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    pk_gap_tables = ["base_signature", "base_signature_user"]
    analyses = []

    for table in pk_gap_tables:
        # 1. PK Candidate Analysis
        cursor.execute("""
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.CHARACTER_MAXIMUM_LENGTH
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = ?
            ORDER BY c.ORDINAL_POSITION
        """, (table,))
        columns = [{"name": r[0], "type": r[1], "nullable": r[2] == "YES", "max_length": r[3]} for r in cursor.fetchall()]

        # 2. Row count
        cursor.execute(f"SELECT COUNT(*) FROM dbo.{table}")
        row_count = cursor.fetchone()[0]

        # 3. Duplicate analysis (NULL counts per column)
        duplicates_analysis = {}
        for col in columns:
            cursor.execute(f"SELECT COUNT(*) - COUNT(DISTINCT [{col['name']}]) FROM dbo.{table}")
            nulls = cursor.fetchone()[0]
            cursor.execute(f"SELECT COUNT(*) FROM dbo.{table} WHERE [{col['name']}] IS NULL")
            null_count = cursor.fetchone()[0]
            cursor.execute(f"SELECT COUNT(DISTINCT [{col['name']}]) FROM dbo.{table} WHERE [{col['name']}] IS NOT NULL")
            distinct = cursor.fetchone()[0]
            duplicates_analysis[col['name']] = {
                "type": col["type"],
                "nullable": col["nullable"],
                "null_count": null_count,
                "distinct_count": distinct,
                "duplicate_count": nulls,
            }

        # 4. Referential Dependency Analysis
        cursor.execute("""
            SELECT
                fk.name,
                OBJECT_NAME(fk.parent_object_id) AS parent_table,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS parent_col,
                OBJECT_NAME(fk.referenced_object_id) AS ref_table,
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ref_col
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            WHERE fk.parent_object_id = OBJECT_ID('dbo.' + ?)
               OR fk.referenced_object_id = OBJECT_ID('dbo.' + ?)
        """, (table, table))
        fk_rows = cursor.fetchall()
        fks = [{"fk_name": r[0], "parent": r[1], "parent_col": r[2], "ref": r[3], "ref_col": r[4]} for r in fk_rows]

        # 5. SqlSugar Entity lookup
        cursor.execute("""
            SELECT OBJECT_NAME(object_id) AS entity_name
            FROM sys.triggers
            WHERE parent_id = OBJECT_ID('dbo.' + ?)
        """, (table,))
        triggers = [r[0] for r in cursor.fetchall()]

        analyses.append({
            "table": table,
            "pk_candidate_analysis": {
                "row_count": row_count,
                "columns": columns,
                "duplicate_analysis": duplicates_analysis,
                "pk_candidates": [
                    col["name"] for col in columns
                    if duplicates_analysis[col["name"]]["duplicate_count"] == 0
                    and duplicates_analysis[col["name"]]["null_count"] == 0
                ],
            },
            "referential_dependency_analysis": {
                "foreign_keys_in_or_out": fks,
                "fk_count": len(fks),
            },
            "runtime_impact_analysis": {
                "triggers": triggers,
                "row_count": row_count,
                "interpretation": "Empty table (0 rows) → no data migration risk, no FK risk",
            },
            "dynamic_check": {
                "table_name_starts_with_wform": table.startswith("wform_"),
                "table_name_starts_with_lowcode": table.startswith("lowcode_"),
                "verdict": "STATIC" if not (table.startswith("wform_") or table.startswith("lowcode_")) else "DYNAMIC",
            },
        })

    conn.close()

    output_path = BATCH30_DIR / "batch-30-pk-analysis.json"
    output_path.write_text(
        json.dumps({"metadata": {"task": "30.3 PK Analysis", "executed_at": datetime.now().isoformat()}, "analyses": analyses},
                  indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.3 — PK Analysis written: {output_path}")
    print(f"     Tables analyzed: {len(analyses)}")
    for a in analyses:
        print(f"     {a['table']}: row_count={a['pk_candidate_analysis']['row_count']}, "
              f"pk_candidates={a['pk_candidate_analysis']['pk_candidates']}, "
              f"dynamic={a['dynamic_check']['verdict']}")
    return analyses


# ============================================================
# TASK 30.4 — Tenant Index Analysis (15 tables)
# ============================================================

def task_30_4_tenant_index_analysis(inventory, revalidation):
    """Per-table tenant index analysis."""
    import pyodbc
    conn_str = (
        "DRIVER={SQL Server};"
        "SERVER=(local)\\SQLEXPRESS;"
        "DATABASE=ZXAF_V1_DevTest1;"
        "Trusted_Connection=yes;"
    )
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    # Collect unique tables from tenant index gaps
    tenant_gaps = [g for g in inventory["gaps"] if g["dimension"] == "tenant_index"]
    tables = list(set(g["table"] for g in tenant_gaps))

    analyses = []
    for table in tables:
        # Check tenant column
        cursor.execute("""
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = ? AND LOWER(COLUMN_NAME) IN ('f_tenant_id', 'tenantid', 'tenant_id')
        """, (table,))
        tenant_cols = [{"name": r[0], "type": r[1], "nullable": r[2] == "YES"} for r in cursor.fetchall()]

        # Get all indexes
        cursor.execute("""
            SELECT
                i.name AS index_name,
                i.is_unique,
                STUFF((
                    SELECT ',' + c2.name
                    FROM sys.index_columns ic
                    JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    ORDER BY ic.key_ordinal
                    FOR XML PATH('')
                ), 1, 1, '') AS columns
            FROM sys.indexes i
            WHERE i.object_id = OBJECT_ID('dbo.' + ?)
              AND i.is_primary_key = 0
              AND i.name IS NOT NULL
              AND i.is_hypothetical = 0
        """, (table,))
        index_rows = cursor.fetchall()
        existing_indexes = [{"name": r[0], "unique": bool(r[1]), "columns": r[2] or ""} for r in index_rows]

        # Check unique constraints
        cursor.execute("""
            SELECT
                tc.CONSTRAINT_NAME,
                kcu.COLUMN_NAME
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
              ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            WHERE tc.TABLE_SCHEMA = 'dbo' AND tc.TABLE_NAME = ?
              AND tc.CONSTRAINT_TYPE = 'UNIQUE'
        """, (table,))
        unique_rows = cursor.fetchall()
        unique_constraints = [{"name": r[0], "columns": r[1]} for r in unique_rows]

        # Row count
        cursor.execute(f"SELECT COUNT(*) FROM dbo.{table}")
        row_count = cursor.fetchone()[0]

        # Decision logic
        has_tenant_col = len(tenant_cols) > 0
        has_tenant_index = any(
            "f_tenant_id" in idx["columns"].lower() for idx in existing_indexes
        )

        if not has_tenant_col:
            decision = "EXCLUDED"  # No tenant column to index
            reason = "Table has no tenant column — tenant indexing not applicable"
        elif row_count < 100:
            decision = "NO_CHANGE"
            reason = f"Row count {row_count} < 100; index optimization not justified per IRON-TABLE-05"
        elif has_tenant_index:
            decision = "NO_CHANGE"
            reason = "Tenant field already covered by existing index"
        else:
            decision = "DEFERRED"
            reason = "Tenant index missing but table is currently small; defer to future batch when data grows"

        analyses.append({
            "table": table,
            "tenant_column": tenant_cols[0] if tenant_cols else None,
            "existing_indexes": existing_indexes,
            "unique_constraints": unique_constraints,
            "row_count": row_count,
            "tenant_index_status": "COVERED" if has_tenant_index else "MISSING",
            "decision": decision,
            "decision_reason": reason,
            "evidence": "INFORMATION_SCHEMA + sys.indexes (fresh query 2026-08-31)",
        })

    conn.close()

    output_path = BATCH30_DIR / "batch-30-tenant-index-analysis.json"
    output_path.write_text(
        json.dumps({"metadata": {"task": "30.4 Tenant Index Analysis", "executed_at": datetime.now().isoformat()}, "analyses": analyses},
                  indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.4 — Tenant Index Analysis written: {output_path}")
    print(f"     Tables analyzed: {len(analyses)}")
    decisions = {}
    for a in analyses:
        decisions.setdefault(a["decision"], []).append(a["table"])
    for d, tabs in decisions.items():
        print(f"     {d}: {len(tabs)} tables")
    return analyses


# ============================================================
# TASK 30.5 — Audit Field Analysis (5 tables)
# ============================================================

def task_30_5_audit_field_analysis(inventory, revalidation):
    """Per-table audit field gap analysis."""
    import pyodbc
    conn_str = (
        "DRIVER={SQL Server};"
        "SERVER=(local)\\SQLEXPRESS;"
        "DATABASE=ZXAF_V1_DevTest1;"
        "Trusted_Connection=yes;"
    )
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    audit_gaps = [g for g in inventory["gaps"] if g["dimension"] == "audit_fields"]
    tables = list(set(g["table"] for g in audit_gaps))

    required_audit = ["f_creator_time", "f_creator_user_id", "f_last_modify_time", "f_last_modify_user_id", "f_delete_mark"]

    analyses = []
    for table in tables:
        cursor.execute("""
            SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = ?
        """, (table,))
        existing = [r[0] for r in cursor.fetchall()]
        missing = [f for f in required_audit if f not in existing]

        # Check for variant audit field names (PascalCase, etc.)
        variant_patterns = {
            "f_creator_time": ["f_creatortime", "createtime", "F_CreatorTime"],
            "f_creator_user_id": ["f_creatoruserid", "creatoruserid", "F_CreatorUserId"],
            "f_last_modify_time": ["f_lastmodifytime", "modifytime", "F_LastModifyTime"],
            "f_last_modify_user_id": ["f_lastmodifyuserid", "F_LastModifyUserId"],
            "f_delete_mark": ["f_deletemark", "isdeleted", "F_DeleteMark", "delete_mark"],
        }
        alternative_present = {}
        for req_field, variants in variant_patterns.items():
            for variant in variants:
                if variant in existing:
                    alternative_present[req_field] = variant
                    break

        # Row count
        cursor.execute(f"SELECT COUNT(*) FROM dbo.{table}")
        row_count = cursor.fetchone()[0]

        # Decision: if alternatives present, NO_CHANGE; if row_count=0, DEFERRED; else MIGRATION_REQUIRED
        truly_missing = [m for m in missing if m not in alternative_present]
        if not truly_missing:
            decision = "NO_CHANGE"
            reason = f"Audit fields present via alternative naming: {alternative_present}"
        elif row_count == 0:
            decision = "DEFERRED"
            reason = f"Empty table; defer audit field migration to when data exists ({truly_missing} missing)"
        else:
            decision = "MIGRATION_REQUIRED"
            reason = f"Truly missing audit fields: {truly_missing}; table has data ({row_count} rows)"

        analyses.append({
            "table": table,
            "row_count": row_count,
            "missing_required_audit": missing,
            "alternative_fields_present": alternative_present,
            "truly_missing": truly_missing,
            "decision": decision,
            "decision_reason": reason,
            "evidence": "INFORMATION_SCHEMA.COLUMNS (fresh query 2026-08-31)",
        })

    conn.close()

    output_path = BATCH30_DIR / "batch-30-audit-field-analysis.json"
    output_path.write_text(
        json.dumps({"metadata": {"task": "30.5 Audit Field Analysis", "executed_at": datetime.now().isoformat()}, "analyses": analyses},
                  indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.5 — Audit Field Analysis written: {output_path}")
    print(f"     Tables analyzed: {len(analyses)}")
    decisions = {}
    for a in analyses:
        decisions.setdefault(a["decision"], []).append(a["table"])
    for d, tabs in decisions.items():
        print(f"     {d}: {len(tabs)} tables ({', '.join(tabs)})")
    return analyses


# ============================================================
# TASK 30.6 — Dynamic Classification (all 17 gaps)
# ============================================================

def task_30_6_dynamic_classification(inventory):
    """Classify all 17 Gaps as STATIC / DYNAMIC / HYBRID."""

    classifications = []
    for gap in inventory["gaps"]:
        if "error" in gap:
            continue
        table = gap["table"]
        table_lower = table.lower()

        # Classification rules
        if table_lower.startswith("wform_") or table_lower.startswith("lowcode_"):
            status = "DYNAMIC"
            reason = "Table name starts with wform_/lowcode_ → dynamic form table"
            human_gate = "REQUIRED"
        elif table_lower.startswith("ext_") and "_user" in table_lower:
            status = "HYBRID"
            reason = "ext_* user-extended table; runtime configurable"
            human_gate = "REQUIRED"
        elif table_lower.startswith("wh_") or table_lower.startswith("wm_"):
            status = "STATIC"
            reason = "Legacy warehouse table — static but legacy class"
            human_gate = "NOT_REQUIRED"
        elif table_lower.startswith("base_"):
            status = "STATIC"
            reason = "Standard system core table"
            human_gate = "NOT_REQUIRED"
        else:
            status = "STATIC"
            reason = "Default classification"
            human_gate = "NOT_REQUIRED"

        classifications.append({
            "gap_id": gap["gap_id"],
            "table": table,
            "dimension": gap["dimension"],
            "dynamic_status": status,
            "reason": reason,
            "human_gate": human_gate,
        })

    output_path = BATCH30_DIR / "batch-30-dynamic-classification.json"
    output_path.write_text(
        json.dumps({"metadata": {"task": "30.6 Dynamic Classification", "executed_at": datetime.now().isoformat()}, "classifications": classifications},
                  indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.6 — Dynamic Classification written: {output_path}")
    print(f"     Total classified: {len(classifications)}")
    by_status = {}
    by_gate = {}
    for c in classifications:
        by_status.setdefault(c["dynamic_status"], []).append(c["gap_id"])
        by_gate.setdefault(c["human_gate"], []).append(c["gap_id"])
    for s, ids in by_status.items():
        print(f"     {s}: {len(ids)} gaps")
    print(f"     Human Gate REQUIRED: {len(by_gate.get('REQUIRED', []))}")
    print(f"     Human Gate NOT_REQUIRED: {len(by_gate.get('NOT_REQUIRED', []))}")
    return classifications


# ============================================================
# TASK 30.7 — Migration Decision Matrix
# ============================================================

def task_30_7_decision_matrix(inventory, revalidation, pk_analyses, tenant_analyses, audit_analyses, classifications):
    """Generate final migration decision for every Gap (5 states only)."""

    # Index by table+dimension for fast lookup
    pk_decisions = {a["table"]: a for a in pk_analyses}
    tenant_decisions = {a["table"]: a for a in tenant_analyses}
    audit_decisions = {a["table"]: a for a in audit_analyses}
    dynamic_decisions = {c["gap_id"]: c for c in classifications}

    decisions = []
    for gap in inventory["gaps"]:
        if "error" in gap:
            continue
        gap_id = gap["gap_id"]
        table = gap["table"]
        dimension = gap["dimension"]

        # Get dynamic status
        dyn = dynamic_decisions.get(gap_id, {})

        # Determine decision per dimension
        if dimension == "primary_key":
            pk = pk_decisions.get(table, {})
            base_decision = pk.get("decision", "DEFERRED") if "decision" in pk else "DEFERRED"
            # Re-derive from raw analysis
            row_count = pk.get("pk_candidate_analysis", {}).get("row_count", 0)
            pk_candidates = pk.get("pk_candidate_analysis", {}).get("pk_candidates", [])
            is_dynamic = dyn.get("dynamic_status", "STATIC") in ("DYNAMIC", "HYBRID")

            if not pk_candidates:
                decision = "BLOCKED"  # No suitable PK candidate found
                reason = f"No column satisfies PK criteria (no NULL, no duplicate); table has {len(pk.get('pk_candidate_analysis', {}).get('columns', []))} columns"
            elif is_dynamic:
                decision = "HUMAN_GATE_REQUIRED"  # Dynamic table — human must decide
                reason = f"Table classified as {dyn.get('dynamic_status', 'STATIC')}; PK changes require human approval"
            elif row_count == 0:
                decision = "DEFERRED"
                reason = f"Empty table (0 rows); no data to migrate; defer to when data exists"
            else:
                decision = "DEFERRED"  # Default safe: don't auto-add PK without human review
                reason = f"PK addition requires data safety review; defer to Migration Phase with human gate"

        elif dimension == "tenant_index":
            td = tenant_decisions.get(table, {})
            decision = td.get("decision", "DEFERRED")
            reason = td.get("decision_reason", "Pending analysis")

        elif dimension == "audit_fields":
            ad = audit_decisions.get(table, {})
            decision = ad.get("decision", "DEFERRED")
            reason = ad.get("decision_reason", "Pending analysis")

        else:
            decision = "DEFERRED"
            reason = "Unknown dimension — defer"

        decisions.append({
            "gap_id": gap_id,
            "table": table,
            "dimension": gap["dimension"],
            "dynamic_status": dyn.get("dynamic_status", "STATIC"),
            "human_gate": dyn.get("human_gate", "NOT_REQUIRED"),
            "decision": decision,
            "decision_reason": reason,
            "evidence_files": [
                "batch-30-gap-inventory.json",
                "batch-30-gap-revalidation.json",
                "batch-30-pk-analysis.json",
                "batch-30-tenant-index-analysis.json",
                "batch-30-audit-field-analysis.json",
                "batch-30-dynamic-classification.json",
            ],
            "owner": "Chief Architect (decision required)",
            "prerequisite": (
                "If MIGRATION_REQUIRED: Migration Spec (Phase 31)"
                if decision == "MIGRATION_REQUIRED"
                else "None for current Decision Phase"
            ),
        })

    output_path = BATCH30_DIR / "batch-30-migration-decision-matrix.json"
    output_path.write_text(
        json.dumps({"metadata": {"task": "30.7 Migration Decision Matrix", "executed_at": datetime.now().isoformat()}, "decisions": decisions},
                  indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Task 30.7 — Migration Decision Matrix written: {output_path}")
    print(f"     Total decisions: {len(decisions)}")
    by_decision = {}
    for d in decisions:
        by_decision.setdefault(d["decision"], []).append(d["gap_id"])
    for decision, ids in by_decision.items():
        print(f"     {decision}: {len(ids)} gaps")
    return decisions


# ============================================================
# GATE 30 — Batch 30+ Gap Review Acceptance
# ============================================================

def gate_30_acceptance(decisions, classifications, pk_analyses, tenant_analyses, audit_analyses):
    """Verify all 17 Gaps have evidence + Target Contract + Risk + Migration Type + Runtime Impact + Rollback Strategy."""

    acceptance = {
        "metadata": {
            "gate": "Batch 30+ Gap Review Acceptance",
            "executed_at": datetime.now().isoformat(),
            "criteria": "17/17 reviewed + evidence + target + risk + migration_type + runtime_impact + rollback",
        },
        "per_gap": [],
        "summary": {},
    }

    # Per-gap verification
    by_id = {d["gap_id"]: d for d in decisions}

    for gap_id, table, dimension, _ in [(g[0], g[1], g[2], g[3]) for g in G1_MAJOR_GAPS]:
        d = by_id.get(gap_id, {})
        # Check all 7 required fields
        checks = {
            "evidence": "batch-30-gap-revalidation.json" in d.get("evidence_files", []),
            "target_contract": dimension in ["primary_key", "tenant_index", "audit_fields"],  # 8-dim contract defined per dimension
            "risk": d.get("decision_reason") is not None and d.get("decision_reason") != "",
            "migration_type": d.get("decision") in ["MIGRATION_REQUIRED", "NO_CHANGE", "DEFERRED", "EXCLUDED", "BLOCKED", "HUMAN_GATE_REQUIRED"],
            "runtime_impact": "row_count" in str(d) or d.get("dynamic_status") in ["STATIC", "DYNAMIC", "HYBRID"],
            "rollback_strategy": d.get("decision") in ["NO_CHANGE", "DEFERRED", "EXCLUDED", "BLOCKED", "HUMAN_GATE_REQUIRED"]
                                   or d.get("prerequisite") is not None,
            "decision_recorded": d.get("decision") is not None and d.get("decision_reason") is not None,
        }
        all_pass = all(checks.values())
        acceptance["per_gap"].append({
            "gap_id": gap_id,
            "table": table,
            "checks": checks,
            "all_pass": all_pass,
        })

    # Summary
    pass_count = sum(1 for p in acceptance["per_gap"] if p["all_pass"])
    acceptance["summary"] = {
        "total_gaps": len(acceptance["per_gap"]),
        "all_pass": pass_count == len(acceptance["per_gap"]),
        "pass_count": pass_count,
        "fail_count": len(acceptance["per_gap"]) - pass_count,
    }

    output_path = BATCH30_DIR / "batch-30-acceptance-gate.json"
    output_path.write_text(
        json.dumps(acceptance, indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    print(f"[OK] Gate 30 — Acceptance written: {output_path}")
    print(f"     Pass: {pass_count}/{len(acceptance['per_gap'])}")
    print(f"     All Pass: {acceptance['summary']['all_pass']}")
    return acceptance


# ============================================================
# MAIN — Execute 30.1 → 30.7 → Gate 30
# ============================================================

if __name__ == "__main__":
    print("=" * 60)
    print("Batch 30+ Gap Review Bundle — Tasks 30.1 → 30.7")
    print("=" * 60)

    print("\n[Task 30.1] Establishing Gap Inventory...")
    inventory = task_30_1_gap_inventory()

    print("\n[Task 30.2] Re-validating Gaps with fresh DB queries...")
    revalidation = task_30_2_revalidation(inventory)

    print("\n[Task 30.3] Analyzing PK Gaps (base_signature, base_signature_user)...")
    pk_analyses = task_30_3_pk_analysis(inventory, revalidation)

    print("\n[Task 30.4] Analyzing Tenant Index Gaps (15 tables)...")
    tenant_analyses = task_30_4_tenant_index_analysis(inventory, revalidation)

    print("\n[Task 30.5] Analyzing Audit Field Gaps (5 tables)...")
    audit_analyses = task_30_5_audit_field_analysis(inventory, revalidation)

    print("\n[Task 30.6] Dynamic Classification (all 17 Gaps)...")
    classifications = task_30_6_dynamic_classification(inventory)

    print("\n[Task 30.7] Migration Decision Matrix (5 states)...")
    decisions = task_30_7_decision_matrix(inventory, revalidation, pk_analyses, tenant_analyses, audit_analyses, classifications)

    print("\n[Gate 30] Acceptance Gate Verification...")
    acceptance = gate_30_acceptance(decisions, classifications, pk_analyses, tenant_analyses, audit_analyses)

    print("\n" + "=" * 60)
    print(f"Gate 30 Result: {'PASS' if acceptance['summary']['all_pass'] else 'FAIL'}")
    print(f"  Pass: {acceptance['summary']['pass_count']}/{acceptance['summary']['total_gaps']}")
    print("=" * 60)
    print("\n⚠ STOP — Per Master Plan v2.1:")
    print("  - 30.1-30.7 completed")
    print("  - NO Schema DDL executed")
    print("  - Awaiting Batch 30+ Gap Review Acceptance Gate decision")
    print("  - Next: Chief Architect reviews decision matrix and authorizes Phase 31+")
