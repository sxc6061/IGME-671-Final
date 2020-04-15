using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TowerBuildColor { Green, Red, Default};

public class Tower : Targetable
{
    public enum TowerState
    {
        Alive,
        Dying
    }

    // Audio members, properties and constants
    [FMODUnity.EventRef]
    public string turretShoot;
    public float shootSpeed;
    [FMODUnity.EventRef]
    public string turretDamage;
    public float damageSpeed;
    [FMODUnity.EventRef]
    public string turretDeath;
    public float deathSpeed;

    //Constants
    public static readonly float SEARCH_RADIUS_SQRT = Mathf.Sqrt(SEARCH_RADIUS);
    public const short MAX_LEVEL = 3;
    private static readonly Color BLUE = new Color(54f / 255f, 89f / 255f, 253f / 255f, 1f);
    private const int SEARCH_RADIUS = 25;
    private const int MAX_HEALTH = 5;

    private Targetable target;
    public short Level { get; private set; }

    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar expBar;

    Collider[] overlapSphereCols;
    private SpriteRenderer spriteObj;
    private GameObject shootSprite;
    private GameObject radiusDisplay;
    private Material towerRadiusMatInst;
    private TowerState currentState;
    private float timeSinceLastShot;
    
    //Tower traits
    private int health;
    private float shootLimit = 1.5f;
 	private short damageAmnt = 1;


    //Initialize vars
    private void Awake()
    {
        //Initialize values
        IsMoveable = false;
        timeSinceLastShot = 0.0f;
        currentState = TowerState.Alive;
        overlapSphereCols = new Collider[30];
        target = null;
        Level = 0;

        //Find children
        spriteObj = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        shootSprite = transform.Find("Sprite/bigBullet").gameObject;
        radiusDisplay = transform.Find("RadiusDisplay").gameObject;
        towerRadiusMatInst = radiusDisplay.GetComponent<MeshRenderer>().material;

        healthBar.Init();
        SetHealth(MAX_HEALTH);
        expBar.Init();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Set radius display to the range
        radiusDisplay.transform.localScale = new Vector3(SEARCH_RADIUS_SQRT, SEARCH_RADIUS_SQRT, SEARCH_RADIUS_SQRT);

        shootSprite.SetActive(false);
        radiusDisplay.SetActive(false);
    }

    /// <summary>
    /// Initialize the sprite rotation
    /// </summary>
    /// <param name="rotation">The initial rotation</param>
    public void InitRotation(Quaternion rotation)
    {
        spriteObj.transform.Rotate(new Vector3(0.0f, 0.0f, -rotation.eulerAngles.y));
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return; 

        switch (currentState)
        {
            case TowerState.Alive:
                timeSinceLastShot += Time.deltaTime;

                //Only aim and shoot at valid targets
                if (IsTargetValid(target))
                {
                    //If a target goes out of range, stop shooting at it
                    if (Vector3.SqrMagnitude(transform.position - target.transform.position) > SEARCH_RADIUS * SEARCH_RADIUS)
                        SetTarget(null);
                    //Shoot target
                    else
                    {
                        if (timeSinceLastShot > shootLimit)
                        {
                            Aim();
                            Shoot();
                            timeSinceLastShot = 0.0f;
                        }
                    }
                }
                //Search for a nearby robot if ours isn't valid anymore
                else
                {
                    Targetable newTarget = FindTarget();
                    if (IsTargetValid(newTarget))
                        //Found a target
                        SetTarget(newTarget);
                    else
                        SetTarget(null);
                }

                if (shootSprite.activeSelf)
                {
                    if (timeSinceLastShot > .25f)
                    {
                        shootSprite.SetActive(false);
                    }
                }
                break;

            case TowerState.Dying:
                GameManager.Instance.RemoveTower(this);
                Destroy(gameObject);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Upgrade this tower up a level
    /// </summary>
    public void Upgrade()
    {
        Level++;
        expBar.UpdateDisplay(Level, MAX_LEVEL, BLUE);

        //Upgrade the tower's stats
        switch (Level)
        {
            case 1:
                shootLimit = 1.1f;
                break;

            case 2:
                shootLimit = 0.65f;
                break;

            case 3:
                damageAmnt = 2;
                break;

            default:
                break;
        }

        if (Level == MAX_LEVEL)
            spriteObj.sprite = GameManager.Instance.UpgradedTowerSprite;
    }

    /// <summary>
    /// Set the color of this tower
    /// </summary>
    /// <param name="tColor"></param>
    public void SetBuildColor(TowerBuildColor tColor)
    {
        switch (tColor)
        {
            case TowerBuildColor.Green:
                spriteObj.color = GameManager.GREEN;
                towerRadiusMatInst.SetColor("_Color", GameManager.GREEN_TRANSPARENT);
                break;
            case TowerBuildColor.Red:
                spriteObj.color = GameManager.RED;
                towerRadiusMatInst.SetColor("_Color", GameManager.RED_TRANSPARENT);
                break;
            case TowerBuildColor.Default:
                spriteObj.color = GameManager.WHITE;
                towerRadiusMatInst.SetColor("_Color", GameManager.GREY_TRANSPARENT);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Sets the build mode setting (display the radius of the turret)
    /// </summary>
    /// <param name="buildOn">Whether build mode is on or not</param>
    public void SetBuildMode(bool buildOn)
    {
        if (GameManager.Instance.RadiusOption)
            radiusDisplay.SetActive(buildOn);
        else radiusDisplay.SetActive(false);

        SetBuildColor(TowerBuildColor.Default);
    }

    /// <summary>
    /// Have this tower take damage
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply</param>
    private void TakeDamage(ushort damageAmount)
    {
        SetHealth(health - damageAmount);
        if (!GameManager.Instance.muteSFX)
        {
            if (health > 0)
            {
                FMODUnity.RuntimeManager.PlayOneShot(turretDamage);
            }
        }
    }

    /// <summary>
    /// Update the tower's health value and update its health display
    /// </summary>
    /// <param name="value">The new health value</param>
    private void SetHealth(int value)
    {
        health = value;

        healthBar.UpdateDisplay(health, MAX_HEALTH);

        if (health < 1)
        {
            health = 0;
            if (!GameManager.Instance.muteSFX)
            {
                FMODUnity.RuntimeManager.PlayOneShot(turretDeath);
            }
            currentState = TowerState.Dying;
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
    /// Check if a target is valid to be targeted
    /// </summary>
    /// <param name="target">The target to check</param>
    /// <returns>Whether it is valid or not</returns>
    private bool IsTargetValid(Targetable target)
    {
        return target != null && target.gameObject.activeSelf;
    }

    private Targetable FindTarget()
    {
        //Perform overlap sphere
        int result = Physics.OverlapSphereNonAlloc(transform.position, SEARCH_RADIUS, overlapSphereCols, LayerMask.GetMask("Robot"), QueryTriggerInteraction.Ignore);

        //Find closest robot
        Collider closest = null;
        float shortestDist = float.MaxValue;
        float sqrDist = 0;
        for (int i = 0; i < result; i++)
        {
            if (overlapSphereCols[i].gameObject.activeSelf)
            {
                sqrDist = Vector3.SqrMagnitude(transform.position - overlapSphereCols[i].transform.position);
                if (sqrDist < shortestDist)
                {
                    shortestDist = sqrDist;
                    closest = overlapSphereCols[i];
                }
            }
        }
        return closest?.GetComponent<Targetable>();
    }

    /// <summary>
    /// Set the target of this tower
    /// </summary>
    private void SetTarget(Targetable target)
    {
        this.target = target;
    }

    private void Aim()
    {
        spriteObj.transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));
        spriteObj.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
    }

    private void Shoot()
    {
        //Make sure that the target has not been destroyed by another tower
        if (IsTargetValid(target))
        {
            //Cast the object into a Robot
            Robot currentRobot = (Robot)target;

            //Stretch sprite
            var scale = shootSprite.transform.localScale;
            scale.y = Mathf.Max(1, Vector3.Distance(transform.position, currentRobot.transform.position) - 4);
            shootSprite.transform.localScale = scale;

            if (!GameManager.Instance.muteSFX)
            {
                FMODUnity.RuntimeManager.PlayOneShot(turretShoot);
            }

            //Give Damage
            currentRobot.TakeDamage(damageAmnt);
            if (IsTargetValid(target))
                SetTarget(null);
        }
        timeSinceLastShot = 0.0f;

        //Display Shot
        shootSprite.SetActive(true);

    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, SEARCH_RADIUS);
    }
#endif
}
