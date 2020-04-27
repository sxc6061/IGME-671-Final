using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public enum GameState { Starting, Playing, Paused, Ended }

public class GameManager : Singleton<GameManager>
{
	public const float CONSTANT_Y_POS = -0.92f;
    public const ushort ROBOT_ATTACK_DAMAGE = 1;

    //Colors
    private const float TRANSPARENT_ALPHA = 110f / 255f;
    public static readonly Color WHITE = Color.white;
    public static readonly Color GREEN = new Color(125f / 255f, 1f, 100f / 255f, 1);
    public static readonly Color RED = new Color(1, 100f / 255f, 115f / 255f, 1);
    public static readonly Color GREEN_TRANSPARENT = new Color(GREEN.r, GREEN.g, GREEN.b, TRANSPARENT_ALPHA);
    public static readonly Color RED_TRANSPARENT = new Color(RED.r, RED.g, RED.b, TRANSPARENT_ALPHA);
    public static readonly Color GREY_TRANSPARENT = new Color(180f / 255f, 180f / 255f, 180f / 255f, TRANSPARENT_ALPHA);

    [SerializeField] RobotSpawnZone[] robotSpawnZones;
    [SerializeField] AnimationCurve spawnCurve;
    [SerializeField] GameObject robotPrefab;
    [SerializeField] GameObject towerPrefab;
    [SerializeField] GameObject zbuckPrefab;
    [SerializeField] Sprite upgradedTowerSprite;
    [SerializeField] TextMeshProUGUI timerGUI;
    [SerializeField] float boundsX;
    [SerializeField] float boundsY;

    public GameObject pauseMenu;

    public Player player { get; private set; }
    public Sprite UpgradedTowerSprite { get => upgradedTowerSprite; }
    public GameObject mainMenu;

    // Background music properties
    [FMODUnity.EventRef]
    public string menuMusic;
    public float mmSpeed;
    [FMODUnity.EventRef]
    public string ingameMusic;
    public float igmSpeed;
    [FMODUnity.EventRef]
    public string gameoverMusic;
    public float goSpeed;

    // Sound Toggle GameObject References
    public GameObject radiusToggleObj;

    // DeathScreen related properties
    public GameObject deathScreen;
    public TextMeshProUGUI deathTimeDisplay;
    public TextMeshProUGUI killCountDisplay;
    public int killCount { get; private set; } = 0;

    public float BoundsX { get { return boundsX; } }
    public float BoundsY { get { return boundsY; } }

    /// <summary>
    /// The current state of the game
    /// </summary>
    public GameState CurrentState { get; private set; }

    public bool RadiusOption { get; private set; } = true;
    public bool muteSFX { get; private set; } = false;
    public bool muteMusic { get; private set; } = false;
    public float sfxVolume { get; private set; } = 1.0f;
    public float musicVolume { get; private set; } = 1.0f;

    private List<Tower> towers;
    private RobotManager robotManager;

    //ZBuck object pooling
    private const int ZBUCKET_SIZE = 250;
    private List<ZBuck[]> zBuckets;
    private int currIndex;
    private int activeBucks;

    //Initialize vars
    private void Awake()
    {
        towers = new List<Tower>();
        robotManager = new RobotManager(robotPrefab, robotSpawnZones, spawnCurve);
        player = GameObject.FindObjectOfType<Player>();
        player.TowerSize = towerPrefab.GetComponent<SphereCollider>().radius * 1.5f;
        zBuckets = new List<ZBuck[]>();
        mainMenu.SetActive(true);
        radiusToggleObj.SetActive(true);
    }

    // Start is called before the first frame update
    public void Start()
    {
        //Initialize object pools
        ResetGame();
    }

    /// <summary>
    /// Create an object pooling bucket for a set amount of zbucks
    /// </summary>
    private void CreateZBucket()
    {
        var zBucket = new ZBuck[ZBUCKET_SIZE];
        for(int i = 0; i < ZBUCKET_SIZE; i++)
        {
            zBucket[i] = Instantiate(zbuckPrefab).GetComponent<ZBuck>();
        }
        zBuckets.Add(zBucket);
    }

    /// <summary>
    /// Reset data in the manager for a new game
    /// </summary>
    private void ResetGame()
    {
        CurrentState = GameState.Starting;
        robotManager.Start();
        player.Init();

        //Remove any towers
        foreach (var t in towers)
           Destroy(t.gameObject);
        towers.Clear();

        //Remove any zbucks and create new ones
        foreach (var b in zBuckets)
            foreach (var zb in b)
                Destroy(zb.gameObject);
        zBuckets.Clear();
        currIndex = 0;
        activeBucks = 0;
        CreateZBucket();
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void BeginGame()
    {
        ResetGame();
        CurrentState = GameState.Playing;
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        CurrentState = GameState.Paused;
        pauseMenu.SetActive(true);
        radiusToggleObj.SetActive(true);
    }

    /// <summary>
    /// Begin the game
    /// </summary>
    public void ResumeGame()
    {
        CurrentState = GameState.Playing;
    }

    /// <summary>
    /// Ends the game
    /// </summary>
    public void EndGame()
    {
        CurrentState = GameState.Ended;
        deathScreen.SetActive(true);
        radiusToggleObj.SetActive(true);
        deathTimeDisplay.text = TimeSpan.FromSeconds(robotManager.TotalTime).ToString("mm':'ss'.'ff");
        killCountDisplay.text = killCount.ToString();
    }

    /// <summary>
    /// Sets the killCount property
    /// </summary>
    public void IncrementKillCount()
    {
        killCount++;
    }

    // Update is called once per frame
    void Update()
    {
        switch (CurrentState)
        {
            case GameState.Starting:
                break;

            case GameState.Playing:
                robotManager.Update();

                timerGUI.text = $"<mspace=0.6em>{TimeSpan.FromSeconds(robotManager.TotalTime).ToString("mm':'ss'.'ff")}</mspace>";

                if (Input.GetKey(KeyCode.P))
                    PauseGame();

#if UNITY_EDITOR
                //Press 'R' to add zombies (debug only)
                if (Input.GetKeyDown(KeyCode.R))
                    robotManager.Spawn();

                if (Input.GetKey(KeyCode.T))
                    SpawnZBucks(2, Vector3.zero, 1);
#endif
                break;

            case GameState.Paused:
                break;

            case GameState.Ended:
                break;

            default:
                Debug.LogError("Unknown game state reached. What did you do??");
                break;
        }
    }

    public void ToggleRadiusOption(Toggle e)
    {
        RadiusOption = e.isOn;
    }

    /// <summary>
    /// Set the game state to a new state.
    /// </summary>
    /// <param name="newState"></param>
    public void SetGamestate(GameState newState)
    {
        CurrentState = newState;
    }

    /// <summary>
    /// Spawns a tower into the world
    /// </summary>
    /// <param name="position">Position to spawn at</param>
    /// <param name="rotation">Rotation to spawn at</param>
    public void SpawnTower(Vector3 position, Quaternion rotation)
    {
        var tower = Instantiate(towerPrefab, position, Quaternion.Euler(90,0,0)).GetComponent<Tower>();
        tower.InitRotation(rotation);
        towers.Add(tower);
    }

    /// <summary>
    /// Remove the tower from the list
    /// </summary>
    public void RemoveTower(Tower tower)
    {
        towers.Remove(tower);
    }

    /// <summary>
    /// Spawn an amount of zbucks at a position
    /// </summary>
    /// <param name="amount">The amount to spawn</param>
    /// <param name="position">The center position</param>
    /// <param name="valuePerBuck">The value of each spawned zbuck</param>
    public void SpawnZBucks(short amount, Vector3 position, ushort valuePerBuck)
    {
        for(ushort i = 0; i < amount; i++)
        {
            //Create more buckets if need be
            if (activeBucks >= zBuckets.Count * ZBUCKET_SIZE)
                CreateZBucket();

            //TODO: stop coins from going offscreen
            float angle = UnityEngine.Random.Range(0, 360);
            Quaternion rotation = Quaternion.Euler(90, 0, angle);
            Vector3 target = position + (rotation * new Vector3(1, CONSTANT_Y_POS, 0));
            
            //Initialize the zbuck
            int bucketIndex = Mathf.FloorToInt((float)currIndex / ZBUCKET_SIZE);
            int index = currIndex % ZBUCKET_SIZE;
            zBuckets[bucketIndex][index].Init(position, target, valuePerBuck, currIndex);
            currIndex++;;
            activeBucks++;
        }
    }

    /// <summary>
    /// Remove a ZBuck at a specific index
    /// </summary>
    /// <param name="index">The index of the zbuck array to remove</param>
    public void RemoveZBuck(int index)
    {
        activeBucks--;

        //Calculate indices
        int bucketIndex = Mathf.FloorToInt((float)index / ZBUCKET_SIZE);
        int arrayIndex = index % ZBUCKET_SIZE;
        int swapBucketIndex = Mathf.FloorToInt((float)(currIndex - 1) / ZBUCKET_SIZE);
        int swapArrayIndex = (currIndex - 1) % ZBUCKET_SIZE;

        //Swap
        ZBuck temp = zBuckets[swapBucketIndex][swapArrayIndex]; // Error here when there are too many Z-Bucks
        zBuckets[swapBucketIndex][swapArrayIndex] = zBuckets[bucketIndex][arrayIndex];
        zBuckets[bucketIndex][arrayIndex] = temp;
        zBuckets[bucketIndex][arrayIndex].Index = index;
        currIndex--;
    }

    /// <summary>
    /// Sets the build mode setting (display the radius of all turrets)
    /// </summary>
    /// <param name="buildOn">Whether build mode is on or not</param>
    public void SetBuildMode(bool buildOn)
    {
        foreach(Tower t in towers)
        {
            t.SetBuildMode(buildOn);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Gizmos
    /// </summary>
    [ExecuteInEditMode]
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(0,0,0), new Vector3(boundsX*2, 1, boundsY*2));

        Gizmos.color = Color.white;
        foreach (var rsz in robotSpawnZones)
            Gizmos.DrawWireCube(new Vector3(rsz.position.x, CONSTANT_Y_POS, rsz.position.y), new Vector3(rsz.size.x * 2, 0, rsz.size.y * 2));
    }
#endif
}
