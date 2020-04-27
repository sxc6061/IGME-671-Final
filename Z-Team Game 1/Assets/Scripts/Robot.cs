using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Robot : Targetable
{
    /// <summary>
    /// Object pooling index. DO NOT MODIFY
    /// </summary>
    public ushort Index { get; set; }
    private enum RobotState { Moving, Attacking, Dying }
    private enum RobotAttackState { Charging, Performing, Recovery}

    /// <summary>
    /// The current target this zombie is chasing
    /// </summary>
    public Targetable Target { get; set; }

    // Audio members, properties and constants
        [FMODUnity.EventRef]
        public string enemyDamage;
        public float edSpeed;
        [FMODUnity.EventRef]
        public string enemyDeathSound;
        public float eDeathSpeed;
    
    //Consts
    private const float SEARCH_TIMER_MAX = 0.5f;
    private const float ATTACK_TIMER_MAX = 2f;
    private const float CHARGE_TIMER_MAX = 0.5f;
    private const float PERFORM_TIMER_MAX = 0.5f;
    private const float RECOVERY_TIMER_MAX = 0.5f;
    private const float ATTACK_RANGE = 100;
    private const float CHARGE_ROTATION_SPEED = 100f;
    private const float PERFORM_MOVE_SPEED = 10f;
    private const short SEARCH_RADIUS = 15;
    private const short MAX_HEALTH = 3;
    private const ushort MAX_TOWERS_TO_SEARCH = 5;

    Collider[] overlapSphereCols;
    private NavMeshAgent agent;
    private GameObject hitboxObj;
    private Vector3 attackDirection;
    private RobotState currentState;
    private RobotAttackState currentAttackState;
    private float searchTimer;
    private float attackTimer;
    private short health;

    //Initialize vars
    private void Awake()
    {
        overlapSphereCols = new Collider[MAX_TOWERS_TO_SEARCH];
        agent = GetComponent<NavMeshAgent>();
        hitboxObj = transform.Find("Hitbox").gameObject;
        IsMoveable = true;
        gameObject.SetActive(false);
    }

    /// <summary>
	/// Init the robot to its starting state
	/// </summary>
    public void Init(ushort index, Vector3 position, float speed)
    {
        Index = index;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        health = MAX_HEALTH;
        searchTimer = 0;
        attackTimer = 0;
        
        agent.speed = speed;
        currentState = RobotState.Moving;
        currentAttackState = RobotAttackState.Charging;
        Target = GameManager.Instance.player;
        gameObject.SetActive(true);
        hitboxObj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
        {
            if (!agent.isStopped) agent.isStopped = true;
            return;
        }

        if (agent.isStopped) agent.isStopped = false;

        switch (currentState)
        {
            case RobotState.Moving:
                searchTimer += Time.deltaTime;
                attackTimer += Time.deltaTime;

                //Search for a nearby tower
                if (searchTimer > SEARCH_TIMER_MAX)
                {
                    searchTimer -= SEARCH_TIMER_MAX;
                    Targetable newTarget = FindTarget();
                    if (newTarget != null)
                    {
                        Target = newTarget;
                        agent.destination = Target.transform.position;
                    }
                }

                //Find a new target because this one died
                if (Target == null)
                {
                    //If still  null, assign to player
                    if ((Target = FindTarget()) == null)
                        Target = GameManager.Instance.player;
                    agent.destination = Target.transform.position;
                }
                //The target is moveable, so continuously update the position
                else if (Target.IsMoveable)
                    agent.destination = Target.transform.position;


                //Activate attack if we can
                if (attackTimer > ATTACK_TIMER_MAX &&
                    Vector3.SqrMagnitude(transform.position - Target.transform.position) <= ATTACK_RANGE)
                {
                    attackTimer = 0;
                    currentState = RobotState.Attacking;
                    currentAttackState = RobotAttackState.Charging;
                    agent.isStopped = true;
                    
                }
                break;

            case RobotState.Attacking:
                switch (currentAttackState)
                {
                    //Charge up the attack
                    case RobotAttackState.Charging:
                        attackTimer += Time.deltaTime;

                        //Find a new target because this one died
                        if (Target == null)
                        {
                            //If still  null, assign to player
                            if ((Target = FindTarget()) == null)
                                Target = GameManager.Instance.player;
                            agent.destination = Target.transform.position;
                        }

                        //Rotate towards target
                        Vector3 direction = (Target.transform.position - transform.position).normalized;
                        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * CHARGE_ROTATION_SPEED);

                        if (attackTimer > CHARGE_TIMER_MAX)
                        {
                            attackDirection = transform.forward;
                            attackTimer = 0;
                            hitboxObj.SetActive(true);
                            currentAttackState = RobotAttackState.Performing;
                        }
                        break;

                    //Perform the actual attack
                    case RobotAttackState.Performing:
                        attackTimer += Time.deltaTime;

                        //Move in attack direction
                        agent.velocity = attackDirection * PERFORM_MOVE_SPEED;

                        if (attackTimer > PERFORM_TIMER_MAX)
                        {
                            attackTimer = 0;
                            hitboxObj.SetActive(false);
                            currentAttackState = RobotAttackState.Recovery;
                        }
                        break;

                    //Recover from the attack
                    case RobotAttackState.Recovery:
                        attackTimer += Time.deltaTime;
                        if(attackTimer > RECOVERY_TIMER_MAX)
                        {
                            attackTimer = 0;
                            currentState = RobotState.Moving;
                            agent.isStopped = false;
                        }
                        break;

                    default:
                        break;
                }
                break;

            //TODO: death animations?
            case RobotState.Dying:
                break;

            default:
                break;
        }


    }

    /// <summary>
    /// Use Physics.OverlapSphere to find any towers in range
    /// </summary>
    /// <returns>The closest tower in range</returns>
    private Targetable FindTarget()
    {
        //Perform overlap sphere
        int result = Physics.OverlapSphereNonAlloc(transform.position, SEARCH_RADIUS, overlapSphereCols, LayerMask.GetMask("Tower"), QueryTriggerInteraction.Ignore);

        //Find closest robot
        Collider closest = null;
        float shortestDist = float.MaxValue;
        float sqrDist = 0;

        for(int i = 0; i < result; i++)
        {
            sqrDist = Vector3.SqrMagnitude(transform.position - overlapSphereCols[i].transform.position);
            if (sqrDist < shortestDist)
            {
                shortestDist = sqrDist;
                closest = overlapSphereCols[i];
            }
        }
        return closest?.GetComponent<Targetable>();
    }

    /// <summary>
    /// Have the robot take damage, possibly killing it
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply</param>
    public void TakeDamage(short damageAmount)
    {
        health -= damageAmount;
        if (health < 1)
        {
            currentState = RobotState.Dying; //TODO: death animations?
            if (!GameManager.Instance.muteSFX)
            {
                
            }
            GameManager.Instance.IncrementKillCount();
            RobotManager.DecrementRobotCount(Index);
            gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, SEARCH_RADIUS);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Sqrt(ATTACK_RANGE));
    }
#endif
}
