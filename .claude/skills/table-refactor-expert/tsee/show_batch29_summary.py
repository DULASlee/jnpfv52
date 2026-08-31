"""Display Batch 29 evidence summary."""
import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
from pathlib import Path

evidence_file = Path("batch-29-evidence.json")
if not evidence_file.exists():
    print("batch-29-evidence.json not found")
    sys.exit(1)

data = json.loads(evidence_file.read_text(encoding="utf-8"))
print(f"OK: {len(data['tables'])} tables collected")
for t in data["tables"]:
    if "error" in t:
        print(f"ERR {t['table_name']}: {t['error']}")
    else:
        print(
            f"{t['table_name']:35} cols={t['column_count']:3} idx={t['index_count']:3} "
            f"pk={t['has_primary_key']} fk={t['fk_count']} rows={t['row_count']:6} "
            f"tenant={t['tenant_field']} soft_del={t['soft_delete_field']}"
        )
