# ТАНКИ — Unity 6 (URP)

Навчальний проєкт курсу GoITeens «Gamedev»: 3D-гра, у якій гравець керує танком і бореться з ворожими танками на арені.

**Unity:** 6000.0.66f1 · **Render Pipeline:** URP 17 · **Сцена гри:** `Assets/Scenes/Game.unity`

## Керування

| Дія | Клавіші |
|---|---|
| Рух корпусу | ↑ ↓ ← → або W A S D (назад — 60% швидкості) |
| Поворот башти | Z / X або рух миші |
| Постріл | ЛКМ |
| Зброя | 1 — стандартна, 2 — скорострільна, 3 — важка |
| День / захід сонця | T |
| Перезапуск після кінця гри | R або кнопка «Грати знову» |

## Що реалізовано

- **Уроки 71–74:** Player Controller на Rigidbody, камера за баштою, бойова система через `TankConfig` (ScriptableObject), снаряди, Muzzle Flash, Trail, ефекти влучання, звуки, ворожий AI на NavMesh (Patrol → Chase → Attack).
- **Урок 75:** `IDamageable`, `TankHealth` з подіями, `TankDeath` з вибухом, HUD здоров'я та World Space смуги ворогів.
- **Урок 76 — левел-дизайн і URP:**
  - арена 50 × 50 з центральним і двома бічними маршрутами, головний орієнтир — радіовежа з маячком;
  - укриття: повні (стіни, контейнери, ангари), часткові (мішки з піском, бетонні блоки), кутові, м'які перешкоди (бочки);
  - ієрархія `Level / Ground, Blockout, Environment, Navigation, PatrolRoutes, SpawnPoints, Lighting`;
  - NavMesh перепечений під фінальну геометрію (радіус агента 1.3 під розмір танка);
  - URP/Lit матеріали, Directional Light з м'якими тінями, Global Volume: Tonemapping (ACES), Color Adjustments, Bloom, Vignette, White Balance;
  - ⭐ SSAO, Decal Projector (сліди вибухів), Fog, дві атмосфери (день і захід сонця, клавіша T), скриншоти до/після постобробки.
- **Урок 77 — фінальні правки:** лічильник знищених ворогів, екрани «Гру закінчено» і «Перемога» з перезапуском, перевірка видимості ворога (Raycast, щоб не стріляв крізь стіни), налаштування Player Settings і Windows Build.

## Скриншоти постобробки

| До | Після (день) | Після (захід) |
|---|---|---|
| ![До](Docs/Screenshots/1_before_postprocessing.png) | ![День](Docs/Screenshots/2_after_postprocessing_day.png) | ![Захід](Docs/Screenshots/3_after_postprocessing_sunset.png) |

## ДЗ урок 79 — ООП

Папка `Assets/OOP_Homework`, сцена `Assets/OOP_Homework/OOP_Homework.unity` (WASD — рух гравця, на екрані видно HP ворогів і золото).

- 🟢 **Рівень 1:** `BaseCharacter` (`Health { get; protected set; }`, `characterName`) → `Warrior` з методом `Attack()`.
- 🟡 **Рівень 2:** `BankBox` (`Gold { get; private set; }`, `AddGold()`), `BankBoxTest` — пряма зміна `bankBox.Gold = 1000` дає помилку CS0272, тому золото додається лише через метод.
- 🔴 **Рівень 3:** `Enemy` (здоров'я, `TakeDamage()`, переслідування гравця) → `Orc` і `Troll`; `Troll.Regenerate()` відновлює 5 HP щосекунди через `Update`, одночасно рухаючись за логікою батька.

## Збірка

`File → Build Profiles → Windows → Build`, або меню **Tanks → 5. Build Windows Player** (результат у `Builds/Windows/Tanks.exe`).
