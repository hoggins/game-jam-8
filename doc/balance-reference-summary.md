# Balance reference — consolidated summary

Synthesis of three independent analyses of the tier/time/distance balance design:

- [`balance-reference-luna.md`](balance-reference-luna.md) — codex `gpt-5.6-luna`, effort max
- [`balance-reference-sol.md`](balance-reference-sol.md) — codex `gpt-5.6-sol`, effort high
- [`balance-reference-opus.md`](balance-reference-opus.md) — claude `opus`, effort medium

Every repo value quoted below was re-verified directly against the assets, not taken on trust
from the agent reports. Where the three disagreed, the disagreement is called out in §6.

---

## 1. Headline

The design is arithmetically coherent but **structurally unimplementable today**. Three separate
analyses converged on the same conclusion for the same reason: the stated hit pattern forces a
**×5 damage increase per tier** (×25 across the run), while the shipped upgrade system is
**linear and hard-capped at +29 damage total**, and there is **no tier concept in the codebase
at all** to hang per-tier HP on.

The numbers in §4 are worth adopting, but they do nothing until the four blockers in §5 are built.

---

## 2. Verified ground truth

Confirmed by direct inspection:

| Fact | Value | Consequence |
|---|---|---|
| `_startingAttackPower` | 50 | vs House HP 20 → the player **one-shots every building at t=0**. The design wants 5 hits. |
| `House` max health | 20 | one global value for all difficulty levels |
| `House01`, `House01_D2`, `House01_D3` | all `_objectType: 0` | **T1/T2/T3 are art labels only — all three are 20 HP** |
| `_attackPowerUpgradeAmount` / `_maxUpgradeLevel` | 1 / 30 | max **+29 damage, ever** |
| `_attackCooldown` (melee prefab) | 0.5 s | 2 swings/s, auto-fired, no input |
| `_attackConeAngle` (melee prefab) | **150°** | *not* the 90° C# default — see §6 |
| `_meleeAttackRadius` × 1.2 | 6.0 wu effective | one swing hits every mob and destructible in the cone |
| `_duckMaxHealth` / `_duckAttackDamage` | 6 / 1 | a mob attacks **exactly once, ever** (`_hasAttacked`) |
| `_startingMaxHealth` | 1000 | death is arithmetically unreachable — see §5.4 |
| Mob spawn ramp | 1 → 15 /s over 0–60 s | not 5/10/20; one linear ramp, scene-serialized |
| `_duckCoinDropChances` | `[0.6, 0.3, 0.1]` | EV **0.5 coins/kill** |
| Building drop | 5–9 @ 100% | EV **7 coins**, one global value for every destructible |
| `_battleDuration` | 90 s | one clock; no 30 s stage boundaries exist |
| Goal position | (4, 0, 132) → **132 wu** | design needs ~360 wu (§4.3) |
| `MapData.cellSize` | 8 | `HouseSet` bands are in **cells, not world units** — an 8× trap |
| `Storage.cs` | persists AttackPower, Speed, Coins to `PlayerPrefs` | tier curve is only true on a fresh save |

`GameBalanceWindow` itself is a pure inspector wrapper over eight assets — it computes nothing and
adds no fields. It is a viewing surface, not a balance model.

---

## 3. The forced arithmetic — and the one clean solution

All three agents independently derived the same constraint. With `D_N` = player damage at tier N
and `H_T` = HP of a tier-T building, the two stated rules are:

```
H_(N+1) = 5·D_N      (5 hits to kill this tier's target building)
H_N     = 1·D_N      (1 hit to kill last tier's building)
```

Substituting gives **`D_(N+1) = 5·D_N`** — damage must quintuple every tier, so ×25 across the run.

**The non-obvious consequence, and the key planning result:** if you set the upgrade step equal to
the starting damage (`step = D_0`), the required purchase count is **exactly 24, independent of
`D_0`**:

```
D_0 → 5·D_0   costs  4 purchases
5·D_0 → 25·D_0 costs 20 purchases
                     ---
                     24 purchases → level 25, fits the existing cap of 30
```

This is why the anchor value is free and the *step* is what matters. Sol and opus both landed on
this; luna picked `step = 4·D_0`, which needs 31 purchases and forces the level cap up to 32 —
which in turn silently permits speed 37, since the cap is shared across all stats.

---

## 4. Recommended starting values

Anchoring at `D_0 = 10`, `step = +10`. This keeps mobs (6 HP) a one-shot at every tier and keeps
every number round.

### 4.1 Combat chain

| | Tier 0 | Tier 1 | Tier 2 |
|---|---:|---:|---:|
| Player damage | **10** | **50** | **250** |
| Attack rate | 2 /s | 2 /s | 2 /s |
| Single-target DPS | 20 | 100 | 500 |
| Move speed | **6** | **12** | **20** |
| T1 building HP | **50** | 50 | 50 |
| T2 building HP | **250** | 250 | 250 |
| T3 building HP | **1250** | 1250 | 1250 |
| Hits — current tier target | 5 | 5 | 5 |
| Hits — previous tier | — | 1 | 1 |
| Mob spawn rate | **5** /s | **10** /s | **15 → 20** /s |
| Mob HP / damage | 6 / 1 | 6 / 1 | 6 / 1 |

Verification: T1 `50/10 = 5` hits ✓, then `50/50 = 1` ✓. T2 `250/50 = 5` ✓, then `250/250 = 1` ✓.
T3 `1250/250 = 5` ✓.

### 4.2 Config deltas

`ProgressionBalanceConfig.asset`:

| Field | Now | → | Rationale |
|---|---:|---:|---|
| `_startingAttackPower` | 50 | **10** | anchor of the ×5 chain |
| `_attackPowerUpgradeAmount` | 1 | **10** | `step = D_0` → exactly 24 purchases (§3) |
| `_startingMaxHealth` | 1000 | **120** | restores a fail state (§5.4) |
| `_maxHealthUpgradeAmount` | 1 | **10** | +1 against 120 max is noise |
| `_attackPowerUpgradeCost` | 7 | 7 (keep) | affordable — see §4.4 |
| `_speedUpgradeCost` / `Amount` | 6 / 1 | keep | 14 purchases gets 6 → 20 |
| `_maxUpgradeLevel` | 30 | 30 (keep) | attack needs 25, speed 15 — both fit |

`BattleBalanceConfig.asset`: `House` 20 → **50** (becomes the T1 value); `Goal` 100 → **1250**
(matches T3, a 5-hit finale; use 2500 if you want a 10-hit boss). T2/T3 house HP have nowhere to
live yet — see §5.1.

`BattleScene.unity` MobSpawner: `_mobsPerSecStart` 1 → **5**, `_mobPerSecondEnd` 15 → **20**,
`_secondEnd` 60 → **90**. That yields r(0)=5, r(30)=10, r(60)=15, r(90)=20 — hitting the stated
rates exactly at the tier boundaries.

### 4.3 Distances

Legs are grid-snapped to `cellSize = 8` and sit just under the stated 10 s limit:

| Leg | Distance | Cells | Speed | Walk time |
|---|---:|---:|---:|---:|
| Start → T1 | **56 wu** | 7 | 6 | 9.33 s |
| T1 → T2 | **112 wu** | 14 | 12 | 9.33 s |
| T2 → T3 | **192 wu** | 24 | 20 | 9.60 s |
| **Cumulative** | **360 wu** | 45 | | |

**The Goal must move from z = 132 to ≈ 360 wu.** The map is currently ~2.7× too short: at speed 6
the player reaches the existing Goal in 22 s, still inside Tier 0, before buying anything.

`HouseSet` bands then want rewriting around 7 / 21 / 45 cells to match.

### 4.4 Economy check

Mandatory spend, all of which must be paid by t = 60:

```
attack: 24 purchases × 7 = 168 coins
speed:  14 purchases × 6 =  84 coins
                           ---
                           252 coins
```

Mob spawns by t = 60 under the new ramp: `mean(5,15) × 60 = 600`. At 0.5 coins/kill, mob income is
`600 × k × 0.5 = 300k`, where `k` is the kill fraction. Buildings must supply the rest:

| Kill fraction `k` | Mob coins by t=60 | Shortfall to cover from buildings | ≈ T1 buildings @ 7 |
|---:|---:|---:|---:|
| 0.50 | 150 | 102 | 15 |
| 0.60 | 180 | 72 | 11 |
| 0.70 | 210 | 42 | 6 |
| 0.84 | 252 | 0 | 0 |

So the economy funds itself from about `k ≥ 0.5` given a reasonable number of buildings on the
route. This is the most comfortable of the three agents' economy models — sol's variant left only
a **3-coin margin** at Tier 1, which is roughly 0.2 standard deviations of drop variance and would
fail in practice.

Per-tier building drops (T1 = 7, T2 = 20, T3 = 50) would widen the margin further, but require a
per-tier drop field that does not exist.

---

## 5. Blockers — build these before the numbers mean anything

### 5.1 There is no per-tier building HP (blocking, verified)

`DestructibleHealth.Awake()` keys HP off `DestructibleObjectType`, and all three house prefabs
carry `_objectType: 0`. **Every building in the world has 20 HP regardless of difficulty level.**
The entire 5-hit/1-hit structure — the thing the whole design rests on — has nowhere to live.

Failure mode: the moment the player buys attack, *every* building including ones 360 wu away
becomes a one-shot. The tier ladder collapses to a single step.

Fix: either a per-difficulty-level HP table on `HouseSet`, or new `HouseT2` / `HouseT3` entries in
`DestructibleObjectType`.

### 5.2 Attack upgrades cannot reach the required damage (blocking, arithmetic-certain)

With `_attackPowerUpgradeAmount = 1` and `_maxUpgradeLevel = 30`, the player gains **at most +29
damage over the entire game**. From `D_0 = 10` they top out at 39 — a 1250 HP T3 building would
take 32 hits (16 s). Tier 2 never starts.

Fixed by `step = 10` (§4.2), but note this is only *barely* solvable linearly: 24 of 29 available
levels. Any fourth tier breaks it. The durable fix is a multiplicative or per-level-scaling
upgrade, which the config cannot currently express.

### 5.3 Meta-progression destroys the tier structure on run 2 (blocking, design-level)

`Storage.cs` persists AttackPower, Speed and Coins. The tier design is a *within-battle* curve, so
run 2 starts at D = 250 / speed 20 with a full bank: everything is a one-shot from t = 0 and the
run collapses to a sprint.

**This is a design decision only you can make.** Either reset combat stats each battle and make the
meta something else (cosmetics, starting coins, timer), or re-express tiers as per-*run* milestones
rather than a per-battle curve.

### 5.4 The player cannot die (high)

1000 HP, mob damage 1, and each mob attacks exactly once. Even if all ~1125 mobs in a run landed
their single hit, that is 1125 damage; realistic leakage is ~10%, so ~110. Death is unreachable,
the only fail state is the clock, and `MaxHealth` upgrades are worthless. 120 HP makes a ~30% leak
rate lethal.

### 5.5 No stage transitions (high)

One 90 s clock, no tier state, no wave counter. House difficulty varies by *distance from origin*;
mob rate varies by *elapsed time*; the two are not linked to each other or to anything else. Three
clean 30 s phases cannot be observed or enforced today. Destroying and respawning the timer object
also resets the clock to the full 90 s, so a run can exceed 90 s while difficulty keeps advancing.

### 5.6 No mob cap (medium → performance cliff)

`MobSpawner.Update()` spawns unconditionally. Mobs beyond 40 wu are **teleported back to the
player, never despawned**. At the proposed ramp a run spawns ~1125 mobs; at `k = 0.7` that leaves
~340 live agents permanently orbiting the player by t = 90, all flow-mapped and flocked. A player
who under-kills does not get a harder game — they get a frame-rate collapse. **Add a live-mob cap
before touching the rate numbers.**

### 5.7 Buying health silently buys AoE (low, but a hidden coupling)

`CharacterScaleFactor = 1 + (MaxHealthLevel − 1) × 0.02`, and melee radius scales with it. Health
upgrades enlarge the attack radius — at 29 levels that is +58% radius, ~2.5× swept area, and
therefore ~2.5× mob income. Health is secretly the best economy upgrade in the game and nothing
says so.

---

## 6. Where the three analyses diverged

| | luna (max) | sol (high) | opus (medium) |
|---|---:|---:|---:|
| `D_0` | 5 | 4 | **10** |
| Upgrade step | +20 | +4 | **+10** |
| T1/T2/T3 HP | 25 / 125 / 625 | 20 / 100 / 500 | **50 / 250 / 1250** |
| Attack purchases | 31 (needs cap **32**) | 24 | 24 |
| Speed path | 6→9→14→20 | 6→12→20 | 6→12→20 |
| Legs (wu) | 80 / 80 / 80 | **56 / 112 / 192** | 60 / 120 / 200 |
| Total run | ~240 wu | 360 wu | 380 wu |
| Kill fraction | 60% | 80% | 70% |

Judgements made in this synthesis:

- **Damage chain from opus.** `D_0 = 10` keeps mobs (6 HP) a one-shot at every tier. Sol's `D_0 = 4`
  preserved the existing House HP of 20 but then needed *invented* mob HP of 4 to stay one-shot.
- **Distances from sol.** 56/112/192 are integer cell counts *and* strictly under 10 s. Opus's
  60/120/200 are exactly 10 s (violating "less than") and 60 wu is 7.5 cells, off-grid.
- **Luna's step is suboptimal.** `step = 4·D_0` needs 31 purchases and a cap raise to 32, which
  also silently permits speed 37 because the cap is shared. Luna correctly flagged this itself.

**One factual error worth knowing:** opus reports the melee cone as **90°**, reading the C# field
initialiser in `MeleeWeapon.cs:16`. The prefab serializes **150°**, and the serialized value is what
runs. The real cone is 1.67× wider than opus assumed, so its AoE reasoning in §4.9 and its
kill-fraction justification are conservative — the true kill fraction is probably *higher* than
0.70. Sol had this right.

---

## 7. What to measure before trusting any coin number

**The kill fraction `k` is invented in all three reports, at three different values (0.6 / 0.7 /
0.8), and it is the single largest lever on the economy.** Mob income scales linearly with it, and
both the survivability model (§5.4) and the mob-cap cliff (§5.6) scale with `1 − k`.

It is cheap to instrument: `Storage.DucksKilled` already counts kills, and total spawns are a
closed-form integral of the spawn ramp. Log both at battle end.

Alongside it, log per 30 s slice:

1. Successful spawns vs. kills → the real `k`
2. Mean mobs hit per swing → whether the AoE assumption holds at each density
3. Coins earned and spent
4. Straight-line vs. actual path distance → whether a 56 wu leg is really a 56 wu walk
5. Time consumed by the upgrade interaction (the shop sets `Time.timeScale = 0`, so it currently
   costs no battle clock — decide whether that is intended)

Do not fine-tune prices or drop rates until (1) and (2) are measured. Everything else in §4 is
arithmetic and will hold; the economy table is the only part resting on a guess.

---

## 8. Suggested order of work

1. **Mob cap** — cheap, prevents a perf cliff that would corrupt every subsequent measurement.
2. **Instrument `k`** (§7) — one battle-end log line.
3. **Decide the persistence question** (§5.3) — it is a design call, and it changes what "tier" means.
4. **Per-tier building HP** (§5.1) — the actual blocker for the whole design.
5. **Apply §4.2 config deltas** and move the Goal to ~360 wu.
6. **Re-measure, then tune prices and drops** against real `k`.
