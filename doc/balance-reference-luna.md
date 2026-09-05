# Luna balance reference

This is a static balance reference. `GameBalanceWindow` is an inspector over existing assets; it
does not add a tier system or new balance fields (`Assets/Scripts/Balance/Editor/GameBalanceWindow.cs:12-17,36-54,79-90`).
The values below are the serialized `Assets/Resources` values, not necessarily the values in an
existing `PlayerPrefs` save. `Storage.Load` gives saved values precedence over the progression asset
(`Assets/Scripts/Model/Storage.cs:134-144`).

## 1. Evidence table

### Fields exposed by `GameBalanceWindow`

| Field | File | Current value | Unit / meaning |
|---|---|---:|---|
| `_startingAttackPower` | `Assets/Resources/ProgressionBalanceConfig.asset:15` | 50 | damage per eligible target per attack |
| `_startingMaxHealth` | `Assets/Resources/ProgressionBalanceConfig.asset:16` | 1000 | player HP |
| `_startingSpeed` | `Assets/Resources/ProgressionBalanceConfig.asset:17` | 6 | world units/s; `PlayerMovement` multiplies clamped input by this stat (`Assets/Scripts/Movement/PlayerMovement.cs:41-43,115-133`) |
| `_startingGunPower` | `Assets/Resources/ProgressionBalanceConfig.asset:18` | 0 | bullets per ranged shot; `RangedWeapon` mirrors `GunPower` (`Assets/Scripts/Weapons/RangedWeapon.cs:82-85`) |
| `_startingTimer` | `Assets/Resources/ProgressionBalanceConfig.asset:19` | 0 | extra seconds added to the base battle clock |
| `_attackPowerUpgradeCost` | `Assets/Resources/ProgressionBalanceConfig.asset:20` | 7 | coins per purchase |
| `_maxHealthUpgradeCost` | `Assets/Resources/ProgressionBalanceConfig.asset:21` | 5 | coins per purchase |
| `_speedUpgradeCost` | `Assets/Resources/ProgressionBalanceConfig.asset:22` | 6 | coins per purchase |
| `_gunPowerUpgradeCost` | `Assets/Resources/ProgressionBalanceConfig.asset:23` | 15 | coins per purchase |
| `_timerUpgradeCost` | `Assets/Resources/ProgressionBalanceConfig.asset:24` | 10 | coins per purchase |
| `_attackPowerUpgradeAmount` | `Assets/Resources/ProgressionBalanceConfig.asset:25` | 1 | damage added per purchase |
| `_maxHealthUpgradeAmount` | `Assets/Resources/ProgressionBalanceConfig.asset:26` | 1 | HP added per purchase |
| `_speedUpgradeAmount` | `Assets/Resources/ProgressionBalanceConfig.asset:27` | 1 | world units/s added per purchase |
| `_gunPowerUpgradeAmount` | `Assets/Resources/ProgressionBalanceConfig.asset:28` | 1 | bullet added per purchase |
| `_timerUpgradeAmount` | `Assets/Resources/ProgressionBalanceConfig.asset:29` | 1 | second added per purchase |
| `_maxUpgradeLevel` | `Assets/Resources/ProgressionBalanceConfig.asset:30` | 30 | level cap; level 1 is the starting value, so this permits 29 purchases (`Assets/Scripts/Balance/ProgressionBalanceConfig.cs:30-36`) |
| `_maxHealthScalePerLevel` | `Assets/Resources/ProgressionBalanceConfig.asset:31` | 0.02 | fraction of authored player size added per max-health level |
| `_battleDuration` | `Assets/Resources/BattleBalanceConfig.asset:15` | 90 | base battle-clock seconds; `BattleService` starts at this plus the saved Timer stat (`Assets/Scripts/Model/BattleService.cs:42-55,117-132`) |
| `_specialBetweenMaxDistance` | `Assets/Resources/BattleBalanceConfig.asset:16` | 50 | world units; far/near special-placement threshold |
| `_specialBetweenMinDistance` | `Assets/Resources/BattleBalanceConfig.asset:17` | 40 | world units; near-player special band lower bound |
| `_specialFenceStartDistance` | `Assets/Resources/BattleBalanceConfig.asset:18` | 35 | world units; minimum timer/player distance before a special fence is arranged |
| `_specialFenceTimerOffset` | `Assets/Resources/BattleBalanceConfig.asset:19` | 14 | world units; timer-to-fence end offset |
| `_specialFencePlayerOffset` | `Assets/Resources/BattleBalanceConfig.asset:20` | 8 | world units; player-to-fence end offset |
| `_duckMaxHealth` | `Assets/Resources/BattleBalanceConfig.asset:21` | 6 | HP for every Mob; one global value, not per tier |
| `_duckAttackDamage` | `Assets/Resources/BattleBalanceConfig.asset:22` | 1 | damage of one mob bite |
| `_duckAttackDistance` | `Assets/Resources/BattleBalanceConfig.asset:23` | 3 | world units; a mob bites when within this distance |
| `_duckRepositionDistance` | `Assets/Resources/BattleBalanceConfig.asset:24` | 40 | world units; mobs farther away are repositioned |
| `_meleeAttackRadius` | `Assets/Resources/BattleBalanceConfig.asset:25` | 5 | base world-unit radius; the player melee prefab scales the query by 1.2, so the fresh effective radius is 6 (`Assets/Resources/Prefabs/Weapons/MeleeWeapon.prefab:47-54`, `Assets/Scripts/Weapons/MeleeWeapon.cs:37-47`) |
| `_duckCoinDropChances` | `Assets/Resources/BattleBalanceConfig.asset:26-29` | `[0.6, 0.3, 0.1]` | probability of 0, 1, or 2 coins per mob kill; expected value `0×.6+1×.3+2×.1 = 0.5` coins/kill (`Assets/Scripts/Balance/BattleBalanceConfig.cs:89-100`) |
| `_buildingCoinDropMin` / `_buildingCoinDropMax` | `Assets/Resources/BattleBalanceConfig.asset:30-31` | 5 / 9 | inclusive integer coin range per destroyed House |
| `_buildingCoinDropChance` | `Assets/Resources/BattleBalanceConfig.asset:32` | 1 | probability a House pays the drop |
| `_buildingCoinDropDistance` | `Assets/Resources/BattleBalanceConfig.asset:33` | 1 | world units; cosmetic pickup target offset |
| `_destructibleMaxHealth` — `House` | `Assets/Resources/BattleBalanceConfig.asset:34-36` | 20 | HP; enum value 0 is `House` (`Assets/Scripts/Destruction/DestructibleObjectType.cs:3-11`) |
| `_destructibleMaxHealth` — `TimerDigit` / `TimerDivider` | `Assets/Resources/BattleBalanceConfig.asset:37-40` | 15 / 10 | HP |
| `_destructibleMaxHealth` — `Arrow` / `HealthBar` | `Assets/Resources/BattleBalanceConfig.asset:41-44` | 12 / 48 | HP |
| `_destructibleMaxHealth` — `Upgrade` / `Goal` | `Assets/Resources/BattleBalanceConfig.asset:45-48` | 20 / 100 | HP |
| `ranges[].initial` — `Timer` | `Assets/Resources/SpecialSpawnSettings.asset:15-24` | 10–30; respawns 20–40, then 30–50 | world units from player; type 0 is Timer (`Assets/Scripts/Map/SpecialHouses.cs:3-6`) |
| `ranges[].initial` — `Upgrade` | `Assets/Resources/SpecialSpawnSettings.asset:25-33` | 0–30; respawns 0–30, then 0–50 | world units; type 1 is Upgrade |
| `ranges[].initial` — `Arrow` | `Assets/Resources/SpecialSpawnSettings.asset:34-42` | 0–20; respawns 0–30, then 0–50 | world units; type 2 is Arrow |
| `ranges[].initial` — `Health` | `Assets/Resources/SpecialSpawnSettings.asset:43-47` | 0–30; no respawns | world units; type 3 is Health |
| `ranges[].initial` — `GoalArrow` | `Assets/Resources/SpecialSpawnSettings.asset:48-56` | 0–20; respawns 0–30, then 0–50 | world units; type 4 is GoalArrow |
| `houses` | `Assets/Resources/Map/HouseSet.asset:15-70` | 11 enabled entries: level 1 has House01 at 1×1, 1×2, 2×1 plus House02 and House02_2 at 1×1; level 2 has House01_D2 at the same three sizes; level 3 has House01_D3 at the same three sizes | footprint in map grid cells; `difficultyLevel` is a prefab-selection label, not HP |
| `specials` | `Assets/Resources/Map/HouseSet.asset:71-91` | 5 enabled entries: Timer 2×1, Upgrade 1×1, Arrow 2×2, Health 2×1, GoalArrow 2×2 | footprint in map grid cells |
| `difficultyRanges` | `Assets/Resources/Map/HouseSet.asset:92-120` | 0–<5: L1 100%; 5–<12: L1 70%/L2 30%; 12–<18: L1 20%/L2 50%/L3 30%; 18–<1000: L2 40%/L3 60% | radial grid-cell distance and selection weights; `HouseSet` picks the first matching range (`Assets/Scripts/Map/HouseSet.cs:35-64`) |
| `_spatialCellSize` / `_flowCellSize` | `Assets/Resources/MovementSettings.asset:15-16` | 2 / 1 | world units per spatial / flow cell |
| `_flowPadding` | `Assets/Resources/MovementSettings.asset:17` | 15 | world units |
| `_flowTargetCellDeviation` | `Assets/Resources/MovementSettings.asset:18` | 2 | flow cells |
| `_maxFlowCellCount` | `Assets/Resources/MovementSettings.asset:19` | 262144 | cells |
| `_wallSkin` / `_wallSlideIterations` | `Assets/Resources/MovementSettings.asset:20-21` | 0.02 / 3 | world units / solver iterations |
| `_selfRadiusModifier` | `Assets/Resources/MovementSettings.asset:22` | 3 | radius multiplier |
| `_avoidanceSpread` / `_avoidanceTargetDistance` | `Assets/Resources/MovementSettings.asset:23-24` | 2 / 3 | avoidance geometry values; the latter is world units |
| `groundLayer` | `Assets/Resources/EnvironmentDecaySettings.asset:15-17` | bit 256 | Unity layer mask |
| `groundRaycastHeight` / `groundRaycastDistance` | `Assets/Resources/EnvironmentDecaySettings.asset:18-19` | 50 / 200 | world units |
| `sinkDepth` | `Assets/Resources/EnvironmentDecaySettings.asset:20` | 0.5 | world units below sampled ground |
| `decayStartDelay` | `Assets/Resources/EnvironmentDecaySettings.asset:21` | 3 | seconds after detachment |
| `maxFallSpeedMultiplier` | `Assets/Resources/EnvironmentDecaySettings.asset:22` | 5 | multiplier cap |
| `visibleRadius` / `hiddenMargin` | `Assets/Resources/EnvironmentVisibilitySettings.asset:15-16` | 100 / 15 | world units; effective hidden radius is `100+15 = 115` (`Assets/Scripts/Map/EnvironmentVisibilitySettings.cs:8-15`) |
| `duration` | `Assets/Resources/HitFxSettings.asset:15` | 0.15 | seconds |
| `curve` | `Assets/Resources/HitFxSettings.asset:16-39` | key 0: `(t=0,value=1)`; key 1: `(t=.9911,value=-.00393)` | hit-FX animation curve |
| `blinkInterval` | `Assets/Scripts/Destruction/HitFxSettings.cs:11-16` | 0.08 | seconds between blink flips; absent from the old YAML, so the source default is used |

### Relevant knobs that exist but are not exposed by `GameBalanceWindow`

These are important to the requested loop, but the window does not include their component types.

| Field | File | Current value | Unit / consequence |
|---|---|---:|---|
| `_attackCooldown` on the player melee prefab | `Assets/Resources/Prefabs/Weapons/MeleeWeapon.prefab:47` and `Assets/Scripts/Weapons/Weapon.cs:51-64` | 0.5 | seconds between attacks, therefore 2 attacks/s; every attack passes `CharacterService.AttackPower` as damage |
| `_mobsPerSecStart` / `_mobPerSecondEnd` | `Assets/Scenes/BattleScene/BattleScene.unity:444-449` | 1 / 15 | mobs/s; one linear ramp from 1 to 15 over seconds 0–60, not 5/10/20 tier rates (`Assets/Scripts/Combat/MobSpawner.cs:49-59`) |
| `_secondStart` / `_secondEnd` | `Assets/Scenes/BattleScene/BattleScene.unity:448-449` | 0 / 60 | battle seconds controlling the mob-rate ramp |
| `MobMovement._speed` | `Assets/Prefabs/Mob.prefab:63-69` | 4 | mob world units/s |
| `MapData.cellSize` | `Assets/Scenes/BattleScene/BattleScene/MapData.asset:15` | 8 | world units per map-grid cell; the HouseSet ranges above are therefore nominally 0–40, 40–96, 96–144, and 144–8000 world units |

### Requested quantities with no backing field

| Design quantity | Backing status | Why it matters |
|---|---|---|
| Tier 0/1/2 state, stage counter, or stage transition | **No field** | Runtime has one active battle clock and no stage/wave counter; House difficulty is selected spatially during map fill (`Assets/Scripts/Map/MapFiller.cs:58-65`), not advanced by gameplay. |
| Separate T1, T2, and T3 building HP | **No field** | `DestructibleHealth` asks `BattleBalanceConfig` for HP by object type only (`Assets/Scripts/Destruction/DestructibleHealth.cs:31-38`); House01, House01_D2, and House01_D3 all serialize `_objectType: 0` (`Assets/Resources/Descructable/House01_D2.prefab:2150-2156`, `House01_D3.prefab:3744-3750`). The one current House value is 20 HP. |
| Building-to-building path length/distance | **No field** | Only radial HouseSet selection distance and special-placement ranges exist. No target-to-target route or path length is authored. |
| Per-tier mob spawn rate | **No field in the window or balance assets** | The scene has one time-ramped `MobSpawner` entry, so 5/10/20 cannot be selected by tier without a new director/configuration or scene-specific setup. |
| Per-tier mob HP and mob damage | **No field** | `_duckMaxHealth=6` and `_duckAttackDamage=1` are global BattleBalanceConfig values. |
| Per-tier mob/building currency drops | **No field** | The mob probability array and House min/max/chance are global. Only Houses pay the building drop (`Assets/Scripts/Destruction/DestructibleObject.cs:199-221`). |
| Attack rate in the balance window | **No field** | The rate is a prefab cooldown. Ranged has a separate 0.25-second cooldown, but fresh `GunPower=0`, so it is not the fresh player’s damage source. |
| Fight-time budget, kill fraction, or active-mob cap | **No field** | Mob spawn progress is accumulated and there is no active-mob cap; the spawner only checks that the battle is active (`Assets/Scripts/Combat/MobSpawner.cs:100-134`). |
| Level-based price curve or per-stat cap | **No field** | Purchases subtract one fixed configured cost and add one fixed configured amount (`Assets/Scripts/Model/CharacterService.cs:147-216`). `_maxUpgradeLevel` is a shared cap for every stat, not a per-stat cap. |

## 2. Proposed starting values

The table below is a deliberately small starting test, not a claim that these values are already
supported by the project. `code-backed` means the value can be traced directly to an existing field;
`derived` means the arithmetic is shown in section 3; `invented` means the repository has no value
for it. The proposed rows treat one 30-second segment as ending at one next-tier House and assume
one such House per segment.

### Combat, route, and mob targets

| Segment | Player damage while taking the five-hit gate | Attack rate / single-target DPS | Next House HP | Speed used on the link | Link distance / walk time | Mob spawn rate | Mob HP | Mob damage |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Tier 0 → T1 | 5 **[invented]** | 2/s **[code-backed]** / 10 DPS **[derived]** | T1 = 25 **[derived, no current per-tier field]** | 9 **[derived target; start is current 6]** | 80 world units / 8.89 s **[distance invented; 80 = 10 current map cells]** | 5/s **[user target; no field]** | 5 **[invented; one global field, chosen to equal Tier-0 damage]** | 1 per bite **[code-backed current global value]** |
| Tier 1 → T2 | 25 **[derived after 1 attack purchase]** | 2/s **[code-backed]** / 50 DPS **[derived]** | T2 = 125 **[derived, no current per-tier field]** | 14 **[derived target]** | 80 world units / 5.71 s **[distance invented]** | 10/s **[user target; no field]** | 5 **[same global field; no per-tier field]** | 1 per bite **[code-backed current global value]** |
| Tier 2 → T3 | 125 **[derived after 5 more attack purchases]** | 2/s **[code-backed]** / 250 DPS **[derived]** | T3 = 625 **[derived, no current per-tier field]** | 20 **[user target / derived purchase target]** | 80 world units / 4.00 s **[distance invented]** | 20/s **[user target; no field]** | 5 **[same global field; no per-tier field]** | 1 per bite **[code-backed current global value]** |

The one-hit checks are: T1 `25/25 = 1` hit in Tier 1, T2 `125/125 = 1` hit in Tier 2, and the
five-hit checks are T1 `25/5 = 5`, T2 `125/25 = 5`, and T3 `625/125 = 5`. The proposed mob HP is
one writable global value, not three runtime values; keeping it at 5 makes a fresh Tier-0 attack
kill one mob per eligible hit. If mob durability should rise by tier, that is a new field and the
values are not directly applicable to the current code.

### Currency and progression knobs

| Knob | Proposed starting value | Status / rationale |
|---|---:|---|
| `StartingAttackPower` | 5 | **Invented**, replacing the current serialized 50. It makes the first five-hit gate 25 HP instead of letting the current 50 damage one-shot the current 20-HP House. |
| `StartingSpeed` | 6 | **Code-backed current value**; keep it. |
| `AttackPowerUpgradeAmount` | 20 damage | **Derived/invented**. A single global step of 20 gives 5 → 25 → 125 → 625 with 1, 5, and 25 purchases. |
| `SpeedUpgradeAmount` | 1 world unit/s | **Code-backed current value**; keep it. |
| `AttackPowerUpgradeCost` | 10 coins | **Invented** fixed price; it is near the current 7-coin price and makes the stage budget simple. |
| `SpeedUpgradeCost` | 10 coins | **Invented** fixed price; it matches the attack price for a readable first test. |
| Incremental attack purchases in T0/T1/T2 | 1 / 5 / 25 | **Derived** from the 20-damage step. |
| Incremental speed purchases in T0/T1/T2 | 3 / 5 / 6 | **Derived** from 6 → 9 → 14 → 20 at a 1-unit step. |
| Attack + speed spend in T0/T1/T2 | 40 / 100 / 310 coins | **Derived**: `(1+3)×10`, `(5+5)×10`, `(25+6)×10`. |
| `MaxUpgradeLevel` | 32 | **Derived/invented**: 31 attack purchases are needed, and level 1 plus 31 purchases is level 32. The current cap 30 stops at `5+29×20 = 585`, below the 625 damage required for the T3 one-hit state. This shared cap would also permit speed 37, so 20 is a Tier-2 target, not an enforced hard cap. |
| Expected mob drop | 1 coin/kill | **Invented**, using the existing three-slot array shape with `[0.25, 0.50, 0.25]` for 0/1/2 coins: `0×.25+1×.50+2×.25 = 1`. This remains one global distribution. |
| House drop | 10 coins/House | **Invented**, using `_buildingCoinDropMin = 10`, `_buildingCoinDropMax = 10`, `_buildingCoinDropChance = 1`; the inclusive integer roll is nominally deterministic (apart from the negligible `Random.value == 1` edge in the current `>=` check). |
| Mob kill fraction used for the first economy test | 60% of spawned mobs | **Invented assumption**, not a field. It must be measured in Play Mode because melee can damage every eligible target in its cone/radius, while the cooldown is only 2 attacks/s. |

## 3. Derivation

### Hit counts, DPS, and attack purchases

Use `r = 1/0.5 = 2` melee attacks/s from the player prefab. For a single building, use the
conservative continuous budget `time = HP/(damage×r)`. This assigns 2.5 seconds to a five-hit
gate; if the first attack lands immediately at `t=0`, the fifth attack completes after four
intervals, or 2.0 seconds. The extra 0.5 seconds is intentional schedule headroom.

Let `d0` be the Tier-0 damage. The requested five-hit/one-hit pattern imposes:

```text
B1 = 5 d0                  T1 is five hits in Tier 0
d1 = B1 = 5 d0             T1 is one hit after the Tier-0 attack upgrade
B2 = 5 d1 = 25 d0          T2 is five hits in Tier 1
d2 = B2 = 25 d0             T2 is one hit after the Tier-1 attack upgrades
B3 = 5 d2 = 125 d0         T3 is five hits in Tier 2
d3 = B3 = 125 d0            T3 would be one hit after the Tier-2 attack upgrades
```

Choosing the small, readable invented baseline `d0=5` gives:

```text
T1 HP = 5×5   = 25
T2 HP = 5×25  = 125
T3 HP = 5×125 = 625
```

The current progression code uses one global additive attack step. To hit these exact integer
breakpoints without overshoot, choose the derived step `a=25−5=20`:

```text
5  + 1×20 = 25       => 1 purchase in Tier 0
25 + 5×20 = 125      => 5 more purchases in Tier 1
125+25×20 = 625      => 25 more purchases in Tier 2
```

Total attack purchases are `1+5+25=31`. Therefore the shared level cap must be at least
`1+31=32`; with the current cap 30, only 29 purchases are possible. The proposed attack DPS is
`damage×2`, hence 10, 50, and 250 DPS in the three rows.

The current asset is not close to this curve: it starts at 50 damage while a House is only 20 HP,
so `ceil(20/50)=1` hit before any purchase. The current +1 step cannot repair the requested
five-to-one pattern economically: from a hypothetical 5 damage it would require 4, 20, and 100
attack purchases rather than 1, 5, and 25.

### Speed and distance

The repository supplies a current player speed of 6 world units/s and a +1-unit/s speed step.
There is no route length, so choose an **invented** test link of 80 world units. This is 10 map
cells because the active MapData has `cellSize=8` (`Assets/Scenes/BattleScene/BattleScene/MapData.asset:15`).

The proposed target speeds are 9, 14, and 20:

```text
6  + 3×1 = 9     => 80/9  = 8.89 s < 10
9  + 5×1 = 14    => 80/14 = 5.71 s < 10
14 + 6×1 = 20    => 80/20 = 4.00 s < 10
```

The incremental purchases are therefore 3, 5, and 6. These speeds are targets after the relevant
purchases; the code has no purchase-animation or upgrade-popup duration to subtract. For an exact
10-second route, the general formula is `distance = 10×speed`; the corresponding 90, 140, and 200
world-unit routes are also invented and are not present in the map data.

The current HouseSet radial boundaries convert to approximately 40, 96, and 144 world units, but
they are probabilities over grid distance rather than target-to-target routes. In particular, an
80-unit location is 10 cells from the origin and falls in the mixed 5–<12-cell band, not a
deterministic T1/T2 boundary.

### Explicit 30-second time budget

For a test schedule, count one link, one five-hit next-building gate, and a remaining mob-attention
window per segment. The building-hit column uses the damage before that row’s attack purchases; the
link column uses the speed after that row’s speed purchases. This makes the buy-before-the-next-link
assumption explicit, while the exact buy point remains one of the design gaps. Mobs actually spawn
concurrently with movement and attacks; “mob time” below is an accounting envelope, not a separate
engine phase.

| Segment | Walking | Five-hit building work | Mob-attention budget | Sum |
|---|---:|---:|---:|---:|
| Tier 0 → T1 | `80/9 = 8.89 s` | `25/(5×2) = 2.50 s` | `30−8.89−2.50 = 18.61 s` | `8.89+2.50+18.61 = 30.00 s` |
| Tier 1 → T2 | `80/14 = 5.71 s` | `125/(25×2) = 2.50 s` | `30−5.71−2.50 = 21.79 s` | `5.71+2.50+21.79 = 30.00 s` |
| Tier 2 → T3 | `80/20 = 4.00 s` | `625/(125×2) = 2.50 s` | `30−4.00−2.50 = 23.50 s` | `4.00+2.50+23.50 = 30.00 s` |

The one-hit checkpoints are only 0.5 seconds of cooldown budget each (`1/2`), so they fit inside
the same building-work allowance. The prose is ambiguous about whether the one-hit version of T1
is meant before or after the T1 building has already been destroyed; this schedule interprets the
five-hit target as the next building in the segment and the one-hit target as the upgraded version
of the current tier’s building.

### Mob counts and income

For the user-specified constant rates, the nominal spawned mobs per 30-second segment are:

```text
5×30  = 150
10×30 = 300
20×30 = 600
```

Using the explicitly invented first-test kill fraction `q=.60` gives 90, 180, and 360 kills. With
the proposed expected drop of one coin per kill and one 10-coin House per segment:

| Segment | Spawned | Killed at 60% | Mob coins | House coins | Total income | Upgrade spend | Income minus spend |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Tier 0 | 150 | 90 | `90×1 = 90` | 10 | 100 | 40 | +60 |
| Tier 1 | 300 | 180 | `180×1 = 180` | 10 | 190 | 100 | +90 |
| Tier 2 | 600 | 360 | `360×1 = 360` | 10 | 370 | 310 | +60 |

Each segment independently funds its intended purchases, before carrying the surplus forward. With
the current drop values, the same 60% assumption yields only `kills×0.5 + 7` coins: 52, 97, and
187. Starting from zero and carrying leftovers, Tier 0 leaves 12 after its 40-coin spend; Tier 1
leaves 9 after spending 100; Tier 2 then has `187+9=196`, short of the required 310 by 114 coins.
That is the concrete reason the current economy cannot fund the proposed late attack curve at this
kill rate.

The current scene’s actual spawn curve is also different. Integrating its 1→15 linear ramp gives
`∫(1+14t/60)dt = 135` mobs in the first 30 seconds, 345 in seconds 30–60, and 450 in seconds
60–90, for 930 over the nominal 90-second battle. Those are projections of the existing ramp, not
three authored tier rates; failed spawn positions can reduce the actual count, while pending spawn
progress can accumulate.

### Mob damage pressure

The current Mob attacks once, sets `_hasAttacked`, and attaches; it does not apply repeated bite DPS
(`Assets/Scripts/Combat/Mob.cs:98-109`). With a 60% kill fraction and one global damage of 1,
the 40% that survive each segment represent 60, 120, and 240 bites, or 60, 120, and 240 incoming
damage. Against the current fresh player MaxHealth of 1000, this is 420 total damage across the
three segments if all damage arrives and no health is restored. That is a survivability baseline,
not a damage-per-second model.

## 4. Biggest gaps

1. **No actual tier identity or per-tier building health — highest impact.** The code can only look
   up House HP by the single `House` enum row, currently 20 HP. House01_D2 and House01_D3 are still
   `House`, and HouseSet only changes prefab selection/weights. The requested 25/125/625 HP curve
   therefore cannot be authored in the current window. Failure mode: every tiered house remains 20
   HP, so the current 50-damage player one-shots every one and the requested five-hit gates never
   exist.

2. **The existing income curve does not fund the required upgrade curve.** At the explicit 60%
   kill assumption, current drops produce 52/97/187 coins against 40/100/310 proposed spend, and
   Tier 2 is 114 coins short even after carrying the prior remainder. The current 0.5 expected
   mob drop, 5–9 House drop, and fixed prices are global. Failure mode: the player reaches T2 with
   enough money for early upgrades but cannot buy the 25 late attack upgrades needed to reach 625
   damage. The project’s own progression notes also call exact prices and drop frequency unfinished
   (`Design/Progression.md:30-37`).

3. **There is no distance target or route-length measurement.** `HouseSet` chooses by radial grid
   distance, while movement time depends on an actual navigable path, collisions, and detours. The
   current 8-unit map cell size gives useful scale, but no T1→T2 or T2→T3 pair is authored. Failure
   mode: an 80-unit straight-line target can become longer than 10 seconds after roads/buildings and
   flow-map detours, so the target cannot be verified from the asset alone.

4. **Mob spawning does not implement the requested 5/10/20-per-second tiers and has no cap.** The
   active scene has one 1→15/s ramp over the first 60 seconds. At the requested late rate, 20×30
   means 600 spawns in a segment. A single-target reading of the current 0.5-second melee cooldown
   can only process 60 attacks in 30 seconds; the real capacity depends on the undocumented number
   of mobs simultaneously inside the cone/radius. Failure mode: the swarm either overwhelms the
   player, builds an unbounded active population, or makes economy output depend on accidental mob
   clumping. The implementation audit explicitly records the missing active-mob cap
   (`Design/ImplementationStatus.md:46-48`).

5. **Mob damage is a one-time contact event, not a tunable DPS curve.** A mob bites once and then
   attaches; there is no bite cooldown or per-tier damage. With MaxHealth 1000 and one damage per
   mob, current damage is negligible unless many mobs reach the player. Failure mode: increasing
   spawn rate changes both economy and a burst of contact damage, while changing mob damage cannot
   express a rising Tier-2 threat without a new stage-aware field.

6. **The 90-second battle clock is not three enforced 30-second stages.** `BattleService` owns one
   90-second timer plus a 5-second timeout grace window; it does not stop, advance, or reset at T1
   and T2 (`Assets/Scripts/Model/BattleService.cs:74-109,117-132`). Timer respawn also resets to the
   base duration, not the saved Timer bonus (`Assets/Scripts/Model/BattleService.cs:185-191`).
   Failure mode: a player can spend longer or shorter than 30 seconds in a spatial tier, and the
   intended three-stage timing/economy cannot be observed as a state transition.

7. **The shared level cap conflicts with the stated speed ceiling.** Current level 30 allows speed
   35 from a starting 6. The proposed attack curve needs level 32, which would allow speed 37 because
   the cap is shared. There is no “speed max 20” field. Failure mode: raising the cap to make the
   T3 attack target reachable silently permits seven extra speed upgrades; keeping the cap at 30
   makes the proposed T3 attack target unreachable.

8. **The hit-count wording and purchase timing are self-contradictory.** T1 is described as five
   hits in Tier 0 and one hit in Tier 1, even though Tier 0 is also described as the duration “till
   T1.” The code has no rule for when coins become spendable relative to the building hit, how long
   an upgrade popup consumes, or how many buildings are cleared per 30 seconds. Failure mode: two
   implementations can both claim to satisfy the prose while giving different money, walk, and hit
   budgets. The derivation above makes one explicit convention so it can be tested.

9. **Saved progression can invalidate every fresh-value test.** `PlayerPrefs` values override the
   asset defaults and persist AttackPower, Speed, coins, and other stats (`Assets/Scripts/Model/Storage.cs:12-24,134-143`). Failure mode: a Play Mode run appears to violate the 5-hit/30-second reference because it starts with an old 50-attack or high-coin save rather than the proposed baseline.

## 5. Sensitivity

| Number / knob | Sensitivity | Why |
|---|---|---|
| AttackPower ÷ House HP | **Load-bearing** | Hit count is integer-stepped. At T1, 25 damage against 25 HP is one hit; 24 damage against 25 HP is two hits. Small changes cause a full extra cooldown interval. |
| Attack upgrade step and shared max level | **Load-bearing** | The late curve needs exactly 31 attack purchases. One missing allowed purchase leaves 20 damage short at the proposed endpoint. |
| Link distance ÷ speed | **Load-bearing** | At 80 units, speed 9 is 8.89 s, speed 8 is exactly 10 s and fails a strict “less than 10” requirement. A one-unit change crosses the goal. |
| Mob rate × kill fraction × expected drop | **Load-bearing** | At Tier 2, each extra expected coin per killed mob is 360 coins at the 60% assumption; the same factors determine active-mob load and contact damage. |
| Upgrade costs | **Load-bearing** | The Tier-2 intended spend is 310 coins. A one-coin price change across 31 purchases changes the requirement by 31 coins. |
| Melee cooldown, cone/radius, and mob clustering | **Load-bearing** | Cooldown alone gives 60 single-target attacks in 30 seconds, while the requested Tier-2 spawn count is 600. The unknown multi-target hit count determines whether the 60% economy assumption is plausible. |
| Building count per segment | **Load-bearing** | The proposed economy assumes one 10-coin House drop. A second House adds 10 coins; zero Houses removes it. No count is currently authored. |
| Mob HP and one-time bite damage | **Medium to high** | HP controls whether an attack clears one or several mobs; bite damage controls burst survival, but current global fields cannot express tier scaling. |
| HouseSet difficulty weights | **Medium** | They change how often a visual D2/D3 prefab appears, but they do not change HP today. They become load-bearing only after House HP becomes tier-aware. |
| `BattleDuration`, timeout grace, and upgrade UI time | **Medium** | The 30-second arithmetic has no backing stage clock and the game adds a 5-second timeout grace. Any real upgrade-popup time must be taken from the mob-attention budget. |
| Max-health scale, special spawn ranges, and timer/gun prices | **Low for this first melee loop** | They matter to other systems, but do not set the requested five-hit House thresholds or route arithmetic unless the test also relies on health-bar specials, ranged attacks, or timer respawns. |
| Hit-FX curve/duration/blink, decay, visibility, and coin pickup distance | **Safe to tune initially** | These are feedback, cleanup, culling, or cosmetic placement values. Keep them within sensible performance/legibility bounds; they do not directly change damage, spawn income, or route time. |

The first Play Mode test should therefore measure only four unknowns before fine-tuning anything else:
actual navigable link length, mobs killed per melee attack at each density, coins earned per 30-second
segment, and the time consumed by the in-level upgrade interaction. Those measurements decide whether
the invented 80-unit link and 60% kill fraction are valid; the formulas above can then be retuned
without changing the rest of the reference.
