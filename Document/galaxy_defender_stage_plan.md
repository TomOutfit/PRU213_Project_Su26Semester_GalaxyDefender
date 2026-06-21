# Galaxy Defender — Stage Plan

> **8 weeks · 4 people · 5 stages**
> Read alongside `galaxy_defender_work_breakdown.md` for full task detail.
> Each stage has a **gate** — all gate criteria must pass before the team moves forward.

---

## Timeline Overview

| Stage | Weeks | Theme | Gate |
|-------|-------|-------|------|
| **S1** | 1–2 | Foundation + Assets | Player moves on screen; all raw assets ready |
| **S2** | 3–4 | Core Loop | Player shoots Drone, earns score, Wave 1 Level 1 clears |
| **S3** | 5–6 | Full Feature Set | All 3 enemies work; all 3 levels have tilemaps; audio plays |
| **S4** | 7 | Integration | Full play-through Level 1→2→3→Boss→Victory without crash |
| **S5** | 8 | QA + Delivery | 60 FPS confirmed; .exe builds; docs submitted |

---

## Dependency Order (Critical Path)

```
P1: Unity Setup → ObjectPool → PlayerController → PlayerHealth
        ↓                              ↓
P2: GameManager ←→ WaveManager    Bullet scripts → EnemyDrone → EnemyHealth
        ↓                              ↓
P2: ScoreManager ← EnemyHealth     EnemyHunter → BossController
        ↓
P2: LevelManager → SaveManager → PowerUpManager
        ↓
P2: Scene architecture → Wire managers → Integration tests
        ↓
P4: QA → .exe Build → Documentation

P4 (assets) runs FULLY PARALLEL from Week 1
P3 (UI/Level) runs PARALLEL from Week 1; depends on P4 imports by Week 3
```

---

---

# STAGE 1 — Foundation (Week 1–2)

**Goal:** Unity project is configured and shared. Player can move and dash on screen. All raw assets exist locally on P4's machine, ready for import.

## Gate Criteria (must ALL be true to exit Stage 1)

- [ ] Unity project on Git; everyone has cloned and opened it without errors
- [ ] Player moves in 8 directions, clamped to screen, at correct speed
- [ ] Dash fires at correct speed/duration/cooldown; no diagonal speed boost bug
- [ ] All physics layers created and collision matrix configured
- [ ] P4: every sprite (AI-generated + downloaded), every SFX, every BGM file exists locally and is correctly named

---

## P1 — Lead Dev (Week 1–2) · 12 tasks

**Week 1 — Pure setup (get everyone unblocked first)**

- [x] Create Unity 6000.4.6f1 project, set build target to Windows x86-64
- [x] Physics2D gravity = (0,0); Application.targetFrameRate = 60
- [x] Create Layers: Player, Enemy, Boss, PlayerBullet, EnemyBullet, PowerUp
- [x] Configure Layer Collision Matrix (PlayerBullet→Enemy/Boss; EnemyBullet→Player; Enemy ignores Enemy)
- [x] Define Tags: Player, Enemy, Boss, PowerUp
- [x] Init Git repo + Unity .gitignore; push; share link with team
- [x] Create full `Assets/` folder structure (Scripts/Player, Scripts/Enemy, Scripts/Managers, Scripts/Systems, Scripts/UI, Prefabs, Sprites, Audio, Scenes, Fonts, Animations)
- [x] Set up AudioMixer: 3 groups (Master, Music, SFX); expose MasterVolume, MusicVolume, SFXVolume parameters

**Week 2 — First scripts**

- [x] `ObjectPool.cs` — Generic pool, Get()/Release(), hard caps (20 player bullets, 30 enemy bullets), auto-return oldest at cap
- [x] `PlayerController.cs` — WASD/Arrow input, `rb.MovePosition()` in FixedUpdate, screen viewport clamp, Shift → dash coroutine
- [x] `PlayerHealth.cs` — maxHP=100, maxShield=50, TakeDamage() checks isDashing first, shield absorbs before HP, OnDeath/OnHPChanged/OnShieldChanged events
- [ ] Quick play test: place Player prefab in temp scene → confirm movement, clamp, dash all work

> **Handoff to P2 at end of Week 1:** Folder structure + Git repo must exist so P2 can create GameManager simultaneously.

---

## P2 — Systems Dev (Week 1–2) · 7 tasks

**Week 1 — Skeleton managers (can write offline, push to shared repo)**

- [x] `GameManager.cs` — Singleton, enum State {Playing, Paused, GameOver, Victory}, SetState() fires OnStateChanged, Paused→timeScale=0, Playing→timeScale=1, track survivalTime float in Update
- [x] `AudioManager.cs` — Singleton skeleton: Dictionary<string, AudioClip> sfx (populated in Inspector), PlaySFX(string), PlayBGM(string) stub, CrossFade coroutine stub (full implementation in S2)

**Week 2 — WaveManager skeleton**

- [x] `WaveManager.cs` — serialized WaveData[] (enemy prefab, count, X positions array, speedMultiplier); StartWave(int) stub; SpawnEnemy(prefab, pos) public method (needed by BossController later); IEnumerator PollWaveCleared() stub
- [x] `ScoreManager.cs` — Singleton skeleton: currentScore, comboCount, AddScore(int) with GetMultiplier() returning 1 for now; OnScoreChanged event; OnPlayerDamaged() resets combo
- [x] Create 3 empty scene files: MainMenu.unity, Level1.unity, GameOver.unity — push to Git so P3 can open them
- [x] Add EventSystem + basic Canvas to Level1.unity so P3 can begin HUD layout
- [x] Wire GameManager ↔ ScoreManager initial references in Level1.unity

> **P2 dependency:** Needs P1's folder structure from Week 1 before pushing any scripts.

---

## P3 — UI & Level Designer (Week 1–2) · 9 tasks

**Week 1 — Layout without scripts (visual-only work, fully parallel)**

- [x] `ParallaxBackground.cs` — store prevCamX/Y, LateUpdate: compute delta, move each layer by delta×speedMult, loop when layer exits viewport
- [x] Open MainMenu.unity (created by P2 end of Week 1); build Canvas hierarchy: Logo TMP "GALAXY DEFENDER" (PressStart2P 48px), 5 stacked Button GOs (no onClick wired yet), HighScorePanel (5 TMP labels, hidden), OptionsPanel (3 Slider + Toggle, hidden), LevelIndicator TMP
- [x] Open Level1.unity; add Canvas_HUD: HPSlider, ShieldSlider, ScoreText TMP, WaveText TMP, LivesText TMP; position elements so none overlap playfield
- [x] Add Canvas_Pause to Level1.unity (inactive by default): Resume/Restart/MainMenu buttons, semi-transparent black overlay

**Week 2 — Animator prep (no sprites needed yet — create empty controllers)**

- [x] Create `PlayerAnimator.controller` in Assets/Animations/; add states: Thruster (default), Damaged; add Trigger param "Hit"; wire transition Thruster→Damaged→Thruster (Exit Time 1.0)
- [x] Create `DroneAnimator.controller`; add Idle state (default)
- [x] Create `HunterAnimator.controller`; add Idle state (default)
- [x] Create `BossAnimator.controller`; add states Phase1/Phase2/Phase3; int param "Phase"
- [x] Add ParallaxBackground GO to Level1.unity: 3 child Image GOs (Layer1 speed=0.1, Layer2 speed=0.3, Layer3 speed=0.6); assign placeholder white sprite for now (real sprites in S2)

> **P3 dependency on P2:** Needs scene files pushed to Git before opening. Needs Canvas in Level1.unity. Both deliverable end of Week 1.

---

## P4 — Asset Manager (Week 1–2) · 24 tasks

> P4 works fully independently. All tasks are local; nothing depends on Unity project access until import (Stage 2).

**Week 1 — Downloads (all audio + visual packs)**

- [x] Download Kenney Sci-Fi Sounds: https://kenney.nl/assets/sci-fi-sounds → unzip → `/raw/kenney_scifi/`
- [x] Download Kenney Interface Sounds: https://kenney.nl/assets/interface-sounds → unzip
- [x] Download Kenney Impact Sounds: https://kenney.nl/assets/impact-sounds → unzip
- [x] Download Kenney UI Audio: https://kenney.nl/assets/ui-audio → unzip
- [x] Download BGM pack: https://opengameart.org/content/space-shooter-game-music-pack → unzip
- [x] Download 5 Chiptunes Action: https://opengameart.org/content/5-chiptunes-action → unzip
- [x] Download Kenney UI Pack: https://kenney.nl/assets/ui-pack → unzip
- [x] Download explosion spritesheet: https://opengameart.org/content/explosion
- [x] Download parallax backgrounds: https://ansimuz.itch.io/spaceship-shooter-environment
- [x] Download PressStart2P font: https://fonts.google.com/specimen/Press+Start+2P

**Week 1–2 — AI Generation (all sprites)**

- [x] Generate `player_ship.png` + `player_thruster_sheet.png` (4-frame strip 128×32)
- [x] Generate `enemy_drone.png` + `enemy_drone_sheet.png` (2-frame 64×32)
- [x] Generate `enemy_hunter.png` + `enemy_hunter_sheet.png` (4-frame 128×32)
- [x] Generate `enemy_boss.png` + `enemy_boss_phase2.png` + `enemy_boss_phase3.png` (all 64×64)
- [x] Generate `bullet_player.png` (8×16) + `bullet_enemy.png` (8×16) + `bullet_boss.png` (12×24)
- [x] Generate `powerup_health.png` + `powerup_shield.png` + `powerup_score.png` (all 16×16)
- [x] Generate `obstacle_mine.png` + `obstacle_mine_sheet.png` (4-frame 96×24)
- [x] Generate `bg_stars_far.png` + `bg_stars_mid.png` + `bg_nebula_l1/l2/l3.png` (all 1280×720)
- [x] Generate `tiles_station.png` + `tiles_asteroid.png` + `tiles_deepspace.png` + `tiles_hazard.png` + `tiles_decor.png`

**Week 2 — Post-processing**

- [x] Photopea: create `player_damage.png` (red overlay 50% on player_ship)
- [x] Verify transparent backgrounds on all AI sprites; fix any white-fill issues in Photopea
- [x] Rename ALL sprites to exact PRD filenames
- [x] From Kenney audio packs: pick + rename all 19 SFX files; pick + rename 4 BGM files
- [x] From Kenney UI Pack: pick + rename UI sprites (healthbar, buttons, panel)

---

---

# STAGE 2 — Core Loop (Week 3–4)

**Goal:** A complete minimal gameplay loop works. Player shoots, Drone dies, score increments, Wave 1 of Level 1 clears and Wave 2 begins. Everything visible (sprites, HUD) is hooked up.

## Gate Criteria

- [ ] Player bullet spawns from pool, flies upward, hits Drone, Drone dies with explosion
- [ ] Score increments by 100 on Drone death; combo tracking active
- [ ] WaveManager spawns Wave 1 (4 Drones); on all killed, 2s gap, Wave 2 spawns (6 Drones)
- [ ] HUD HP bar and Score text update in real time
- [ ] Pause (ESC) works: game freezes, Resume restores
- [ ] All sprites imported, correct import settings, no pink/magenta errors in Scene view
- [ ] At least 1 BGM track plays in Level1.unity

---

## P1 — Lead Dev (Week 3–4) · 12 tasks

**Week 3 — Bullet + Drone systems**

- [x] `BulletPlayer.cs` — velocity upward 12 u/s, OnTriggerEnter2D tag Enemy/Boss → EnemyHealth.TakeDamage(10) → Release() to pool
- [x] `BulletEnemy.cs` — velocity downward 10 u/s, OnTriggerEnter2D tag Player → PlayerHealth.TakeDamage(10) → Release()
- [x] `EnemyDrone.cs` — downward movement 2 u/s FixedUpdate; FireRoutine coroutine every 2s; OnBecameInvisible → return to pool
- [x] `EnemyHealth.cs` — TakeDamage(int), OnDeath event → ScoreManager.AddScore(points) + 30% chance PowerUpManager.Drop(pos)
- [ ] **Player prefab** (full assembly): SpriteRenderer=player_ship.png, Rigidbody2D Kinematic gravity=0, PolygonCollider2D trimmed 70%, Animator=PlayerAnimator, PlayerController, PlayerHealth; child BulletSpawnPoint at nose
- [ ] **BulletPlayer prefab**: SpriteRenderer=bullet_player.png, CapsuleCollider2D isTrigger, Rigidbody2D gravity=0, BulletPlayer; layer=PlayerBullet
- [ ] **BulletEnemy prefab**: same setup, BulletEnemy script; layer=EnemyBullet

**Week 4 — Drone prefab + physics validation**

- [ ] **EnemyDrone prefab**: SpriteRenderer=enemy_drone.png, Rigidbody2D Dynamic gravity=0, CircleCollider2D, Animator=DroneAnimator, EnemyDrone, EnemyHealth(20, 100pts); layer=Enemy
- [ ] **ExplosionSmall prefab**: SpriteRenderer, Animator, AnimationEvent on last frame → ReturnToPool
- [ ] **HitEffect prefab**: same, smaller sprite
- [ ] Validate Layer Collision Matrix: player bullet hits Enemy, enemy bullet hits Player, no cross-contamination
- [ ] Tune PolygonCollider2D on Player (exclude wing tips); test in Play mode via Scene gizmos

---

## P2 — Systems Dev (Week 3–4) · 11 tasks

**Week 3 — WaveManager + ScoreManager complete**

- [x] `WaveManager.cs` complete: WaveData ScriptableObject, StartWave() instantiates enemies from pool at correct X positions, IEnumerator PollWaveCleared() polls activeEnemies every 0.5s, on clear → wait 2s → StartWave(next) or LevelManager.LevelComplete()
- [x] `ScoreManager.cs` complete: GetMultiplier() (×1/×2/×3 based on combo), combo reset on PlayerHealth.OnDamageTaken, powerUpMultiplierActive timer, OnScoreChanged event fired correctly
- [x] Create WaveData ScriptableObject for Level1 Wave1: 4 EnemyDrone, X=[20,40,60,80]%screenWidth, speedMult=1.0
- [x] Create WaveData for Level1 Wave2: 6 EnemyDrone, X=[10,25,40,60,75,90]%, speedMult=1.2

**Week 4 — Level1 scene + AudioManager wiring**

- [x] `AudioManager.cs` complete: Dictionary populated in Inspector, PlaySFX() implementation, PlayBGM() with 1s cross-fade coroutine
- [x] Build Level1.unity complete hierarchy: Camera, ParallaxBG (3 layers), Grid (4 Tilemap children - empty for now), Player prefab placed, WaveManager GO with WaveData references, ObjectPoolContainer (BulletPlayerPool×20, BulletEnemyPool×30, ExplosionSmallPool×10, HitEffectPool×15), Canvas_HUD, Canvas_Pause, AudioManager GO, BGM AudioSource (bgm_gameplay.ogg, loop)
- [x] Wire WaveManager → SpawnEnemy() from pool; confirm enemies use Get() not Instantiate()
- [x] Wire GameManager.OnStateChanged → PauseMenuController (show/hide panel) + timeScale
- [x] **ExplosionSmall + HitEffect prefabs** placed in ObjectPoolContainer
- [x] Assign AudioManager clip dictionary entries: sfx_shoot_player, sfx_explosion_small, sfx_player_hit entries populated in Inspector
- [ ] Quick integration test: kill Drone → explosion plays, score +100, wave count decrements

---

## P3 — UI & Level Designer (Week 3–4) · 11 tasks

**Week 3 — Scripts + Tilemap start**

- [x] `HUDController.cs` — Awake: subscribe PlayerHealth.OnHPChanged → HPSlider.value; OnShieldChanged → ShieldSlider; ScoreManager.OnScoreChanged → ScoreText; WaveManager.OnWaveChanged → WaveText; all instant (no lerp)
- [x] `PauseMenuController.cs` — Update: ESC when state==Playing → SetState(Paused); OnResumeClick → SetState(Playing); OnRestartClick → timeScale=1 + LoadScene(current); OnMainMenuClick → timeScale=1 + LoadScene("MainMenu")
- [x] `MainMenuController.cs` — OnStartClick → LoadScene("Level1"); OnOptionsClick → toggle OptionsPanel; OnHighScoreClick → toggle HighScorePanel; OnExitClick → Application.Quit()
- [x] Open Window → 2D → Tile Palette; import `tiles_station.png` (P4 must have imported this first) → Slice 16×16 → create "Station Palette"
- [x] Import `tiles_hazard.png`, `tiles_decor.png` → add to Station Palette

**Week 4 — Tilemap Level 1 + animation clips**

- [x] Paint Tilemap_BG (full coverage, no gaps), Tilemap_Collision (border walls + 2 zigzag interior pillars), Tilemap_Hazard (2 strips), Tilemap_Decor (scattered props)
- [x] Add TilemapCollider2D + CompositeCollider2D + Rigidbody2D(Static) to Tilemap_Collision; add TilemapCollider2D(isTrigger) + `TilemapHazard` script to Tilemap_Hazard
- [x] Create AnimationClip `Player_Thruster`: 4 frames Sample=12 Loop=true; assign to PlayerAnimator Thruster state
- [x] Create AnimationClip `Player_Damaged`: 6 frames alternating, Sample=8 Loop=false; assign to PlayerAnimator Damaged state
- [x] Create AnimationClip `Drone_Idle`: 2 frames Sample=4 Loop=true; assign to DroneAnimator
- [x] Create AnimationClip `Explosion_Small`: 6 frames Sample=12 Loop=false + AnimationEvent last frame → ReturnToPool()

---

## P4 — Asset Manager (Week 3–4) · 17 tasks

> P4 import work **unblocks** P3's tilemap and P1's sprite assignments. Import as early in Week 3 as possible.

**Week 3 — Full sprite import (high priority, do first)**

- [x] Pull Git repo; import all sprites into `Assets/Sprites/` subfolders (Player/, Enemies/, Bullets/, Effects/, PowerUps/, Tiles/, Backgrounds/, UI/)
- [x] Set import settings on EVERY sprite: Texture Type=Sprite, Filter Mode=**Point (no filter)**, Compression=None, PPU=32
- [x] Slice `player_thruster_sheet.png` → Sprite Editor → Grid 32×32; verify 4 named sprites generated
- [x] Slice `enemy_drone_sheet.png` (32×32), `enemy_hunter_sheet.png` (32×32), `obstacle_mine_sheet.png` (24×24)
- [x] Slice `explosion_small_sheet.png` (32×32 → 6 sprites), `explosion_large_sheet.png` (64×64 → 8 sprites), `effect_hit_sheet.png` (16×16 → 4 sprites)
- [x] Slice all tile sheets (16×16 each): tiles_station, tiles_asteroid, tiles_deepspace, tiles_decor
- [x] Background images (bg_*.png): Filter Mode=Bilinear, PPU=1 (exception — do NOT use Point filter on these)

**Week 3 — Font import**

- [x] Import `PressStart2P-Regular.ttf` → Assets/Fonts/
- [x] Window → TextMeshPro → Font Asset Creator: Source=PressStart2P, Atlas=512×512, Charset=ASCII → Generate → Save as `PressStart2P_TMP` in Assets/Fonts/
- [x] Update ALL existing TMP Text components in MainMenu.unity and Level1.unity to use PressStart2P_TMP

**Week 4 — Audio import**

- [x] Import all SFX .wav → Assets/Audio/SFX/; select all → Load Type=Decompress on Load; Apply
- [x] Import all BGM .ogg → Assets/Audio/BGM/; select all → Load Type=Streaming, Loop=true; Apply
- [x] Open Level1.unity → BGM AudioSource: assign bgm_gameplay.ogg; verify it plays on start
- [x] Spot-check Console for red errors after import — fix any misnamed/missing files
- [ ] Test: enter Play mode in Level1.unity → confirm no pink sprites, correct font renders, BGM audible

---

---

# STAGE 3 — Full Feature Set (Week 5–6)

**Goal:** All 3 enemy types work. All 3 levels have tilemaps. All manager systems (Save, PowerUp, Boss) are implemented. Audio plays correctly for all events.

## Gate Criteria

- [ ] Hunter tracks player X and fires; takes more hits than Drone
- [ ] Boss spawns in Level3, transitions Phase1→2→3 at correct HP thresholds, Phase3 spread shot fires at ±15°
- [ ] Boss Phase3 spawns exactly 2 Drones once, not repeatedly
- [ ] Level1→Level2 loads automatically after Wave2 cleared; Level2→Level3 same
- [ ] PowerUp drops on 30% of enemy deaths; all 3 power-up types apply correct effects
- [ ] SaveManager: Save writes to PlayerPrefs; Load reads and restores level + score
- [ ] Level2 and Level3 tilemaps are painted (not empty)
- [ ] bgm_boss.ogg plays when Boss spawns; all critical SFX fire (shoot, hit, explosion, powerup)

---

## P1 — Lead Dev (Week 5–6) · 7 tasks

**Week 5**

- [ ] `EnemyHunter.cs` — X tracking with MoveTowards speed=3.5 u/s FixedUpdate; Y moves down 1 u/s; fire every 1.5s; despawn off-screen
- [ ] `BossController.cs` — Phase thresholds 300/200/100HP; Phase1 stationary straight shot 1s; Phase2 sine wave X (A=0.3×screenW, period=4s) barrage 0.7s; Phase3 spread shot 3 bullets ±15° 0.4s + SpawnEnemy() exactly 2 Drones once; isDying flag; OnPhaseChanged event; OnBossDead event
- [ ] **EnemyHunter prefab**: Rigidbody2D Dynamic, CircleCollider2D, Animator=HunterAnimator, EnemyHunter, EnemyHealth(40, 200pts); layer=Enemy
- [ ] **Boss prefab**: Rigidbody2D Kinematic, PolygonCollider2D, Animator=BossAnimator, BossController, EnemyHealth(300, 1000pts); layer=Boss; child BulletSpawnPoint
- [ ] **BulletBoss prefab**: sprite=bullet_boss.png (12×24), damage=20, layer=EnemyBullet
- [ ] **ExplosionLarge prefab**: SpriteRenderer, Animator with Explosion_Large clip, AnimationEvent last frame → ReturnToPool; add to ObjectPoolContainer (cap=3)

**Week 6 — Physics + knockback**

- [ ] Implement knockback in `PlayerHealth.TakeDamage()`: `rb.AddForce(hitDir * 3f, ForceMode2D.Impulse)` (hitDir = normalize(player.pos - bullet.pos))
- [ ] Boss death explosion force: `Physics2D.OverlapCircleAll(pos, 5f)` → each rb → `AddForce(away * 5f, Impulse)`

---

## P2 — Systems Dev (Week 5–6) · 12 tasks

**Week 5 — Remaining managers**

- [x] `LevelManager.cs` — LevelComplete(): show "LEVEL COMPLETE!" via HUD → wait 2s → LoadScene(nextScene) lookup table (Level1→Level2, Level2→Level3Boss, Level3Boss→Victory); Victory(): show "VICTORY!" → wait 3s → SaveHighScore → LoadScene("MainMenu")
- [x] `SaveManager.cs` — SaveGame(): write LastLevel (int) + LastScore (int) to PlayerPrefs; LoadGame(): read → ScoreManager.currentScore = saved → LoadScene(savedLevel); SaveHighScore(): read 5 slots → insert if qualifies → sort desc → write back; GetHighScores(): return int[5]
- [x] `PowerUp.cs` abstract base + 3 subclasses: PowerUpHealth (+30HP cap 100), PowerUpShield (+25Shield cap 50), PowerUpScore (flag ScoreManager.powerUpMultiplierActive + start 10s coroutine to clear)
- [x] `PowerUpManager.cs` — Drop(pos): Random.value < 0.3f → pick random from 3 prefabs → Instantiate at pos
- [x] **PowerUp prefabs** (3): SpriteRenderer, CircleCollider2D isTrigger r=0.3, Rigidbody2D gravity=0, respective PowerUp script

**Week 5–6 — Level scenes + WaveManager configs**

- [x] Build `Level2.unity`: copy Level1 hierarchy → rename → swap nebula layer to bg_nebula_l2 → swap scene name references
- [x] Build `Level3_Boss.unity`: copy Level2 → add BossSpawnPoint Transform at top-center (X=0, Y=camera.top-1); activate BossHUD in Canvas_HUD
- [x] Create WaveData for Level2: Wave1 (2 Drone + 2 Hunter, Hunter delay=2s, speedMult=1.0); Wave2 (4 Hunter + 1 Drone, Hunter speedMult=1.15)
- [x] Create WaveData for Level3: Wave1 (3 Drone + 3 Hunter, random X each spawn, speedMult=1.2); Wave2 → trigger BossController.StartFight() after 2s delay
- [x] Wire Level3_Boss.unity: BossController subscribes AudioManager to switch to bgm_boss.ogg on StartFight(); wire BossHUDController to BossController events
- [x] Add PowerUpManager GO to all 3 level scenes; assign 3 power-up prefabs in Inspector
- [x] Wire AudioManager full clip dictionary: populate ALL 19 SFX + 4 BGM entries in Inspector across all scenes

**Week 6 — Integration smoke test**

- [x] Verify Level1→Level2→Level3 auto-transition works (all enemies killed → "Level Complete" → next scene loads)

---

## P3 — UI & Level Designer (Week 5–6) · 16 tasks

**Week 5 — Remaining UI scripts**

- [ ] `BossHUDController.cs` — ShowWarning(): activate WarningBanner → wait 3s → deactivate → activate BossHPBar; subscribe BossController.OnPhaseChanged → BossHPSlider.value = hp/maxHP; subscribe OnBossDead → deactivate BossHPBar
- [ ] `OptionsController.cs` — Awake load PlayerPrefs → set slider+toggle; OnMasterChanged(v) → AudioMixer.SetFloat("MasterVolume", Log10(max(v,0.0001))×20); same for Music/SFX; OnFullscreenToggle → Screen.fullScreen; OnAnyChange → PlayerPrefs.Save()
- [ ] `GameOverController.cs` — OnEnable: populate FinalScore, EnemiesDefeated, SurvivalTime (M:SS format); Restart → LoadScene("Level1"); MainMenu → LoadScene("MainMenu")
- [ ] Add BossHUD elements to Level3 Canvas: WarningBanner TMP (hidden), BossHPSlider (256×20 fill), boss name label
- [ ] Wire OptionsController sliders to AudioMixer; wire Fullscreen toggle; wire OptionsPanel show/hide button in MainMenuController

**Week 5–6 — Tilemap Level 2 + Level 3**

- [ ] Import tiles_asteroid.png (P4 already imported in S2) → create "Asteroid Palette" in Tile Palette window
- [ ] Build Level2 tilemap: paint all 4 layers with asteroid rock theme; jagged walls, more open center than Level1; configure TilemapCollider2D + CompositeCollider2D
- [ ] Import tiles_deepspace.png → create "DeepSpace Palette"
- [ ] Build Level3 tilemap: dark alien tech theme; wide open center (boss fight needs space); hazard tiles only in corners
- [ ] Verify all 3 levels' Tilemap_Collision has CompositeCollider2D set to Polygon (not Outline)

**Week 6 — Remaining animation clips**

- [ ] AnimationClip `Hunter_Idle`: 4 frames, Sample=8, Loop=true → DroneAnimator
- [ ] AnimationClip `Boss_Phase1` (static 1 frame), `Boss_Phase2` (2 frames subtle flash Sample=4), `Boss_Phase3` (2 frames rapid Sample=16) → BossAnimator; int param "Phase" wired to transitions
- [ ] AnimationClip `Explosion_Large`: 8 frames Sample=10 Loop=false + ReturnToPool AnimationEvent
- [ ] AnimationClip `HitEffect`: 4 frames Sample=16 Loop=false + ReturnToPool
- [ ] AnimationClip `Mine_Pulse`: 4 frames Sample=6 Loop=true
- [ ] Assign ALL Animator Controllers to ALL matching Prefabs (Player→PlayerAnimator, Drone→DroneAnimator, etc.)

---

## P4 — Asset Manager (Week 5–6) · 4 tasks

> P4's main import work is done. Week 5–6 is light; P4 begins QA preparation.

- [x] Verify all audio clips are in AudioManager GO Inspector slot → no missing references (pink fields) across all 3 level scenes
- [x] Spot-check all 3 levels in Play mode: no pink sprites, no missing audio, font renders correctly in all scenes
- [x] Create `Assets/Scenes/GameOver.unity` (empty scene with EventSystem) so P3 can build it in S4
- [x] Write smoke-test checklist (Google Doc or Notion): list every feature to verify in final QA with pass/fail column

---

---

# STAGE 4 — Integration (Week 7)

**Goal:** The full game loop from Main Menu to Victory (or Game Over) runs end-to-end without crash or missing feature. All UI wired. Save/Load confirmed. All scenes transition correctly.

## Gate Criteria

- [ ] Main Menu → Level1 → Level2 → Level3 → Boss (all 3 phases) → Victory → Main Menu: completes without runtime errors
- [ ] Die during Level2 → Game Over screen shows correct Score, Enemies Defeated, Survival Time; Restart goes to Level1
- [ ] ESC pauses, Resume continues exactly where left off (timeScale correct)
- [ ] Options: sliders affect volume in real time; fullscreen toggles; values persist after restart
- [ ] High Score: after 3 play sessions, top scores visible on Main Menu
- [ ] Save during Level2 → restart app → Load → Level2 starts with correct score

---

## P1 — Lead Dev (Week 7) · 11 tasks

> P1's entire focus this week is core system testing. Fix bugs as discovered.

- [ ] Test movement: 8 directions correct speed; no diagonal speed boost (input.normalized)
- [ ] Test screen clamp: ship stops at all 4 edges; Y clamp prevents reaching top 50%
- [ ] Test shooting: hold Space 3s → ~20 bullets visible; fire rate = 1 per 0.15s
- [ ] Test ObjectPool: open Profiler → fire 60s → GC Alloc column = 0B
- [ ] Test dash: speed 15 u/s, duration 0.15s, cooldown 2s; trail shows during dash only
- [ ] Test i-frames: take hit during dash → HP/Shield unchanged
- [ ] Test Drone: downward 2 u/s; fires every 2s ±0.1s
- [ ] Test Hunter: X tracks player every frame; cannot outrun horizontally
- [ ] Test Boss Phase transitions: P1→P2 at HP=200; P2→P3 at HP=100
- [ ] Test Boss Phase3: exactly 3 bullets at ±15°; exactly 2 Drones spawned once, not on each fire
- [ ] Fix any bugs found in above tests

---

## P2 — Systems Dev (Week 7) · 14 tasks

> P2 runs full integration testing across all managers. Fix bugs immediately.

- [ ] Test ScoreManager: kill Drone→+100, Hunter→+200, Boss→+1000; verify realtime display
- [ ] Test combo ×2: 5 kills no hit → 6th kill = 200 (Drone base 100×2)
- [ ] Test combo ×3: 10 kills no hit → 11th = 300 (Drone 100×3)
- [ ] Test combo reset: take damage at combo=7 → next kill = 100 (×1)
- [ ] Test PowerUp multiplier override: active during combo ×3 → kills use ×2 (power-up wins); expires after 10s → streak resumes
- [ ] Test WaveManager Level1: 4 Drone spawn at [20,40,60,80]%X; all killed → exactly 2s → Wave2 (6 Drone)
- [ ] Test WaveManager Level2 and Level3
- [ ] Test AudioManager: bgm_gameplay plays on Level1 load; bgm_boss starts when boss spawns; sfx_shoot_player fires on each shot
- [ ] Test BGM cross-fade Level2→Level3: no abrupt cut; 1s smooth fade
- [ ] Test SaveManager: Save mid-Level2 → close Unity → reopen → Load → Level2 with correct score
- [ ] Test high score across 6 sessions: scores [500,300,800,200,600,400] → top 5 = [800,600,500,400,300]
- [ ] Test PowerUp drop: observe 30 kills → expect 7–11 drops (RNG tolerance)
- [ ] Test PowerUpHealth/Shield at max: HP=80 → collect health → HP=100 (not 110)
- [ ] Full end-to-end: Level1→2→3→Boss→Victory; note and file any bugs

---

## P3 — UI & Level Designer (Week 7) · 10 tasks

**Build GameOver scene + final UI wiring**

- [ ] Build `GameOver.unity`: Canvas with "GAME OVER" header (48px), FinalScoreText + EnemiesText + SurvivalTimeText (18px each), Restart + MainMenu buttons; wire GameOverController; add bgm_gameover AudioSource
- [ ] `MainMenuController.cs` Awake: populate 5 HighScore TMP labels from SaveManager.GetHighScores(); show LevelIndicator "Last played: Level N" if PlayerPrefs.HasKey("LastLevel")
- [ ] Wire ALL MainMenu buttons onClick → correct MainMenuController methods
- [ ] Wire OptionsPanel sliders to OptionsController; verify AudioMixer connection
- [ ] Write `TilemapHazard.cs` (inline on P3, ~10 lines): OnTriggerStay2D → PlayerHealth.TakeDamage(Mathf.RoundToInt(10×Time.deltaTime)); assign to all Tilemap_Hazard objects

**UI + Level tests**

- [ ] Test Main Menu: all 5 buttons; OptionsPanel + HighScorePanel show/hide correctly; scores populate
- [ ] Test HUD: deal damage → HP/Shield bars update immediately; kill enemy → score text changes
- [ ] Test Boss HUD: enter Level3 boss fight → WarningBanner 3s → disappears → boss HP bar visible; HP tracks damage to zero
- [ ] Test Level1 collision: fly into all walls → blocked; no corner-clipping
- [ ] Test hazard tile: stand on it → HP falls at ~10/s; step off → stops

---

## P4 — Asset Manager (Week 7) · 5 tasks

- [ ] Run the smoke-test checklist from S3: pass/fail every item; report failures to P1/P2/P3
- [ ] Verify no missing references in Inspector across all 5 scenes (AudioManager slots, Tilemap references, HUD slider references)
- [ ] Confirm bgm_gameover plays on GameOver scene; sfx_level_complete plays on Level Complete transition
- [ ] Verify game runs at target FPS: open Profiler → play Level3 boss fight → note minimum FPS
- [ ] Begin documentation draft: Game Concept + Design Decisions sections

---

---

# STAGE 5 — QA & Delivery (Week 8)

**Goal:** Game is bug-free, runs at 60 FPS, .exe builds and launches standalone. All documentation written and submitted.

## Gate Criteria

- [ ] Profiler: ≥55 FPS during Level3 boss fight with max active enemies
- [ ] Profiler: 0 GC alloc during sustained bullet fire (ObjectPool confirmed working)
- [ ] .exe launches on a machine without Unity Editor installed
- [ ] Zip of project opens on a second machine without errors
- [ ] Documentation: all 5 sections written and compiled into final PDF/DOCX

---

## P1 — Lead Dev (Week 8) · 4 tasks

- [ ] Fix any remaining bugs flagged by P2/P4 from Stage 4 testing
- [ ] Profiler session: Level3 boss fight → confirm ≥55 FPS; if not, profile the hotspot (most likely: too many active Rigidbody2Ds or draw calls from particle effects)
- [ ] Final play-through as fresh player: confirm game feels fair, controls respond correctly, no janky moments
- [ ] Code review: remove all Debug.Log statements from production code (they generate GC alloc)

---

## P2 — Systems Dev (Week 8) · 3 tasks

- [ ] Fix any manager bugs found during Stage 4 (most likely: score saving edge case, BGM not switching, power-up timer resetting wrong)
- [ ] Verify final Save/Load on the build (not in Editor) — PlayerPrefs paths differ in builds
- [ ] Confirm high score persists across multiple .exe launches

---

## P3 — UI & Level Designer (Week 8) · 5 tasks

- [ ] Fix any tilemap collision bugs (common: corner gaps, hazard trigger not firing at tile edges)
- [ ] Final animation pass: confirm no animation states are stuck; Boss phase clips switch on time
- [ ] UI polish: verify all text strings match final values (score text, wave text, boss warning text)
- [ ] Verify PressStart2P font renders correctly in .exe (not just in Editor)
- [ ] Write "Development Challenges" section for documentation (P3 knows the level design + animation pain points)

---

## P4 — Asset Manager & QA (Week 8) · 11 tasks

- [ ] Full play-through Level1 + Level2 + Level3 Boss → record pass/fail
- [ ] Death test: die → verify all 3 Game Over stats are accurate (manually track during play)
- [ ] Save/Load test on .exe: Save → close .exe → relaunch → Load → correct state
- [ ] High Score test: 6 sessions, varied scores → verify top 5 sorted correctly
- [ ] Options persist test: change all sliders + toggle → close .exe → relaunch → verify persisted
- [ ] FPS + GC Alloc final confirmation (run Profiler from built player, not Editor)
- [ ] **Build .exe**: File → Build Settings → PC Standalone → Windows x86-64 → Build → select output folder
- [ ] **Verify .exe**: launch standalone (no Editor) → confirm game runs correctly, all scenes load
- [ ] **Zip project**: compress Unity project folder → verify zip < 500MB → open on second machine to confirm no corruption
- [ ] Write documentation sections: Implemented Systems (checklist vs rubric), Future Improvements (5 items)
- [ ] **Compile final report**: merge all 5 sections (Concept, Decisions, Challenges, Systems, Improvements) → export PDF or DOCX → submit

---

---

## Parallel Work Summary

```
Week:        1    2    3    4    5    6    7    8
P1 (Lead): [Setup──][Scripts─────][Boss+Phys][Tests──][Bugfix]
P2 (Sys):  [Skel───][Wave+Score──][LvMgr+Sav][IntTest][Bugfix]
P3 (UI):   [Layout─][HUD+Tile1───][UI+Tile23][UITest─][Polish]
P4 (Asset):[DL+Gen─][Gen+Rename──][Import────][Smoke──][QA+Build+Docs]
```

### Earliest P4 can start importing (unblocks P3 tilemap):
> End of Week 2 — if P4 has all sprites renamed and ready, and P1 has pushed the Unity project with folder structure.

### Earliest P3 can paint tilemaps:
> Start of Week 3 — after P4 has imported and sliced all tile sheets and pushed to Git.

### Earliest integration tests can start:
> Week 4 — after WaveManager, ScoreManager, EnemyDrone, and Level1 hierarchy are all wired.

### Latest any feature can be added without risk:
> End of Week 6 (Stage 3 gate) — anything added in Week 7+ risks destabilizing the integration.
