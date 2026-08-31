"""
Batch 29 Schema Evidence Collector
Per Group A of Batch 29 Execution Package.
Collects metadata for 15 candidate tables from SQL Server.
Outputs: batch-29-evidence.json
"""
import json
import sys
from datetime import datetime
from pathlib import Path


BATCH29_TABLES = [
    "base_advanced_query_scheme",
    "base_app_data",
    "base_columns_purview",
    "base_data_interface_user",
    "base_data_interface_variate",
    "base_db_link",
    "base_im_content",
    "base_im_reply",
    "base_integrate",
    "base_integrate_node",
    "base_organize_relation",
    "base_portal",
    "base_portal_data",
    "base_signature",
    "base_signature_user",
]

# JNPF Audit / Tenant field conventions (per JNPF Project Extension)
TENANT_FIELD_PATTERNS = ["f_tenant_id", "F_TenantId", "tenant_id", "TenantId"]
CREATED_TIME_PATTERNS = ["f_creator_time", "F_CreatorTime", "creator_time", "created_time"]
CREATED_BY_PATTERNS = ["f_creator_user_id", "F_CreatorUserId", "creator_user_id", "created_by"]
MODIFIED_TIME_PATTERNS = ["f_last_modify_time", "F_LastModifyTime", "last_modify_time", "modified_time"]
MODIFIED_BY_PATTERNS = ["f_last_modify_user_id", "F_LastModifyUserId", "last_modify_user_id", "modified_by"]
SOFT_DELETE_PATTERNS = ["f_delete_mark", "F_DeleteMark", "delete_mark", "is_deleted", "deleted"]
PRIMARY_KEY_FIELD_PATTERNS = ["f_id", "F_Id", "id"]


def collect_table_metadata(table_name, conn_factory):
    """Collect full metadata for a single table using raw SQL queries."""
    conn = conn_factory()
    cursor = conn.cursor()

    result = {"table_name": table_name, "schema": "dbo"}

    # Columns
    cursor.execute("""
        SELECT 
            c.COLUMN_NAME,
            c.DATA_TYPE,
            c.IS_NULLABLE,
            c.CHARACTER_MAXIMUM_LENGTH,
            c.ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.COLUMNS c
        WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = ?
        ORDER BY c.ORDINAL_POSITION
    """, (table_name,))
    columns = []
    for row in cursor.fetchall():
        columns.append({
            "name": row[0],
            "data_type": row[1],
            "is_nullable": row[2] == "YES",
            "max_length": row[3],
            "ordinal": row[4],
        })
    result["columns"] = columns
    result["column_count"] = len(columns)

    # Primary Key
    cursor.execute("""
        SELECT c.COLUMN_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE c 
          ON tc.CONSTRAINT_NAME = c.CONSTRAINT_NAME
        WHERE tc.TABLE_SCHEMA = 'dbo'
          AND tc.TABLE_NAME = ?
          AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
    """, (table_name,))
    pk_rows = cursor.fetchall()
    result["primary_key"] = [row[0] for row in pk_rows]
    result["has_primary_key"] = len(pk_rows) > 0

    # Indexes (non-PK)
    cursor.execute("""
        SELECT 
            i.name AS index_name,
            i.type_desc,
            i.is_unique,
            STUFF((
                SELECT ',' + c2.name + (CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END)
                FROM sys.index_columns ic
                JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                ORDER BY ic.key_ordinal
                FOR XML PATH('')
            ), 1, 1, '') AS columns,
            i.filter_definition
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID('dbo.' + ?)
          AND i.is_primary_key = 0
          AND i.name IS NOT NULL
          AND i.is_hypothetical = 0
    """, (table_name,))
    indexes = []
    for row in cursor.fetchall():
        indexes.append({
            "name": row[0],
            "type": row[1],
            "is_unique": bool(row[2]),
            "columns": row[3],
            "filter": row[4],
        })
    result["indexes"] = indexes
    result["index_count"] = len(indexes)

    # Foreign Keys (outgoing)
    cursor.execute("""
        SELECT 
            fk.name,
            OBJECT_NAME(fk.referenced_object_id) AS ref_table,
            COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ref_column,
            COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS parent_column
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        WHERE fk.parent_object_id = OBJECT_ID('dbo.' + ?)
    """, (table_name,))
    fks = []
    for row in cursor.fetchall():
        fks.append({
            "name": row[0],
            "ref_table": row[1],
            "ref_column": row[2],
            "parent_column": row[3],
        })
    result["foreign_keys"] = fks
    result["fk_count"] = len(fks)

    # Row count + table metadata
    cursor.execute("""
        SELECT 
            SUM(p.rows) AS row_count,
            t.create_date,
            t.modify_date
        FROM sys.tables t
        JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
        WHERE t.object_id = OBJECT_ID('dbo.' + ?)
        GROUP BY t.create_date, t.modify_date
    """, (table_name,))
    row = cursor.fetchone()
    result["row_count"] = int(row[0]) if row and row[0] else 0
    result["table_created"] = row[1].isoformat() if row else None
    result["table_modified"] = row[2].isoformat() if row else None

    # Field contract checks
    col_names = {c["name"] for c in columns}

    def find_field(patterns):
        for p in patterns:
            if p in col_names:
                return p
        return None

    result["tenant_field"] = find_field(TENANT_FIELD_PATTERNS)
    result["created_time_field"] = find_field(CREATED_TIME_PATTERNS)
    result["created_by_field"] = find_field(CREATED_BY_PATTERNS)
    result["modified_time_field"] = find_field(MODIFIED_TIME_PATTERNS)
    result["modified_by_field"] = find_field(MODIFIED_BY_PATTERNS)
    result["soft_delete_field"] = find_field(SOFT_DELETE_PATTERNS)
    result["id_field"] = find_field(PRIMARY_KEY_FIELD_PATTERNS)

    conn.close()
    return result


def collect_all_evidence():
    """Connect to DB and collect evidence for all Batch 29 tables."""
    import pyodbc
    conn_str = (
        "DRIVER={SQL Server};"
        "SERVER=(local)\\SQLEXPRESS;"
        "DATABASE=ZXAF_V1_DevTest1;"
        "Trusted_Connection=yes;"
    )
    conn = pyodbc.connect(conn_str)

    def conn_factory():
        return pyodbc.connect(conn_str)

    evidence = {
        "metadata": {
            "batch": "Batch 29",
            "purpose": "Baseline Confirmation (NO-CHANGE evidence)",
            "collected_at": datetime.now().isoformat(),
            "collector": "tsee.batch29.collect_evidence (v0.1)",
            "total_tables": len(BATCH29_TABLES),
        },
        "tables": []
    }

    for table_name in BATCH29_TABLES:
        try:
            table_evidence = collect_table_metadata(table_name, conn_factory)
            evidence["tables"].append(table_evidence)
        except Exception as e:
            evidence["tables"].append({
                "table_name": table_name,
                "error": str(e),
            })

    conn.close()
    return evidence


if __name__ == "__main__":
    import sys
    sys.stdout.reconfigure(encoding='utf-8')

    evidence = collect_all_evidence()

    output_path = Path(__file__).parent / "batch-29-evidence.json"
    output_path.write_text(
        json.dumps(evidence, indent=2, ensure_ascii=False, default=str),
        encoding="utf-8"
    )
    print(f"[OK] Evidence collected for {len(evidence['tables'])} tables")
    print(f"[OK] Output: {output_path}")
    print(f"[OK] Tables with PK: {sum(1 for t in evidence['tables'] if t.get('has_primary_key'))}")
    print(f"[OK] Tables with FK: {sum(1 for t in evidence['tables'] if t.get('fk_count', 0) > 0)}")
    print(f"[OK] Tables with tenant field: {sum(1 for t in evidence['tables'] if t.get('tenant_field'))}")
    print(f"[OK] Tables with soft_delete field: {sum(1 for t in evidence['tables'] if t.get('soft_delete_field'))}")