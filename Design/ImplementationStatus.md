# Статус реализации

Аудит от **5 сентября 2026**. Дизайн сначала сверен с полным transcript
[sync-встречи](https://fathom.video/share/vtzRsJNys75YeiBosoqPy8Gx8qRKmVVc),
который является [главным источником истины](SourceOfTruth.md), затем — с C#,
сценами, префабами и ScriptableObject-ассетами.

Обозначения: ✅ готово и подключено; 🟡 есть рабочая часть; ⬜ реализации нет;
⚠️ код работает по отменённому/другому лупу.

Проверка ограничена статическим аудитом. Автотестов в проекте нет, а `unity test`
не дошёл до компиляции: Unity Licensing Client не открыл IPC-канал, и CLI
завершил процесс по timeout 180 с. Пункты ✅ означают, что код и сохранённая
wiring-конфигурация найдены; нужен Play Mode smoke test.

## Короткий итог

Сейчас собран хороший combat/destruction prototype и первая техническая
вертикаль разрушаемого HUD — физический Timer с живой камерой в HUD. Новый
утверждённый луп ещё не замкнут: разрушение таймера останавливает его навсегда,
вместо reset/respawn; нет остальных HUD-объектов, in-level upgrades и мега-босса.

## Готово

| Статус | Система | Свидетельство в проекте |
|---|---|---|
| ✅ | Сцены и базовая навигация | Main menu, BattleScene, loading overlay, pause, defeat/win popup prefabs и переходы включены в Build Settings |
| ✅ | Игрок | Input System movement, анимация и melee attack по мобам и разрушаемым объектам |
| ✅ | Attack и HP persistence | Значения, покупки и `PlayerPrefs` storage существуют; UI подключён в main menu |
| ✅ | Один тип уточки | Движение, контактный урон/прилипание, здоровье, death animation, pooling и монетный drop |
| ✅ | Hit FX мобов | `MeleeWeapon` создаёт hit FX при попадании по Mob |
| ✅ | Поток мобов | Частота линейно растёт от 1 до 15 мобов/с за первые 60 с, спавн идёт вне камеры |
| ✅ | Разрушаемый город | Здоровье домов, физические части, освобождение flow-map, decay и renderer culling |
| ✅ | Генерация окружения | Seeded fill большой сохранённой сетки, уровни домов по дистанции и unique placement |
| ✅ | Физический Timer | Четыре разрушаемые цифры, countdown и физический debris prefab |
| ✅ | Первая живая HUD-трансляция | Timer camera → runtime RenderTexture → `BattleTimerWidget` |
| ✅ | Монетная база | Монеты начисляются за уточек, сохраняются и показываются в progression UI |

## Частично готово или конфликтует с новым лупом

| Статус | Решение встречи | Текущее состояние | Осталось |
|---|---|---|---|
| ⚠️ | Разбитый Timer сбрасывает время и респаунится | Уничтожение всех цифр вызывает `DestroyTimer()` и навсегда останавливает countdown | Сделать reset/cleanup/respawn и продолжить ту же сессию |
| 🟡 | Timer всегда появляется и становится новой целью | В `HouseSet` он unique и ставится в случайную подходящую клетку | Проверить старый spawn bug; выбирать достижимую новую позицию, не повторять предыдущую |
| 🟡 | Поражение при пропуске Timer | Timeout defeat работает после короткой паузы на `00:00` | Проверить вместе с многократным respawn и активным upgrade popup |
| ⚠️ | Прокачка внутри текущего уровня | После defeat/win игра возвращается в main menu и открывает старый progression screen | Сделать Upgrade HUD-объект и in-level popup без завершения BattleService |
| 🟡 | Attack и HP уже реализованы | Покупки работают, но цены обеих = 3, HP стартует с debug-значения 300000, caps/curve нет | Перенести в новый random upgrade flow и дать боевой баланс |
| 🟡 | Настраиваемый mob flow длинной сессии | Временная кривая есть, но active-mob cap отсутствует | Перебалансировать для повторных таймеров и добавить cap |
| 🟡 | Arrow — физический HUD-объект | Есть только `ArrowService` с флагом destroyed | Сделать world prefab, camera/widget, tracking текущего Timer и respawn |
| 🟡 | Win popup | UI и `BattleService.WinBattle()` существуют | Нет gameplay-вызова `WinBattle()` |
| 🟡 | Damage feedback крупных объектов | Hit/debris FX есть | Camera shake, staged damage/color feedback и частотный лимит отсутствуют |
| 🟡 | Камера поддерживает рост игрока | В сцене есть Cinemachine camera/noise profile | Нет стартового rebalance дистанции и zoom-out от Size |

## Осталось для утверждённого MVP

1. ⬜ **Timer loop:** reset времени, удаление старого Timer, выбор новой
   достижимой позиции, respawn и перепривязка Arrow/HUD без перезагрузки сцены.
2. ⬜ **Respawn director:** размещать Arrow, HP, Ammo и Upgrade на маршруте к
   следующему Timer; управлять их повторным появлением и cleanup.
3. ⬜ **Arrow HUD-object:** физический указатель, камера, widget, наведение на
   актуальный Timer, разрушение и последующий respawn.
4. ⬜ **HP HUD-object:** отражать текущий HP и пополнять его при разрушении;
   никакой permanent invulnerability или Spare Heart.
5. ⬜ **Machine Gun + Ammo HUD-object:** дальняя атака, расход/ёмкость ammo,
   физический индикатор и refill при разрушении.
6. ⬜ **In-level progression:** Upgrade HUD-object, popup, покупка без выхода из
   боя и рандомизированный набор вариантов.
7. ⬜ **Новые upgrades:** Speed, Size, Timer, несколько Attack upgrades,
   Machine Gun и Ammo; связать Size с camera zoom.
8. ⬜ **Экономика:** уменьшить приток и/или поднять цены, добавить sinks,
   настроить curve под одну длинную сессию.
9. ⬜ **Mega Boss:** prefab/поведение/большой HP, spawn/presence в мире и вызов
   `WinBattle()` после уничтожения.

## Polish / следующие задачи встречи

| Статус | Задача |
|---|---|
| ✅ | Hit FX на мобах |
| ⬜ | Camera shake на разрушении с лимитом |
| ⬜ | Красная damage vignette с проверкой частого урона |
| ⬜ | Начальная дистанция камеры и zoom-out при Size upgrade |
| ⬜ | Интро-комикс/мультик |
| ⏸️ | Floating HP bars отложены; сначала visual damage через цвет/деформацию/части |
| ⬜ | Автотесты Timer reset/respawn, resource refill, upgrade flow, timeout и boss win |

## Распределение со встречи

| Ответственный | Зафиксированные задачи |
|---|---|
| Anatolii | Destructible HUD (Timer/Arrow/HP/Ammo), respawn logic; timer spawn fix/comment; in-level upgrade UI; Speed/Size/Timer stats |
| Maksym | Документировать новый луп; Timer/HUD respawn и баланс прогрессии/экономики после spawn fix |
| Serhii Kryvych | Hit FX на мобах; начальная дистанция/zoom камеры; damage vignette |
| Serhii Soroka | Machine Gun; camera shake; Attack upgrades и randomization согласно action items полного transcript |
| Команда | Intro comic |

Auto-summary Fathom частично иначе распределяет задачи между двумя Serhii. В этой
таблице приоритет отдан именованным action items из полного transcript.

## Проверенные области кода

- Battle/Timer: `Assets/Scripts/Model/BattleService.cs`,
  `Assets/Scripts/Timer/`, `Assets/Resources/Descructable/BattleTimer.prefab`.
- HUD: `Assets/Scripts/SceneHud/`,
  `Assets/Resources/Prefabs/UI/Battle/BattleHud.prefab`.
- Combat: `Assets/Scripts/Weapons/`, `Assets/Scripts/Combat/`,
  `Assets/Prefabs/Player.prefab`, `Assets/Prefabs/Mob.prefab`.
- Map/destruction: `Assets/Scripts/Map/`, `Assets/Scripts/Destruction/`,
  `Assets/Resources/Map/HouseSet.asset`, `Assets/Scenes/BattleScene/BattleScene/MapData.asset`.
- Progression: `Assets/Scripts/Model/Storage.cs`,
  `Assets/Scripts/Model/CharacterService.cs`, `Assets/Scripts/Balance/ProgressionBalance.cs`,
  `Assets/Resources/Prefabs/UI/Meta/MainMenuUi.prefab`.
