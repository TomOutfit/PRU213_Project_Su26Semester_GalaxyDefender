using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class ShowroomController : MonoBehaviour
{
    [System.Serializable]
    public class ShowroomItem
    {
        public string name;
        public string subtitle;
        [TextArea(3, 5)]
        public string description;
        [TextArea(3, 5)]
        public string stats;
        public Sprite mainSprite;
        public Sprite extraSprite; // Optional bullet/projectile sprite
    }

    [Header("Category Data")]
    public List<ShowroomItem> spaceships = new List<ShowroomItem>();
    public List<ShowroomItem> arsenal = new List<ShowroomItem>();
    public List<ShowroomItem> enemies = new List<ShowroomItem>();

    [Header("UI Text References")]
    public TMP_Text itemNameText;
    public TMP_Text itemSubtitleText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemStatsText;

    [Header("UI Visual References")]
    public Image itemMainImage;
    public Image itemExtraImage; // Bullet sprite
    public GameObject extraImageContainer; // Holds bullet label & bullet sprite

    [Header("Navigation Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    [Header("Category Tab Buttons")]
    public Button shipsTabButton;
    public Button arsenalTabButton;
    public Button enemiesTabButton;

    [Header("Tab Visual Feedback")]
    public Color activeTabColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
    public Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.6f); // Faded White/Gray

    private int currentCategoryIndex = 0; // 0 = Ships, 1 = Arsenal, 2 = Enemies
    private int currentItemIndex = 0;

    private void Awake()
    {
        // Add UIPanelEffects automatically if missing
        if (GetComponent<UIPanelEffects>() == null)
        {
            gameObject.AddComponent<UIPanelEffects>();
        }

        // Bind buttons
        if (prevButton != null) prevButton.onClick.AddListener(OnPrevClick);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClick);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShowroom);

        if (shipsTabButton != null) shipsTabButton.onClick.AddListener(() => SetCategory(0));
        if (arsenalTabButton != null) arsenalTabButton.onClick.AddListener(() => SetCategory(1));
        if (enemiesTabButton != null) enemiesTabButton.onClick.AddListener(() => SetCategory(2));
    }

    private void OnEnable()
    {
        if (spaceships == null || spaceships.Count != 5 || arsenal == null || arsenal.Count != 7 || enemies == null || enemies.Count != 10)
        {
            PopulateDefaultData();
        }
        currentItemIndex = 0;
        SetCategory(0);
    }

    public void SetCategory(int categoryIndex)
    {
        currentCategoryIndex = categoryIndex;
        currentItemIndex = 0;

        // Update Tab Button Colors for visual feedback
        UpdateTabButtonsVisual();

        // Refresh details
        UpdateDisplay();
    }

    private void OnNextClick()
    {
        List<ShowroomItem> currentList = GetCurrentList();
        if (currentList.Count == 0) return;

        currentItemIndex = (currentItemIndex + 1) % currentList.Count;
        UpdateDisplay();
        AudioManager.Instance?.PlaySFX("sfx_shoot_player"); // Hover/Click SFX
    }

    private void OnPrevClick()
    {
        List<ShowroomItem> currentList = GetCurrentList();
        if (currentList.Count == 0) return;

        currentItemIndex = (currentItemIndex - 1 + currentList.Count) % currentList.Count;
        UpdateDisplay();
        AudioManager.Instance?.PlaySFX("sfx_shoot_player"); // Hover/Click SFX
    }

    private void UpdateDisplay()
    {
        List<ShowroomItem> currentList = GetCurrentList();
        if (currentList.Count == 0 || currentItemIndex < 0 || currentItemIndex >= currentList.Count)
        {
            ClearDisplay();
            return;
        }

        ShowroomItem item = currentList[currentItemIndex];

        if (itemNameText != null) itemNameText.text = item.name;
        if (itemSubtitleText != null) itemSubtitleText.text = item.subtitle;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        if (itemStatsText != null) itemStatsText.text = item.stats;

        // Set Main Sprite
        if (itemMainImage != null)
        {
            if (item.mainSprite != null)
            {
                itemMainImage.sprite = item.mainSprite;
                itemMainImage.gameObject.SetActive(true);
            }
            else
            {
                itemMainImage.gameObject.SetActive(false);
            }
        }

        // Set Extra Sprite (e.g. Ammo)
        if (itemExtraImage != null)
        {
            if (item.extraSprite != null)
            {
                itemExtraImage.sprite = item.extraSprite;
                if (extraImageContainer != null) extraImageContainer.SetActive(true);
                itemExtraImage.gameObject.SetActive(true);
            }
            else
            {
                if (extraImageContainer != null) extraImageContainer.SetActive(false);
                itemExtraImage.gameObject.SetActive(false);
            }
        }
        else
        {
            if (extraImageContainer != null) extraImageContainer.SetActive(false);
        }
    }

    private void ClearDisplay()
    {
        if (itemNameText != null) itemNameText.text = "";
        if (itemSubtitleText != null) itemSubtitleText.text = "";
        if (itemDescriptionText != null) itemDescriptionText.text = "";
        if (itemStatsText != null) itemStatsText.text = "";
        if (itemMainImage != null) itemMainImage.gameObject.SetActive(false);
        if (extraImageContainer != null) extraImageContainer.SetActive(false);
        if (itemExtraImage != null) itemExtraImage.gameObject.SetActive(false);
    }

    private List<ShowroomItem> GetCurrentList()
    {
        switch (currentCategoryIndex)
        {
            case 0: return spaceships;
            case 1: return arsenal;
            case 2: return enemies;
            default: return new List<ShowroomItem>();
        }
    }

    private void UpdateTabButtonsVisual()
    {
        SetTabStyle(shipsTabButton, currentCategoryIndex == 0, new Color(0f, 1f, 1f, 1f));     // Cyan
        SetTabStyle(arsenalTabButton, currentCategoryIndex == 1, new Color(1f, 0.75f, 0f, 1f)); // Gold / Orange
        SetTabStyle(enemiesTabButton, currentCategoryIndex == 2, new Color(1f, 0.25f, 0.25f, 1f)); // Red
    }

    private void SetTabStyle(Button btn, bool isActive, Color activeColor)
    {
        if (btn == null) return;

        TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.color = isActive ? activeColor : inactiveTabColor;
        }

        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = isActive ? new Color(activeColor.r, activeColor.g, activeColor.b, 0.8f) : new Color(0.15f, 0.15f, 0.15f, 0.5f);
        }

        Outline outline = btn.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = isActive ? activeColor : new Color(0.5f, 0.5f, 0.5f, 0.2f);
        }
    }

    public void CloseShowroom()
    {
        UIPanelEffects panelEffects = GetComponent<UIPanelEffects>();
        if (panelEffects != null)
        {
            panelEffects.Hide();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private Sprite LoadSpriteRuntime(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

#if UNITY_EDITOR
        Sprite editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (editorSprite != null) return editorSprite;
#endif

        if (SpriteDatabase.Instance != null)
        {
            Sprite dbSprite = SpriteDatabase.Instance.GetSprite(path);
            if (dbSprite != null) return dbSprite;
        }

        return null;
    }

    [ContextMenu("Populate Default Showroom Data")]
    public void PopulateDefaultData()
    {
        // 1. Spaceships
        spaceships.Clear();
        string[] shipNames = { "Default Ship", "Iron Vanguard", "Nova Prism", "Shadow Wraith", "Star Swift" };
        string[] shipSubtitles = { 
            "Playable Class: Terran Interceptor (Standard Issue)", 
            "Playable Class: Heavy Vanguard Juggernaut", 
            "Playable Class: Energy Assault Cruiser", 
            "Playable Class: Tactical Stealth Fighter", 
            "Playable Class: Ultra-Speed Reconnaissance Vessel" 
        };
        string[] shipDescriptions = {
            "The backbone of the United Earth Defense Fleet. This standard-issue interceptor is highly balanced, offering recruits a reliable and stable platform to begin their career. Equipped with twin pulse cannons, standard shield generators, and a micro-warp thruster, it remains a favorite among veterans for its handling and predictable performance in dense asteroid fields.",
            "Designed for frontline breakthrough operations, the Iron Vanguard is a heavily armored warship capable of weathering intense enemy fire. It trades speed for raw defensive power and is armed with prototype heavy kinetic shell launchers that deal massive impact damage. Its titanium-reinforced hull allows it to shrug off collisions that would vaporize lighter fighters.",
            "A state-of-the-art prototype vessel powered by raw stellar energy crystals. The Nova Prism projects concentrated energy bolts in wide sweeping arcs, allowing the pilot to clear large formations of enemy ships simultaneously. While its shields require significant power to maintain, its offensive coverage is unmatched.",
            "Developed for deep-space espionage and assassination runs, the Shadow Wraith utilizes a cloaking field generator to slip past enemy radars. It fires high-velocity phantom rounds that bypass minor physical shields. Its lightweight chassis and overcharged thrusters make it incredibly agile, though its hull is relatively fragile.",
            "Built on a lightweight carbon-nanotube chassis and equipped with experimental antimatter thrusters, the Star Swift is the fastest ship in the fleet. It is capable of executing lightning-fast dash maneuvers with minimal cooldown. It fires rapid-fire photon bursts, overwhelming enemies with a continuous barrage of light."
        };
        string[] shipStats = {
            "Speed: 12.0 (Balanced)\nDash Cooldown: 1.5 Seconds\nHull Integrity: Standard (3 HP)\nPrimary Arsenal: Twin Pulse Cannons\nSpecial Attribute: High Maneuverability & Agile Braking\nProjectile Type: Standard Player Laser (Blue)",
            "Speed: 9.5 (Heavy Armored)\nDash Cooldown: 1.8 Seconds\nHull Integrity: Reinforced (4 HP)\nPrimary Arsenal: Kinetic Shell Cannon\nSpecial Attribute: Frontal Defense Shielding\nProjectile Type: Heavy Kinetic Bolt (Orange)",
            "Speed: 11.5 (Energy Cruiser)\nDash Cooldown: 1.4 Seconds\nHull Integrity: Standard (3 HP)\nPrimary Arsenal: Wide-Arc Energy Crystal\nSpecial Attribute: Wide Spread Projectile Coverage\nProjectile Type: Prism Scatter Laser (Teal)",
            "Speed: 13.0 (Espionage Class)\nDash Cooldown: 1.2 Seconds\nHull Integrity: Lightweight (2 HP)\nPrimary Arsenal: Phantom Bolt Launcher\nSpecial Attribute: Phase Dash & Reduced Hitbox\nProjectile Type: Purple Phantom Bolt (Purple)",
            "Speed: 15.0 (Extreme Recon)\nDash Cooldown: 1.0 Seconds\nHull Integrity: Lightweight (2 HP)\nPrimary Arsenal: Rapid Photon Gatling\nSpecial Attribute: Hyper-Speed Thruster Boost\nProjectile Type: Rapid Photon Burst (Yellow)"
        };
        string[] shipPaths = {
            "Assets/Sprites/Player/player_ship.png",
            "Assets/Sprites/Player/iron_vanguard.png",
            "Assets/Sprites/Player/nova_prism.png",
            "Assets/Sprites/Player/shadow_wraith.png",
            "Assets/Sprites/Player/star_swift.png"
        };
        string[] bulletPaths = {
            "Assets/Sprites/Bullets/bullet_player.png",
            "Assets/Sprites/Bullets/player_iron_vanguard_bullet.png",
            "Assets/Sprites/Bullets/player_nova_prism_bullet.png",
            "Assets/Sprites/Bullets/player_shadow_wraith_bullet.png",
            "Assets/Sprites/Bullets/player_star_swift_bullet.png"
        };

        for (int i = 0; i < shipNames.Length; i++)
        {
            spaceships.Add(new ShowroomItem
            {
                name = shipNames[i],
                subtitle = shipSubtitles[i],
                description = shipDescriptions[i],
                stats = shipStats[i],
                mainSprite = LoadSpriteRuntime(shipPaths[i]),
                extraSprite = LoadSpriteRuntime(bulletPaths[i])
            });
        }

        // 2. Arsenal / Power-ups
        arsenal.Clear();
        string[] arsenalNames = { "Repair Kit", "Energy Shield", "Triple Fire Upgrade", "Mega Bomb", "Thruster Spark", "Item Magnet", "Score Booster" };
        string[] arsenalSubtitles = { 
            "Nanotech Repair Module", 
            "Hyper-Frequency Deflector Shield", 
            "Weapon System Matrix Upgrade", 
            "Anti-Matter Field Disrupter", 
            "Temporal Speed Booster", 
            "Gravitational Harvester Field",
            "Quantum Point Multiplier"
        };
        string[] arsenalDescriptions = {
            "Deploys localized nanite swarms to instantly patch up structural breaches and restore spaceship hull integrity. This module is vital for surviving long skirmishes inside enemy territory, converting scrap metal and cosmic debris into functional armor plating.",
            "Generates a bubble of polarized plasma around the ship's hull. The deflector shield is tuned to absorb one incoming projectile or physical collision of any magnitude, shattering after impact to dissipate the energy safely away from the ship.",
            "Overclocks the ship's primary weapon capacitor, routing power to two additional diagonal firing ports. This upgrade significantly increases the ship's damage output and horizontal area coverage, allowing the pilot to suppress incoming fleets.",
            "Releases a highly unstable anti-matter wave that expands outward from the player's ship. The resulting shockwave instantly vaporizes all minor enemy vessels and neutralizes all active enemy projectiles within the local sector, providing a critical reset.",
            "Injects highly refined hypergolic fuel directly into the ship's thruster chambers. This temporarily boosts sub-light speed, increases dash acceleration, and reduces dash cooldown times, allowing the pilot to weave through bullet hell patterns.",
            "Activates a low-frequency gravitational pull that draws all floating items, power-ups, and scrap metal within the sector directly towards the ship. This allows the pilot to focus entirely on dodging hostile fire while collecting resources.",
            "Calibrates the ship's telemetry systems to double all points received for a short duration. Essential for pilot ranking optimization and breaking high score records."
        };
        string[] arsenalStats = {
            "Hull Restoration: +1 Health Unit\nTarget System: Armor/Hull Integrity\nActivation: Instantaneous on pickup\nCooldown: N/A",
            "Shield Capacity: 1 Active Charge\nDuration: Unlimited until broken\nTarget System: Defensive Grid\nActivation: Automatic protection",
            "Firing Lanes: 3 Simultaneous (Spread)\nTarget System: Weapon Matrix\nDuration: Level/Wave duration\nDamage Output: Triple coverage",
            "Blast Radius: Screen-wide clearing\nTarget System: Tactical Payload\nDamage: Ultimate Area Damage\nSpecial Effect: Bullet deletion",
            "Velocity Increase: +25% Speed boost\nDuration: 8.0 Seconds\nTarget System: Propulsion/Thrusters\nDash Cooldown Reduction: -30%",
            "Attraction Range: Sector-wide pull\nDuration: 10.0 Seconds\nTarget System: Auxiliary Utility\nPull Velocity: Progressive acceleration",
            "Multiplier: 2x Score Multiplier\nDuration: 10.0 Seconds\nTarget System: Score Telemetry\nActivation: Instantaneous on pickup"
        };
        string[] arsenalPaths = {
            "Assets/Sprites/PowerUps/powerup_health.png",
            "Assets/Sprites/PowerUps/powerup_shield.png",
            "Assets/Sprites/PowerUps/powerup_triplefire.png",
            "Assets/Sprites/PowerUps/powerup_megabomb.png",
            "Assets/Sprites/PowerUps/powerup_speedbolt.png",
            "Assets/Sprites/PowerUps/powerup_itemmagnet.png",
            "Assets/Sprites/PowerUps/powerup_score.png"
        };

        for (int i = 0; i < arsenalNames.Length; i++)
        {
            arsenal.Add(new ShowroomItem
            {
                name = arsenalNames[i],
                subtitle = arsenalSubtitles[i],
                description = arsenalDescriptions[i],
                stats = arsenalStats[i],
                mainSprite = LoadSpriteRuntime(arsenalPaths[i]),
                extraSprite = null
            });
        }

        // 3. Enemies
        enemies.Clear();
        string[] enemyNames = { 
            "Drone Scout", 
            "Aegis Guardian", 
            "Harvester Curved", 
            "Hunter Interceptor", 
            "Pulse Ray", 
            "Void Stinger", 
            "Proximity Space Mine",
            "Gargantuan Command Ship - Phase I",
            "Gargantuan Command Ship - Phase II",
            "Gargantuan Command Ship - Phase III"
        };
        string[] enemySubtitles = { 
            "Threat Class: Class-I Light Scout Ship", 
            "Threat Class: Class-II Heavy Escort", 
            "Threat Class: Class-I Resource Gatherer", 
            "Threat Class: Class-II Precision Hunter", 
            "Threat Class: Class-III Sniper Ray", 
            "Threat Class: Class-III Heavy Stinger", 
            "Threat Class: Class-III Tactical Hazard",
            "Threat Class: Class-V Boss Dreadnought (Stationary)",
            "Threat Class: Class-V Boss Dreadnought (Strafing)",
            "Threat Class: Class-V Boss Dreadnought (Overdrive)"
        };
        string[] enemyDescriptions = {
            "Mass-produced scout drones deployed by the enemy vanguard. While they lack heavy shields or weapons, they attack in high-speed, synchronized swarms, attempting to overwhelm defensive lines through numbers and flanking maneuvers.",
            "Equipped with dense forward shielding, the Aegis Guardian is designed to protect vulnerable scout units. It slowly drifts forward, absorbing oncoming attacks and returning fire with slow but high-yield energy spheres.",
            "A swift resource harvester repurposed for frontline combat. It moves in sweeping, curved patterns, collecting space debris while deploying hazardous biological spore pods to deter pursuit.",
            "An advanced hunter-killer ship designed to track and eliminate key target vessels. Outfitted with high-yield sniper lasers and searchlight sensors, it tracks the player's movement and fires high-velocity energy beams with extreme precision.",
            "A long-range specialist utilizing focused cyan laser emitters. The Pulse Ray hovers at the top of the sector, locking onto the player's coordinates before releasing continuous high-velocity sniper beams.",
            "An aggressive, fast-moving fighter that dives towards the player ship. Its stinger-shaped weapon matrix fires multiple dense plasma spikes to restrict the player's flight paths.",
            "Heavy space mines deployed to block choke points and asteroid paths. When detonated by proximity or weapon fire, they release a deadly burst of high-velocity physical shrapnel that inflicts massive damage to any nearby hulls.",
            "The flagship of the invading alien fleet. In its first phase, the colossal vessel remains stationary, firing focused heavy energy spheres directly at the player. Its massive shields deflect all minor attacks.",
            "Once its armor is damaged past 66% HP, the Command Ship begins to strafe horizontally across the sector. It overclocks its primary weapon capacitor, significantly increasing its rate of fire.",
            "Operating under 33% HP, the Boss enters overdrive. It moves at maximum speed, deploys twin escort drones, and fires a devastating wide 3-bullet spread spanning ±15 degrees, saturating the entire flight space."
        };
        string[] enemyStats = {
            "Hull Integrity: 10 HP\nThreat Level: Green (Low)\nWeapon Class: Light Plasma Cannon\nScoring Value: 10,000 Points\nProjectile Type: Standard Red Laser",
            "Hull Integrity: 30 HP\nThreat Level: Green (Low)\nWeapon Class: Teal Energy Orb\nScoring Value: 30,000 Points\nProjectile Type: Teal Plasma Sphere",
            "Hull Integrity: 15 HP\nThreat Level: Green (Low)\nWeapon Class: Purple Bio-Spore Pods\nScoring Value: 15,000 Points\nProjectile Type: Purple Spore Pod",
            "Hull Integrity: 50 HP\nThreat Level: Orange (Medium)\nWeapon Class: Red Energy Spike\nScoring Value: 25,000 Points\nProjectile Type: Red Plasma Spike",
            "Hull Integrity: 50 HP\nThreat Level: Orange (Medium)\nWeapon Class: Cyan Sniper Beam\nScoring Value: 50,000 Points\nProjectile Type: High-Velocity Cyan Beam",
            "Hull Integrity: 60 HP\nThreat Level: Orange (Medium)\nWeapon Class: Red Energy Spike\nScoring Value: 60,000 Points\nProjectile Type: Red Plasma Spike",
            "Hull Integrity: 30 HP (Obstacle)\nThreat Level: Yellow (Caution)\nWeapon Class: Contact Shrapnel Blast\nScoring Value: 5,000 Points\nProjectile Type: None",
            "Hull Integrity: 30,000,000 HP\nThreat Level: Red (Critical)\nWeapon Class: Heavy aimed plasma spheres\nScoring Value: 1,000,000 Points\nProjectile Type: Orange Boss Plasma Phase 1",
            "Hull Integrity: 20,000,000 HP\nThreat Level: Red (Critical)\nWeapon Class: Overclocked strafing plasma\nScoring Value: 1,000,000 Points\nProjectile Type: Orange Boss Plasma Phase 2",
            "Hull Integrity: 10,000,000 HP\nThreat Level: Red (Critical)\nWeapon Class: Triple Spread Plasma & Drone Escort\nScoring Value: 1,000,000 Points\nProjectile Type: Orange Boss Plasma Phase 3"
        };
        string[] enemyPaths = {
            "Assets/Sprites/Enemies/enemy_drone.png",
            "Assets/Sprites/Enemies/enemy_aegis_guardian.png",
            "Assets/Sprites/Enemies/enemy_harvester_curved.png",
            "Assets/Sprites/Enemies/enemy_hunter.png",
            "Assets/Sprites/Enemies/enemy_pulse_ray.png",
            "Assets/Sprites/Enemies/enemy_void_stinger.png",
            "Assets/Sprites/Obstacles/obstacle_mine.png",
            "Assets/Sprites/Enemies/enemy_boss.png",
            "Assets/Sprites/Enemies/enemy_boss_phase2.png",
            "Assets/Sprites/Enemies/enemy_boss_phase3.png"
        };
        string[] enemyBulletPaths = {
            "Assets/Sprites/Bullets/bullet_enemy.png",
            "Assets/Sprites/Bullets/enemy_teal_energy_orb.png",
            "Assets/Sprites/Bullets/enemy_purple_bio-spore.png",
            "Assets/Sprites/Bullets/enemy_red_energy_spike.png",
            "Assets/Sprites/Bullets/enemy_cyan_sniper_beam.png",
            "Assets/Sprites/Bullets/enemy_red_energy_spike.png",
            "", // Mine has no projectile sprite
            "Assets/Sprites/Bullets/bullet_boss_phase1.png",
            "Assets/Sprites/Bullets/bullet_boss_phase2.png",
            "Assets/Sprites/Bullets/bullet_boss_phase3.png"
        };

        for (int i = 0; i < enemyNames.Length; i++)
        {
            enemies.Add(new ShowroomItem
            {
                name = enemyNames[i],
                subtitle = enemySubtitles[i],
                description = enemyDescriptions[i],
                stats = enemyStats[i],
                mainSprite = LoadSpriteRuntime(enemyPaths[i]),
                extraSprite = LoadSpriteRuntime(enemyBulletPaths[i])
            });
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[ShowroomController] Default Showroom Data populated successfully!");
    }
}
