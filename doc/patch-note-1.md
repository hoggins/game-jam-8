# Patch note 1 — post-telemetry balance pass

Derived from [`balance-run-evaluation.md`](balance-run-evaluation.md) and the three runs in
`artifacts/`. Every change below is justified by a measured number, not a model.

---

## The organising principle

The telemetry gives us something the theory never had: **the actual kill-capacity curve.**

| Player state | Melee radius | Mobs / swing | Kill capacity |
|---|---:|---:|---:|
| Base (damage 10, health 120) | 6.0 wu | 1.44 | **2.8 /s** |
| Maxed health (29 upgrades) | 9.5 wu | 6.69 | **12.7 /s** |

Note the mid-game slices: capacity and spawn rate track each other almost exactly (2.70 vs 3.07,
4.73 vs 4.73, 5.37 vs 5.40). That is a **saturated** system — the player kills precisely what
arrives, because the cap is holding the population flat. Capacity is only truly visible at the two
ends, where the player is swamped.

**Every balance decision follows from one rule: spawn rate must stay under kill capacity, or a
backlog forms and the mob cap silently takes over the economy.** The shipped 5→20 ramp violated this
at t=0 by 2.6× (7.5/s spawn against 2.8/s capacity), which is the root cause of everything that went
wrong.

---

## Changes

### 1. Melee radius 5 → 7 `BattleBalanceConfig`

`_meleeAttackRadius: 5` → **7**

The opening is unclearable at base power: 1.44 mobs per swing is simply too few against 7.5/s. Rather
than gutting the spawn rate to match a weak swing, raise the swing.

Effective radius goes 6.0 → 8.4 wu (×1.2 prefab multiplier), area ×1.96, so early mobs/swing should
land near 2.8 and capacity near **5.5/s**.

*This is the change that preserves the swarm feel. The run was fun; the fix should not be "fewer
mobs" alone.*

### 2. Spawn ramp 5→20 becomes 4→12 `BattleScene.unity`

`_mobsPerSecStart: 5` → **4**  ·  `_mobPerSecondEnd: 20` → **12**  ·  `_secondEnd: 90` (keep)

With change 1, capacity runs ~5.5/s at base and ~9–12/s once upgraded. The new ramp sits just under
that at both ends:

| Slice | New nominal spawns | Capacity |
|---|---:|---:|
| 0–30 s | 160 | ~165 |
| 30–60 s | 240 | rising |
| 60–90 s | 320 | ~12/s |

20/s was never reachable — measured peak capacity is 12.7/s, and only with health fully maxed.

### 3. Per-difficulty building drops — **new field** `BattleBalanceConfig`

Add `_buildingCoinDropByDifficulty`, mirroring the existing `_houseMaxHealthByDifficulty`:

| Difficulty | HP | Coins |
|---|---:|---:|
| 1 | 50 | **12** |
| 2 | 250 | **40** |
| 3 | 1250 | **120** |

Replaces the flat 5–9 roll for houses. This is the single biggest cause of the tier loop never
engaging: **7 coins for 2.5 seconds of standing still, while the swarm closes, loses to just swinging
at the crowd.** At 12 coins a T1 house pays ~4.8 coins/s against the crowd's ~2.4 — now worth
stopping for, and the reward scales with the tier ladder as the design always intended.

*(The summary proposed 7/20/50 per tier; it was never implemented. These are higher because the
telemetry showed buildings losing the trade badly at 7.)*

### 4. Health repricing and decoupling `ProgressionBalanceConfig`

`_maxHealthUpgradeCost: 5` → **10**  ·  `_maxHealthScalePerLevel: 0.02` → **0.01**

Health was the cheapest upgrade in the game (5, against attack 7 and speed 6) *and* bought survival
*and* bought 2.5× melee area — therefore income. The player maxed it: **29 of 29 possible purchases,
36% of all spend**, on a stat the theory treated as optional.

Halving the scale gives ×1.29 at max instead of ×1.58 (area ×1.66, not ×2.50), and doubling the cost
makes it compete with attack. The coupling stays as a real build choice; it stops being a free
dominant strategy.

### 5. Flat tier distribution `HouseSet.asset`

Replace the radial `difficultyRanges` bands with flat weights: **T1 50% / T2 35% / T3 15%**.

This is the decision that has been pending. Measured detour ratio is **8.6–11.5×** — the player
orbits near spawn rather than travelling outward, so radial banding describes a run shape this game
does not have. T1-heavy weighting keeps the opening viable, since a 250 HP T2 house is 25 hits at
damage 10 and acts as a soft wall until attack comes up.

### 6. Timer respawn preserves remaining time `BattleService`

The winning run lasted **163.7 s on a 90 s clock with zero timer purchases**. Destroying and
respawning the timer resets it to the full duration. Respawn should restore the remaining time, not
reset the clock.

### 7. Mob cap stays at 150 — but stops being the governor

No value change. With changes 1 and 2 the population should no longer saturate, returning the cap to
its intended role as a pure performance guard.

**Diagnostic:** in the next run, compare actual spawns per slice against the nominal ramp. If any
slice reads below ~0.9 of nominal, the cap is *still* governing the economy and spawn-rate tuning is
a no-op. Last run read 0.25–0.27 across the middle slices.

---

## Not changing

The damage/HP ladder is validated and should be left alone. The attack chain needed 23 purchases
against a predicted 24, coin EVs matched within noise (mob 359 vs 380 expected, building 73 vs 70),
and swing cadence matched to 2.5%. **The arithmetic was never the problem** — the behavioural
assumptions layered on top of it were.

Mob coin EV (0.5/kill) also stays put this pass, deliberately: changing income on both sides at once
would make the next run uninterpretable.

---

## Predicted outcomes — check these against run 4

| Metric | Last run | Target |
|---|---:|---:|
| Slice-0 kill fraction | 0.38 | **> 0.80** |
| Spawns actual ÷ nominal, all slices | 0.25–0.27 | **> 0.90** |
| Buildings destroyed | 10 | **> 25** |
| Buildings destroyed in first 90 s | **0** | **> 8** |
| Building share of income | 17% | **> 40%** |
| Health purchases | 29 (capped out) | **12–18** |
| Run duration | 163.7 s | **~90 s** |
| Deaths in opening 30 s | 2 of 3 runs | rare |

If buildings are still not being destroyed after change 3, the problem is not price — it is that
nothing *requires* the player to engage them, and the next lever is making them gate progress rather
than being optional scenery.

If total income exceeds roughly 2× mandatory spend (~400 coins), trim mob EV from 0.5 to 0.3 on the
following pass rather than touching building drops again.
