# Patch note 1 — post-telemetry balance pass

**Revised after `d5ab8c7` (house difficulty centering fix) and run 4.** The first draft of this note
was written against runs 1–3, which were invalid: house difficulty was centered on the map spawner
transform instead of the player, so the player was surrounded by houses above their damage tier. Two
recommendations have been **retracted** as a result — see the end.

Basis: [`balance-run-evaluation.md`](balance-run-evaluation.md) and `artifacts/`.

---

## What the fix already proved

Run 4 shows **Tier 0 working exactly as designed**: start at damage 10, buy four attack upgrades in
the first 30 seconds, arrive at tier 1 with damage 50 — to the purchase. Buildings went from 3.7/min
to **11.2/min**, and building income from 17% to **47%** of the total, with no balance change at all.

**The tier ladder is sound. Do not re-derive it.** The remaining problems are narrower than they
looked.

---

## Changes

### 1. Fix the melee swing telemetry — **blocking** `MeleeWeapon.cs`

Run 4 reports 2413 swings in 69.5 s (34.7/s) and 0.09 mobs per swing. Both are wrong. The Player
prefab now carries a second `MeleeWeapon` — a props aura with a 0.02 s cooldown, 360° cone and
`_damageOnlyProps: true` — which fires every frame and calls `RecordMeleeSwing(0)`, because the
props path skips the mob loop.

Guard the call so only the real combat weapon reports:

```csharp
if (!_damageOnlyProps)
  _telemetry?.RecordMeleeSwing(_hitMobs.Count);
```

Corrected run-4 figures: ~139 real swings, **1.59 mobs/swing, capacity 3.18/s**. Fix this first —
`meanMobsPerSwing` is the metric every other decision below is calibrated against, and right now the
instrumentation is lying.

### 2. Melee radius 5 → 6.5 `BattleBalanceConfig`

`_meleeAttackRadius: 5` → **6.5**

The opening is still under-powered: slice-0 capacity is 3.18/s against 7.5/s of spawn, a 2.4×
deficit that builds a backlog and hands the economy to the mob cap. Effective radius goes 6.0 → 7.8
wu, area ×1.69, so capacity should land near **5.4/s**.

### 3. Spawn ramp 5→20 becomes 4→12 `BattleScene.unity`

`_mobsPerSecStart: 5` → **4**  ·  `_mobPerSecondEnd: 20` → **12**  ·  `_secondEnd: 90` (keep)

| Slice | Mean spawn rate | Capacity (with change 2) |
|---|---:|---:|
| 0–30 s | 5.3 /s | ~5.4 /s |
| 30–60 s | 8.0 /s | rising with upgrades |
| 60–90 s | 10.7 /s | ~12.7 /s at max |

20/s was never reachable — measured peak capacity is 12.7/s and only with health fully maxed.
Together with change 2 this should keep the population off the cap, which returns spawn rate to
being a real dial. Currently slice 1 delivers 0.37 of nominal.

### 4. Health repricing and decoupling `ProgressionBalanceConfig`

`_maxHealthUpgradeCost: 5` → **10**  ·  `_maxHealthScalePerLevel: 0.02` → **0.01**

Health is the cheapest upgrade in the game (5, against attack 7 and speed 6) *and* buys survival
*and* scales melee radius — therefore income. In run 3 the player maxed it: 29 of 29 purchases, 36%
of all spend. Halving the scale gives ×1.29 at max instead of ×1.58, and doubling the cost makes it
compete with attack. Raising the base radius (change 2) makes the coupling absolutely stronger, so
these two belong in the same pass.

### 5. Timer respawn preserves remaining time `BattleService`

Run 3 lasted **163.7 s on a 90 s clock with zero timer purchases**. Destroying and respawning the
timer resets it to full duration; it should restore remaining time.

### 6. Investigate shop access — needs diagnosis, not a number

Run 4's player died holding **118 unspent coins**, having earned 104 in slice 1 and spent nothing:
**zero shop visits after t = 30 s**. Late-run progression stalled on shop *access*, not income —
damage sat at 50 from t=30 to death.

Check whether Upgrade houses respawn often enough and near enough once the player has moved.
`SpecialSpawnSettings` puts Upgrade at 0–30 wu initially, then 0–30 / 0–50 on respawn. If that is
the constraint, widening it is cheaper than any economy change.

### 7. Price the props damage

Slice 2 of run 4 took **45 damage with only one mob connecting** — roughly 44 damage from props,
about 4.6 dps. Against 120–180 max health that is a major unmodelled threat, and it is what killed
that run. Either reduce it, or make it intentional and telegraphed, but it needs a number in the
balance model rather than arriving as a surprise.

### 8. Mob cap stays at 150

No change. With 2 and 3 the population should stop saturating, returning the cap to a pure
performance guard.

**Diagnostic:** compare actual spawns per slice against the nominal ramp. Below ~0.9 means the cap is
still governing and spawn tuning is a no-op. Run 4 read 1.00 / 0.37.

---

## Retracted from the first draft

- ~~Raise building drops to 12 / 40 / 120.~~ **Not needed.** Buildings deliver 47% of income at the
  existing 5–9 roll, and coins-per-building measured 7.2 against a model EV of 7.0. The premise —
  that buildings lose an incentive trade to the mob crowd — was an artefact of the centering bug.
  Note the tier ladder keeps time-to-kill constant at 5 hits per tier, so a flat 7 coins is already
  rate-neutral when fighting your own tier.
- ~~Flatten `HouseSet` to random tier weights 50/35/15.~~ **Not needed.** With difficulty correctly
  centered on the player, radial banding does exactly what the design intended: T1 near spawn,
  higher tiers outward. Run 4's 5 buildings in the first 30 seconds is that working. Leave the bands
  alone and tune their radii only if telemetry shows a tier starving.

---

## Predicted outcomes — check against run 5

| Metric | Run 4 | Target |
|---|---:|---:|
| Reported swings/s | 34.7 (bug) | **~2.0** |
| Mobs per swing | 1.59 (corrected) | **> 2.5** |
| Slice-0 kill fraction | 0.50 | **> 0.85** |
| Spawns actual ÷ nominal, all slices | 1.00 / 0.37 | **> 0.90** |
| Buildings destroyed per minute | 11.2 | **hold ≥ 10** |
| Unspent coins at end | 118 | **< 40** |
| Damage at t = 60 s | 50 | **250** |
| Run duration | 69.5 s | **~90 s** |

Damage at t=60 is the one to watch. Tier 0 hit its mark exactly; tier 1 stalled because the player
could not spend. If change 6 lands and damage still sits at 50 by t=60, the problem is the tier-1
income curve rather than shop access.
