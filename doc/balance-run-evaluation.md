# Balance run evaluation — theory vs. first telemetry

Empirical check of [`balance-reference-summary.md`](balance-reference-summary.md) against the first
three instrumented runs in `artifacts/`. All numbers below come from the run dumps; nothing here is
modelled.

| Run | Outcome | Duration | Spawned | Killed | k | Buildings | Earned | Spent | Detour |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 19:16 | defeat | 62.9 s | 542 | 392 | 0.72 | **0** | 189 | 105 | 10.8× |
| 19:17 | defeat | 22.0 s | 150 | 63 | 0.42 | **0** | 23 | 0 | 8.6× |
| 19:27 | **win** | 163.7 s | 900 | 760 | 0.84 | 10 | 432 | 408 | 11.5× |

---

## 1. Verdict

**The per-unit models were right. The volume assumptions were wrong.**

Everything the theory computed *per event* — coins per kill, coins per building, swings per second,
spawns per second, attack purchases required — matched the telemetry closely. Everything it assumed
about *how many* of those events a run contains was off, in one case by a factor of three.

The headline failure: **the building/tier loop never engaged.** Across all three runs, **zero
buildings were destroyed in the first 90 seconds** — the entire span the three-tier design describes.
The winning run destroyed its first building at t ≈ 90 s and only 10 in total.

---

## 2. Scorecard

### Validated

| Prediction | Theory | Actual | |
|---|---:|---:|---|
| Melee cadence | 2.00 swings/s | 1.95 /s | ✓ |
| Spawn ramp, uncapped (slice 0) | 225 | 224 | ✓ |
| Mob coin EV 0.5/kill | 380 coins | 359 | ✓ (1.1σ) |
| Building coin EV 7/building | 70 coins | 73 | ✓ (0.7σ) |
| Attack purchases to finish the chain | 24 | 23 | ✓ |
| Fail state restored by 120 HP (§5.4) | death reachable | 2 of 3 runs died | ✓ |
| Kill fraction > 0.70 (cone is 150°, not 90°) | "probably higher" | 0.84 | ✓ |

The kill-fraction call is worth noting: the three agents guessed 0.60 / 0.80 / 0.70, and the summary
argued the true value must exceed opus's 0.70 because it had misread the melee cone as 90° when the
prefab serialises 150°. Measured: **0.84**. Sol's 0.80 was closest.

### Falsified

| Prediction | Theory | Actual | Miss |
|---|---:|---:|---|
| Buildings destroyed | ~32 | **10** (0 in first 90 s) | **3×, and mistimed** |
| Movement is roughly radial | ~1× detour | **8.6–11.5×** | model premise false |
| Damage 250 by t = 60 s | t = 60 s | t ≈ 164 s | 2.7× slow |
| Health is a "low impact" knob (§4.9) | low | **36% of all spend, maxed** | rating wrong |
| Building share of income | ~59% | **17%** | 3.5× |
| Run length | 90 s | 163.7 s | timer respawn (§5.5) |

---

## 3. The three mechanisms that broke the model

### 3.1 The mob cap became the economy's governor

The cap of 150 was added as a *performance* guard. It is now the binding constraint on income.

| Slice | Actual spawns | Uncapped nominal | Ratio |
|---:|---:|---:|---:|
| 0–30 s | 224 | 225 | **1.00** |
| 30–60 s | 92 | 375 | 0.25 |
| 60–90 s | 142 | 525 | 0.27 |
| 90–120 s | 162 | 600 | 0.27 |
| 120–150 s | 105 | 600 | 0.17 |
| **Total** | **900** | **2600** | **0.35** |

Slice 0 tracks the ramp exactly. From slice 1 onward the game delivers ~25% of the configured rate.

The cause is visible in slice 0: 224 spawned, only 85 killed (**k = 0.38**), leaving a ~139-mob
backlog that immediately pins the population at the cap. Every later slice is throttled by that
opening deficit. The player then claws it back — slice 4 shows k = 1.11, killing more than spawned
because it is draining the backlog.

**Consequence: tuning `_mobsPerSecEnd` above the cap does nothing.** Spawn rate is no longer the
lever; the cap is.

### 3.2 Health is the dominant strategy, via the hidden AoE coupling

§4.9 flagged that `CharacterScaleFactor` scales the melee radius with health level, and rated it
**"low, but a hidden coupling."** That rating was wrong — it is the strongest upgrade in the game.

The winning run bought **29 health upgrades — the maximum the level cap allows** — spending 145
coins, 36% of all spend, on a stat the theory treated as optional.

```
scale  = 1 + 29 × 0.02      = 1.58
radius = 5 × 1.58 × 1.2     = 9.48 wu   (vs 6.00 base)
area                         = 2.50×
```

And the telemetry shows it working: `meanMobsPerSwing` climbs **1.44 → 6.69** across the run. Health
buys survival, melee area, and therefore income, all at once, and nothing in the UI says so. The
player found it without being told.

### 3.3 Movement is circular, so the distance model never applied

The winning run travelled **1477 world units** and finished **129 units** from spawn — a detour ratio
of **11.5×**, consistent across all three runs (8.6–11.5×).

The 56 / 112 / 192 wu leg design assumed directed radial travel toward progressively distant tiers.
Actual play is orbiting near spawn kiting mobs. The straight-line displacement of 129 wu is
essentially just the Goal's position (132 wu) — the player got there, but by a path 11× longer.

**"Reach the next tier in under 10 seconds of walking" was never the constraint that shaped the run.**
Speed reached 23 and barely mattered.

---

## 4. Why no buildings were destroyed

This is the core failure and it follows from the above. Melee is auto-firing AoE: the player never
chooses to attack. They kite, and whatever is in the 150° cone takes damage. A building is static, so
engaging one means standing still while the swarm closes — and in the opening the swarm is already
winning (k = 0.38).

Buildings only started dying once damage was high enough to destroy them *incidentally*: 17 building
hits for 10 buildings destroyed is **1.7 hits each**, i.e. mostly one-shot T1 houses caught in a swing
aimed at mobs. The designed "5 hits, then 1 hit" tier ladder never happened at any tier.

Building income was 73 of 432 coins (**17%**), against the ~59% the theory projected.

---

## 5. Recommendations

Ranked by how much they change the next run.

1. **Fix the opening 30 seconds.** It is where everything goes wrong: k = 0.38, zero buildings, and a
   backlog that throttles the rest of the run. At damage 10 with a 6.0 wu radius, an opening rate of
   5/s is too hot. Either drop the opening rate to ~2–3/s, or raise starting damage/radius so the
   player clears the first wave. Two of three runs died here.
2. **Decide what the mob cap is for.** Right now a perf guard is silently governing the economy.
   Either raise it and let spawn rate matter again, or treat it as the real difficulty dial and stop
   tuning the ramp.
3. **Deal with the health→radius coupling.** It is currently an invisible dominant strategy. Either
   sever it, or make it explicit and price it accordingly — but do not leave it as a trap that
   rewards players who happen to notice.
4. **Make buildings worth stopping for.** 7 coins for 2.5 seconds of standing still, while mobs close,
   loses to simply swinging at the crowd. If the tier ladder is the design, buildings need
   substantially higher drops, or they need to gate progress rather than being optional scenery.
5. **Drop the radial leg model.** Detour is ~10×. Distances derived from `speed × 10 s` describe a run
   shape this game does not have. Tier gating should key off something other than radius.
6. **The 90 s clock is not holding.** The winning run lasted 163.7 s with zero timer purchases, which
   matches the §5.5 prediction that destroying and respawning the timer resets the clock to a full 90 s.

---

## 6. What to keep

The damage/HP ladder itself is sound and should not be re-derived. The attack chain needed 23
purchases against a predicted 24, the coin EVs were accurate to within noise, and the swing cadence
matched to 2.5%. The arithmetic in §3 and §4 of the summary is fine — it was the behavioural
assumptions layered on top of it that failed.
