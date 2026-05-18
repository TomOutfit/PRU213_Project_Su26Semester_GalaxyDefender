# Product Requirements Document: Galaxy Defender

---

## Product Overview

**Product Vision:** Galaxy Defender is a 2D pixel-art space shooter for PC where the player pilots a spaceship through 3 levels, fighting enemy waves and a final boss, aiming for the highest score possible.

**Target Users:**
- **Primary:** Casual to mid-core PC gamers (15–30 years old) who enjoy retro arcade shooters.
- **Secondary:** Fellow students or lecturers reviewing the Unity 2D project structure.

**Business Objectives:**
- Deliver a complete, crash-free game build (`.exe`) covering all rubric criteria within a 2-month student timeline.
- Implement a full gameplay loop: Main Menu → Play → Game Over → Main Menu.

**Success Metrics:**
- Stable 60 FPS on a standard student laptop (i5 Gen 8+, 8 GB RAM, no discrete GPU).
- All rubric features demoed without runtime errors.
- Full game loop (3 levels + boss) completable in under 10 minutes.

---

## User Personas

### Persona 1: Casual Player
- **Demographics:** 16–25, student, plays PC games regularly.
- **Goals:** Quick fun sessions, satisfying enemy kills, beating the boss.
- **Pain Points:** Doesn't want to read tutorials; expects to play immediately.
- **User Journey:** Launch → Main Menu → Start → Play → Die or Win → See Score → Replay.

### Persona 2: Reviewer / Grader
- **Demographics:** Lecturer or TA evaluating a student game project.
- **Goals:** Verify all required features work correctly.
- **Pain Points:** Crashes, missing features, broken UI.
- **User Journey:** Launch → check menu → play at least 1 level → die intentionally → verify Game Over → return to menu.

---

## Feature Requirements

| Feature | Description | User Story | Priority | Acceptance Criteria | Dependencies |
|---------|-------------|-----------|----------|---------------------|--------------|
| **Main Menu** | Entry screen with navigation buttons | As a player, I want a clear menu on startup | Must | Buttons: Start, Options, High Score, Exit; animated background loops; hover effects work | UI Canvas, SceneManager |
| **Options Menu** | Volume sliders + fullscreen toggle | As a player, I want to adjust audio and display | Must | Master/Music/SFX sliders update AudioMixer in real time; Fullscreen toggle works; settings saved to PlayerPrefs | AudioMixer, PlayerPrefs |
| **High Score Display** | Shows top 5 scores | As a player, I want to see my best scores | Must | Scores persist via PlayerPrefs; displayed on Main Menu; sorted descending | ScoreManager, PlayerPrefs |
| **3 Levels** | 2 normal levels + 1 boss level | As a player, I want to progress through distinct stages | Must | Each level loads correctly; Level 1 → Level 2 → Level 3 (boss); no scene loading errors | SceneManager, LevelManager |
| **Tile-Based Levels** | Unity Tilemap for each level | As a player, I want a structured environment | Must | Each scene has ≥ 3 tile layers (Background, Collision, Decorative); collision tiles block the player | Tilemap, TilemapCollider2D |
| **Player Movement** | 8-direction, screen-bounded | As a player, I want responsive ship control | Must | WASD/Arrow Keys move the ship; ship cannot leave screen bounds; smooth at 60 FPS | Rigidbody2D |
| **Shooting System** | Hold Space to fire bullets | As a player, I want to shoot enemies | Must | Bullets spawn at ship nose; travel upward; despawn on hit or off-screen | Prefab, ObjectPool |
| **Dash Mechanic** | Left Shift to dodge | As a player, I want to evade enemy bullets | Must | Dash moves ship 3× speed for 0.15s; 2s cooldown; i-frames during dash; visual trail | Coroutine |
| **Wave System** | 1–2 waves per level | As a player, I want to fight enemies in waves | Must | Wave 1 spawns on level start; Wave 2 spawns after Wave 1 cleared; "Wave Clear" text shown between waves | WaveManager |
| **Enemy: Drone** | Moves straight down, shoots periodically | As a player, I want a basic enemy | Must | Moves downward at constant speed; fires every 2s; dies in 1–2 hits; introduced in Level 1 | EnemyDrone script |
| **Enemy: Hunter** | Tracks player X-axis, shoots | As a player, I want a smarter enemy | Must | Mirrors player's X position; fires every 1.5s; tankier than Drone; introduced in Level 2 | EnemyHunter script |
| **Enemy: Boss** | Large enemy with 3 attack phases | As a player, I want a challenging final fight | Must | 3 phases at 100%/66%/33% HP; attack pattern changes each phase; boss HP bar shown; introduced in Level 3 | BossController |
| **Collision & Triggers** | Full hit detection between all objects | System detects all interactions accurately | Must | Bullet↔Enemy: lose HP; Enemy↔Player: lose HP; Player↔PowerUp: apply effect; Player↔Hazard tile: damage | Collider2D, Triggers |
| **HUD** | HP bar, Shield bar, Score, Wave number | As a player, I want to see my status at all times | Must | All values update immediately; no overlap with playfield | Canvas (Overlay) |
| **Pause Menu** | ESC pauses; Resume/Restart/Main Menu | As a player, I want to pause at any time | Must | ESC sets timeScale=0; panel appears; Resume restores timeScale=1 | GameManager, Time |
| **Boss Battle UI** | Warning banner + boss HP bar | As a player, I want to know when the boss arrives | Must | "⚠ WARNING: BOSS APPROACHING" shown for 3s before spawn; boss HP bar appears on spawn | BossHUDController |
| **Power-ups** | Health Pack, Shield, Score Multiplier | As a player, I want items to help me survive | Must | 30% drop chance on enemy death; visual flash + SFX on collect; effect applied immediately | PowerUpManager |
| **Scoring & Combo** | Points per kill + streak multiplier | As a player, I want score to reflect skill | Must | Drone=100pts, Hunter=200pts, Boss=1000pts; 5 kills without hit = ×2 multiplier; real-time display | ScoreManager |
| **Game Over Screen** | Stats + Restart/Main Menu buttons | As a player, I want to see my results | Must | Shows: Final Score, Enemies Defeated, Survival Time; Restart = reload Level 1; Main Menu = menu scene | SceneManager |
| **Animations** | Thruster, damage flash, explosion, shoot | As a player, I want the game to feel alive | Must | All animations play in correct state; no stuck or looping animations | Animator, Sprite Sheets |
| **Physics** | Rigidbody2D for ship, enemies, bullets | Realistic movement and knockback | Must | Knockback on player hit; explosion force pushes nearby objects; bullets have constant velocity | Rigidbody2D |
| **Parallax Background** | Multi-layer scrolling background | As a player, I want a sense of movement | Should | ≥ 2 layers scroll at different speeds; no gaps during loop | ParallaxBackground.cs |
| **Audio System** | BGM per scene + SFX for all events | As a player, I want audio feedback for everything | Must | SFX plays within 1 frame of event; no unexpected silence; volume adjustable via Options | AudioSource, AudioMixer |
| **Save / Load** | Save level + score; load from Main Menu | As a player, I want to continue later | Should | Save writes level index + score to PlayerPrefs; Load resumes from that level | PlayerPrefs |

---

## Level Design

### Level Structure

| Level | Scene Name | Theme | New Enemy Introduced | Waves | Ends With |
|-------|------------|-------|---------------------|-------|-----------|
| 1 | `Level1.unity` | Earth Orbit | **Drone** | 2 | Level Complete → Load Level 2 |
| 2 | `Level2.unity` | Asteroid Field | **Hunter** | 2 | Level Complete → Load Level 3 |
| 3 | `Level3_Boss.unity` | Deep Space | **Boss** | 2 (Wave 2 = Boss only) | Victory → Main Menu |

### Wave Composition

**Level 1 — Earth Orbit**
- Wave 1: 4 Drones spawn from top, evenly spaced across screen width.
- Wave 2: 6 Drones in a V-pattern; movement speed +20% vs Wave 1.

**Level 2 — Asteroid Field**
- Wave 1: 2 Drones + 2 Hunters. Hunters spawn 2s after Drones.
- Wave 2: 4 Hunters + 1 Drone. Hunters move 15% faster than Level 1.

**Level 3 — Deep Space**
- Wave 1: 3 Drones + 3 Hunters, mixed spawn across the screen top.
- Wave 2: Boss only. Spawns 2s after Wave 1 is cleared.

### Tilemap Layers (same setup across all 3 levels, different tile art per level)

| Layer | Tilemap Name | Has Collider | Purpose |
|-------|-------------|-------------|---------|
| 0 | `Tilemap_BG` | No | Background decoration (space scenery) |
| 1 | `Tilemap_Collision` | Yes (CompositeCollider2D) | Walls / station edges blocking movement |
| 2 | `Tilemap_Hazard` | Yes (isTrigger = true) | Deal 10 damage/s on contact |
| 3 | `Tilemap_Decor` | No | Foreground decorative elements |

---

## User Flows

### Flow 1: Start a New Game
1. Launch `.exe` → Main Menu with looping animated background.
2. Player clicks **Start Game** → Level 1 loads.
3. "Wave 1" text (1.5s) → Wave 1 enemies spawn.
4. Wave 1 cleared → "Wave 2" text (1.5s) → Wave 2 spawns.
5. Wave 2 cleared → "Level Complete!" (2s) → Level 2 loads automatically.
6. Repeat for Level 2 → Level 3.
7. Boss defeated → "VICTORY!" screen → player returns to Main Menu; score saved.

### Flow 2: Player Death
1. Player HP = 0 → explosion animation (0.8s).
2. Game Over screen: Final Score | Enemies Defeated | Survival Time.
3. **Restart** → Level 1 reloads from scratch.
4. **Main Menu** → Main Menu loads.

### Flow 3: Pause
1. Press **ESC** → `Time.timeScale = 0`; Pause panel appears.
2. **Resume** → `Time.timeScale = 1`.
3. **Restart Level** → reload current scene (reset timeScale first).
4. **Main Menu** → load Main Menu scene (reset timeScale first).

### Flow 4: Boss Encounter (Level 3, Wave 2)
1. Wave 1 cleared → 2s pause.
2. Screen flashes red; "⚠ WARNING: BOSS APPROACHING" banner (3s) + alert SFX.
3. Boss slides in from top-center; boss HP bar appears in HUD.
4. Boss at 66% HP → Phase 2: speed +50%, new straight bullet barrage.
5. Boss at 33% HP → Phase 3: speed +100%, spread shot (3 bullets, ±15°), spawns 2 Drones once.
6. Boss HP = 0 → large explosion (0.8s) → "BOSS DEFEATED" + 1000pts → Victory screen.

### Flow 5: Options
1. Main Menu → **Options** → panel opens.
2. Drag sliders → AudioMixer updates in real time.
3. Toggle **Fullscreen** → `Screen.fullScreen = !Screen.fullScreen`.
4. **Back** → settings auto-saved to PlayerPrefs.

---

## Non-Functional Requirements

### Performance
- **Target:** Stable 60 FPS on i5 Gen 8, 8 GB RAM, no discrete GPU.
- **Object Pooling:** Required for bullets and explosions — no `Instantiate`/`Destroy` per frame.
- **Max Active Rigidbody2D:** ≤ 50 at any time (easily achievable with simplified wave counts).

### Compatibility
- **OS:** Windows 10 / 11 (64-bit).
- **Unity:** 2022.3 LTS.
- **Resolution:** 1280×720 default; 1920×1080 supported. Aspect: 16:9 only.
- **Input:** Keyboard + mouse for UI only.

### Scope Constraints (Student Project — 2 Months)
- No multiplayer, no online features, no procedural generation.
- No in-game cutscenes — text banners only.
- No gamepad support.
- No unlockable ships, no secret levels, no multiple save slots.
- Maximum 3 enemy types (Drone, Hunter, Boss). No new types.

---

## Technical Specifications

### Project Folder Structure
```
Assets/
├── Audio/
│   ├── BGM/            ← .ogg, Streaming load, loop=true
│   └── SFX/            ← .wav, Decompress on Load, loop=false
├── Fonts/              ← .ttf → generate TMP Font Asset
├── Prefabs/
│   ├── Enemies/
│   ├── Player/
│   ├── Bullets/
│   ├── PowerUps/
│   └── Effects/
├── Scenes/
│   ├── MainMenu.unity
│   ├── Level1.unity
│   ├── Level2.unity
│   ├── Level3_Boss.unity
│   └── GameOver.unity
├── Scripts/
│   ├── Player/
│   ├── Enemies/
│   ├── Managers/
│   ├── Systems/
│   └── UI/
└── Sprites/
    ├── Player/
    ├── Enemies/
    ├── Bullets/
    ├── Effects/
    ├── PowerUps/
    ├── Tiles/
    ├── Backgrounds/
    └── UI/
```

### Unity Component Map

| System | Approach |
|--------|---------|
| Player movement | `Rigidbody2D` (Kinematic) + `rb.MovePosition()` each FixedUpdate |
| Shooting | Bullet Prefab + `UnityEngine.Pool.ObjectPool<GameObject>` |
| Enemy AI | `MonoBehaviour` + `Coroutine`; `Vector2.MoveTowards` for Hunter X tracking |
| Collision | `Collider2D` (isTrigger=false) + `OnCollisionEnter2D` |
| Triggers | `Collider2D` (isTrigger=true) + `OnTriggerEnter2D` |
| Tilemap | `Tilemap` + `TilemapCollider2D` + `CompositeCollider2D` |
| UI | `Canvas` + `TextMeshPro` + `Slider` + `Button` + `Image` |
| Audio | `AudioSource` + `AudioMixer` (3 groups: Master, Music, SFX) |
| Save | `PlayerPrefs` only (scores, options, last level) |
| Animation | `Animator` + `AnimationClip` from sprite sheets |
| Object Pool | `UnityEngine.Pool.ObjectPool<T>` (built-in, Unity 2021+) |
| Parallax | `ParallaxBackground.cs` reading `Camera.main.position` delta per frame |
| Physics gravity | Set `Physics2D.gravity = Vector2.zero` in Project Settings |

### Scripts List

| Script | Folder | Responsibility |
|--------|--------|----------------|
| `PlayerController.cs` | Player/ | Movement input, dash, screen clamp |
| `PlayerHealth.cs` | Player/ | HP & Shield tracking, damage, death event |
| `BulletPlayer.cs` | Bullets/ | Move upward, despawn on hit or off-screen |
| `BulletEnemy.cs` | Bullets/ | Move downward, despawn on hit or off-screen |
| `EnemyDrone.cs` | Enemies/ | Move straight down, fire at set interval |
| `EnemyHunter.cs` | Enemies/ | Track player X, fire at set interval |
| `BossController.cs` | Enemies/ | Phase thresholds, attack switching, Phase 3 Drone spawn |
| `EnemyHealth.cs` | Enemies/ | HP, damage intake, death → score event + drop |
| `WaveManager.cs` | Managers/ | Wave 1 → Wave 2 sequence; poll enemy count; trigger boss wave |
| `ScoreManager.cs` | Managers/ | Add points, manage combo streak, save high score |
| `GameManager.cs` | Managers/ | State machine: Playing / Paused / GameOver / Victory |
| `LevelManager.cs` | Managers/ | Level Complete delay → load next scene |
| `AudioManager.cs` | Managers/ | Singleton; `PlaySFX(string)`, `PlayBGM(string)` |
| `SaveManager.cs` | Managers/ | Read/write PlayerPrefs for score, options, last level |
| `PowerUp.cs` | Systems/ | Base class; `OnTriggerEnter2D` → call effect |
| `ObjectPool.cs` | Systems/ | Generic pool for bullets and effects |
| `ParallaxBackground.cs` | Systems/ | Scroll layers at different multiplied speeds |
| `HUDController.cs` | UI/ | Update HP slider, Shield slider, Score text, Wave text |
| `BossHUDController.cs` | UI/ | Show warning banner; show/hide boss HP bar |
| `MainMenuController.cs` | UI/ | Button event handlers |
| `PauseMenuController.cs` | UI/ | ESC handling, timeScale control |
| `GameOverController.cs` | UI/ | Populate stats text; Restart / Main Menu handlers |
| `OptionsController.cs` | UI/ | Sliders → AudioMixer; toggle → Screen.fullScreen; save |

---

## Asset Specifications

> **Strategy:** All visual assets are AI-generated using free tools (see prompts below). Audio assets are downloaded from verified CC0 sources. Only tested, working links are listed.

---

### 🎨 Visual Assets — AI-Generated

**Recommended free AI tools:**
- **Adobe Firefly** — https://firefly.adobe.com (free tier, no download needed)
- **Bing Image Creator** — https://www.bing.com/images/create (free, DALL-E 3 powered)
- **DALL-E 3 via ChatGPT** — https://chat.openai.com (free tier)

**Universal import settings in Unity for ALL sprites:**
- Texture Type: `Sprite (2D and UI)`
- Filter Mode: `Point (no filter)` ← critical, keeps pixels sharp
- Compression: `None`
- Pixels Per Unit: `32` ← must be consistent across all assets

**Universal prompt prefix — paste before every prompt below:**
> `pixel art, 2D game sprite, transparent background, top-down view, retro arcade sci-fi style, clean pixel edges —`

---

#### A. Player Ship

| File | Size | AI Prompt (append to prefix above) |
|------|------|-------------------------------------|
| `player_ship.png` | 32×32 | `blue sleek triangular spaceship, neon blue engine glow at rear, single ship, top-down view` |
| `player_thruster_sheet.png` | 128×32 (4 frames) | `blue spaceship thruster flame animation, 4 frames horizontal strip, 32x32 per frame, blue-white flame cycling brightness` |
| `player_damage.png` | 32×32 | Same as `player_ship.png`, then apply red color overlay at 50% opacity in Photopea (https://www.photopea.com, free) |

---

#### B. Enemy Ships

| File | Size | AI Prompt |
|------|------|-----------|
| `enemy_drone.png` | 32×32 | `small red angular enemy drone spaceship, glowing red engine, menacing, top-down` |
| `enemy_drone_sheet.png` | 64×32 (2 frames) | `red enemy drone spaceship 2-frame idle animation, horizontal strip, 32x32 per frame, alternating engine brightness` |
| `enemy_hunter.png` | 32×32 | `orange wide-wing hunter spaceship, aggressive angular design, glowing orange thrusters, top-down` |
| `enemy_hunter_sheet.png` | 128×32 (4 frames) | `orange hunter spaceship thruster animation, 4 frames horizontal strip, 32x32 per frame, orange flame cycling` |
| `enemy_boss.png` | 64×64 | `large purple menacing boss spaceship, symmetrical, multiple cannons, glowing purple energy core, highly detailed, top-down` |
| `enemy_boss_phase2.png` | 64×64 | `purple boss spaceship with battle damage, cracks and sparks, darker tones, top-down` |
| `enemy_boss_phase3.png` | 64×64 | `critically damaged purple boss ship, glowing red exposed core, pieces breaking apart, top-down` |

---

#### C. Bullets & Projectiles

| File | Size | AI Prompt |
|------|------|-----------|
| `bullet_player.png` | 8×16 | `small neon blue laser bullet, vertical capsule shape, glowing, transparent background` |
| `bullet_enemy.png` | 8×16 | `small red laser bullet, vertical capsule, glowing red, transparent background` |
| `bullet_boss.png` | 12×24 | `large purple energy orb projectile, glowing aura, transparent background` |

---

#### D. Explosion & Hit Effects

| File | Size | Frames | AI Prompt |
|------|------|--------|-----------|
| `explosion_small_sheet.png` | 192×32 | 6 frames (32×32 each) | `small explosion animation, 6 frames horizontal strip, orange-yellow blast, 32x32 per frame, transparent background` |
| `explosion_large_sheet.png` | 512×64 | 8 frames (64×64 each) | `large explosion animation, 8 frames horizontal strip, orange-red-white blast, 64x64 per frame, transparent background` |
| `effect_hit_sheet.png` | 64×16 | 4 frames (16×16 each) | `hit impact spark flash, 4 frames horizontal strip, white-yellow burst, 16x16 per frame, transparent background` |

> **Alternative (download instead of generating):**
> Free CC0 explosion spritesheet: https://opengameart.org/content/explosion

---

#### E. Power-Up Icons

| File | Effect | AI Prompt |
|------|--------|-----------|
| `powerup_health.png` | Restore 30 HP | `red glowing health pack icon, cross or heart shape, sci-fi style, 16x16, transparent background` |
| `powerup_shield.png` | Restore 25 Shield | `blue hexagonal energy shield icon, glowing blue aura, sci-fi, 16x16, transparent background` |
| `powerup_score.png` | ×2 score for 10s | `golden star icon, pixel art, glowing yellow, 16x16, transparent background` |

---

#### F. Obstacles

| File | Size | AI Prompt |
|------|------|-----------|
| `obstacle_mine.png` | 24×24 | `space mine, dark grey sphere with spikes, glowing red center eye, top-down, transparent background` |
| `obstacle_mine_sheet.png` | 96×24 (4 frames) | `space mine pulsing animation, 4 frames horizontal strip, 24x24 per frame, red glow brightening and dimming` |

---

#### G. Parallax Background Layers

> Generate at **1280×720** (no transparency — solid fill).

| File | Layer | Scroll Speed | AI Prompt |
|------|-------|-------------|-----------|
| `bg_stars_far.png` | 1 (slowest, 0.1×) | 0.1× camera | `1280x720 pixel art deep space background, sparse tiny distant stars, dark navy blue, seamlessly tileable vertically` |
| `bg_stars_mid.png` | 2 (0.3×) | 0.3× camera | `1280x720 pixel art space background, medium-size stars, tiny galaxy wisps, dark blue-black, tileable vertically` |
| `bg_nebula_l1.png` | 3 — Level 1 | 0.6× camera | `1280x720 pixel art space nebula, blue-purple glowing clouds, Earth visible in corner, tileable vertically` |
| `bg_nebula_l2.png` | 3 — Level 2 | 0.6× camera | `1280x720 pixel art asteroid field background, grey dust clouds, floating rocks, dark tones, tileable vertically` |
| `bg_nebula_l3.png` | 3 — Level 3 | 0.6× camera | `1280x720 pixel art deep space background, dark void, distant galaxy spiral, ominous feel, tileable vertically` |

> **Alternative free download:** https://ansimuz.itch.io/spaceship-shooter-environment (free, pre-made layered parallax backgrounds)

---

#### H. Tile Assets

> Generate as tile sheets. Each tile: **16×16 px**. Arrange 8 tiles per row on a single PNG for easy slicing in Unity Sprite Editor.

| File | Level | AI Prompt |
|------|-------|-----------|
| `tiles_station.png` | Level 1 | `pixel art sci-fi space station tileset, 8 tiles horizontal, 16x16 each, grey metal panels, blue neon trim, control panel tiles, top-down` |
| `tiles_asteroid.png` | Level 2 | `pixel art asteroid rock tileset, 8 tiles horizontal, 16x16 each, grey-brown rocky chunks, space debris, top-down` |
| `tiles_deepspace.png` | Level 3 | `pixel art dark sci-fi tileset, 8 tiles horizontal, 16x16 each, black panels with purple energy lines, alien tech style` |
| `tiles_hazard.png` | All levels | `pixel art hazard warning tile, 16x16, red and yellow danger stripes, glowing edges, sci-fi floor panel` |
| `tiles_decor.png` | All levels | `pixel art sci-fi decorative tileset, 8 tiles horizontal, 16x16 each, blinking lights, pipes, vents, control terminals` |

> In Unity Sprite Editor: Slice → Grid by Cell Size → 16×16 → apply. Each sliced cell becomes an individual tile in the Tile Palette.

---

#### I. UI Elements

> Download the confirmed-working Kenney UI Pack (CC0) — faster than generating: https://kenney.nl/assets/ui-pack

From the pack, use these files:

| File in Kenney Pack | Rename To | Usage |
|--------------------|-----------|-------|
| `barRed.png` | `ui_healthbar_fill.png` | HP bar fill |
| `barBlue.png` | `ui_shieldbar_fill.png` | Shield bar fill |
| `barBack_horizontalLeft.png` | `ui_bar_bg.png` | Any bar background |
| `buttonSquare_blue.png` | `ui_button_normal.png` | Normal button |
| `buttonSquare_beige_pressed.png` | `ui_button_pressed.png` | Pressed button |
| `panel_beige.png` | `ui_panel.png` | Menu/HUD panel (9-slice) |

> If generating by AI instead: `pixel art sci-fi game UI [element name], dark background, neon blue trim, retro HUD style, transparent background`

---

### 🔤 Fonts

| File | Usage | Download |
|------|-------|----------|
| `PressStart2P-Regular.ttf` | All in-game text | https://fonts.google.com/specimen/Press+Start+2P (OFL, free) |

> After import: **Window → TextMeshPro → Font Asset Creator** → generate at atlas size 512×512, character set: ASCII.

---

### 🔊 Audio Assets (All Verified CC0)

> **Unity import settings:**
> - BGM (`.ogg`): Load Type = `Streaming`, Loop = true
> - SFX (`.wav`): Load Type = `Decompress on Load`, Loop = false

#### A. Background Music

| File Name | Scene | Download Source |
|-----------|-------|----------------|
| `bgm_menu.ogg` | Main Menu | https://opengameart.org/content/5-chiptunes-action — use track 1 (CC0) |
| `bgm_gameplay.ogg` | Level 1 & Level 2 | https://opengameart.org/content/space-shooter-game-music-pack — "Battles" track (CC0) |
| `bgm_boss.ogg` | Level 3 (Boss) | Same pack above — "Boss Battle" track (CC0) |
| `bgm_gameover.ogg` | Game Over screen | https://opengameart.org/content/5-chiptunes-action — slowest track (CC0) |

#### B. Sound Effects

> All from verified, live Kenney packs — download once, use the files matching descriptions below.

| Pack | URL | Contents |
|------|-----|---------|
| Sci-Fi Sounds | https://kenney.nl/assets/sci-fi-sounds | 70 sci-fi SFX: lasers, engines, explosions |
| Interface Sounds | https://kenney.nl/assets/interface-sounds | 100 SFX: chimes, notifications, alerts |
| Impact Sounds | https://kenney.nl/assets/impact-sounds | 130 SFX: hits, crashes, impacts |
| UI Audio | https://kenney.nl/assets/ui-audio | 50 SFX: clicks, switches |

| File Name | Trigger Event | Source Pack | Which file to use |
|-----------|--------------|-------------|-------------------|
| `sfx_shoot_player.wav` | Player fires | Sci-Fi Sounds | Any `laser*.ogg`, high pitch |
| `sfx_shoot_enemy.wav` | Enemy fires | Sci-Fi Sounds | Any `laser*.ogg`, lower pitch |
| `sfx_shoot_boss.wav` | Boss fires | Sci-Fi Sounds | Heaviest laser variant |
| `sfx_explosion_small.wav` | Drone/Hunter dies | Impact Sounds | Short explosion burst |
| `sfx_explosion_large.wav` | Boss dies / Player dies | Impact Sounds | Longest explosion available |
| `sfx_player_hit.wav` | Player takes damage | Impact Sounds | Mid-weight impact |
| `sfx_player_shield_hit.wav` | Hit with shield active | Impact Sounds | Metallic clang variant |
| `sfx_player_dash.wav` | Player dashes | Sci-Fi Sounds | Short whoosh / engine burst |
| `sfx_powerup_health.wav` | Collect health pack | Interface Sounds | Pleasant chime |
| `sfx_powerup_shield.wav` | Collect shield | Interface Sounds | Electronic hum |
| `sfx_powerup_score.wav` | Collect score multiplier | Interface Sounds | Short ascending jingle |
| `sfx_boss_warning.wav` | Boss warning banner | Sci-Fi Sounds | Alarm / siren sound |
| `sfx_boss_phase.wav` | Boss phase transition | Sci-Fi Sounds | Power surge effect |
| `sfx_wave_clear.wav` | Wave cleared | Interface Sounds | Notification chime |
| `sfx_level_complete.wav` | Level complete | Interface Sounds | Positive multi-note jingle |
| `sfx_ui_click.wav` | Button click | UI Audio | Standard click |
| `sfx_ui_hover.wav` | Button hover | UI Audio | Soft tick |
| `sfx_game_over.wav` | Game Over | Impact Sounds | Heavy low-frequency hit |

---

## Player & Enemy Stats

### Player (Default — no unlockable ships)

| Stat | Value |
|------|-------|
| Max HP | 100 |
| Max Shield | 50 (absorbs damage first) |
| Move Speed | 5 units/s |
| Dash Speed | 15 units/s for 0.15s |
| Dash Cooldown | 2.0s |
| Dash I-frames | Full duration (0.15s) |
| Fire Rate | 1 bullet per 0.15s (hold Space) |
| Bullet Speed | 12 units/s upward |
| Lives | 1 (die once = Game Over) |

### Enemies

| Enemy | Max HP | Speed | Fire Interval | Drop Chance | Points |
|-------|--------|-------|--------------|------------|--------|
| Drone | 20 | 2.0 u/s downward | 2.0s | 30% | 100 |
| Hunter | 40 | 3.5 u/s (X-axis tracking) | 1.5s | 30% | 200 |
| Boss Phase 1 | 300 total | 1.0 u/s | 1.0s (straight shot) | — | — |
| Boss Phase 2 | — (triggers at 200 HP) | 1.5 u/s | 0.7s (straight barrage) | — | — |
| Boss Phase 3 | — (triggers at 100 HP) | 2.0 u/s | 0.4s (±15° spread shot) | — | 1000 |

### Scoring

| Event | Points |
|-------|--------|
| Kill Drone | 100 |
| Kill Hunter | 200 |
| Kill Boss | 1000 |
| Collect power-up | 50 |
| Level Complete, no death | 300 bonus |
| Kill streak ×2 (5 consecutive kills, no hit) | Double kill points |
| Kill streak ×3 (10 consecutive kills, no hit) | Triple kill points |
| Taking damage | Resets streak to 0 |
| Power-up score multiplier (×2, 10s) | Overrides streak while active |

---

## Release Planning

### MVP — Submission Build

**Week 1–2:** Player movement + shooting + Drone enemy + basic collision + HUD skeleton + ObjectPool.
**Week 3:** Hunter AI + Boss (all 3 phases) + WaveManager + power-ups + Game Over screen + Pause.
**Week 4:** Main Menu + Options + BGM/SFX integration + Tilemap all 3 levels + parallax + polish + `.exe` build.

**Cut entirely for submission (out of scope):**
- Unlockable ships
- Multiple save slots
- Gamepad support
- Secret/hidden content
- Animated tiles (static tiles only)
- In-game story/cutscenes

---

## Open Questions & Assumptions

| # | Question | Assumption |
|---|----------|------------|
| Q1 | How many lives does the player have? | **1 life.** Dying once triggers Game Over immediately. No extra lives or respawns. |
| Q2 | Does Shield regenerate passively over time? | **No.** Shield only restores via Shield power-up. Makes power-ups feel meaningful. |
| Q3 | What happens when the player tries to leave the screen? | **Hard clamped.** Player position is clamped to camera viewport bounds every `FixedUpdate`. Cannot exit screen in any direction. |
| Q4 | Do player bullets and enemy bullets collide with each other? | **No.** Bullets are on separate physics layers. Bullet-bullet collision disabled in Unity Layer Collision Matrix. |
| Q5 | Do enemies collide with each other? | **No.** All enemies share a layer that ignores self-collision. Only player↔enemy and bullet↔enemy are active. |
| Q6 | How is the Boss Phase 3 spread shot implemented? | **3 bullets simultaneously:** center direction + `Quaternion.Euler(0,0,15°)` + `Quaternion.Euler(0,0,-15°)` relative to player direction. |
| Q7 | Does Boss Phase 3 keep spawning Drones? | **No. Exactly 2 Drones spawned once** when Phase 3 begins. No further Drone spawns during boss fight. |
| Q8 | How long is the cooldown between waves? | **2 seconds.** "Wave N Cleared!" text shows for 2s, then Wave N+1 begins. No enemies can spawn during cooldown. |
| Q9 | Where exactly do enemies spawn on screen? | **Top edge of camera viewport, Y = camera top − 0.5 units.** X positions are evenly distributed across screen width based on wave count. |
| Q10 | What triggers the transition to the next level? | **Automatic 2-second delay** after last enemy (or boss) dies, then `SceneManager.LoadScene()`. No player input required. |
| Q11 | How many high scores are saved? | **Top 5.** Stored as `PlayerPrefs.SetInt("HighScore_1")` through `HighScore_5`, sorted descending. |
| Q12 | Does the player take damage during a dash? | **No.** Dash grants full i-frames for 0.15s duration. Combo streak does NOT reset if hit during dash. |
| Q13 | Does the score multiplier power-up stack with the kill streak multiplier? | **No stacking.** Power-up multiplier (×2, 10s) overrides streak multiplier temporarily. When power-up expires, streak multiplier resumes. |
| Q14 | What audio plays during the Level Complete transition? | **`sfx_level_complete.wav` plays once; BGM fades out over 1 second** before next scene loads. |
| Q15 | Are parallax backgrounds different per level? | **Only the nebula/cloud layer (Layer 3) changes.** The two star layers reuse the same sprites; nebula layer swaps to a different color/image per level. |
| Q16 | What shape is the player hitbox? | **PolygonCollider2D, manually trimmed to ~70% of sprite width at the ship body.** Wing tips are excluded from the hitbox for fairness. |
| Q17 | What font sizes are used in the HUD? | **Score text: 24px. Wave indicator: 18px. All other HUD labels: 16px.** All use PressStart2P via TextMeshPro. |
| Q18 | How does Save/Load work exactly? | **Save writes:** `currentLevel` (int), `currentScore` (int). **Load reads** these and calls `SceneManager.LoadScene(currentLevel)` with score restored. High Score is saved separately and never overwritten by Load. |
| Q19 | What happens if the player dies during the boss death animation? | **Boss death animation takes priority.** A bool flag `isDying = true` in `BossController.cs` blocks all damage and prevents double-death triggers. Boss explosion always plays to completion. |
| Q20 | What is the player's vertical movement range? | **Player Y is clamped between bottom 10% and bottom 50% of the screen.** Player cannot go above the midpoint (enemy spawn zone) or below the visible area. |
| Q21 | What is the exact vertical spawn range enemies use? | **Top 5% of screen height only.** Enemies should never spawn inside the player's movement zone. |
| Q22 | Can the player collect a power-up during the dash? | **Yes.** Triggers are still detected during dash (i-frames only prevent damage, not pickup). |
| Q23 | Does the boss move laterally (left/right) as well as vertically? | **Yes, during Phase 2 and 3.** Boss moves in a slow horizontal sine wave pattern (amplitude = 30% of screen width, period = 4 seconds). Phase 1: stationary. |
| Q24 | What happens to on-screen enemies when a Level Complete triggers? | **All remaining enemies are immediately destroyed** (no drop, no score) when the wave clear condition is met and Level Complete fires. |
| Q25 | Is there a maximum bullet count on screen at once? | **Yes: 20 player bullets + 30 enemy bullets max.** ObjectPool capped at these limits. Oldest bullet is auto-returned to pool if cap is reached. |

---

## Appendix

### Asset Source Quick Reference

| Asset Type | Method | Link |
|------------|--------|------|
| All sprites (ships, bullets, tiles, BG) | **AI-generated** (prompts in doc) | Adobe Firefly / Bing Image Creator / DALL-E 3 |
| Explosion effects | Download (CC0) | https://opengameart.org/content/explosion |
| Parallax backgrounds (alternative) | Download (free) | https://ansimuz.itch.io/spaceship-shooter-environment |
| UI elements | Download (CC0) | https://kenney.nl/assets/ui-pack |
| BGM | Download (CC0) | https://opengameart.org/content/space-shooter-game-music-pack |
| BGM (alternative) | Download (CC0) | https://opengameart.org/content/5-chiptunes-action |
| SFX — Lasers & Sci-Fi | Download (CC0) | https://kenney.nl/assets/sci-fi-sounds |
| SFX — Impacts & Explosions | Download (CC0) | https://kenney.nl/assets/impact-sounds |
| SFX — Notifications & Chimes | Download (CC0) | https://kenney.nl/assets/interface-sounds |
| SFX — UI Clicks | Download (CC0) | https://kenney.nl/assets/ui-audio |
| SFX — any individual sound | Download (CC0 filter) | https://freesound.org |
| Font | Download (OFL) | https://fonts.google.com/specimen/Press+Start+2P |

### Glossary

| Term | Definition |
|------|------------|
| Wave | A predefined group of enemies; Wave 2 only starts after Wave 1 is fully cleared |
| Tilemap | Unity system for building levels from small reusable tile sprites |
| Rigidbody2D | Unity 2D physics component; Gravity Scale must be 0 (space = no gravity) |
| ObjectPool | Reusing pre-created GameObjects (bullets, explosions) instead of Instantiate/Destroy each frame |
| Parallax | Multiple background layers scrolling at different speeds to simulate depth |
| isTrigger | Collider mode: detects overlaps via `OnTriggerEnter2D` without blocking physics |
| Sprite Sheet | Single PNG file containing multiple animation frames laid out in a horizontal strip |
| PPU | Pixels Per Unit — number of sprite pixels per 1 Unity unit; must be 32 for all assets |
| 9-slice | Scaling method that preserves corners of a UI sprite when the panel is resized |
| CC0 | Creative Commons Zero — fully free, no attribution required |
| OFL | Open Font License — free for all uses including commercial |
| I-frames | Invincibility frames — period during which the player cannot take damage (active during dash) |
| TMP | TextMeshPro — Unity's advanced text renderer; required for pixel-accurate font rendering |
| Phase | A stage within the Boss fight triggered by HP thresholds (100% / 66% / 33%) |
