"""D5: scan pnpm-lock.yaml (lockfileVersion 6.0) for multi-version packages.
Usage: cd jnpf-web-vue3 && python ../../.claude/evidence/frontend-ct/d5-dups.py
Writes: .claude/evidence/frontend-ct/d5-multiversion.txt
"""
import re, os, sys

lock = 'pnpm-lock.yaml'
if not os.path.exists(lock):
    print('NO_LOCK_FILE: ' + lock); sys.exit(1)

versions = {}
in_packages = False
with open(lock, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        if re.match(r'^packages:\s*$', line):
            in_packages = True; continue
        if re.match(r'^\S', line) and not line.startswith('#'):
            in_packages = False
        if in_packages:
            m = re.match(r'^\s+/(@[^/]+/[^@]+|[^@]+)@([^(:]+)', line)
            if m:
                versions.setdefault(m.group(1).strip(), set()).add(
                    m.group(2).strip().split('(')[0].strip())

dups = {k: sorted(v) for k, v in versions.items() if len(v) > 1}
out = '../.claude/evidence/frontend-ct/d5-multiversion.txt'
with open(out, 'w', encoding='utf-8') as o:
    o.write('D5 multi-version packages (pnpm-lock lockfileVersion 6.0)\n')
    o.write('TOTAL_DISTINCT_PACKAGES=%d\nMULTI_VERSION_PACKAGES=%d\n\n' % (len(versions), len(dups)))
    for name in sorted(dups, key=lambda n: (-len(dups[n]), n)):
        o.write('  %-45s %dv: %s\n' % (name, len(dups[name]), ', '.join(dups[name])))
print('written: ' + out + '  (%d multi-version packages)' % len(dups))
