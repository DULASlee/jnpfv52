# P8-A Productivity Baseline

> **Phase**: 8 — P8-A.5
> **Status**: ESTABLISHED
> **Date**: 2026-08-30
> **Protocol**: Adversarial Track B (Blind Review unavailable)

---

## 1. Purpose

Establish the productivity baseline for Phase 8 based on P8-A Shadow Mode execution. This baseline serves as reference for P8-B, P8-C, P8-D efficiency targets.

**Important caveat**: Under Adversarial Protocol, "Human Review Time" is actually "AI Adversarial Review Time". This is NOT directly comparable to a true human review baseline. The values are recorded but must be interpreted with this caveat.

---

## 2. AI Execution Time (Track A)

| Table | AI Duration (min) | Notes |
|---|---|---|
| 01 base_sys_config | ~2 | Simple table, fast |
| 02 base_user | ~3 | Largest table (68 cols), more depth |
| 03 base_visual_dev | ~3 | JSON blobs + cross-module |
| 04 ext_table_example | ~2 | Simple extension |
| 05 sa_data_dictionary | ~3 | Most complex, R3+, DEFERRED |
| **Total** | **~13 min** | |

**AI Metrics**:
- AI Median: 2.4 min/table
- AI P90: 3 min/table
- AI Total: 13 min
- Tables / AI-hour: 5 / (13/60) = **~23 tables/AI-hour**

---

## 3. Adversarial Review Time (Track B)

| Table | Review Duration (min) | Notes |
|---|---|---|
| 01 base_sys_config | ~35 | Simple but multiple findings |
| 02 base_user | ~50 | Critical table, most divergent |
| 03 base_visual_dev | ~45 | Cross-module, F_EN_CODE gap |
| 04 ext_table_example | ~35 | Calibration table |
| 05 sa_data_dictionary | ~50 | Most analytical, HG borderline dodge |
| **Total** | **~215 min = ~3.6 hours** | |

**Adversarial Review Metrics**:
- Review Median: 45 min/table
- Review P90: 50 min/table
- Review Total: ~3.6 hours
- Review hours / table: ~43 min (~0.72 hours/table)

**Note**: Under Blind Review (per Blind Review Protocol §5), expected time was 40-55 min/table = 3.5-4.5 hours total. Adversarial Review time is in the same range, slightly faster due to no isolation overhead.

---

## 4. Comparison Time

| Table | Comparison Duration (min) |
|---|---|
| 01 base_sys_config | ~20 |
| 02 base_user | ~30 |
| 03 base_visual_dev | ~25 |
| 04 ext_table_example | ~20 |
| 05 sa_data_dictionary | ~30 |
| **Total** | **~125 min = ~2.1 hours** |

**Comparison Metrics**:
- Comparison Median: 25 min/table
- Comparison Total: ~2.1 hours

---

## 5. Cumulative Phase 8 Time

| Phase | Duration | Notes |
|---|---|---|
| Track A | ~13 min | AI evaluation |
| Track B (Adversarial) | ~215 min | AI adversarial review |
| Comparison | ~125 min | AI/Human comparison |
| Shadow Gate | ~15 min | Final calculation |
| **Total P8-A** | **~368 min = ~6.1 hours** | Single reviewer/AI |

---

## 6. Productivity Baseline (for P8-B reference)

| Metric | Value | Use in P8-B |
|---|---|---|
| AI Median Time / Table | 2.4 min | Target: ≤ 2.4 min for R0/R1, ≤ 3 min for R2 |
| AI P90 / Table | 3 min | P90 boundary |
| Tables / AI-hour | ~23 | Target baseline |
| Review Median / Table | 45 min | Review time budget per table |
| Review P90 / Table | 50 min | P90 boundary |
| Review hours / Table | ~0.72 | Track B budget per table |
| Comparison Median / Table | 25 min | Comparison time budget |

---

## 7. Productivity vs Quality Trade-off

The P8-A adversarial review discovered **44 findings** across 5 tables (~9 findings/table average). Under Blind Review, expected finding rate would be different (likely higher confidence, fewer apparent disagreements).

**Interpretation under Adversarial Protocol**:
- High finding rate (~9/table) is EXPECTED for adversarial review
- This is calibration data, not signal of low-quality Track A
- True productivity assessment comes from P8-B (real execution)

---

## 8. P8-B Efficiency Targets (Recommended)

Based on P8-A baseline:

| P8-B Target | Recommended Value | Rationale |
|---|---|---|
| AI Median Time / Table | ≤ 3 min | Allow slight increase for complexity |
| Tables / AI-hour | ≥ 20 | Maintain throughput |
| Review hours / Table | ≤ 0.75 | Maintain review rigor |
| Comparison hours / Table | ≤ 0.5 | Maintain comparison efficiency |
| Finding rate (per table) | 5-12 | Track A finding rate baseline |

---

## 9. Productivity Limitations

Under Adversarial Protocol, these productivity numbers have limitations:

1. **AI Adversarial Review ≠ Human Review**: A true human reviewer would likely take longer (4.5-5.5 hours total) and produce different findings.

2. **Comparison time compressed**: Real human-vs-AI comparison might take longer for unfamiliar topics.

3. **Single-session execution**: P8-A was done in one session. A multi-day P8-A might show different time distribution.

4. **First-time execution**: P8-A was the first time Track A/Track B ran. Subsequent runs may be faster due to familiarity.

---

## 10. Baseline Re-calibration Trigger

This baseline should be RECALIBRATED when:
- P8-B Batch 01 completes (real execution data)
- Skill Evolution changes affect evaluation time
- Master Spec changes affect comparison criteria

The recalibration should produce a P8-B-specific baseline that supersedes this P8-A baseline.
