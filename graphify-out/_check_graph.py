import json
from pathlib import Path

g = json.loads(Path('D:/JNPF-v52/graphify-out/graph.json').read_text(encoding='utf-8'))

print(f"Nodes: {len(g['nodes'])}, Links: {len(g['links'])}, Hyperedges: {len(g.get('hyperedges', []))}")
print(f"Built at: {g.get('built_at_commit', 'unknown')}")
print()

# Community distribution
communities = {}
for n in g['nodes']:
    c = n.get('community')
    if c is not None:
        communities[c] = communities.get(c, 0) + 1
if communities:
    print("Community distribution:")
    for cid, cnt in sorted(communities.items(), key=lambda x: -x[1]):
        # Find a label name for this community
        samples = [n.get('label', n['id']) for n in g['nodes'] if n.get('community') == cid][:3]
        print(f"  C{cid}: {cnt} nodes (e.g. {', '.join(samples[:2])})")

print()

# Top nodes by degree
degrees = {}
for l in g['links']:
    s = l['source']
    t = l['target']
    degrees[s] = degrees.get(s, 0) + 1
    degrees[t] = degrees.get(t, 0) + 1

id_to_label = {n['id']: n.get('label', n['id']) for n in g['nodes']}

print("Top 20 nodes by degree:")
for nid, deg in sorted(degrees.items(), key=lambda x: -x[1])[:20]:
    label = id_to_label.get(nid, nid)[:80]
    print(f"  deg={deg:3d}  {label}")

print()

# Node type distribution
types = {}
for n in g['nodes']:
    t = n.get('type', 'unknown')
    types[t] = types.get(t, 0) + 1
print("Node types:")
for t, cnt in sorted(types.items(), key=lambda x: -x[1]):
    print(f"  {t}: {cnt}")

# Confidence distribution
confs = {}
for n in g['nodes']:
    c = n.get('confidence', 'unknown')
    confs[c] = confs.get(c, 0) + 1
print("Confidence distribution:")
for c, cnt in sorted(confs.items(), key=lambda x: -x[1]):
    print(f"  {c}: {cnt}")

# Show some node labels to understand the graph content
print("\nSample nodes from each type:")
seen_types = set()
for n in g['nodes']:
    t = n.get('type', 'unknown')
    if t not in seen_types:
        seen_types.add(t)
        label = n.get('label', n['id'])[:100]
        print(f"  [{t}] {label} (community={n.get('community')}, confidence={n.get('confidence')})")
