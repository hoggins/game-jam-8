# Balance Reference — tier/time/distance model

Scope: turn the stated design (Tier 0/1/2, 30 s each, 10 s walk between building tiers,
mobs 5/10/20 per sec) into concrete numbers, grounded in what the project actually ships today.

Everything in §1 was read out of the repo. Numbers in §2 that have no backing field are
marked **INVENTED**. `.asset` values beat C# field initialisers everywhere — the serialized
assets are what runs.

---

## 1. Evidence — what knobs exist today

### 1.1 Reachable from `GameBalanceWindow`

`Assets/Scripts/Balance/Editor/GameBalanceWindow.cs` is a pure wrapper: it finds one asset per
type via `AssetDatabase.FindAssets($"t:{section.AssetType.Name}")` (line 83), draws the default
Inspector through `Editor.CreateEditor()` (line 90), and `SetDirty`s on change (line 145). It
computes and derives **nothing** — no DPS, no time-to-kill, no income readout. It surfaces eight
assets in three groups:

| Group | Section title | Asset type |
|---|---|---|
| Hero | Hero Progression | `ProgressionBalanceConfig` |
| Hero | Battle & Mobs | `BattleBalanceConfig` |
| Map / Spawning | Special Object Spawn Ranges | `SpecialSpawnSettings` |
| Map / Spawning | House Difficulty Selection | `HouseSet` |
| Systemic | Movement Tuning | `MovementSettings` |
| Systemic | Environment Decay | `EnvironmentDecaySettings` |
| Systemic | Environment Visibility | `EnvironmentVisibilitySettings` |
| Systemic | Hit FX | `HitFxSettings` |

### 1.2 Player / progression

| Field | File | Current value | Unit |
|---|---|---|---|
| `_startingAttackPower` | `Assets/Resources/ProgressionBalanceConfig.asset` | **50** | damage / hit |
| `_startingMaxHealth` | same | **1000** | hp |
| `_startingSpeed` | same | **6** | world units / s |
| `_startingGunPower` | same | **0** | bullets per shot |
| `_startingTimer` | same | **0** | s added to clock |
| `_attackPowerUpgradeCost` | same | **7** | coins (flat, never scales) |
| `_maxHealthUpgradeCost` | same | **5** | coins (flat) |
| `_speedUpgradeCost` | same | **6** | coins (flat) |
| `_gunPowerUpgradeCost` | same | **15** | coins (flat) |
| `_timerUpgradeCost` | same | **10** | coins (flat) |
| `_attackPowerUpgradeAmount` | same | **1** | damage / purchase |
| `_maxHealthUpgradeAmount` | same | **1** | hp / purchase |
| `_speedUpgradeAmount` | same | **1** | u/s / purchase |
| `_gunPowerUpgradeAmount` | same | **1** | bullets / purchase |
| `_timerUpgradeAmount` | same | **1** | s / purchase |
| `_maxUpgradeLevel` | same | **30** | ⇒ 29 purchases per stat |
| `_maxHealthScalePerLevel` | same | **0.02** | character scale / health level |

Cost is deducted flat in `CharacterService.UpgradeAttackPower()` etc.
(`Assets/Scripts/Model/CharacterService.cs:147-216`) — `_storage.CurrentCoins -= cost`.
There is **no cost curve of any kind**: the 29th purchase costs the same as the 1st.

Level is derived, not stored: `GetLevel(value, starting, amount) = (value - starting)/amount + 1`,
clamped to `[1, MaxUpgradeLevel]` (`CharacterService.cs:70-77`).

### 1.3 Combat

| Field | File | Current value | Unit |
|---|---|---|---|
| `_attackCooldown` (melee) | `Assets/Resources/Prefabs/Weapons/MeleeWeapon.prefab:47` | **0.5** | s ⇒ **2 swings/s** |
| `_attackCooldown` (ranged) | `Assets/Resources/Prefabs/Weapons/RangeWeapon.prefab:47` | **0.25** | s ⇒ 4 shots/s |
| `_meleeAttackRadius` | `Assets/Resources/BattleBalanceConfig.asset` | **5** | world units |
| `_attackScaleMultiplier` | `Assets/Scripts/Weapons/MeleeWeapon.cs:16` | **1.2** | ⇒ effective radius **6.0 wu** |
| `_attackConeAngle` | `Assets/Scripts/Weapons/MeleeWeapon.cs:15` | **90** | degrees, forward only |

`Weapon.Update()` calls `TryAttack()` unconditionally (`Assets/Scripts/Weapons/Weapon.cs:46-48`) —
the player **auto-swings every 0.5 s**, no input, no aiming beyond facing. One swing hits *every*
mob and *every* destructible inside the radius+cone (`MeleeWeapon.cs:64-114`). Damage is applied
1:1 with no multiplier: `Attack(_playerService.AttackPower)` (`Weapon.cs:58`).

### 1.4 Mobs

| Field | File | Current value | Unit |
|---|---|---|---|
| `_duckMaxHealth` | `Assets/Resources/BattleBalanceConfig.asset` | **6** | hp |
| `_duckAttackDamage` | same | **1** | damage |
| `_duckAttackDistance` | same | **3** | world units |
| `_duckRepositionDistance` | same | **40** | world units |
| `_speed` (mob) | `Assets/Prefabs/Mob.prefab:64` | **4** | world units / s |
| `_mobsPerSecStart` | `Assets/Scenes/BattleScene/BattleScene.unity:446` | **1** | mobs / s |
| `_mobPerSecondEnd` | same:447 | **15** | mobs / s |
| `_secondStart` / `_secondEnd` | same:448-449 | **0** / **60** | s |

Spawn rate is `Mathf.Lerp(1, 15, InverseLerp(0, 60, t))` (`MobSpawner.cs:49-58`) — linear ramp
1 → 15 over the first 60 s, then flat at 15 for the remaining 30 s of the 90 s battle.

A duck attacks **exactly once, ever**: `_hasAttacked` (`Mob.cs:63`) is set on the first attack and
only reset in `OnEnable` (`Mob.cs:88`), i.e. on pool reuse. There is no attack cooldown.

### 1.5 Buildings and world

| Field | File | Current value | Unit |
|---|---|---|---|
| `House` max health | `Assets/Resources/BattleBalanceConfig.asset` | **20** | hp |
| `TimerDigit` | same | **15** | hp |
| `TimerDivider` | same | **10** | hp |
| `Arrow` | same | **12** | hp |
| `HealthBar` | same | **48** | hp |
| `Upgrade` | same | **20** | hp |
| `Goal` | same | **100** | hp |
| `cellSize` | `Assets/Scripts/Map/MapData.cs:17` + `BattleScene/MapData.asset` | **8** | world units |
| `DifficultyLevelCount` | `Assets/Scripts/Map/HouseSet.cs:25` | **5** | (only 1–3 populated) |
| Player spawn | `BattleScene.unity` | **(0, 0, 0)** | world units |
| `TheGoal` position | `BattleScene.unity:~373` | **(4, 0, 132)** | ⇒ **132 wu** from spawn |
| `_battleDuration` | `Assets/Resources/BattleBalanceConfig.asset` | **90** | s |

House difficulty bands, `Assets/Resources/Map/HouseSet.asset` — **`minDistance`/`maxDistance` are
in GRID CELLS, not world units**: `MapFiller.cs:63` computes
`Vector2.Distance(cell, originCell)` on integer cell coordinates. Multiply by 8 for world units.

| Band (cells) | Band (world units) | Level weights |
|---|---|---|
| 0 – 5 | 0 – 40 | L1 100% |
| 5 – 12 | 40 – 96 | L1 70%, L2 30% |
| 12 – 18 | 96 – 144 | L1 20%, L2 50%, L3 30% |
| 18 – 1000 | 144 – 8000 | L2 40%, L3 60% |

### 1.6 Economy

| Field | File | Current value | Expected value |
|---|---|---|---|
| `_duckCoinDropChances` | `BattleBalanceConfig.asset` | `[0.6, 0.3, 0.1]` for 0/1/2 coins | **0.5 coins / kill** |
| `_buildingCoinDropMin` / `Max` | same | **5** / **9** | |
| `_buildingCoinDropChance` | same | **1.0** | **7.0 coins / building** |
| `_buildingCoinDropDistance` | same | **1** | world units (cosmetic) |

Coins are credited **immediately** on kill/destruction (`CharacterService.cs:116-145`); the
`CoinPickup` flight is purely cosmetic. There is no pickup radius and no magnet.

### 1.7 Persistence

`Assets/Scripts/Model/Storage.cs:11-19` writes `AttackPower`, `MaxHealth`, `Speed`, `GunPower`,
`Timer` and `CurrentCoins` to `PlayerPrefs`. **Upgrades and coins persist across battles.**

### 1.8 Quantities in the stated design that have NO backing field

| Design quantity | Status |
|---|---|
| **Per-tier building HP (T1 / T2 / T3)** | **Does not exist.** `DestructibleHealth.Awake()` does `_maxHealth = _battleBalance.GetDestructibleMaxHealth(_objectType)` (`DestructibleHealth.cs:35`), keyed on `DestructibleObjectType` only. `House01`, `House01_D2`, `House01_D3` all carry `_objectType: 0` (verified in all three prefabs) ⇒ **all three difficulty levels have 20 HP**. D2/D3 differ in art and breakable-part count only. |
| **A "Tier" / stage concept** | **Does not exist.** No `tier`, `stage` or `wave` type anywhere. The only two things that vary with progress are (a) house *art* level, chosen by distance from origin, and (b) mob spawn rate, chosen by elapsed time. They are not linked to each other or to anything else. |
| **Per-tier mob HP / damage / speed** | Does not exist. Single global value each, constant for the whole battle. |
| **Per-tier mob spawn rate** | Not expressible. `SpawnItem` supports one linear ramp between two rates; three discrete tier rates need a step table. |
| **Per-tier coin drop** | Does not exist. One global 5–9 roll for every destructible, from a shack to the Goal. |
| **Upgrade cost curve** | Does not exist; cost is flat per stat. |
| **Mob spawn cap** | Does not exist. `MobSpawner.Update()` spawns unconditionally; mobs beyond 40 wu are teleported back, never despawned. |
| **Attack rate as a balance knob** | Lives on weapon prefabs, not in any config, not in `GameBalanceWindow`. |
| **Mob speed as a balance knob** | Lives on `Mob.prefab`, same problem. |
| **Player i-frames** | Do not exist. `CharacterService.TakeDamage` has no cooldown; `IsInvincible` is only set by `DestroyHealth()`. |

---

## 2. Proposed starting values

Anchor choice: **player damage must go ×5 per tier**, because "one-shot the previous tier's
building, 5 hits for this tier's" forces `D_(N+1) = 5·D_N` (derived in §3.1). I anchor
`D_0 = 10` — a clean base that keeps the ×5 chain in round numbers and keeps ducks a one-shot
throughout.

### 2.1 Per-tier targets

| | Tier 0 (0–30 s) | Tier 1 (30–60 s) | Tier 2 (60–90 s) |
|---|---|---|---|
| Player damage / hit | **10** | **50** | **250** |
| Attack rate | 2 /s (unchanged) | 2 /s | 2 /s |
| **Player DPS (single target)** | **20** | **100** | **500** |
| Move speed | **6** (unchanged) | **12** | **20** (as stated) |
| T1 building HP | **50** | 50 | 50 |
| T2 building HP | **250** | 250 | 250 |
| T3 building HP | **1250** | 1250 | 1250 |
| Hits to kill current-tier building | 5 (T1) | 5 (T2) | 5 (T3) |
| Hits to kill previous-tier building | — | 1 (T1) | 1 (T2) |
| Mob spawn rate at tier start | **5** /s | **10** /s | **15** /s (→20 at 90 s) |
| Mob HP | **6** (unchanged) | 6 | 6 |
| Mob damage | **1** (unchanged) | 1 | 1 |
| Coins per mob (EV) | 0.5 (unchanged) | 0.5 | 0.5 |
| Coins per building (EV) | **7** (T1, unchanged) | **20** (T2) | **50** (T3) |

Building HP T1/T2/T3 and per-tier building coin values are **INVENTED** — no field exists to
hold them (§1.8). They require a new per-difficulty-level HP and drop lookup.

### 2.2 Config changes

`ProgressionBalanceConfig.asset`:

| Field | Now | Proposed | Why |
|---|---|---|---|
| `_startingAttackPower` | 50 | **10** | anchor of the ×5 chain |
| `_startingMaxHealth` | 1000 | **120** | 1000 is unkillable — see §4.4 |
| `_startingSpeed` | 6 | **6** (keep) | already the tier-0 target |
| `_attackPowerUpgradeAmount` | 1 | **10** | with step 1, reaching 250 needs 240 purchases against a 29-purchase cap — impossible (§4.1) |
| `_attackPowerUpgradeCost` | 7 | **7** (keep) | affordable at the derived income (§3.4) |
| `_speedUpgradeAmount` | 1 | **1** (keep) | 14 purchases gets 6 → 20 |
| `_speedUpgradeCost` | 6 | **6** (keep) | |
| `_maxHealthUpgradeAmount` | 1 | **10** | +1 hp against 120 max is noise |
| `_maxHealthUpgradeCost` | 5 | **10** | |
| `_timerUpgradeAmount` | 1 | **5** | +1 s against a 90 s clock is noise |
| `_timerUpgradeCost` | 10 | **10** (keep) | |
| `_gunPowerUpgradeAmount` / `Cost` | 1 / 15 | keep | |
| `_maxUpgradeLevel` | 30 | **30** (keep) | attack needs 25, speed needs 15 — both fit |

`BattleBalanceConfig.asset`:

| Field | Now | Proposed | Why |
|---|---|---|---|
| `House` max health | 20 | **50** | becomes the T1 value; T2/T3 need new entries |
| `Goal` max health | 100 | **2500** | at D=250 the current Goal is a single swing |
| `Upgrade` max health | 20 | **50** | track T1 |
| `_duckMaxHealth` | 6 | **6** (keep) | one-shot at every tier, by design |
| `_duckAttackDamage` | 1 | **1** (keep) | |
| `_meleeAttackRadius` | 5 | **5** (keep) | 6.0 wu effective |

`BattleScene.unity` MobSpawner:

| Field | Now | Proposed |
|---|---|---|
| `_mobsPerSecStart` | 1 | **5** |
| `_mobPerSecondEnd` | 15 | **20** |
| `_secondStart` | 0 | **0** |
| `_secondEnd` | 60 | **90** |

Gives r(0)=5, r(30)=10, r(60)=15, r(90)=20 — hits the stated 5/10/20 at tier boundaries.

`HouseSet.asset` difficulty bands — rewritten in **cells** to match the distances derived in §3.2:

| Band (cells) | Band (wu) | Weights |
|---|---|---|
| 0 – 7.5 | 0 – 60 | L1 100% |
| 7.5 – 22.5 | 60 – 180 | L1 30%, L2 70% |
| 22.5 – 1000 | 180 – 8000 | L2 30%, L3 70% |

`BattleScene.unity` — move `TheGoal` from **z = 132** to **z ≥ 380** (§3.2).

---

## 3. Derivation

### 3.1 Why damage must go ×5 per tier

Let `D_N` = player damage at tier N, `H_T` = HP of a tier-T building.
The two stated rules per tier N are:

- 5 hits to kill the *current* tier's building: `H_(N+1) = 5·D_N`
- 1 hit to kill the *previous* tier's building: `H_N = 1·D_N`

Substituting the second into the first one tier later: `H_(N+1) = D_(N+1)` and `H_(N+1) = 5·D_N`,
therefore

```
D_(N+1) = 5 · D_N
```

With `D_0 = 10`:

```
D_0 = 10    H_T1 = 5 · 10  =   50
D_1 = 50    H_T2 = 5 · 50  =  250
D_2 = 250   H_T3 = 5 · 250 = 1250
```

Check: at tier 1, `H_T1 / D_1 = 50 / 50 = 1` hit ✓. At tier 2, `H_T2 / D_2 = 250 / 250 = 1` ✓.

### 3.2 Distances from the 10-second walk rule

The player walks for 10 s at the speed they hold during that tier:

```
Tier 0 gap =  6 u/s × 10 s =  60 wu   (=  7.5 cells)
Tier 1 gap = 12 u/s × 10 s = 120 wu   (= 15.0 cells)
Tier 2 gap = 20 u/s × 10 s = 200 wu   (= 25.0 cells)
                             -------
Total run                    380 wu   (= 47.5 cells)
```

Cumulative distance at each tier boundary: 60 wu, 180 wu, 380 wu. Those are the numbers the
`HouseSet` bands in §2.2 encode (divided by cellSize = 8).

**The Goal currently sits at 132 wu.** The design needs 380. The map is 2.9× too short.

### 3.3 Time budget — 30 s per tier

Melee is AoE and auto-fires every 0.5 s, so mob-killing overlaps with building-hitting whenever
both are in the 6.0 wu / 90° cone. The budget below counts *dedicated* seconds and states the
overlap explicitly.

**Tier 0** — speed 6, D = 10, T1 buildings at 50 HP (5 hits = 2.5 s each):

| Activity | Seconds | Arithmetic |
|---|---|---|
| Travel | 10.0 | 60 wu ÷ 6 u/s |
| Demolish 6 × T1 | 15.0 | 6 × 5 hits × 0.5 s |
| Mob-only combat + repositioning | 5.0 | remainder |
| **Total** | **30.0** | ✓ |

**Tier 1** — speed 12, D = 50, T2 at 250 HP (5 hits), T1 now 1 hit:

| Activity | Seconds | Arithmetic |
|---|---|---|
| Travel | 10.0 | 120 wu ÷ 12 u/s |
| Demolish 5 × T2 | 12.5 | 5 × 5 hits × 0.5 s |
| Mop up 8 × T1 (1 hit each) | 4.0 | 8 × 1 hit × 0.5 s |
| Mob-only combat + repositioning | 3.5 | remainder |
| **Total** | **30.0** | ✓ |

**Tier 2** — speed 20, D = 250, T3 at 1250 HP (5 hits), T2 now 1 hit:

| Activity | Seconds | Arithmetic |
|---|---|---|
| Travel | 10.0 | 200 wu ÷ 20 u/s |
| Demolish 5 × T3 | 12.5 | 5 × 5 hits × 0.5 s |
| Mop up 8 × T2 (1 hit each) | 4.0 | 8 × 1 hit × 0.5 s |
| Goal (2500 HP ÷ 250 = 10 hits) | 5.0 | 10 × 0.5 s |
| Mob-only combat | -1.5 | **overruns** — see §4.6 |
| **Total** | **30.0** | ✓ only if the Goal fight replaces mob time |

### 3.4 Income vs. required spend

Spawn rate `r(t) = 5 + 15·(t/90)`. Mobs spawned per tier = mean rate × 30 s:

```
Tier 0:  mean(5, 10) × 30 =  7.5 × 30 = 225 spawned
Tier 1:  mean(10, 15) × 30 = 12.5 × 30 = 375 spawned
Tier 2:  mean(15, 20) × 30 = 17.5 × 30 = 525 spawned
```

Kill fraction `k = 0.70` — **INVENTED**; nothing in the codebase measures this. Justification:
one-shot kills, 6.0 wu AoE, 2 swings/s vs. mobs closing at 4 u/s that must reach 3 wu to attack.

```
Tier 0 kills: 225 × 0.7 = 158   → 158 × 0.5 =  79 coins
Tier 1 kills: 375 × 0.7 = 262   → 262 × 0.5 = 131 coins
Tier 2 kills: 525 × 0.7 = 368   → 368 × 0.5 = 184 coins
```

Building income, from the demolition counts in §3.3:

```
Tier 0:  6 × 7                =  42 coins
Tier 1:  5 × 20  +  8 × 7     = 100 + 56 = 156 coins
Tier 2:  5 × 50  +  8 × 20    = 250 + 160 = 410 coins
```

Totals and the required buys:

| | Tier 0 | Tier 1 | Tier 2 |
|---|---|---|---|
| Mob income | 79 | 131 | 184 |
| Building income | 42 | 156 | 410 |
| **Tier income** | **121** | **287** | **594** |
| Attack purchases needed | (50−10)/10 = **4** | (250−50)/10 = **20** | 0 |
| Attack cost | 4 × 7 = 28 | 20 × 7 = 140 | 0 |
| Speed purchases needed | 12−6 = **6** | 20−12 = **8** | 0 |
| Speed cost | 6 × 6 = 36 | 8 × 6 = 48 | 0 |
| **Required spend** | **64** | **188** | **0** |
| Surplus this tier | +57 | +99 | +594 |
| Running surplus | 57 | 156 | 750 |

Coverage ratio (income ÷ required spend): **1.9× in tier 0, 1.5× in tier 1.** The income curve
does fund the mandatory upgrades, with roughly a third of income left over for health / gun /
timer — which is what makes the shop a choice rather than a formality.

Level-cap check against `_maxUpgradeLevel = 30`:

```
Attack: 4 + 20 = 24 purchases → level 25 ≤ 30  ✓
Speed:  6 +  8 = 14 purchases → level 15 ≤ 30  ✓
```

---

## 4. Biggest gaps — ranked by impact

### 4.1 Attack upgrades cannot reach the required damage (blocking, arithmetic-certain)

The design needs ×25 damage across the run. The upgrade system is **linear with a hard cap**:
`_attackPowerUpgradeAmount = 1` and `_maxUpgradeLevel = 30` allow at most **+29 damage, ever**
(`CharacterService.GetLevel` clamps at 30, `UpgradeAttackPower` throws past it).

Failure mode: from 10 damage the player tops out at 39. A 250 HP T2 building takes
`250 / 39 = 7` hits, and a 1250 HP T3 building takes **32 hits = 16 s each**. Tier 2 never
starts; the player grinds T2 buildings until the 90 s clock runs out.

Fix in §2.2: `_attackPowerUpgradeAmount = 10`, which brings the requirement to 24 purchases.
Note this is only *barely* solvable linearly — 24 of 29 available levels. Any tier 3 breaks it,
and the honest fix is a multiplicative or per-level-scaling upgrade, which the config cannot
currently express.

### 4.2 There is no per-tier building HP at all (blocking)

`DestructibleHealth.cs:35` keys HP off `DestructibleObjectType`, and all three house difficulty
prefabs carry `_objectType: 0`. **T1, T2 and T3 buildings all have 20 HP today.** The entire
"5 hits / 1 hit" structure — the thing the whole design rests on — has nowhere to live.

Failure mode: whatever you set `House` to, every building in the world is that value. At
D_0 = 10 and House = 50, tier 0 works; the moment the player buys attack, *every* building in the
world becomes a one-shot, including the ones 380 wu away. The tier ladder collapses into a single
step.

This needs either a per-difficulty-level HP table on `HouseSet`, or new
`DestructibleObjectType` entries (`HouseT2`, `HouseT3`).

### 4.3 Meta-progression destroys the tier structure on run 2 (blocking, design-level)

`Storage.cs` persists `AttackPower`, `Speed` and `CurrentCoins` to `PlayerPrefs` and reloads them
on next battle. The tier design is a *within-battle* curve: 30 s at D=10, 30 s at D=50, 30 s at
D=250.

Failure mode: the player finishes run 1 at D = 250, speed 20, plus ~750 banked coins. Run 2
starts there. Every building in the world including the Goal is a one-shot from t = 0, the
walking is 3× faster than the map is laid out for, and the run collapses to a straight sprint to
the Goal. The stated design is only ever true on a **fresh save**.

You have to pick one: reset combat stats each battle and make the meta something else
(cosmetics, starting coins, timer), or abandon the per-battle tier curve and re-express tiers as
per-*run* milestones.

### 4.4 The player cannot die (high)

`_startingMaxHealth = 1000`, `_duckAttackDamage = 1`, and each duck attacks **exactly once**
(`_hasAttacked`, `Mob.cs:63`, only reset on pool reuse at `Mob.cs:88`).

Failure mode: with 1125 mobs spawned across the run, even if **every single one** landed its one
attack the player would take 1125 damage against 1000 HP — and realistically ~10% get through, so
~110 damage against 1000. Death is arithmetically unreachable. The only real fail state is the
timer, so mob density is pure friction with no threat, and `MaxHealth` upgrades are worthless.

Proposed 120 HP makes a ~30% leak rate (≈340 damage) lethal, which restores the fail state.

### 4.5 The map is 2.9× too short (high)

The Goal sits at **132 wu** (spawn at origin). The 10-s-per-tier walk rule requires **380 wu**
(§3.2). Worse, the `HouseSet` bands top out at 18 cells = 144 wu.

Failure mode: at speed 6 the player reaches the Goal in 22 s — inside tier 0, before buying
anything. If you instead gate the Goal, the player spends tiers 1 and 2 in a 144 wu box that has
already run out of difficulty bands, so tier 3 art shows up during tier 0's second half.

Related, and easy to trip over: **`HouseSet` distances are in cells, not world units**
(`MapFiller.cs:63` measures on integer cell coords). `maxDistance: 1000` reads like world units
and actually means 8000 wu. Anyone tuning these by eye will be off by 8×.

### 4.6 Tier 2 has no time for the Goal (medium)

§3.3 tier 2 sums to 30.0 s only by borrowing 1.5 s from mob combat. With a 2500 HP Goal the
finale eats 5 s of a 30 s tier, and the player also has to *find* it.

Either shorten tier 2's building content (3 × T3 instead of 5, freeing 5 s), or accept that the
Goal fight lives outside the 90 s tier budget and extend `_battleDuration`.

### 4.7 Unbounded mob count (medium, becomes a perf cliff)

`MobSpawner.Update()` has no cap. Mobs past `_duckRepositionDistance = 40` are **teleported back
to the player** (`MobSpawner.cs:137-163`), never despawned. Live count = spawned − killed, and it
only grows if the player falls behind.

Failure mode: at the proposed 5→20/s the run spawns **1125 mobs**. At the assumed k = 0.7 that
leaves ~340 live agents permanently orbiting the player by t = 90, all flow-mapped and flocked. A
player who under-kills does not get a harder game — they get a frame-rate collapse. Add a live-mob
cap before touching the rate numbers.

### 4.8 Special-object HP cannot be right at two damage levels (medium)

`TimerDigit` 15, `Arrow` 12, `HealthBar` 48, `Upgrade` 20 are single global values, but the player
meets these objects at D = 10 *and* at D = 250. At tier 0 the Timer takes 2 hits; at tier 2 it
takes one swing along with everything else in the cone. The Upgrade house in particular is meant
to be a deliberate stop and becomes incidental collateral.

### 4.9 Buying health silently buys AoE (low, but a hidden coupling)

`CharacterService.CharacterScaleFactor = 1 + (MaxHealthLevel − 1) × 0.02`, and `MeleeWeapon`
computes `AttackRadius = BaseAttackRadius × CharacterScaleFactor × 1.2`. Health upgrades enlarge
the melee radius. With the proposed 29 levels that is **+58% radius**, i.e. 6.0 → 9.5 wu, which is
a ~2.5× increase in swept area and therefore in mob income. Health is secretly the best DPS-and-
economy upgrade in the game, and nothing in the UI says so.

### 4.10 Flat upgrade costs (low here, but load-bearing later)

Every purchase costs the same. That is fine at the volumes in §3.4, but it means income and cost
grow at different rates by construction: income grows ~4.9× across the run (121 → 594) while
mandatory spend grows 2.9× (64 → 188) and then to 0. Tier 2 ends with ~750 spare coins and
nothing that must be bought.

---

## 5. Sensitivity

### Load-bearing — small change, large behaviour swing

| Knob | Why |
|---|---|
| `_attackPowerUpgradeAmount` | The ×5-per-tier chain and the 30-level cap interact multiplicatively. At 10 the design needs 24 of 29 levels; at 8 it needs 30 and **fails**. Anything below ~9 breaks the run outright (§4.1). |
| Building HP ratios (1 : 5 : 25) | Directly define the 5-hit/1-hit feel. A 20% error in T2 HP moves it from 5 hits to 4 or 6 — a 2.5 s → 2.0/3.0 s swing per building, ×5 buildings, ×2 tiers. |
| `_startingAttackPower` | Everything downstream is `D_0 × 5^N`. Changing 10 → 12 changes tier-2 damage 250 → 300 and every building HP with it. |
| `_maxUpgradeLevel` | A hard wall, not a curve. 30 works; 25 makes §4.1 unsolvable at any step size that also keeps tier 0 reasonable. |
| Mob spawn rate end value | Income scales linearly with it, but live-mob count scales with the *unkilled* fraction, which is non-linear near saturation. 20/s vs 25/s is the difference between "busy" and the §4.7 cliff. |
| `_meleeAttackRadius` (and therefore `_maxHealthScalePerLevel`) | Area scales as r². 5 → 6 is a 44% income increase from mobs, for free, invisibly (§4.9). |
| Goal position / map length | 132 vs 380 wu is the difference between a 22 s sprint and the intended three-tier run (§4.5). |
| `_startingMaxHealth` | Currently sits so far above total incoming damage that the fail state is off. Anywhere in 100–150 is a game; 1000 is not (§4.4). |

### Safe to tune freely

| Knob | Why |
|---|---|
| `_duckCoinDropChances` | Only shifts income, and income has 1.5–1.9× headroom (§3.4). Halving it still funds the mandatory buys. |
| Per-tier building coin values (7 / 20 / 50) | Same — a pure income dial with a large margin. |
| `_speedUpgradeCost` / `_attackPowerUpgradeCost` | 64 and 188 coins of mandatory spend against 121 and 287 of income. These can move ±40% before anything gates. |
| `_gunPowerUpgradeCost` / `Amount`, `_timerUpgradeCost` / `Amount` | Entirely discretionary; nothing in the tier design depends on them. |
| `_duckMaxHealth` | Anything ≤ D_0 = 10 behaves identically (one-shot). 1–10 are the same game. |
| `_buildingCoinDropDistance`, `CoinPickup` flight timings | Purely cosmetic — coins are credited at kill time (`CharacterService.cs:141-145`). |
| `_specialBetween*` / `_specialFence*` distances | Affect pacing texture, not the tier arithmetic. |
| `_duckRepositionDistance` | 40 wu; changing it moves *where* mobs re-enter, not how many exist. |
| Everything in `MovementSettings`, `EnvironmentDecaySettings`, `HitFxSettings` | No coupling to the balance model at all. |

### One thing worth measuring before trusting §3.4

The kill fraction `k = 0.70` is invented and it is the single largest lever on income
(mob coins scale linearly with it, and 4.4 / 4.7 both scale with `1 − k`). It is cheap to
instrument: `Storage.DucksKilled` already counts kills, and total spawns are a closed-form
integral of the spawn ramp. Log both at battle end and replace the assumption with a measurement
before committing to any of the coin numbers.
