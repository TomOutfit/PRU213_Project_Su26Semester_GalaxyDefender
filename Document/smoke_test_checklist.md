# Galaxy Defender - Smoke Test Checklist

| Feature | Pass / Fail | Notes |
| :--- | :---: | :--- |
| **1. UI & Menus** | | |
| Main Menu loads correctly | | |
| Main Menu buttons (Start, Options, Exit) work | | |
| Options menu controls adjust settings correctly | | |
| Pause Menu opens/closes via Escape | | |
| Game Over screen displays score and stats correctly | | |
| Game Over buttons (Restart, Main Menu) work | | |
| Victory screen loads correctly on completing Level 3 | | |
| Victory buttons work properly | | |
| All fonts render without corruption (no missing/pink fonts) | | |
| **2. Audio** | | |
| Background music plays in all scenes (Main Menu, Levels, Victory) | | |
| SFX plays for player shooting | | |
| SFX plays for enemy shooting | | |
| SFX plays for player hit / shield hit | | |
| SFX plays for enemy hit / explosion | | |
| SFX plays for boss phase changes and abilities | | |
| No missing audio references or errors in console | | |
| **3. Gameplay & Visuals** | | |
| Player movement is smooth and bounds are enforced | | |
| Player can shoot and weapons upgrade via power-ups | | |
| Enemies spawn correctly in all 3 levels according to waves | | |
| Boss spawns in Level 3 and behaves correctly | | |
| Boss phase transitions (sprites update) at 66% and 33% health | | |
| Parallax scrolling and background elements render correctly | | |
| VFX particles play correctly for explosions and impacts | | |
| Dynamic Weather system works without particle errors | | |
| All sprites render correctly (no pink/missing materials) | | |
| **4. Stability** | | |
| No NullReferenceExceptions or errors in Console during full playthrough | | |
| Transitions between levels are seamless | | |
| Performance remains stable during intensive waves (Level 3) | | |
