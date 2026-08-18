"""Scan C# files for foreach loops that modify the collection being iterated."""
import re
import os
import sys

def scan_file(filepath):
    """Scan a single C# file for the dangerous pattern."""
    with open(filepath, encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()

    results = []

    for i, line in enumerate(lines):
        # Match: foreach (Type varName in COLLECTION)
        # Capture the collection expression (last word before closing paren)
        m = re.search(r'foreach\s*\([^)]+\s+in\s+(\w+(?:\.\w+)*)\)', line)
        if not m:
            continue

        coll_expr = m.group(1)
        coll_var = coll_expr.split('.')[-1]  # last segment if dotted

        # Skip trivial variable names
        if coll_var in ('item', 'it', 'x', 'y', 'key', 'value', 'kvp', 'entry',
                         'element', 'e', 'i', 'j', 'k', 'obj', 'data', 'row',
                         'Items', 'Keys', 'Values'):
            continue

        # Track braces to find end of foreach body
        brace_count = 0
        in_body = False
        end_line = i

        for j in range(i, min(i + 60, len(lines))):
            l = lines[j]
            brace_count += l.count('{')
            brace_count -= l.count('}')

            if '{' in l:
                in_body = True

            if in_body and brace_count <= 0:
                end_line = j
                break

        # Now search within the foreach body for modifications to coll_var
        for j in range(i + 1, end_line + 1):
            l = lines[j]
            # Skip comments
            if l.strip().startswith('//') or l.strip().startswith('/*'):
                continue

            # Check for collection modification
            for mod_op in ['.Remove(', '.Add(', '.Insert(', '.RemoveAt(', '.Clear(']:
                # Search for coll_var.mod_op within the line
                pattern = r'\b' + re.escape(coll_var) + re.escape(mod_op)
                if re.search(pattern, l):
                    results.append({
                        'file': filepath,
                        'foreach_line': i + 1,
                        'coll_var': coll_var,
                        'mod_line': j + 1,
                        'mod_code': l.strip()[:150]
                    })
                    break  # one finding per foreach is enough

    return results


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else 'backend'

    all_results = []
    for dirpath, _, filenames in os.walk(root):
        for fname in filenames:
            if not fname.endswith('.cs'):
                continue
            fpath = os.path.join(dirpath, fname)
            results = scan_file(fpath)
            all_results.extend(results)

    if all_results:
        for r in all_results:
            print(f"!!! {r['file']}:{r['foreach_line']} foreach({r['coll_var']})")
            print(f"    modified at line {r['mod_line']}: {r['mod_code']}")
            print()
        print(f"Total: {len(all_results)} potential issues found")
    else:
        print("No potential issues found.")

if __name__ == '__main__':
    main()
