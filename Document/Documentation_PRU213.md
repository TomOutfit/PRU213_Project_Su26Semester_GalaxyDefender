# **Game: “Galaxy Defender”**

## **Overview**

**Genre:** 2D Space Shooter  
**Platform:** PC

---

# **Gameplay Mechanics**

Galaxy Defender is a 2D space shooter where the player controls a spaceship and fights against waves of enemy ships in a sci-fi environment. The player must survive enemy attacks, destroy hostile units, avoid obstacles, and complete each stage to progress through the game.

Core gameplay features include:

* Eight-direction movement  
* Shooting mechanics  
* Enemy wave system  
* Power-up collection  
* Boss battles  
* Score and progression system

---

# **UI Design: Main Menu (5 points)**

## **Main Menu Features**

* Start Game button with a pixel-art sci-fi style  
* Options menu  
* Exit Game button  
* High Score display  
* Save game   
* Load game from saved files  
* Current level or progression indicator

## **Options Menu**

Players can configure:

* Master volume  
* Music and sound effects volume  
* Screen resolution  
* Fullscreen mode  
* Key bindings

## **Visual Design**

The menu uses:

* Pixel-art interface elements  
* Animated space background  
* Consistent sci-fi theme

---

# **Level Design with Tiles (15 points)**

## **Tile-Based Environment**

The game uses Unity Tilemap to create:

* Space stations  
* Asteroid fields  
* Enemy sectors  
* Hazard zones

## **Tile Categories**

* Background tiles  
* Collision tiles  
* Decorative sci-fi tiles  
* Hazard tiles

## **Level Structure**

Levels are designed to:

* Increase difficulty progressively  
* Introduce new enemy patterns  
* Encourage movement and reaction timing

---

# **Player Controls and Animations (15 points)**

## **Player Controls**

| Action | Input |
| ----- | ----- |
| Move | WASD / Arrow Keys |
| Shoot | Space |
| Special Ability | Left Shift |
| Pause | ESC |

## **Player Mechanics**

* Smooth spaceship movement  
* Continuous shooting system  
* Dash mechanic for evasion  
* Weapon upgrade functionality

## **Animations**

The player spaceship includes:

* Thruster animation  
* Damage feedback effect  
* Explosion animation  
* Shooting effects

---

# **Unity Physics Engine: Rigid Bodies and Forces (10 points)**

## **Physics Implementation**

Unity Rigidbody2D is used for:

* Player spaceship  
* Enemy ships  
* Bullets and projectiles

## **Physics Features**

* Collision handling  
* Knockback effects  
* Explosion force effects  
* Projectile movement

---

# **Interactive UI Systems (10 points)**

## **HUD Elements**

The gameplay UI displays:

* Health bar  
* Shield bar  
* Current score  
* Current wave  
* Ammo or energy level

## **Pause Menu**

The pause system includes:

* Resume Game  
* Restart Level  
* Return to Main Menu

## **Boss Interface**

During boss encounters:

* Boss health bar is displayed  
* Warning notifications appear before battle

---

# **Handling Collisions and Triggers (15 points)**

## **Collision Detection**

The system detects interactions between:

* Bullets and enemies  
* Enemies and player  
* Player and obstacles  
* Player and collectible items

## **Trigger Systems**

Triggers are used for:

* Enemy wave activation  
* Boss battle events  
* Power-up spawning

## **Collectibles**

Players can collect:

* Health packs  
* Shield boosters  
* Score multipliers

---

# **Enemy AI and Obstacles (15 points)**

## **Enemy Types**

| Enemy Type | Behavior |
| ----- | ----- |
| Drone | Basic forward movement |
| Hunter | Tracks player position |
| Boss | Multiple attack patterns |

## **Enemy AI Features**

* Randomized shooting intervals  
* Patrol movement  
* Player tracking  
* Attack pattern switching

## **Obstacles**

Environmental hazards include:

* Space mines

---

# **Game Logic and Scoring Systems (15 points)**

## **Scoring System**

Players earn points by:

* Destroying enemies  
* Collecting items  
* Completing stages  
* Maintaining combo streaks

## **Bonus System**

Additional score bonuses:

* Kill streak multiplier

## **Respawn and Checkpoints**

* Mid-level checkpoints  
* Respawn after player death

## **Game Over System**

The game over screen displays:

* Final score  
* Enemies defeated  
* Survival time

Options include:

* Restart  
* Return to Main Menu

---

# **Visuals and Aesthetics**

## **Art Style**

* Pixel-art graphics  
* Retro arcade-inspired visual effects  
* Neon projectile effects  
* Animated explosions

## **Environment Design**

* Scrolling space backgrounds  
* Parallax visual effects  
* Animated stars and planets

## **Theme Consistency**

The game maintains a consistent:

* Futuristic sci-fi setting  
* Pixel-art aesthetic  
* Arcade gameplay atmosphere

---

# **Additional Features (Optional)**

## **Multiple Worlds**

* Earth Orbit  
* Asteroid Belt  
* Alien Planet  
* Deep Space

## **Unlockable Spaceships**

| Ship Name | Special Ability |
| ----- | ----- |
| Falcon | Increased speed |
| Titan | Higher durability |
| Phantom | Stealth dash ability |

## **Secret Content**

* Hidden levels  
* Secret boss encounters  
* Unlockable weapons

---

# **Submission Guidelines**

## **Required Submission**

* Unity project folder  
* Compressed ZIP file  
* Playable game build (.exe)

## **Documentation**

Include a short report containing:

* Game concept  
* Design decisions  
* Development challenges  
* Implemented systems  
* Future improvements

---

# **Recommended Unity Systems**

| Feature | Unity Component |
| ----- | ----- |
| Player Movement | Rigidbody2D |
| Shooting System | Prefabs and Instantiate |
| Enemy AI | Scripts and Coroutines |
| User Interface | Canvas System |
| Audio | AudioSource |
| Save System | PlayerPrefs |
| Animation | Animator |

---

# **Recommended Development Scope**

## **Minimum Viable Features**

* Main Menu  
* Player movement  
* Shooting system  
* Three enemy types  
* Score system  
* Game over screen

## **Additional Features**

* Boss battle  
* Upgrade system  
* Multiple weapons  
* Save system  
* Advanced visual effects

---

# **Recommended Free Assets**

* [Unity Asset Store](https://assetstore.unity.com/?utm_source=chatgpt.com)  
* [Kenney Assets](https://kenney.nl/assets?utm_source=chatgpt.com)  
* [itch.io Game Assets](https://itch.io/game-assets/free?utm_source=chatgpt.com)  
* [OpenGameArt](https://opengameart.org/?utm_source=chatgpt.com)  
* AI-Generated Art