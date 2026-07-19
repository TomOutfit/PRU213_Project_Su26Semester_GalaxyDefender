# Galaxy Defender - Smoke Test Checklist

| Feature | Pass / Fail | Notes |
| :--- | :---: | :--- |
| **1. UI & Menus** | | |
| Main Menu loads correctly | Passed | |
| Main Menu buttons (Start, Options, Exit) work | Passed | |
| Options menu controls adjust settings correctly | Passed | |
| Pause Menu opens/closes via Escape | Passed | |
| Game Over screen displays score and stats correctly | Passed | |
| Game Over buttons (Restart, Main Menu) work | Passed | |
| Victory screen loads correctly on completing Level 3 | Passed | |
| Victory buttons work properly | Passed | |
| All fonts render without corruption (no missing/pink fonts) | Passed | |
| **2. Audio** | | |
| Background music plays in all scenes (Main Menu, Levels, Victory) | Passed | |
| SFX plays for player shooting | Passed | |
| SFX plays for enemy shooting | Passed | |
| SFX plays for player hit / shield hit | Passed | |
| SFX plays for enemy hit / explosion | Passed | |
| SFX plays for boss phase changes and abilities | Passed | |
| No missing audio references or errors in console | Passed | |
| **3. Gameplay & Visuals** | | |
| Player movement is smooth and bounds are enforced | Passed | |
| Player can shoot and weapons upgrade via power-ups | Passed | |
| Enemies spawn correctly in all 3 levels according to waves | Passed | |
| Boss spawns in Level 3 and behaves correctly | Passed | |
| Boss phase transitions (sprites update) at 66% and 33% health | Passed | |
| Parallax scrolling and background elements render correctly | Passed | |
| VFX particles play correctly for explosions and impacts | Passed | |
| Dynamic Weather system works without particle errors | Passed | |
| All sprites render correctly (no pink/missing materials) | Passed | |
| **4. Stability** | | |
| No NullReferenceExceptions or errors in Console during full playthrough | Passed | |
| Transitions between levels are seamless | Passed | |
| Performance remains stable during intensive waves (Level 3) | Passed | |
