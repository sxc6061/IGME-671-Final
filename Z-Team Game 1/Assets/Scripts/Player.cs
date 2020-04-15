using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Player : Targetable
{
    private enum PlayerState
    {
        Alive,
        Dying,
        Dead
    }

    public const short ZBUCK_COLLECTION_RADIUS = 50;

    // Audio members, properties and constants
    [FMODUnity.EventRef]
    public string playerMove;
    public float moveSpeed;
    private bool playerMoving;

    [FMODUnity.EventRef]
    public string playerDamage;
    public float damageSpeed;

    private AudioSource audioSource;
    public AudioSource moveSoundSource;
    private const float MOVE_VOLUME = 0.1f;
    public AudioClip hitSound;
    private const float HIT_PITCH = 0.35f;
    private const float HIT_VOLUME = 0.3f;
    public AudioClip deathSound;
    private const float DEATH_PITCH = 0.8f;
    private const float DEATH_VOLUME = 0.3f;
    public AudioClip placeTurretSound;
    private const float PLACE_TURRET_PITCH = 1.0f;
    private const float PLACE_TURRET_VOLUME = 0.3f;
    

    private const float MOVE_SPEED = 30.0f;
    private const float ROTATION_SPEED = 10.0f;
    private const int MAX_HEALTH = 10;
    private static readonly ushort[] TOWER_UPGRADE_PRICES = { 5, 15, 30 };
    private const ushort BASE_TOWER_PRICE = 5;
    

    // Turret Cost UI Overlays
    public GameObject turretCostMessagesObj;
    public TextMeshProUGUI turretCostDisplay;

    public float TowerSize { get; set; }

    [SerializeField] PlayerHealthBar healthBar;
    [SerializeField] TextMeshProUGUI zBucksCounter;

    private PlayerState currentState = PlayerState.Dead;
    private Tower lastHighlightedTower;
    private SpriteRenderer towerGhost;
    private Material towerRadiusMatInst;
    private Collider[] towerResults;
    private RaycastHit towerHit;

    private Vector3 moveDirection = Vector3.zero;

    private float leftBound;
    private float topBound;
    private float rightBound;
    private float bottomBound;

    private uint zBucks;
    private int health = 0;
    private ushort currTowerPrice;
    private bool isBuilding;
    private bool canPlace;

    private void Awake()
    {
        IsMoveable = true;
        isBuilding = false;
        towerGhost = transform.Find("TowerGhost").GetComponent<SpriteRenderer>();
        towerGhost.transform.GetChild(0).localScale = new Vector3(Tower.SEARCH_RADIUS_SQRT, Tower.SEARCH_RADIUS_SQRT, Tower.SEARCH_RADIUS_SQRT);
        towerRadiusMatInst = towerGhost.GetComponentInChildren<MeshRenderer>().material;
        towerResults = new Collider[10];

        audioSource = GetComponent<AudioSource>();

        float spriteHalfWidth = transform.Find("Sprite").GetComponent<SpriteRenderer>().size.x / 2;

        leftBound = -GameManager.Instance.BoundsX + spriteHalfWidth;
        rightBound = GameManager.Instance.BoundsX - spriteHalfWidth;
        bottomBound = -GameManager.Instance.BoundsY + spriteHalfWidth;
        topBound = GameManager.Instance.BoundsY - spriteHalfWidth;
    }

    /// <summary>
    /// Initialize the player at the start of each round.
    /// </summary>
    public void Init()
    {
        InvokeRepeating("CallPlayerMove", 0, moveSpeed);
        gameObject.SetActive(true);

        moveSoundSource.volume = 0.05f;

        currentState = PlayerState.Alive;

        SetHealth(MAX_HEALTH);

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + 180.0f, 0);

        towerGhost.gameObject.SetActive(false);

        canPlace = true;
        currTowerPrice = BASE_TOWER_PRICE;
        turretCostDisplay.text = currTowerPrice.ToString();
        zBucks = currTowerPrice;
        UpdateZBucksDisplay();
        lastHighlightedTower = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        switch (currentState)
        {
            case PlayerState.Alive:
                //Moving
                moveDirection.x = Input.GetAxisRaw("Horizontal");
                moveDirection.y = 0.0f;
                moveDirection.z = Input.GetAxisRaw("Vertical");
                moveDirection.Normalize();

                //Update tower ghost
                if(isBuilding)
                {
                    //Update our tower's ghost on whether it can be placed or not
                    UpdateTowerGhost();

                    //Check if we are hovering over a tower
                    Tower t = null;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out towerHit, Mathf.Infinity, LayerMask.GetMask("Tower")))
                    {
                        t = towerHit.collider.GetComponent<Tower>();
                        var level = t.Level;
                        if (level < Tower.MAX_LEVEL) //only upgrade towers that are lower than the max level
                        {
                            if (zBucks >= TOWER_UPGRADE_PRICES[level])
                            {
                                //Update that tower's visuals
                                t.SetBuildColor(TowerBuildColor.Green);

                                //When the player clicks to upgrade
                                if (Input.GetMouseButtonDown(0))
                                {
                                    RemoveZBucks(TOWER_UPGRADE_PRICES[level]);
                                    t.Upgrade();
                                    SetBuildMode(false);
                                }
                            }
                            else
                            {
                                //Update that tower's visuals
                                t.SetBuildColor(TowerBuildColor.Red);
                            }
                        }
                    }

                    //Reset tower if need-be
                    if (t != lastHighlightedTower)
                        lastHighlightedTower?.SetBuildColor(TowerBuildColor.Default);
                    lastHighlightedTower = t;
                }

                //Placing towers
                if (Input.GetKeyDown(KeyCode.Space))
		        {
		            //Show shadow of tower before placing
		            if(!isBuilding)
		                SetBuildMode(true);
		            //Place the tower
		            else if (canPlace)
		            {
                        RemoveZBucks(currTowerPrice);
                        if (!GameManager.Instance.muteSFX)
                        {
                            audioSource.pitch = PLACE_TURRET_PITCH;
                            audioSource.PlayOneShot(placeTurretSound, PLACE_TURRET_VOLUME * GameManager.Instance.sfxVolume);
                        }
		                GameManager.Instance.SpawnTower(towerGhost.transform.position, towerGhost.transform.rotation);
                        currTowerPrice += 2;
                        turretCostDisplay.text = currTowerPrice.ToString();
                        SetBuildMode(false);
		            }
		        }
		        //Cancel placement
		        else if(isBuilding && Input.GetKeyDown(KeyCode.F))
		            SetBuildMode(false);
                break;

            case PlayerState.Dying:
                gameObject.SetActive(false);
                currentState = PlayerState.Dead;
                GameManager.Instance.EndGame();
                break;

            case PlayerState.Dead:
                break;

            default:
                Debug.LogError("Player state unknown");
                break;
        }
    }

    /// <summary>
    /// Move the character
    /// </summary>
    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        if (currentState == PlayerState.Alive)
        {
            Move();
        }
    }

    /// <summary>
    /// Move the player, check for collisions and update sprite
    /// </summary>
    private void Move()
    {
        if (moveDirection != Vector3.zero)
        {
            playerMoving = true;
            if (!GameManager.Instance.muteSFX)
            {
                if (!moveSoundSource.isPlaying)
                {
                    moveSoundSource.Play();

                    LeanTween.value(gameObject, (float vol) =>
                    {
                        moveSoundSource.volume = vol * GameManager.Instance.sfxVolume;
                    }, 
                    moveSoundSource.volume, 
                    MOVE_VOLUME, 
                    0.5f);
                }
            }
            else if (moveSoundSource.isPlaying)
            {
                moveSoundSource.Pause();
            }

            transform.position = transform.position + moveDirection * MOVE_SPEED * Time.fixedDeltaTime;

            LerpSpriteRotation();
            // RotateSprite();

            CheckBounds();
        }
        else
        {
            playerMoving = false;
        }
    }

    public void CallPlayerMove()
    {
        if (playerMoving == true)
        {
            FMODUnity.RuntimeManager.PlayOneShot(playerMove);
        }
    }

    public void OnDisable()
    {
        playerMoving = false;
    }

    /// <summary>
    /// Have the player take damange
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply</param>
    public void TakeDamage(ushort damageAmount)
    {
        int newHealth = health - damageAmount;

        if (!GameManager.Instance.muteSFX)
        {
            FMODUnity.RuntimeManager.PlayOneShot(playerDamage);
        }
        
        SetHealth(newHealth);
    }

    /// <summary>
    /// Update the players health value and update the health display
    /// </summary>
    /// <param name="value">The new health value</param>
    public void SetHealth(int value)
    {
        health = value;

        healthBar.UpdateDisplay(health, MAX_HEALTH);

        if (health < 1)
        {
            health = 0;
            if (!GameManager.Instance.muteSFX)
            {
                audioSource.pitch = DEATH_PITCH;
                audioSource.PlayOneShot(deathSound, DEATH_VOLUME * GameManager.Instance.sfxVolume);
            }
            currentState = PlayerState.Dying;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "RobotHitbox")
        {
            TakeDamage(GameManager.ROBOT_ATTACK_DAMAGE);
        }
    }

    /// <summary>
    /// Immediately update the sprite's rotation based on the move direction
    /// </summary>
    /// <returns></returns>
    private void RotateSprite()
    {
        Quaternion moveAngle = Quaternion.Euler(0, Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + 180.0f, 0);
        transform.rotation = moveAngle;
    }

    /// <summary>
    /// Update the sprite's rotation based on the looking direction and move direction
    /// </summary>
    /// <returns></returns>
    private void LerpSpriteRotation()
    {
        Quaternion moveAngle = Quaternion.Euler(0, Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + 180.0f, 0);

        float angleOffset = moveAngle.eulerAngles.y - transform.rotation.eulerAngles.y;

        if (angleOffset == 0)
        {
            return;
        }

        if (angleOffset > 180.0f) angleOffset -= 360;
        else if (angleOffset < -180.0f) angleOffset += 360;

        if (angleOffset > ROTATION_SPEED) angleOffset = ROTATION_SPEED;
        else if (angleOffset < -ROTATION_SPEED) angleOffset = -ROTATION_SPEED;

        transform.Rotate(Vector3.up, angleOffset);
    }

    /// Turn on or off build mode for the player
    /// </summary>
    /// <param name="buildOn">What mode to set</param>
    private void SetBuildMode(bool buildOn)
    {
        isBuilding = buildOn;
        towerGhost.gameObject.SetActive(buildOn);
        GameManager.Instance.SetBuildMode(buildOn);

        turretCostMessagesObj.SetActive(buildOn);

        if (isBuilding)
            UpdateTowerGhost();
    }

    /// <summary>
    /// Update the tower ghost to represent whether the player can build a tower
    /// </summary>
    private void UpdateTowerGhost()
    {
        //Find a tower
        var numTowers = Physics.OverlapSphereNonAlloc(towerGhost.transform.position, 
            TowerSize, towerResults, 
            LayerMask.GetMask("Tower"));

        //Update ghost
        if (numTowers > 0 || zBucks < currTowerPrice)
        {
            canPlace = false;
            towerGhost.color = GameManager.RED_TRANSPARENT;
            towerRadiusMatInst.SetColor("_Color", GameManager.RED_TRANSPARENT);
        }
        else
        {
            canPlace = true;
            towerGhost.color = GameManager.GREEN_TRANSPARENT;
            towerRadiusMatInst.SetColor("_Color", GameManager.GREEN_TRANSPARENT);
        }
    }

    /// <summary>
    /// Check the player's relationship to the bounds of the background and lock the player within it
    /// </summary>
    private void CheckBounds()
    {
        Vector3 newTransform = transform.position;

        if (transform.position.x < leftBound)
        {
            newTransform.x = leftBound;
        }
        else if (transform.position.x > rightBound)
        {
            newTransform.x = rightBound;
        }
        if (transform.position.z < bottomBound)
        {
            newTransform.z = bottomBound;
        }
        else if (transform.position.z > topBound)
        {
            newTransform.z = topBound;
        }

        transform.position = newTransform;
    }

    /// <summary>
    /// Remove an amount of zbucks from the player
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void RemoveZBucks(ushort amount)
    {
        zBucks -= amount;

        UpdateZBucksDisplay();
    }

    /// <summary>
    /// Add an amount of zbucks to the player
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddZBucks(ushort amount)
    {
        zBucks += amount;

        UpdateZBucksDisplay();

        if (isBuilding)
            UpdateTowerGhost();
    }

    /// <summary>
    /// Update the zBucks display
    /// </summary>
    public void UpdateZBucksDisplay()
    {
        zBucksCounter.text = zBucks.ToString();
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Sqrt(ZBUCK_COLLECTION_RADIUS));
    }
#endif
}
