# Galaxy Defender — Work Breakdown
---

## Overview

| Person | Role | Tasks | Est. Hours |
|--------|------|-------|-----------|
| P1 — Lead Dev | Core gameplay systems | 40 | ~120h |
| P2 — Systems Dev | Managers + integration | 35 | ~90h |
| P3 — UI & Level | UI scripts + tilemap + animation | 54 | ~80h |
| P4 — Asset & QA | All assets + testing + docs | 65 | ~60h | 

| **Total** | **194 tasks** | **~350h** | ~11h/person/week over 8 weeks |

---

## PERSON 1 — Lead Core Dev | ~120h | 40 tasks

> Owns the player, all enemies, physics, and core collision. If these break, nothing else works.

### 🔧 Project Setup (8 tasks)
- [X] Create Unity 2022.3 LTS project, configure build target Windows x86-64
- [X] Project Settings → Physics 2D → Gravity = (0, 0); Target Frame Rate = 60
- [X] Create Layers: Player, Enemy, Boss, PlayerBullet, EnemyBullet, PowerUp
- [X] Configure Layer Collision Matrix: PlayerBullet hits Enemy + Boss only; EnemyBullet hits Player only; Enemy ignores Enemy; Boss ignores Enemy
- [X] Define Tags: Player, Enemy, Boss, PowerUp, Checkpoint
- [X] Initialize Git repo + copy Unity `.gitignore` template; first commit
- [X] Create full folder structure: `Assets/Scripts/Player,Enemy,Managers,Systems,UI` + `Prefabs,Sprites,Audio,Scenes,Fonts,Animations`
- [X] Set up AudioMixer: create 3 groups (Master → Music, Master → SFX); expose "MasterVolume", "MusicVolume", "SFXVolume" parameters

### 💻 Core Scripts (9 tasks)
- [X] `ObjectPool.cs` — Generic pool; hard cap 20 player bullets / 30 enemy bullets; auto-return oldest if cap hit; `Get()` and `Release()` methods
- [X] `PlayerController.cs` — WASD/Arrow input; `Rigidbody2D.MovePosition()` each FixedUpdate; clamp position to Camera viewport bounds; Shift key → start dash coroutine
- [X] `PlayerHealth.cs` — maxHP=100, maxShield=50; `TakeDamage(int)` checks `isDashing` flag first; shield absorbs before HP; `OnDeath` UnityEvent; `OnHPChanged`, `OnShieldChanged` events
- [X] `BulletPlayer.cs` — velocity upward 12 u/s; `OnTriggerEnter2D` with tag "Enemy"/"Boss" → `EnemyHealth.TakeDamage(10)` → `Release()` to pool
- [X] `BulletEnemy.cs` — velocity downward 10 u/s; `OnTriggerEnter2D` tag "Player" → `PlayerHealth.TakeDamage(10)` → `Release()`
- [X] `EnemyDrone.cs` — constant downward movement 2 u/s each FixedUpdate; `StartCoroutine(FireRoutine())`: wait 2s → spawn EnemyBullet at BulletSpawnPoint → repeat; `OnBecameInvisible()` → return to pool
- [X] `EnemyHunter.cs` — each FixedUpdate: `rb.position.x = Mathf.MoveTowards(rb.x, player.x, 3.5 * deltaTime)`; move down 1 u/s; fire every 1.5s
- [X] `EnemyHealth.cs` — HP field; `TakeDamage(int)` → if HP ≤ 0 → `OnDeath()` event; `OnDeath`: fire `ScoreManager.AddScore(points)` + 30% chance `PowerUpManager.Drop(transform.position)`
- [x] `BossController.cs` — Phase thresholds at HP 300/200/100; Phase1: stationary, straight shot every 1s; Phase2: sine wave X (A=0.3×screenWidth, period 4s), barrage every 0.7s; Phase3: 3-bullet spread (±15° via `Quaternion.Euler`) every 0.4s + spawn exactly 2 Drones once via `WaveManager.SpawnEnemy()`; `bool isDying` blocks further damage; `OnPhaseChanged` event for BossHUDController

### 🧩 Prefab Assembly (7 tasks)
- [x] **Player prefab**: SpriteRenderer, Rigidbody2D (Kinematic, gravity=0, Collision Detection=Continuous), PolygonCollider2D (trimmed to 70% body, excluding wing tips), Animator, PlayerController, PlayerHealth; child transform `BulletSpawnPoint` at nose tip
- [x] **EnemyDrone prefab**: SpriteRenderer, Rigidbody2D (Dynamic, gravity=0), CircleCollider2D, Animator, EnemyDrone, EnemyHealth(maxHP=20, points=100); set layer=Enemy
- [x] **EnemyHunter prefab**: SpriteRenderer, Rigidbody2D, CircleCollider2D, Animator, EnemyHunter, EnemyHealth(maxHP=40, points=200); layer=Enemy
- [x] **Boss prefab**: SpriteRenderer (sprite=enemy_boss.png), Rigidbody2D (Kinematic), PolygonCollider2D, Animator, BossController, EnemyHealth(maxHP=300, points=1000); layer=Boss; child `BulletSpawnPoint`
- [x] **BulletPlayer prefab**: SpriteRenderer (8×16), CapsuleCollider2D (isTrigger=true, size=(0.2, 0.5)), Rigidbody2D (gravity=0), BulletPlayer; layer=PlayerBullet
- [x] **BulletEnemy prefab**: same structure, BulletEnemy script; layer=EnemyBullet
- [x] **BulletBoss prefab**: SpriteRenderer (12×24 bullet_boss.png), CapsuleCollider2D, Rigidbody2D, BulletEnemy (damage=20); layer=EnemyBullet

### ⚙️ Physics Validation (5 tasks)
- [x] Play test: fire player bullet → verify it hits Enemy/Boss but NOT Player (check Layer Matrix)
- [x] Play test: enemy bullet → hits Player, NOT Enemy or Boss
- [x] Tune PolygonCollider2D: wings excluded, body covered; verify via Scene view Collider gizmo
- [x] Implement knockback: in `PlayerHealth.TakeDamage()` → `rb.AddForce(hitDirection * 3f, ForceMode2D.Impulse)`
- [x] Implement boss death explosion force: `foreach Rigidbody2D r in Physics2D.OverlapCircleAll(boss.pos, 5f)` → `r.AddForce(dir * 5f, Impulse)`

### 🧪 Core System Tests (11 tasks)
- [x] Move in all 8 directions — verify no diagonal speed boost (normalize input vector)
- [x] Verify screen clamp on all 4 edges
- [x] Verify player Y clamp: cannot reach top 50% of screen
- [x] Shooting at 0.15s interval: hold Space 3s → count bullets in Scene view → expect ~20
- [x] Open Unity Profiler → fire 60s sustained → verify 0 GC alloc from ObjectPool
- [x] Dash: correct speed 15 u/s, duration 0.15s, cooldown 2s; visual trail active during dash only
- [x] I-frames: take a hit during dash → HP/Shield unchanged
- [x] Drone AI: moves straight down at 2 u/s, fires every 2s ±0.1s tolerance
- [x] Hunter AI: mirrors player X in real time; cannot be outrun horizontally
- [x] Boss Phase transitions: HP=201 → Phase1; HP=200 → Phase2 (speed/pattern change); HP=100 → Phase3
- [x] Boss Phase3: confirm exactly 3 bullets, angles correct, exactly 2 Drones spawned once (not repeatedly)

---

## PERSON 2 — Systems Dev | ~90h | 35 tasks

> Owns all managers (game state, waves, score, audio, save). The glue between everything.

### 💻 Manager Scripts (8 tasks)
- [x] `GameManager.cs` — Singleton; `enum State { Playing, Paused, GameOver, Victory }`; `SetState()` fires `OnStateChanged` event; Paused → `Time.timeScale=0`; Playing → `Time.timeScale=1`; GameOver → load GameOver scene after 1s delay; track `float survivalTime` in Update when Playing
- [x] `WaveManager.cs` — serialized `WaveData[]` per level (enemy type, count, X positions array, speed multiplier); `StartWave(int index)`: spawn enemies at top of screen from pool; `IEnumerator PollWaveCleared()`: check activeEnemies count every 0.5s; on cleared → wait 2s → next wave or `LevelManager.LevelComplete()`; `SpawnEnemy(prefab, pos)` public method for BossController Phase3
- [x] `ScoreManager.cs` — Singleton; `int currentScore`, `int comboCount`, `bool powerUpMultiplierActive`; `AddScore(int base)`: multiply by `GetMultiplier()` → increment; `GetMultiplier()`: if powerUpActive→×2; elif combo≥10→×3; elif combo≥5→×2; else→×1; `OnPlayerDamaged()`: comboCount=0; `OnEnemyKilled()`: comboCount++; `OnScoreChanged` UnityEvent<int>
- [x] `LevelManager.cs` — `LevelComplete()`: show "Level Complete!" via HUD → wait 2s → `SceneManager.LoadScene(nextScene)` (hardcoded: Level1→Level2, Level2→Level3Boss, Level3Boss→Victory); `Victory()`: show "VICTORY!" → wait 3s → SaveHighScore → LoadScene("MainMenu")
- [x] `AudioManager.cs` — Singleton with `Dictionary<string, AudioClip> sfx`; populated in Inspector (list of named clips); `PlaySFX(string key)`: finds clip → `audioSource.PlayOneShot(clip)`; `PlayBGM(string key)`: coroutine fades out current BGM over 1s → fades in new; AudioSource for BGM separate from SFX
- [x] `SaveManager.cs` — `SaveGame()`: `PlayerPrefs.SetInt("LastLevel", sceneIndex)`, `PlayerPrefs.SetInt("LastScore", score)`, `PlayerPrefs.Save()`; `LoadGame()`: read prefs → `ScoreManager.currentScore = saved` → `SceneManager.LoadScene(savedLevel)`; `SaveHighScore(int score)`: read 5 slots → insert if qualifies → sort descending → write back; `GetHighScores()`: returns `int[]` of 5 values
- [x] `PowerUp.cs` — abstract `MonoBehaviour`; `abstract void ApplyEffect(PlayerHealth ph)`; `OnTriggerEnter2D`: if tag=="Player" → `ApplyEffect(player.GetComponent<PlayerHealth>())` → `ScoreManager.AddScore(50)` → `Destroy(gameObject)` (power-ups are not pooled); Subclasses: `PowerUpHealth` (+30 HP, capped at 100), `PowerUpShield` (+25 Shield, capped at 50), `PowerUpScore` (`StartCoroutine(MultiplierTimer(10f))` → set flag in ScoreManager)
- [x] `PowerUpManager.cs` — `Drop(Vector3 pos)`: `Random.value < 0.3f` → pick random from 3 prefabs → `Instantiate(prefab, pos, Quaternion.identity)`

### 🎭 Scene Architecture (7 tasks)
- [x] Build `Level1.unity` hierarchy: Camera, ParallaxBackground parent (3 layer children), Grid (4 Tilemap children), Player prefab, WaveManager GO, ObjectPoolContainer GO (with BulletPlayerPool cap=20, BulletEnemyPool cap=30, ExplosionSmallPool cap=10, ExplosionLargePool cap=3, HitEffectPool cap=15), Canvas_HUD, Canvas_Pause (inactive), AudioManager GO, BGM AudioSource
- [x] Build `Level2.unity`: duplicate Level1 → update WaveManager data; swap nebula BG layer sprite to bg_nebula_l2; update scene name
- [x] Build `Level3_Boss.unity`: duplicate Level2 → add BossSpawnPoint Transform at top-center; activate BossHUD in Canvas_HUD; update WaveManager data
- [x] Configure WaveManager WaveData ScriptableObjects for Level 1: Wave1 (4×Drone, X=[20,40,60,80]% screen width, speedMult=1.0); Wave2 (6×Drone, X=[10,25,40,60,75,90]%, speedMult=1.2)
- [x] Configure Level 2 WaveData: Wave1 (2×Drone + 2×Hunter, Hunter delay=2s, speedMult=1.0); Wave2 (4×Hunter + 1×Drone, Hunter speedMult=1.15)
- [x] Configure Level 3 WaveData: Wave1 (3×Drone + 3×Hunter mixed, random X each spawn, speedMult=1.2); Wave2 → trigger BossController.StartFight() instead of normal wave
- [x] Wire all Manager inspector references: GameManager holds refs to WaveManager, ScoreManager, LevelManager, AudioManager; subscribe events between managers (e.g., WaveManager.OnEnemyKilled → ScoreManager.OnEnemyKilled)

### 🧩 Systems Prefabs (6 tasks)
- [x] **ExplosionSmall prefab**: SpriteRenderer, Animator; AnimationEvent on last frame calls `ObjectPool.Release(this)`
- [x] **ExplosionLarge prefab**: same setup, larger sprite
- [x] **HitEffect prefab**: same, tiny sprite
- [x] **PowerUpHealth prefab**: SpriteRenderer (powerup_health.png), CircleCollider2D (isTrigger=true, r=0.3), Rigidbody2D (gravity=0), PowerUpHealth script
- [x] **PowerUpShield prefab**: same, PowerUpShield script
- [x] **PowerUpScore prefab**: same, PowerUpScore script

### 🧪 Integration Tests (14 tasks)
- [x] ScoreManager: kill Drone → +100; kill Hunter → +200; kill Boss → +1000
- [x] Combo ×2: 5 kills no hit → next kill = base × 2
- [x] Combo ×3: 10 kills no hit → next kill = base × 3
- [x] Combo reset: take damage at combo=7 → next kill = base × 1
- [x] PowerUp multiplier overrides streak: activate score powerup during combo ×3 → score uses ×2 (powerup wins), not ×3; expires after 10s → streak resumes
- [x] WaveManager Level1: Wave1 spawns 4 Drone at correct X positions; all killed → 2s pause → Wave2 spawns 6 Drone in V
- [x] WaveManager Level2 & Level3: repeat above tests for both levels
- [x] AudioManager: BGM plays immediately on scene load; correct track per level; SFX "sfx_shoot_player" plays on fire
- [x] BGM crossfade: Level2 BGM fades out, Level3 boss BGM fades in during transition
- [x] SaveManager save → close → relaunch → Load → correct level + score restored
- [x] High score: 6 sessions with scores [500,300,800,200,600,400] → top 5 = [800,600,500,400,300]
- [x] PowerUp drop rate: observe 30 kills → confirm roughly 7-11 drops (±5 tolerance for random)
- [x] PowerUpHealth: HP at 50 → collect → HP = 80 (not 130)
- [x] Full end-to-end play-through: Level1 → Level2 → Level3 → Boss all phases → Victory → Main Menu

---

## PERSON 3 — UI & Level Designer | ~80h | 54 tasks

> Owns all UI, tilemap building, animation setup, and parallax. Work is highly visual and parallelizable with P1/P2.

### 💻 UI Scripts (6 tasks)
- [x] `HUDController.cs` — `Awake`: subscribe `PlayerHealth.OnHPChanged → HPSlider.value`; `OnShieldChanged → ShieldSlider.value`; `ScoreManager.OnScoreChanged → ScoreText.SetText("{0}", score)`; `WaveManager.OnWaveChanged → WaveText.SetText("Wave {0}", wave)`; no Lerp — all updates instant
- [x] `BossHUDController.cs` — `ShowWarning()`: activate WarningBanner → `yield return new WaitForSeconds(3f)` → deactivate; subscribe `BossController.OnPhaseChanged → UpdateBossHP()`; `UpdateBossHP()`: `BossHPSlider.value = boss.currentHP / boss.maxHP`; subscribe `BossController.OnBossDead → HideBossHUD()`
- [x] `MainMenuController.cs` — `OnStartClick()`: `SceneManager.LoadScene("Level1")`; `OnOptionsClick()`: toggle OptionsPanel activeself; `OnHighScoreClick()`: toggle HighScorePanel; `OnExitClick()`: `Application.Quit()`; `Start()`: populate 5 score TextMeshPro labels from `SaveManager.GetHighScores()`; show "Last played: Level N" if PlayerPrefs has data
- [x] `PauseMenuController.cs` — `Update()`: `if Input.GetKeyDown(Escape) && GameManager.state==Playing` → `GameManager.SetState(Paused)` → show PausePanel; `OnResumeClick()` → `SetState(Playing)` → hide; `OnRestartClick()` → `Time.timeScale=1` → `LoadScene(current)`; `OnMainMenuClick()` → `Time.timeScale=1` → `LoadScene("MainMenu")`
- [x] `GameOverController.cs` — `OnEnable()`: `FinalScoreText = ScoreManager.finalScore.ToString("N0")`; `EnemiesText = ScoreManager.totalKills.ToString()`; `TimeText = FormatTime(GameManager.survivalTime)` as "M:SS"; `OnRestartClick()` → `LoadScene("Level1")`; `OnMainMenuClick()` → `LoadScene("MainMenu")`
- [x] `OptionsController.cs` — `Awake()`: load all PlayerPrefs → set slider values + toggle state; `OnMasterChanged(float v)` → `AudioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f)` (dB conversion); same for Music/SFX; `OnFullscreenToggle(bool b)` → `Screen.fullScreen = b`; `OnAnyChange()` → `PlayerPrefs.SetFloat/Save()`

### 💻 Parallax Script (1 task)
- [x] `ParallaxBackground.cs` — array of `(Transform layer, float speedMult)`; `LateUpdate()`: `deltaX = Camera.main.transform.position.x - prevCamX`; foreach layer: `layer.position.x += deltaX * speedMult`; loop: if layer exits viewport bounds, shift by layer width; store `prevCamY` and apply same for Y; update prev positions

### 🖼️ MainMenu Scene (6 tasks)
- [x] Create `MainMenu.unity`; set camera background to solid black
- [x] Add Canvas (Screen Space Overlay, Ref Resolution 1280×720): Logo TextMeshPro ("GALAXY DEFENDER", PressStart2P_TMP, 48px, center-top), MenuPanel (5 Button stacked, 240×40px each, PressStart2P 14px), HighScorePanel (hidden, 5 TMP labels), OptionsPanel (hidden, MasterSlider, MusicSlider, SFXSlider, FullscreenToggle), LevelIndicator TMP (bottom-left, 14px)
- [x] Wire all 5 button `onClick` events → `MainMenuController` public methods
- [x] Add `AudioSource` on Main Camera: clip=bgm_menu, Play On Awake=true, Loop=true
- [x] Add `ParallaxBackground` parent GO; children: ImageBG_Stars_Far (sprite=bg_stars_far, speedMult=0.1), ImageBG_Stars_Mid (sprite=bg_stars_mid, speedMult=0.3)
- [x] Verify EventSystem exists in scene (added automatically with Canvas — double-check it's present)

### 🖥️ GameOver Scene (2 tasks)
- [x] Create `GameOver.unity`: Canvas with "GAME OVER" header (PressStart2P 32px), FinalScoreText, EnemiesDefeatedText, SurvivalTimeText (PressStart2P 18px each), Restart button, MainMenu button; add `GameOverController` to Canvas
- [x] Wire buttons + add bgm_gameover AudioSource on Camera

### 🗺️ Tilemap — Level 1 (9 tasks)
- [x] Import `tiles_station.png` → Sprite Editor → Slice → Grid by Cell Size 16×16 → Apply; verify 8 distinct tiles
- [x] Import `tiles_hazard.png` (single tile), `tiles_decor.png` (8 tiles); slice same way
- [x] Window → 2D → Tile Palette → create "Station Palette" → drag all 10 sliced tiles into palette
- [x] In Level1.unity: add Grid GO → 4 child Tilemap GOs (BG, Collision, Hazard, Decor); set Order in Layer: BG=-10, Collision=-5, Decor=+5
- [x] Add TilemapCollider2D + CompositeCollider2D + Rigidbody2D(Static) to Tilemap_Collision
- [x] Add TilemapCollider2D (isTrigger=true) to Tilemap_Hazard; add `TilemapHazard.cs` script (P3 writes inline: `OnTriggerStay2D` → `PlayerHealth.TakeDamage(Mathf.RoundToInt(10 * Time.deltaTime))`)
- [x] Paint Tilemap_BG: full screen coverage using station floor tiles, no visible gaps
- [x] Paint Tilemap_Collision: frame edges (thin border walls) + 2 interior pillars creating a zigzag corridor
- [x] Paint Tilemap_Hazard (2 strips) + Tilemap_Decor (scattered control panels, lights)

### 🗺️ Tilemap — Level 2 (4 tasks)
- [x] Import + slice `tiles_asteroid.png` → create "Asteroid Palette"
- [x] Copy Level1 Grid structure into Level2.unity; delete all painted tiles
- [x] Paint asteroid-theme walls: jagged overhangs, more open center than Level1
- [x] Verify TilemapCollider2D + CompositeCollider2D configured correctly on Collision layer

### 🗺️ Tilemap — Level 3 (3 tasks)
- [x] Import + slice `tiles_deepspace.png` → create "DeepSpace Palette"
- [x] Paint all 4 layers with dark alien tech aesthetic; wider open layout (boss fight needs movement room)
- [x] Place Hazard tiles only in corners; center screen fully clear for boss fight

### 🎬 Animation Setup (13 tasks)
- [x] Create Animator Controller `PlayerAnimator.controller` in Assets/Animations/
- [x] Create AnimationClip `Player_Thruster`: Sprite property, keyframes at 0/0.083/0.167/0.25s using thruster frames 0-3, Sample=12, Loop=true
- [x] Create AnimationClip `Player_Damaged`: 6 keyframes alternating normal↔damage sprite at Sample=8, Loop=false
- [x] PlayerAnimator states: Default=Thruster; Trigger param "Hit"; Triggered → Damaged → (Exit Time 1.0) → back to Thruster
- [x] Create `DroneAnimator.controller` + `Drone_Idle` clip (2 frames, Sample=4, Loop=true)
- [x] Create `HunterAnimator.controller` + `Hunter_Idle` clip (4 frames, Sample=8, Loop=true)
- [x] Create `BossAnimator.controller`; int param "Phase" (0/1/2); states: Boss_Phase1 (static, 1 frame), Boss_Phase2 (subtle flash, 2 frames Sample=4), Boss_Phase3 (rapid flash, 2 frames Sample=16)
- [x] Create AnimationClip `Explosion_Small`: 6 frames Sample=12, Loop=false; add AnimationEvent at last frame: Function=`ReturnToPool` (implement on ExplosionEffect script)
- [x] Create AnimationClip `Explosion_Large`: 8 frames Sample=10, Loop=false; same AnimationEvent
- [x] Create AnimationClip `HitEffect`: 4 frames Sample=16, Loop=false; AnimationEvent → ReturnToPool
- [x] Create AnimationClip `Mine_Pulse`: 4 frames Sample=6, Loop=true
- [x] Assign all Animator Controllers to matching Prefabs (drag controller into Animator component's Controller field)
- [x] Write `ExplosionEffect.cs` (10 lines): `public void ReturnToPool() { pool.Release(this.gameObject); }` — used by AnimationEvents above

### 🧪 UI & Level Tests (10 tasks)
- [x] Main Menu: all 5 buttons work; panels show/hide; 5 high scores populate; LevelIndicator shows correct data
- [x] Options: all 3 sliders change audio in real time; fullscreen toggle works; values persist after app restart
- [x] HUD: take damage → HP bar updates immediately; collect power-up → Shield bar updates; kill → score text increments
- [x] Boss HUD: reach Level3 Wave2 → warning banner appears for exactly 3s then disappears; HP bar tracks boss HP down to 0
- [x] Pause: ESC pauses (confirm `Time.timeScale==0` in debug); Resume works; Restart reloads level correctly; no ESC-double-press bug
- [x] Game Over stats: note score/kills/time manually during play → compare with Game Over screen values
- [x] Level1 tilemap: player blocked by all collision tiles; no corner-sliding through walls
- [x] Level2 tilemap: same collision tests; layout visually different from Level1
- [x] Hazard tiles: stand on hazard 3s → lose ~30HP; move off → HP stops decreasing
- [x] All animations: thruster loops, damaged flashes 3× and returns to thruster, all explosions play once and object disappears, mine pulses continuously

---

## PERSON 4 — Asset Manager & QA | ~60h | 65 tasks

> Owns ALL raw asset acquisition, post-processing, importing, and final QA. Parallelizable from Day 1.

### ⬇️ Downloads — Audio (6 tasks)
- [x] Download Kenney Sci-Fi Sounds: https://kenney.nl/assets/sci-fi-sounds → unzip → folder `/raw/kenney_scifi/`
- [x] Download Kenney Interface Sounds: https://kenney.nl/assets/interface-sounds → unzip → `/raw/kenney_interface/`
- [x] Download Kenney Impact Sounds: https://kenney.nl/assets/impact-sounds → unzip → `/raw/kenney_impact/`
- [x] Download Kenney UI Audio: https://kenney.nl/assets/ui-audio → unzip → `/raw/kenney_ui_audio/`
- [x] Download Space Shooter BGM pack: https://opengameart.org/content/space-shooter-game-music-pack → unzip
- [x] Download 5 Chiptunes Action: https://opengameart.org/content/5-chiptunes-action → unzip

### ⬇️ Downloads — Visual (4 tasks)
- [x] Download Kenney UI Pack: https://kenney.nl/assets/ui-pack → unzip → `/raw/kenney_ui/`
- [x] Download explosion spritesheet: https://opengameart.org/content/explosion → save PNG
- [x] Download parallax background pack: https://ansimuz.itch.io/spaceship-shooter-environment → unzip (can use as reference or direct replacement for AI-generated BG)
- [x] Download PressStart2P font: https://fonts.google.com/specimen/Press+Start+2P → download TTF ZIP → extract `.ttf`

### 🤖 AI Generation — Character Sprites (9 tasks)
> Open https://www.bing.com/images/create or https://firefly.adobe.com; use prompt prefix: `pixel art, 2D game sprite, transparent background, top-down view, retro arcade sci-fi style, clean pixel edges —`
- [x] Generate `player_ship.png` (32×32): add `blue sleek triangular spaceship, neon blue engine glow at rear`
- [x] Generate `player_thruster_sheet.png` (4 frames, 128×32): add `blue spaceship thruster flame animation, 4 frames horizontal strip, 32x32 per frame, cycling brightness`
- [x] Generate `enemy_drone.png` (32×32): add `small red angular enemy drone, glowing red engine, menacing, top-down`
- [x] Generate `enemy_drone_sheet.png` (2 frames, 64×32): add `2-frame idle, alternating engine brightness`
- [x] Generate `enemy_hunter.png` (32×32): add `orange wide-wing hunter ship, aggressive angular, glowing orange thrusters`
- [x] Generate `enemy_hunter_sheet.png` (4 frames, 128×32): add `4-frame thruster animation strip`
- [x] Generate `enemy_boss.png` (64×64): add `large purple boss ship, symmetrical, multiple cannons, glowing energy core, highly detailed`
- [x] Generate `enemy_boss_phase2.png` (64×64): add `battle-damaged version, cracks and sparks`
- [x] Generate `enemy_boss_phase3.png` (64×64): add `critically damaged, glowing red exposed core, pieces breaking`

### 🤖 AI Generation — Bullets, Effects, Items (5 tasks)
- [x] Generate `bullet_player.png` (8×16): `small neon blue laser bullet, vertical capsule, glowing`
- [x] Generate `bullet_enemy.png` (8×16): `small red laser bullet, vertical capsule, glowing`
- [x] Generate `bullet_boss.png` (12×24): `large purple energy orb projectile, glowing aura`
- [x] Generate `powerup_health.png` + `powerup_shield.png` + `powerup_score.png` (all 16×16) — can use one session for all 3; separate saves
- [x] Generate `obstacle_mine.png` (24×24) + `obstacle_mine_sheet.png` (4 frames, 96×24)

### 🤖 AI Generation — Environments (10 tasks)
- [x] Generate `bg_stars_far.png` (1280×720): `pixel art deep space, sparse tiny stars, dark navy, seamlessly tileable vertically`
- [x] Generate `bg_stars_mid.png` (1280×720): `pixel art space, medium stars, dark blue-black, tileable vertically`
- [x] Generate `bg_nebula_l1.png` (1280×720): `pixel art blue-purple glowing nebula, Earth visible in corner, tileable`
- [x] Generate `bg_nebula_l2.png` (1280×720): `pixel art grey asteroid dust clouds, floating rocks, tileable`
- [x] Generate `bg_nebula_l3.png` (1280×720): `pixel art dark void, distant galaxy spiral, ominous, tileable`
- [x] Generate `tiles_station.png` (128×16, 8 tiles): `pixel art sci-fi station tileset, 8 tiles row, 16x16 each, grey metal panels, neon blue trim`
- [x] Generate `tiles_asteroid.png` (128×16): `pixel art asteroid rock tileset, 8 tiles row, 16x16 each, grey-brown rocky`
- [x] Generate `tiles_deepspace.png` (128×16): `pixel art dark sci-fi tileset, 8 tiles row, 16x16, black panels purple energy lines`
- [x] Generate `tiles_hazard.png` (16×16 single): `pixel art red-yellow hazard warning tile, danger stripes, glowing edges`
- [x] Generate `tiles_decor.png` (128×16): `pixel art sci-fi decorative tileset, 8 tiles, blinking lights, pipes, vents`

### 🖌️ Post-Processing (5 tasks)
- [x] Open Photopea (https://www.photopea.com): open `player_ship.png` → New Layer → fill red (#FF0000) → Opacity 50% → Flatten Image → Export as PNG → save as `player_damage.png`
- [x] Inspect ALL generated PNGs: if background is white instead of transparent → Photopea: Edit → Magic Wand tool → click white background → Delete → Export PNG
- [x] Verify all sprite sheet strips: each frame must be exactly the same pixel width; if misaligned → Photopea crop to fix
- [x] Verify tile sheets: must align on a strict 16×16 pixel grid; no sub-pixel shifts
- [x] Verify explosion sheets from OpenGameArt: count frames (need 6 for small, 8 for large); if different → check if there's a variant on the same page, or generate replacement

### 📂 File Renaming (4 tasks)
- [x] Rename all AI-generated sprites to exact filenames from PRD asset spec
- [x] From Kenney audio packs: open each folder, listen to clips, pick best match for each SFX in PRD table, copy to `/ready_sfx/` with renamed filenames (sfx_shoot_player.wav, sfx_explosion_small.wav, etc.)
- [x] Rename BGM files: bgm_menu.ogg, bgm_gameplay.ogg, bgm_boss.ogg, bgm_gameover.ogg — convert .mp3 to .ogg if needed using Audacity (free) or https://cloudconvert.com
- [x] From Kenney UI Pack: identify matching UI sprites → copy → rename to PRD names (ui_healthbar_fill.png, ui_button_normal.png, etc.)

### 📥 Unity Import — Sprites (7 tasks)
- [x] Import all PNG sprites into correct `Assets/Sprites/` subfolders; create subfolders if missing
- [x] Set import settings on every sprite file (select all in folder → Inspector): Texture Type=Sprite (2D and UI), Filter Mode=Point (no filter), Compression=None, PPU=32 — do not skip any file
- [x] Slice `player_thruster_sheet.png`: Sprite Editor → Slice → Grid by Cell Size 32×32 → Apply; verify 4 named sprites generated
- [x] Slice `enemy_drone_sheet.png` (32×32), `enemy_hunter_sheet.png` (32×32), `obstacle_mine_sheet.png` (24×24)
- [x] Slice `explosion_small_sheet.png` (32×32 → 6 sprites), `explosion_large_sheet.png` (64×64 → 8 sprites), `effect_hit_sheet.png` (16×16 → 4 sprites)
- [x] Slice all tile sheet PNGs (16×16) — run Slice in Sprite Editor for each sheet; verify correct tile count
- [x] Background images: import separately; Filter Mode = Bilinear (exception to Point rule); PPU = 1

### 📥 Unity Import — Audio & Font (3 tasks)
- [x] Import all SFX .wav → `Assets/Audio/SFX/`; select all → Inspector: Load Type=Decompress on Load, Preload Audio Data=true; Apply
- [x] Import all BGM .ogg → `Assets/Audio/BGM/`; select all → Load Type=Streaming, Loop=true; Apply; verify green checkmarks in Console
- [x] Import PressStart2P-Regular.ttf → `Assets/Fonts/`; Window → TextMeshPro → Font Asset Creator: Source Font=PressStart2P, Atlas Resolution=512×512, Character Set=ASCII, Render Mode=SDF → Generate → Save as `PressStart2P_TMP` in Assets/Fonts/

### 🧪 Full QA Testing (11 tasks)
- [x] Launch game from MainMenu → confirm no red errors in Console
- [x] Full Level 1 play: Wave1 spawns 4 Drone, kill all, 2s wait, Wave2 spawns 6, kill all, "Level Complete" shows, Level2 auto-loads
- [x] Full Level 2 play: Hunter tracks player, all waves complete, Level3 loads
- [x] Full Boss fight: all 3 phases; verify spread shot fires in 3 directions in Phase3; Boss dies → Victory screen
- [x] Death test: die → Game Over screen; verify Score/Enemies/Time all accurate (track manually while playing)
- [x] Save → close app → relaunch → Load → correct level + score restored
- [x] High Score: 6 play sessions → open Main Menu → verify only top 5 shown, sorted descending
- [x] Options persistence: change sliders to 50% → close → relaunch → sliders still at 50%
- [x] FPS stress test: Unity Profiler → play Level3 boss fight → confirm ≥55 FPS maintained
- [x] GC test: Profiler → fire bullets continuously → "GC Alloc" column = 0B during sustained fire
- [x] Final build: File → Build Settings → PC x86-64 → Build → verify .exe launches standalone; zip project folder; verify zip opens correctly

### 📝 Documentation (5 tasks)
- [x] Write "Game Concept" (~200 words): describe Galaxy Defender's arcade loop, 3-level structure, enemy types, and core mechanic hooks
- [x] Write "Design Decisions": explain wave simplification (why 2 waves/level), AI-generated art (why, which tools), Pareto team split rationale, ObjectPool reasoning, Physics2D gravity=0 setup
- [x] Write "Development Challenges": honest list of 5 real issues encountered and how resolved (e.g., collision matrix bugs, sprite import settings, audio clipping)
- [x] Write "Implemented Systems": checklist of all rubric items, mark Complete / Partial / Not Implemented for each
- [x] Write "Future Improvements": 5 concrete items (e.g., unlockable ships, parallax asteroid shader, online leaderboard, controller support, wave editor tool); compile all 5 sections → export as PDF or DOCX for submission

---

## Contingency Plan

| Who Leaves | Immediately Reassign To |
|------------|------------------------|
| P4 (Asset & QA) | P3 takes all import + QA tasks; P2 writes documentation |
| P3 (UI & Level) | P2 takes all UI scripts; P4 takes tilemap painting (follow PRD tile spec); animations simplified to single-frame sprites |
| P2 (Systems) | P1 takes GameManager + WaveManager + AudioManager (critical); P3 takes ScoreManager + SaveManager; PowerUpManager merged into EnemyHealth |
| P1 (Lead Dev) | **Emergency:** P2 takes PlayerController + PlayerHealth; P3 takes EnemyDrone + EnemyHunter; Boss Phase3 spread shot cut; all remaining scope reviewed |
