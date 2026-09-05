# Balance reference solution

## 1. Evidence table

Provenance used throughout this report:

- **CODE** — a value or rule present in the repository.
- **TARGET** — supplied in the design brief but not backed by a field unless stated otherwise.
- **DERIVED** — arithmetic from CODE/TARGET values.
- **INVENTED** — an explicit starting assumption needed because the project has no value or rule for it.

For the brief's strict terminology, every proposed TARGET or DERIVED value that is not also CODE is an **invented, not-currently-backed value**; DERIVED only distinguishes arithmetic consequences from arbitrary assumptions.

The Unity Editor was not connected (`unity status --format json` returned `STATUS_NO_INSTANCES`), so current values below are taken from serialized project files. `GameBalanceWindow` does not own balance data: it discovers the first asset of each of eight types and draws its normal inspector ([`GameBalanceWindow.cs:36-54,79-90`](../Assets/Scripts/Balance/Editor/GameBalanceWindow.cs)). There is currently one asset of each type, but `FirstOrDefault()` would silently ignore duplicates.

### Directly relevant knobs

| Knob / field | Current value | Unit / runtime meaning | Evidence |
|---|---:|---|---|
| `_startingAttackPower` | 50 | damage per melee hit on a fresh save | [`ProgressionBalanceConfig.asset:15`](../Assets/Resources/ProgressionBalanceConfig.asset); passed directly to attacks in [`Weapon.cs:51-60`](../Assets/Scripts/Weapons/Weapon.cs) |
| `_startingMaxHealth` | 1000 | player HP on a fresh save | [`ProgressionBalanceConfig.asset:16`](../Assets/Resources/ProgressionBalanceConfig.asset) |
| `_startingSpeed` | 6 | world units/s on a fresh save | [`ProgressionBalanceConfig.asset:17`](../Assets/Resources/ProgressionBalanceConfig.asset); used directly in [`PlayerMovement.cs:41-54,115-134`](../Assets/Scripts/Movement/PlayerMovement.cs) |
| `_startingGunPower` | 0 | bullets per ranged volley | [`ProgressionBalanceConfig.asset:18`](../Assets/Resources/ProgressionBalanceConfig.asset) |
| `_startingTimer` | 0 | seconds added to the battle clock | [`ProgressionBalanceConfig.asset:19`](../Assets/Resources/ProgressionBalanceConfig.asset) |
| Attack / health / speed / gun / timer upgrade cost | 7 / 5 / 6 / 15 / 10 | coins per purchase; each cost is flat at every level | [`ProgressionBalanceConfig.asset:20-24`](../Assets/Resources/ProgressionBalanceConfig.asset); purchase code [`CharacterService.cs:147-215`](../Assets/Scripts/Model/CharacterService.cs) |
| Attack / health / speed / gun / timer upgrade amount | +1 / +1 / +1 / +1 / +1 | corresponding stat units per purchase | [`ProgressionBalanceConfig.asset:25-29`](../Assets/Resources/ProgressionBalanceConfig.asset) |
| `_maxUpgradeLevel` | 30 | displayed level; base is level 1, so at most 29 purchases/stat | [`ProgressionBalanceConfig.asset:30`](../Assets/Resources/ProgressionBalanceConfig.asset); level formula [`CharacterService.cs:39-77`](../Assets/Scripts/Model/CharacterService.cs) |
| `_maxHealthScalePerLevel` | 0.02 | +2% authored player scale per health level above 1 | [`ProgressionBalanceConfig.asset:31`](../Assets/Resources/ProgressionBalanceConfig.asset) |
| `_battleDuration` | 90 | seconds for the one global battle clock | [`BattleBalanceConfig.asset:15`](../Assets/Resources/BattleBalanceConfig.asset); clock rule [`BattleService.cs:42-44,74-97`](../Assets/Scripts/Model/BattleService.cs) |
| `_duckMaxHealth` | 6 | global mob HP | [`BattleBalanceConfig.asset:21`](../Assets/Resources/BattleBalanceConfig.asset) |
| `_duckAttackDamage` | 1 | player HP lost once per mob that connects; mobs do not repeatedly attack | [`BattleBalanceConfig.asset:22`](../Assets/Resources/BattleBalanceConfig.asset); [`Mob.cs:98-110`](../Assets/Scripts/Combat/Mob.cs) |
| `_duckAttackDistance` | 3 | world-unit center distance at which the one mob hit occurs | [`BattleBalanceConfig.asset:23`](../Assets/Resources/BattleBalanceConfig.asset) |
| `_duckRepositionDistance` | 40 | world units from player before a mob is teleported back near view | [`BattleBalanceConfig.asset:24`](../Assets/Resources/BattleBalanceConfig.asset) |
| `_meleeAttackRadius` | 5 | base world-unit radius | [`BattleBalanceConfig.asset:25`](../Assets/Resources/BattleBalanceConfig.asset) |
| `_duckCoinDropChances` | `[0.6, 0.3, 0.1]` | P(0/1/2 coins); mean **0.5 coin/kill**, variance **0.45** | [`BattleBalanceConfig.asset:26-29`](../Assets/Resources/BattleBalanceConfig.asset); index-is-coin-count rule [`BattleBalanceConfig.cs:89-101`](../Assets/Scripts/Balance/BattleBalanceConfig.cs) |
| `_buildingCoinDropMin/Max/Chance` | 5 / 9 / 1 | uniform 5..9 coins on every ordinary House; mean **7**, variance **2** | [`BattleBalanceConfig.asset:30-33`](../Assets/Resources/BattleBalanceConfig.asset); [`BattleBalanceConfig.cs:103-109`](../Assets/Scripts/Balance/BattleBalanceConfig.cs) |
| `_buildingCoinDropDistance` | 1 | cosmetic pickup scatter beyond building footprint, world units | [`BattleBalanceConfig.asset:33`](../Assets/Resources/BattleBalanceConfig.asset) |
| `_destructibleMaxHealth[House]` | 20 | HP shared by **every** ordinary house difficulty | [`BattleBalanceConfig.asset:34-37`](../Assets/Resources/BattleBalanceConfig.asset); lookup by object type only in [`DestructibleHealth.cs:17-37`](../Assets/Scripts/Destruction/DestructibleHealth.cs) |
| Other destructible HP | digit 15; divider 10; arrow 12; health bar 48; upgrade 20; goal 100 | HP by `DestructibleObjectType` | [`BattleBalanceConfig.asset:38-48`](../Assets/Resources/BattleBalanceConfig.asset); enum mapping [`DestructibleObjectType.cs:3-12`](../Assets/Scripts/Destruction/DestructibleObjectType.cs) |
| `_attackCooldown` | 0.5 | seconds between automatic melee attacks = nominal 2 attacks/s | Not in Balance Window. [`MeleeWeapon.prefab:47`](../Assets/Resources/Prefabs/Weapons/MeleeWeapon.prefab); scheduler [`Weapon.cs:46-60`](../Assets/Scripts/Weapons/Weapon.cs) |
| Melee cone / scale multiplier | 150 / 1.2 | degrees / radius multiplier; initial effective radius is 5×1.2 = **6 units** | Not in Balance Window. [`MeleeWeapon.prefab:53-54`](../Assets/Resources/Prefabs/Weapons/MeleeWeapon.prefab); AoE rule [`MeleeWeapon.cs:37-46,64-113`](../Assets/Scripts/Weapons/MeleeWeapon.cs) |
| Mob movement `_speed` | 4 | world units/s | Not in Balance Window. [`Mob.prefab:63-69`](../Assets/Prefabs/Mob.prefab) |
| Mob spawn curve | 1/s at 0 s → 15/s at 60 s, then 15/s | scene-owned linear rate; theoretical 30-second slice totals 135 / 345 / 450 | Not in Balance Window. [`BattleScene.unity:444-453`](../Assets/Scenes/BattleScene/BattleScene.unity); interpolation/accumulator [`MobSpawner.cs:23-67,120-133`](../Assets/Scripts/Combat/MobSpawner.cs) |
| Map `cellSize` | 8 | world units per grid cell | Not in Balance Window. [`MapData.asset:15`](../Assets/Scenes/BattleScene/BattleScene/MapData.asset); placement conversion [`MapEnvironmentSpawner.cs:129-169`](../Assets/Scripts/Map/MapEnvironmentSpawner.cs) |

Coins are credited immediately when a mob/building dies; the spawned coin pickup is visual, not a collection gate ([`CharacterService.cs:114-145`](../Assets/Scripts/Model/CharacterService.cs), [`CoinPickup.cs:7-12`](../Assets/Scripts/Combat/CoinPickup.cs)). Actual starting stats and coins may instead come from persistent `PlayerPrefs`, so a dirty save can invalidate any clean-run test ([`Storage.cs:117-143`](../Assets/Scripts/Model/Storage.cs)).

### Spatial tier evidence

`HouseSet` has five enabled T1 entries, three T2 entries, and three T3 entries. All D1/D2/D3 prefabs serialize `_objectType: 0` (`House`), so all are currently the same 20 HP and 5..9 coin reward. Difficulty affects prefab selection only ([`HouseObject.cs:7-16`](../Assets/Scripts/Map/HouseObject.cs), [`MapFiller.cs:48-65`](../Assets/Scripts/Map/MapFiller.cs)).

| Current radial range | World range at 8 units/cell | Selection weights | Evidence |
|---:|---:|---|---|
| [0, 5) cells | [0, 40) units | 100% T1 | [`HouseSet.asset:92-97`](../Assets/Resources/Map/HouseSet.asset) |
| [5, 12) cells | [40, 96) units | 70% T1, 30% T2 | [`HouseSet.asset:98-104`](../Assets/Resources/Map/HouseSet.asset) |
| [12, 18) cells | [96, 144) units | 20% T1, 50% T2, 30% T3 | [`HouseSet.asset:105-113`](../Assets/Resources/Map/HouseSet.asset) |
| [18, 1000) cells | [144, 8000) units | 40% T2, 60% T3 | [`HouseSet.asset:114-120`](../Assets/Resources/Map/HouseSet.asset) |

These are stochastic population bands, not a guaranteed T1→T2→T3 route. The final goal is at `(4, 0, 132)`, **132.06 units** straight-line from the player start, so unobstructed travel is 22.01 s at speed 6 or 6.60 s at speed 20 ([`BattleScene.unity:353-376,750-760`](../Assets/Scenes/BattleScene/BattleScene.unity)).

### Other knobs actually surfaced by Game Balance Window

| Asset | Current fields | Unit |
|---|---|---|
| `SpecialSpawnSettings` | Timer initial 10..30, respawn 20..40 then 30..50; Upgrade initial/respawn 0..30 then 0..50; Arrow initial 0..20, respawn 0..30 then 0..50; Health initial 0..30 with no respawns; GoalArrow initial 0..20, respawn 0..30 then 0..50 | world-unit radial ranges; these are specials, not ordinary tiered houses ([asset](../Assets/Resources/SpecialSpawnSettings.asset)) |
| `BattleBalanceConfig` special fence | between min/max 40/50; fence start 35; timer offset 14; player offset 8 | world units ([asset:16-20](../Assets/Resources/BattleBalanceConfig.asset)) |
| `MovementSettings` | spatial cell 2; flow cell 1; flow padding 15; target deviation 2; max cells 262144; wall skin 0.02; slide iterations 3; self-radius modifier 3; avoidance spread 2; avoidance target 3 | units are respectively world units, cells/counts, or unitless steering multipliers ([asset:15-24](../Assets/Resources/MovementSettings.asset)) |
| `EnvironmentDecaySettings` | ground layer bits 256 (layer 8); ray height 50; ray distance 200; sink 0.5; delay 3; max fall multiplier 5 | mask, world units, seconds, multiplier ([asset:15-22](../Assets/Resources/EnvironmentDecaySettings.asset)) |
| `EnvironmentVisibilitySettings` | visible radius 100; hidden margin 15 → hidden radius 115 | world units ([asset:15-16](../Assets/Resources/EnvironmentVisibilitySettings.asset)) |
| `HitFxSettings` | duration 0.15; curve keys `(0,1)` and `(0.99113405,-0.003931284)`; blink interval 0.08 from code initializer (absent from YAML) | seconds / normalized curve ([asset:15-39](../Assets/Resources/HitFxSettings.asset), [`HitFxSettings.cs:8-16`](../Assets/Scripts/Destruction/HitFxSettings.cs)) |

### Quantities in the proposed design with no backing field

| Missing quantity | What exists instead |
|---|---|
| Stage/tier state, transitions, and three 30 s stage durations | One 90 s battle clock; no stage boundaries |
| T1/T2/T3 building HP or building reward | One global `House` HP and one global House drop |
| Per-tier player damage, speed, or attack rate | Persistent base stat plus flat upgrades; one prefab attack cooldown |
| Guaranteed T1/T2/T3 target distances | Probabilistic house difficulty bands and separately placed special objects |
| Per-tier mob spawn rate in the Balance Window | One scene-serialized time curve |
| Per-tier mob HP, damage, speed, or reward | One global mob configuration and one mob prefab speed |
| Mob attack rate / DPS | A mob hits exactly once and then attaches |
| Tiered/scaling upgrade costs or step sizes | One flat cost and one flat additive step per stat |
| Kill fraction, mobs hit per swing, spawn-success rate, or combat-time budget | No authored value; AoE, pathing, density, and off-camera placement decide them at runtime |
| Economy reset or per-stage purse | Coins and stats persist across battles in `PlayerPrefs` |

## 2. Proposed starting values

This is a **reference model**, not a set of values the current schema can fully represent. Values marked INVENTED require either a new tier field or an agreed design assumption.

The model anchors T1 HP to the current 20 HP (CODE), preserves the current 0.5 s melee cooldown, 0.5 expected mob coin, 7 expected building coin, speed 6, speed cost 6, and mob damage 1. It normalizes opening damage down to 4 so current T1 takes exactly five hits, then uses a fivefold damage/HP ladder.

| Tier / target | Player damage | Attack rate; DPS | Building HP T1 / T2 / T3 | Entry move speed | Mob spawn rate | Mob HP | Mob damage | Mob drop | Target-building drop | Attack upgrade | Speed upgrade |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| T0 → T1 | **4** (DERIVED from 20/5) | **2/s** CODE; **8 DPS** DERIVED | **20 / 100 / 500** (20 CODE; 100/500 DERIVED) | **6 units/s** CODE | **5/s** TARGET, no tier field | **4 HP** INVENTED one-hit-mob rule | **1 HP once** CODE | **0.5 EV/kill** CODE | **7 EV** CODE | **5 coins, +4 damage** (DERIVED/INVENTED) | **6 coins, +2 units/s** (cost CODE; step INVENTED) |
| T1 → T2 | **20** DERIVED | **2/s** CODE; **40 DPS** DERIVED | **20 / 100 / 500** | **12 units/s** INVENTED waypoint | **10/s** TARGET, no tier field | **20 HP** INVENTED one-hit-mob rule | **1 HP once** CODE | **0.5 EV/kill** CODE | **7 EV** CODE | **5 coins, +4 damage** | **6 coins, +2 units/s** |
| T2 → T3 | **100** DERIVED | **2/s** CODE; **200 DPS** DERIVED | **20 / 100 / 500** | **20 units/s** TARGET | **20/s** TARGET, no tier field | **100 HP** INVENTED one-hit-mob rule | **1 HP once** CODE | **0.5 EV/kill** CODE | **7 EV** CODE | **5 coins, +4 damage** | **6 coins, +2 units/s** |

The recommended layout reference is also invented because no deterministic target-distance field exists. Distances are snapped to the current 8-unit map cell and leave less than 10 s unobstructed travel:

| Leg | Reference leg distance | Entry speed | Straight travel | Cumulative radius |
|---|---:|---:|---:|---:|
| Start → T1 | **56 units = 7 cells** (INVENTED) | 6 | **9.33 s** DERIVED | 56 units |
| T1 → T2 | **112 units = 14 cells** (INVENTED) | 12 | **9.33 s** DERIVED | 168 units |
| T2 → T3 | **192 units = 24 cells** (INVENTED) | 20 | **9.60 s** DERIVED | 360 units |

Required purchases at each stage end:

| Stage income funds entry to | Attack purchases | Speed purchases | Expected spend | Result |
|---|---:|---:|---:|---|
| T1 | `(20−4)/4 = 4` | `(12−6)/2 = 3` | `4×5 + 3×6 = 38` coins | damage 20, speed 12 |
| T2 | `(100−20)/4 = 20` | `(20−12)/2 = 4` | `20×5 + 4×6 = 124` coins | damage 100, speed 20 |
| After T2 | none required by the brief | none | 0 | T3 remains a five-hit target |

## 3. Derivation

### Damage, HP, cadence, and cap

Use the current House HP as the one code-backed anchor:

1. T0 damage: `D0 = H1 / 5 = 20 / 5 = 4`.
2. T1 must become one-hit: `D1 = H1 = 20`.
3. T2 must take five T1 hits: `H2 = 5 × D1 = 100`.
4. T2 entry damage must one-hit T2: `D2 = H2 = 100`.
5. T3 must take five T2 hits: `H3 = 5 × D2 = 500`.
6. With cooldown `c = 0.5 s`, attack rate is `1/c = 2/s`; DPS is `D/c`, giving 8 / 40 / 200 DPS.
7. The first attack may happen immediately, so a five-hit sequence spans `(5−1)×0.5 = 2.0 s`. A throughput accounting convention would charge `5×0.5 = 2.5 attack-seconds`; this report uses the actual first-to-fifth-hit span for the 30 s stage budget.
8. Damage must rise by `100−4 = 96` within at most 29 purchases. The smallest integer flat step that can do that is `ceil(96/29) = 4`, requiring 24 purchases and ending at displayed level 25. The current +1 step would cap this normalized path at only `4+29 = 33`.

The proposed mob HP equals entry damage. That **one-hit-mob rule is INVENTED**; it prevents mob TTK from multiplying the already steep 5/10/20 spawn load. It is not implementable per tier with the current single `_duckMaxHealth` field.

### Distances and explicit 30-second budgets

The maximum unobstructed distance for “less than 10 s” is `speed×10`. To stay on the 8-unit grid and below the strict limit, the invented reference legs use 7, 14, and 24 cells:

| Stage | Walking | Five-hit target | Fighting / path / collection slack | Total |
|---|---:|---:|---:|---:|
| T0 → T1 | `56/6 = 9.33 s` | `4 intervals×0.5 = 2.00 s` | `30−9.33−2 = 18.67 s` | **30.00 s** TARGET |
| T1 → T2 | `112/12 = 9.33 s` | `4×0.5 = 2.00 s` | `30−9.33−2 = 18.67 s` | **30.00 s** TARGET |
| T2 → T3 | `192/20 = 9.60 s` | `4×0.5 = 2.00 s` | `30−9.60−2 = 18.40 s` | **30.00 s** TARGET |

This sequential budget is explicitly **INVENTED as an accounting model**. Runtime movement and combat overlap: the player keeps moving while `Weapon.Update()` auto-attacks, and one swing can hit mobs and destructibles together. Actual stage time is closer to the maximum of travel/path time and combat-clear time, plus blocking, rather than their sum. Opening the progression UI sets `Time.timeScale = 0`, so shopping consumes no battle-clock seconds ([`InBattleProgressionUi.cs:65-72`](../Assets/Scripts/Ui/Battle/InBattleProgressionUi.cs)).

### Spawn volume, AoE demand, and income

At the TARGET rates for 30 s:

| Tier | Spawned | INVENTED kill fraction | Killed | Expected mob coins | + one target building (INVENTED count) | Expected stage income |
|---|---:|---:|---:|---:|---:|---:|
| T0 | `5×30 = 150` | 80% | 120 | `120×0.5 = 60` | 7 | **67** |
| T1 | `10×30 = 300` | 80% | 240 | `240×0.5 = 120` | 7 | **127** |
| T2 | `20×30 = 600` | 80% | 480 | `480×0.5 = 240` | 7 | **247** |

The **80% kill fraction and exactly one rewarded target building per stage are INVENTED**. Coins have no pickup loss once a kill occurs. At 2 attacks/s there are 60 swing opportunities per 30 s, so the assumed kill rate requires averages of `120/60 = 2`, `240/60 = 4`, and `480/60 = 8` mob kills per swing. The 150°/6-unit melee AoE makes batches possible, but no code-backed density or kills-per-swing value proves these averages.

Economy fit:

- T0: income 67 − spend 38 = **29-coin expected surplus**.
- T1: income 127 − spend 124 = **3-coin expected surplus**. The minimum kill fraction for this stage alone is `(124−7)/(300×0.5) = 78%`.
- Across T0+T1, expected income is `67+127 = 194`, required spend is `38+124 = 162`, and the persistent bank leaves 32 coins.
- For fixed 120+240 kills, drop variance is `360×0.45 + 2×2 = 166`, so two-stage standard deviation is `sqrt(166) = 12.88` coins. The 32-coin cumulative margin is about 2.48 standard deviations; the isolated T1 margin of 3 coins is fragile.
- Attack price 5 is derived from the tight T1 budget: after four speed purchases cost 24, `floor((127−24)/20) = 5` coins is the highest integer flat attack cost that fits 20 purchases at the 80% assumption.
- Keeping the current attack cost 7 makes isolated T1 spend `20×7 + 4×6 = 164`, greater than even its **100%-kill ceiling** of `300×0.5+7 = 157`. With 80% kills, T0+T1 income 194 also misses combined current-price spend `(4+20)×7 + 7×6 = 210` by 16 coins.
- The target building's 7 coins arrive only after it is destroyed. Pre-building T1 income at 80% is 120, four coins short of its 124 spend; the prior-tier bank is therefore part of this reference model.

For comparison, the live 1→15/s curve produces theoretical slice counts 135 / 345 / 450, not 150 / 300 / 600. At full kills that is 67.5 / 172.5 / 225 expected mob coins. `TrySpawn` can fail, so these are ceilings rather than guarantees.

## 4. Biggest gaps

1. **There is no combat tier model, so the requested hit ladder is impossible to author.** T1/T2/T3 are prefab-selection labels only; all resolve `House` HP 20 and the same reward. Likewise mob HP/damage/drop and player cadence are global. Before balance iteration, the design needs a tier index that selects HP, spawn, reward, and distance values—or three distinct destructible types/config entries. Otherwise changing House HP changes every tier together.

2. **The current baseline already bypasses the intended loop.** Damage 50 versus House HP 20 and mob HP 6 means `ceil(20/50)=1` hit/house and `ceil(6/50)=1` hit/mob at T0. The requested opening is five hits. Persistent `PlayerPrefs` can raise stats further, so testing without resetting save data can conceal even corrected starting values.

3. **Fivefold power gates fight a merely twofold income curve.** Exact five-hit→one-hit progression multiplies damage and next-building HP by five every tier. Spawn-funded income only doubles (5→10→20/s), while the implementation offers one flat additive step, one flat cost, and 29 purchases. With the current damage 50 as an anchor, exact T1 HP would be 250 (INVENTED from 5×50); reaching one-hit damage 250 would require 200 current +1 purchases costing 1,400 coins—far beyond the cap and the T0 full-kill expected 82 coins. The proposed rescaling to 4/+4 only postpones the issue: after reaching 100, one-hitting 500 would require 100 more +4 purchases, while only five purchases remain before level 30.

4. **Income is based on an unproved combat-throughput assumption.** Spawn rate is not kill rate. At T2, the 80% budget requires eight kills per automatic swing while 20 mobs/s spawn. AoE permits it, but mob pathing, the 150° facing cone, off-camera spawn placement, obstacles, and `TrySpawn` failures determine realized kills. If T1 falls from 80% to 77%, its income falls from 127 to 122.5 and can no longer pay the 124 standalone spend.

5. **The clock, spawn curve, and spatial progression are independent.** There is one 90 s clock, no 30 s stage transition, and `MobSpawner` advances its own elapsed time. Destroying and respawning the timer resets the displayed clock to the full 90 s ([`BattleService.cs:174-191`](../Assets/Scripts/Model/BattleService.cs)); timer upgrades can extend it. Effective battle length can therefore exceed 90 s while mob difficulty keeps advancing. Three clean 30 s phases are not enforced.

6. **Current map bands do not express the intended route.** T1 persists out to 144 units, T3 begins at 96, and selection is random. The final goal is only 132.06 units from the start—22.01 unobstructed seconds at starting speed, less than one proposed three-stage journey. The invented 56/112/192-unit legs cannot be entered as deterministic tier targets today.

7. **Economy can be bypassed or stranded.** Coins and stats persist across battles, there is no stage purse or prerequisite, and all upgrades can be bought whenever the progression screen is available. The screen pauses time, so purchasing is free in the time budget. Conversely, destroying the Upgrade house disables its in-battle shortcut ([`BattleUpgradeObject.cs:32-73`](../Assets/Scripts/Upgrade/BattleUpgradeObject.cs)). Decide whether the intended economy is per run, per stage, or permanent metaprogression before treating a 30 s income target as binding.

8. **Survival has no usable target.** Mob damage is one hit per mob, not DPS; current player HP 1000 survives 1,000 connecting mobs. No connect fraction, desired damage-taken rate, death probability, healing rate, or HP-upgrade budget was specified. Keeping mob damage at 1 is conservative and code-backed, but it is not a derived survival balance.

9. **“Time fighting mobs” is not an independent runtime phase.** Movement, mob clearing, and building damage overlap. A sequential 30 s allocation is useful as a planning envelope, but it cannot be validated unless stages add gates such as “clear wave, then unlock target” or telemetry reports actual overlap and blocking time.

## 5. Sensitivity

### Load-bearing values

| Value | Why it swings behaviour | Concrete sensitivity around the proposal |
|---|---|---|
| Damage / building-HP ratio | Hit count is `ceil(HP/damage)`, a discontinuous threshold | T1 at 20 HP: damage 4 = 5 hits; damage 3 = 7 hits; damage 5 = 4 hits |
| Attack cooldown | Controls both building TTK and available crowd-clearing swings | 0.5→0.6 s changes rate 2→1.67/s and five-hit span 2.0→2.4 s |
| Mob HP relative to damage | Crossing the one-hit boundary doubles required swing throughput | T1 mob HP 20 at damage 20 is one hit; HP 21 is two hits |
| AoE radius/cone and mobs per swing | Economy assumes 2/4/8 kills per swing at 80% | A modest density/facing loss directly cuts both survival and income |
| T1 kill fraction | The standalone budget has only 3 expected coins of margin | Each percentage point is `300×0.5×0.01 = 1.5` coins; 78% is break-even |
| Mob coin EV | Multiplied by hundreds of kills | +0.1 coin/kill at T1 adds `240×0.1 = 24` expected coins under the 80% assumption |
| Spawn rate | Raises income and threat simultaneously | +1 mob/s for 30 s at 80% adds 24 kills and 12 expected coins, but also 30 more bodies |
| Attack upgrade cost | Paid 20 times in T1 | +1 coin/cost adds 20 spend and turns the 3-coin margin into a 17-coin deficit |
| Attack step / level cap | Determines whether breakpoints are reachable at all | +4 reaches damage 100 in 24 purchases; current +1 cannot reach it from normalized damage 4 within 29 |
| Speed and leg distance | Travel is `distance/speed`; grid snapping gives little buffer | 112 units at speed 12 is 9.33 s; one proposed −2 speed step makes it 11.2 s |
| Timer respawn/reset | Adds large amounts of economy and spawn exposure | One current respawn restores 90 s, equal to all three intended stages |

### Safer knobs to tune freely

- Hit-FX duration/curve/blink, building coin scatter distance, debris decay timing, and visual material weights do not change the reference combat/economy arithmetic.
- Building-drop shape is relatively low leverage only if its mean stays near 7: one building is small beside 60/120/240 expected mob coins. It becomes load-bearing in the isolated T1 budget because the margin is just 3.
- Special-object fence offsets are safe for the hit/income equations, but can affect pathing and access, so verify traversal after large changes.
- Environment visibility and movement-cell settings are not balance rewards, but are not truly free: aggressive changes may alter performance, path quality, spawn success, and therefore realized kill rate.

Start playtests by logging, per 30-second slice: successful spawns, kills, mean mobs hit per swing, mobs that connect, coins earned/spent, straight versus actual distance traveled, target hit count, and target-destruction timestamp. The proposed profile is viable only if T1 kill fraction stays at or above 78% (or prior-tier savings are intentionally guaranteed) and the T2 crowd averages about eight kills per swing.
