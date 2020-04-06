using UnityEngine;

class RobotManager
{
	//Events and delegates
	private delegate void DecrementRobotDelegate(ushort index);
	private static event DecrementRobotDelegate decrementRobotEvent;

	public float TotalTime { get; private set; }

	//Spawning
	private Robot[] robots;
	private RobotSpawnZone[] spawnZones;
	private AnimationCurve spawnCurve;
	private ushort currIndex;

	//Robot spawning
	public const ushort MAX_ROBOTS = 500;
	private uint currAmount;
	private uint toSpawn;
	private uint maxToSpawnPerFrame;
	private int spawnAmount;

	//Timers
	private const float ADD_PER_FRAME_MAX = 30; //every N seconds allow more robots to spawn per frame
	private float addPerFrameTimer;
	private const float ADD_TIMER_MAX = 7; //add robots to spawn queue every N seconds
	private float addTimer;
	private const float SPAWN_TIMER_MAX = 0.5f; //spawns robots (if there are any to spawn) every N seconds
	private float spawnTimer;


	/// <summary>
	/// Create a manager to control robot spawning
	/// </summary>
	/// <param name="robotPrefab">The basic robot prefab to spawn</param>
	public RobotManager(GameObject robotPrefab, RobotSpawnZone[] spawnZones, AnimationCurve spawnCurve)
	{
		//Setup event
		decrementRobotEvent += (index) =>
		{
			short zbuckAmnt;
			if (spawnAmount > 40)
				zbuckAmnt = (short)Random.Range(-2, 3);
			else if (spawnAmount > 20)
				zbuckAmnt = (short)Random.Range(-1, 3);
			else if (spawnAmount > 5)
				zbuckAmnt = (short)Random.Range(0, 3);
			else
				zbuckAmnt = (short)Random.Range(1, 3);
			GameManager.Instance.SpawnZBucks(zbuckAmnt, robots[index].transform.position, 1); ;

			currAmount--;
			
			//Swap the last spawned robot with this newly dead one
			int swapInd = (currIndex - 1) % MAX_ROBOTS;
			Robot temp = robots[swapInd];
			robots[swapInd] = robots[index];
			robots[index] = temp;
			robots[index].Index = index;
			currIndex--;
		};

		//Assign members
		this.spawnZones = spawnZones;
		this.spawnCurve = spawnCurve;

		//Instantiate all robots
		robots = new Robot[MAX_ROBOTS];
		for(int i = 0; i < MAX_ROBOTS; i++)
		{
			robots[i] = GameObject.Instantiate(robotPrefab).GetComponent<Robot>();
		}
	}

	/// <summary>
	/// Start the manager
	/// </summary>
	public void Start()
	{
		currIndex = 0;
		foreach(var r in robots)
		{
			r.gameObject.SetActive(false);
		}

		currAmount = 0;
		toSpawn = 0;
		maxToSpawnPerFrame = 1;

		TotalTime = 0;
		addTimer = ADD_TIMER_MAX / 2;
		addPerFrameTimer = 0;
		spawnTimer = 0;
	}

	/// <summary>
	/// Update the manager (controls robot spawning)
	/// </summary>
	public void Update()
	{
		TotalTime += Time.deltaTime;
		addTimer += Time.deltaTime;
		spawnTimer += Time.deltaTime;
		addPerFrameTimer += Time.deltaTime;

		//Every N seconds allow more robots to spawn per frame, up to 10 robots
		if (maxToSpawnPerFrame < 10 && addPerFrameTimer > ADD_PER_FRAME_MAX)
		{
			addPerFrameTimer -= ADD_PER_FRAME_MAX;
			maxToSpawnPerFrame++;
		}

		//Add robots to spawn queue
		if (addTimer > ADD_TIMER_MAX)
		{
			//Get amount per frame
			//At 3 minutes, the spawner caps out and just spawns the same amount from there on
			spawnAmount = Mathf.FloorToInt(spawnCurve.Evaluate(TotalTime));
			addTimer -= ADD_TIMER_MAX;
			toSpawn += (uint)spawnAmount;
		}

		//Spawn robots
		if(spawnTimer > SPAWN_TIMER_MAX)
		{
			spawnTimer -= SPAWN_TIMER_MAX;
			ushort currentSpawned = 0;
			while (toSpawn > 0 && currentSpawned < maxToSpawnPerFrame && currAmount < MAX_ROBOTS)
			{
				currentSpawned++;
				toSpawn--;
				Spawn();
			}
		}
	}

	/// <summary>
	/// Spawn a zombie at a random position
	/// </summary>
#if UNITY_EDITOR
	public void Spawn()
#else
	private void Spawn()
#endif
	{
		float speed = 3.6f + spawnAmount / 40.0f;

		robots[currIndex].Init(currIndex, spawnZones[Random.Range(0, spawnZones.Length)].GetRandomPointInZone(), speed);
		currIndex = (ushort)((currIndex + 1) % MAX_ROBOTS);
		currAmount++;
	}

	/// <summary>
	/// Decrement the robot count by 1
	/// </summary>
	public static void DecrementRobotCount(ushort index)
	{
		decrementRobotEvent?.Invoke(index);
	}
}
