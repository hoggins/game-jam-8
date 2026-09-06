# Timer-route pacing proposal

## Executive recommendation

Keep timer placement on a predefined Goalward zig-zag. Do not adapt the route to
current attack power: the intended 100--150 second level is a pressure schedule, and
a player who does not buy the required upgrades should eventually fail that schedule.
Instead, make house difficulty use the same route coordinate as the timer. The player
then meets T1, T2 and T3 in the places the authored route expects, rather than at
unrelated Euclidean-radius constants.

The preferred implementation is a route-aligned difficulty field: project each house
cell onto the authored spawn-to-Goal route (ideally the same zig-zag polyline used by
`TimerRespawnService`) and choose its tier from normalized route progress. If the
route length changes, the tier boundaries move with it. If a route-aligned field
cannot be added immediately, tune radial boundaries from the actual route waypoints
as a transitional approximation; do not gate or retreat timer placement based on
attack power.

The recommendation uses values already present in the map and balance data except
where explicitly marked as a tuning choice:

* T1: 50 HP; safe player damage is 10--49.
* T2: 250 HP; safe player damage is 50--249.
* T3: 1250 HP; safe player damage is 250 or more.
* The attack ladder reaches 50 after 4 purchases and 250 after 24 purchases.
* `MapData.cellSize` is 8 world units.
* `difficultyRanges` are currently 0--8, 8--24, 24--41 and 41+ cells; those radial
  bands are the coordinate system to replace or retune.

## 1. Diagnosis

### What the map considers a tier

`MapFiller.Fill` measures `Vector2.Distance(cell, originCell)` and passes that
value to `HouseSet.PickDifficultyLevel`. The origin cell is derived from the
player's spawn position, and one cell is 8 world units. Therefore the current
bands are:

| Map band | World radius | House mix |
| ---: | ---: | --- |
| 0 <= r < 8 cells | 0 <= r < 64 wu | T1 100% |
| 8 <= r < 24 cells | 64 <= r < 192 wu | T1 40% / T2 60% |
| 24 <= r < 41 cells | 192 <= r < 328 wu | T2 40% / T3 60% |
| 41+ cells | 328+ wu | T2 20% / T3 80% |

The damage ladder makes crossing those boundaries meaningful, not cosmetic:

| Attack power | T1 (50 HP) | T2 (250 HP) | T3 (1250 HP) |
| ---: | ---: | ---: | ---: |
| 10 | 5 hits | 25 hits | 125 hits |
| 50 | 1 hit | 5 hits | 25 hits |
| 250 | 1 hit | 1 hit | 5 hits |

The 5-hit values are the intended tier-matched fights. The 1-hit values are the
previous-tier cleanup. Sending a 10-damage player into the 192+ region therefore
creates 25--125-hit fights, not a slightly harder version of the same loop.

### Route advance in the three failed runs

The available per-hop telemetry does not record the anchor's absolute radius. The
best direct route-progress proxy is the cumulative `straightLineDistance` of the
timer hops. It is conservative about the actual route's forward component because
each hop includes lateral geometry, but it shows how quickly the timer sequence
crosses the map bands.

| Run | Attack at 30 s | Attack at 60 s | Attack at 90 s / end | Timer hops | Cumulative hop distance | Approx. cells | Buildings / hits | Building income share | Hits / kill |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `23-31-55` | 10 | end at 53 s: 110 | -- | 4 | 293.5 wu | 36.7 | 5 / 26 | 33 / 233 = 14.2% | 5.2 |
| `23-34-34` | 20 | 60 | end at 70 s: 80 | 5 | 367.7 wu | 46.0 | 5 / 26 | 34 / 270 = 12.6% | 5.2 |
| `23-39-33` | 10 | 60 | 110 | 6 | 443.7 wu | 55.5 | 2 / 42 | 13 / 533 = 2.4% | 21.0 |

The hop distances by run were:

* `23-31-55`: 31.5, 81.4, 88.7, 91.9 wu; cumulative 31.5, 112.9, 201.6,
  293.5 wu.
* `23-34-34`: 34.6, 82.5, 89.3, 84.7, 76.6 wu; cumulative 34.6, 117.1,
  206.4, 291.1, 367.7 wu.
* `23-39-33`: 38.8, 68.7, 82.9, 93.5, 81.7, 78.1 wu; cumulative 38.8, 107.4,
  190.4, 283.9, 365.6, 443.7 wu.

The first two hops already put the route proxy around 107--117 wu, inside the
64--192 wu band where 60% of generated houses are T2. By hop 3 it is at about
190--206 wu, at or beyond the T2/T3 boundary. By hop 4 it is around 284--294 wu,
inside the 40% T2 / 60% T3 band. The last run continued to a T3-heavy 328+ proxy
radius at hops 5--6.

Attack power did not keep pace:

* The best pre-timer run (`20-28-07`) reached attack 100 by 30 s, 150 by 60 s,
  200 by 90 s and 300 by 120 s. It destroyed 39 buildings, with 75 hits and 53.0%
  building-income share.
  It walked 1498.6 wu for only 224.1 wu of straight-line displacement (6.69 detour
  ratio), so travelled distance is not a reliable substitute for route radius.
* The failed runs were at attack 10, 20 and 10 at 30 s. At 60 s the two runs that
  lasted that long were at 60; the third had already ended at 53 s. Thus the route
  had reached the mixed T1/T2 and T2/T3 regions while the players were still in the
  10--60 damage range.
* The `23-39-33` run eventually recorded attack 300, but only after the player had
  spent most of the run with attack 10--110; its two kills came from 42 building
  hits. Reaching the final value late does not repair the time lost to impossible
  targets.

The divergence is therefore approximately one to two timer hops per 30--60 seconds
versus zero to five attack purchases in the same early windows. That divergence is
useful pressure and should remain. The problem is that the current radial bands put
the pressure on the wrong side of the damage ladder: the route's T2/T3 crossings are
not authored to coincide with the intended upgrade milestones. House tiers should
move with the route schedule, while the route continues to expose whether the player
made the right power-up choices. The result is visible in the outcome metrics:
5 / 5 / 2 building kills instead of 39, 14.2% / 12.6% / 2.4% building income instead
of 53.0%, and 5.2 / 5.2 / 21 hits per kill instead of 75 / 39 = 1.92 overall in the
good run.

### Timer-budget evidence

The current route balance in the asset is `travelSpeedFactor = 0.8`,
`pathOverhead = 1.45`, `wuPerBuilding = 38`, `secondsPerBuilding = 2.5`,
`slack = 1.15`, and `minSeconds = 12`. `CalculateTimerBudget` computes:

```text
path   = straightLineDistance * 1.45
travel = path / (0.8 * playerSpeed)
build  = (path / 38) * 2.5
T      = max(1.15 * (travel + build), 12)
```

For example, a 42-wu hop at speed 6 gives:

```text
path = 42 * 1.45 = 60.9 wu
T = 1.15 * (60.9 / (0.8 * 6) + (60.9 / 38) * 2.5)
  = 17.8 s
```

That agrees with the intended ~17-second first-hop budget. The failed runs recorded
12.4--36.9 seconds per hop, but every `secondsRemainingOnArrival` value was 0,
including hops marked `reached: true`. That field cannot yet demonstrate the desired
15--20% arrival reserve; it must be treated as a telemetry/semantics issue until a
run produces non-zero values.

## 2. Candidate coupling mechanisms

### Mechanism A: fixed route with route-aligned house tiers

Use one authored progress coordinate for both systems. For a house cell, find its
closest point on the predefined spawn-to-Goal route (preferably the same zig-zag
polyline used by `TimerRespawnService`) and call the distance along that polyline
`s`. Let `S` be the route's total length and `u = s / S`.

The measured forward schedule is approximately 30, 30, 60, 60, 60, 60 wu. Its
two-hop checkpoints are therefore:

```text
after hop 2: 30 + 30 = 60 wu,       u ~= 60 / 300 = 0.20
after hop 4: 30 + 30 + 60 + 60 = 180 wu, u ~= 180 / 300 = 0.60
after hop 6: 300 wu,                u ~= 1.00
```

The `300` total is the sum of the measured target forward legs; the actual `S`
should be queried from the route and can be about 327 wu for the current map. If
`S = 327`, the same normalized checkpoints are about 65.4 and 196.2 wu along the
route. These are route distances, not radial distances from spawn.

Assign tiers by route stage:

```text
0.00 <= u < 0.20: T1
0.20 <= u < 0.60: T2
0.60 <= u <= 1.00: T3
```

This gives two timer hops of T1, two of T2 and two of T3. The percentages are a
design choice grounded in the six-hop/two-hops-per-stage schedule; the route leg
lengths make the middle stage longer. A route corridor can use the nearest point on
the polyline. For cells outside the corridor, either extend the perpendicular stage
bands across the generated map or keep the existing radial rule as an explicit
fallback; the choice should be consistent across a whole run.

Tradeoffs:

* It preserves the intended pressure exactly: timers keep moving even when the
  player makes poor power-up choices.
* It removes the mismatch between zig-zag progress and radial `difficultyRanges`.
  Changing the initial player-to-Goal distance moves tier boundaries automatically.
* It requires MapFiller to know the same route definition before it assigns house
  difficulty. A route generated only after map fill cannot drive house tiers without
  changing initialization order or supplying a serialized route definition.
* A player who skips attack upgrades will meet an intentionally bad matchup. That is
  a failure signal for the build, not a reason to move the next timer backward.

### Mechanism B: radial bands calibrated from route checkpoints

If the existing radial `HouseSet.difficultyRanges` format must remain temporarily,
derive its boundaries from the actual fixed route instead of choosing global
constants. Sample the route anchors at the hop-2 and hop-4 checkpoints, measure
their horizontal radius from the same map origin used by `MapFiller`, and set the
T1/T2 and T2/T3 boundaries at those sampled transition radii. Recompute them when
the Goal distance or route geometry changes.

For a current route length `S`, the target route checkpoints are `0.20*S` and
`0.60*S`; their corresponding radial measurements are `r(0.20*S)` and
`r(0.60*S)`. Do not substitute `0.20*S` and `0.60*S` directly for radii: the
zig-zag lateral offset makes those quantities different. The exact conversion is
geometry-dependent and must be sampled from the route.

Tradeoffs:

* It is a smaller change to `HouseSet` and can work with the current radial picker.
* It is only an approximation. A single circle can intersect a zig-zag route more
  than once, and houses beside a lateral leg can receive a tier that does not match
  the player's next target.
* It must be recalculated after route changes. A hand-entered pair such as 64 / 192
  wu will drift again when the initial-to-Goal distance, lateral ratio or hop ranges
  change.

### Mechanism C: damage-adaptive route placement

Cap or slow `_routeProgress` according to attack power, for example at 56 wu below
50 damage and 184 wu below 250 damage, then release the cap at 250. This is the
approach described by the original draft of this document.

Tradeoffs:

* It is the strongest protection against impossible houses.
* It directly weakens the desired fixed 100--150 second pressure and can make a
  player who made poor choices feel that the level waits for them.
* It still does not solve the root coordinate mismatch if houses remain radial; it
  merely keeps the timer from exposing that mismatch as quickly. It should be a
  diagnostic or emergency fail-safe, not the normal route policy.

## 3. Recommended implementation contract

Adopt Mechanism A. Keep timer placement deterministic and use power-up progress as a
success criterion, not as a route input:

1. Define or serialize the route once, including its origin, Goal endpoint, zig-zag
   geometry and the six forward legs. `TimerRespawnService` and `MapFiller` must
   consume that same definition.
2. At map generation, convert each house cell to normalized route progress `u` by
   nearest-point projection onto the route polyline. Keep `MapData.cellSize = 8` for
   cell placement, but do not interpret route progress as cells.
3. Assign T1/T2/T3 from `u < 0.20`, `0.20 <= u < 0.60`, and `u >= 0.60`.
4. Keep the current timer hop schedule: approximately 30, 30, 60, 60, 60, 60 wu
   forward, with the existing lateral zig-zag and placement validation.
5. Validate the player's choices at the stage checkpoints rather than changing the
   route. The intended thresholds are attack 50 by the T1/T2 transition and attack
   250 by the T2/T3 transition. These are derived from 4 and 24 attack purchases;
   they are expectations to expose in telemetry, not runtime gates.
6. If the route length changes, keep the normalized 0.20 / 0.60 thresholds so the
   two-hop stage structure follows the new Goal distance. If the number of hops
   changes, derive the thresholds from the new authored checkpoint hops instead of
   retaining these fractions.

The resulting progression is:

| Route stage | Checkpoint | Intended houses | Expected attack choice |
| --- | --- | --- | --- |
| T1 | hops 1--2, `u < 0.20` | T1 only | Buy 4 attack upgrades: 10 -> 50 |
| T2 | hops 3--4, `0.20 <= u < 0.60` | T2 | Keep buying attack: 50 -> 250 in 20 more purchases |
| T3 | hops 5--6, `u >= 0.60` | T3 and final Goal approach | Arrive with 250 to make 5-hit T3 fights |

The attack cap of 30 already contains the 24 purchases needed for the ladder. No
route-specific cap or damage change is required. A player who buys health, speed or
other upgrades instead of enough attack is expected to lose the race; that is the
intended consequence of the fixed pressure schedule.

## 4. Predicted effect and verification

The fixed route deliberately separates success from failure. A player who makes the
intended attack purchases should see the good-run economy; a player who spends those
coins elsewhere should be exposed to the 25- or 125-hit wall and can lose within the
100--150 second pressure window. The route must not soften that outcome by waiting
for the player's damage.

| Metric | Failed baseline | Expected signal for a correct build |
| --- | ---: | --- |
| Buildings destroyed | 5, 5, 2 | Move toward the pre-timer baseline of 39 per 124 s; low kills remain an expected failure signal when attack checkpoints are missed. |
| Building-income share | 14.2%, 12.6%, 2.4% | A successful run should move toward the good-run 53.0%; 40%+ overall is a proposed first-pass target, not measured ground truth. |
| Building hits / kills | 5.2, 5.2, 21.0 | Successful tier-matched fights should be about 5 hits; after the next breakpoint, previous-tier kills should be 1 hit. The 21-hit tail should identify a failed power-up path. |
| Route stage / attack | Attack 10/20/10 at 30 s while route reached outer bands | At hop 2, record whether attack is at least 50; at hop 4, record whether it is at least 250. These are success checkpoints, not route gates. |
| Timer arrival reserve | 0 in all 15 recent hop records | After telemetry semantics are confirmed, target a median `secondsRemainingOnArrival / secondsGranted` of 0.15--0.20. A zero value is still an instrumentation finding. |

The next run should report, per timer hop:

```text
hopIndex
routeProgress / totalRouteProgress
attackPowerAtSpawn
intendedHouseTier
pathWalked / straightLineDistance
secondsGranted
secondsRemainingOnArrival
reached
```

The existing `timerHops` fields provide `hopIndex`, path distances, granted seconds,
arrival seconds and `reached`; they do not provide route progress, attack power or
the intended house tier. The key checks are:

* route progress is approximately 0.20 after hop 2 and 0.60 after hop 4;
* a successful run reaches attack 50 before the T2 stage and attack 250 before the
  T3 stage;
* `reached` is at least 0.9 over a run;
* reached-hop path-overhead ratios are reviewed as a distribution. The existing
  reached-hop aggregate is about 1.47, close to the configured 1.45; do not replace
  1.45 based on the incomplete-hop ratios of 0.42 and 0.75;
* a successful run improves building income and hits-per-kill without requiring
  route adaptation. A failed run should be explainable by its checkpoint power-up
  choices.

## 5. Risks and timer-budget interaction

### Deliberate failure versus accidental stalling

With a fixed route, a player below 50 damage will eventually face T2 and a player
below 250 will eventually face T3. That is intentional pressure, but it is only fair
if the earlier stages contain enough tier-matched houses and the shop offers enough
income to reach the checkpoints. The first two hops are T1, so the player has the
5-hit T1 economy and four attack purchases as the clear opening objective. The next
two hops are T2, so the player must continue choosing attack rather than relying on
health or speed to reach the T3 stage.

The route reaches the Goal after the final authored hops for a player who complies;
there is no adaptive ceiling that can strand a slow build. If a compliant run still
times out before the Goal, tune the fixed hop budgets or the route-aligned house
density, not the player's route permission.

This proposal does not add a Goal damage gate. `TheGoal` currently wins when its
destructible body is destroyed, without checking route progress or attack power. If
the Goal is physically reachable from an unintended map direction, a timer route
alone cannot prevent an early win; that remains a level-design or Goal-state decision.

### Timer duration and the 100--150 second target

The expected hop table gives approximately 17, 13, 21, 18, 16 and 15 seconds, or
about 100 seconds in total. Using the current serialized `pathOverhead = 1.45`,
the same formula produces approximately 17.8 and 14.3 seconds for 42-wu hops at
speeds 6 and 9, and approximately 21.2, 17.5, 14.9 and 13.5 seconds for 85-wu
hops at speeds 12, 16, 21 and 25. The sum is still about 99 seconds. Initial
movement, combat and any non-timer opening time provide the remaining margin toward
the intended 100--150 second full-level duration.

The build term is also consistent with the measured density: a 42-wu hop becomes
60.9 path wu and estimates `60.9 / 38 = 1.6` buildings, or about 4.0 seconds of
building work at 2.5 seconds each. An 85-wu hop becomes 123.25 path wu and estimates
about 3.2 buildings, or 8.1 seconds of building work. Route-aligned tiers make those
estimates meaningful at each stage instead of charging the player for houses they
cannot kill.

Do not recalculate a hop budget from a power-adapted distance. Calculate it from the
fixed route anchor and keep `_timerSlack` as the global timing dial. If the current
clock replacement semantics restore each `secondsGranted`, the six-hop sum is a
bounded pacing schedule; any additive or repeated reset behavior should be checked
because it can push a run beyond 150 seconds.

The recent zero arrival reserves must be resolved before tuning slack. Confirm whether
`secondsRemainingOnArrival` is sampled before or after the clock is replaced. Once it
is valid, use the 15--20% median target to tune slack globally, not to move individual
timer anchors for weak players.

### Route and house-generation data must stay unified

The first timer is 25--35 wu from spawn, the route has lateral offsets, and the map
currently measures difficulty in cells from the spawn. A scalar radius such as 64,
192 or 328 wu cannot reliably describe a zig-zag's stage transitions. The route
definition must be available before `MapFiller` assigns houses, or the route must be
serialized so both systems can consume it deterministically.

If the project keeps radial bands as a short-term bridge, calculate the boundary
radii from sampled hop-2 and hop-4 route anchors every time the Goal or route changes.
Do not hardcode the current 64 / 192 boundaries as if they were route coordinates.

### Persistence across runs

`Storage` persists attack power, speed and coins. A run beginning at attack 250 should
receive the full fixed Goalward route; a fresh-save run is required to measure the
intended 10 -> 50 -> 250 choices. If combat stats later reset per battle, the same
authored route and route-aligned house field continue to work.

## Sources

* `doc/balance-run-evaluation.md`
* `doc/patch-note-1.md`
* `artifacts/run-2026-09-05T20-28-07.147Z.json`
* `artifacts/run-2026-09-05T23-31-55.010Z.json`
* `artifacts/run-2026-09-05T23-34-34.795Z.json`
* `artifacts/run-2026-09-05T23-39-33.670Z.json`
* `Assets/Scripts/Map/MapFiller.cs`
* `Assets/Scripts/Map/HouseSet.cs`
* `Assets/Resources/Map/HouseSet.asset`
* `Assets/Scripts/Timer/TimerRespawnService.cs`
* `Assets/Scripts/Balance/BattleBalanceConfig.cs`
* `Assets/Resources/BattleBalanceConfig.asset`
* `Assets/Scripts/Model/CharacterService.cs`
* `Assets/Scripts/Telemetry/EconomyTelemetryService.cs`
* `Assets/Scripts/Destruction/TheGoal.cs`
