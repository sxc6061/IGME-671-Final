using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZBuck : MonoBehaviour
{
    private enum ZBuckState { Entering, Standing, Exiting, Dead }
    private const float ENTER_TIME = 0.5f;
    private const float EXIT_TIME = 0.5f;
    private const float MAX_TIME = 10.0f;

    Player player;
    private ZBuckState state;
    private Vector3 enterTarget;
    private float timer;
    private float decayTimer;
    private ushort value;

    /// <summary>
    /// Object pooling index. DO NOT MODIFY
    /// </summary>
    public int Index { get; set; }

    private void Start()
    {
        player = GameManager.Instance.player;
        gameObject.SetActive(false);
        decayTimer = 0;
    }

    /// <summary>
    /// Initialize the zbuck
    /// </summary>
    /// <param name="enterTarget"></param>
    public void Init(Vector3 enterPosition, Vector3 enterTarget, ushort value, int index)
    {
        transform.position = enterPosition;
        Index = index;
        this.enterTarget = enterTarget;
        timer = 0;
        decayTimer = 0;
        state = ZBuckState.Entering;
        this.value = value;
        Color col = gameObject.GetComponent<Renderer>().material.color;
        col.a = 1.0f;
        gameObject.GetComponent<Renderer>().material.color = col;
        gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            //Lerp in direction
            case ZBuckState.Entering:
                timer += Time.deltaTime / ENTER_TIME;
                transform.position = Vector3.Lerp(transform.position, enterTarget, timer);
                if(timer > 1)
                {
                    state = ZBuckState.Standing;
                }
                break;
            
            //Stand until player is near
            case ZBuckState.Standing:
                if(Vector3.SqrMagnitude(player.transform.position - transform.position) < Player.ZBUCK_COLLECTION_RADIUS)
                {
                    timer = 0;
                    state = ZBuckState.Exiting;
                }
                break;
            
            //Lerp to player
            case ZBuckState.Exiting:
                timer += Time.deltaTime / EXIT_TIME;
                transform.position = Vector3.Lerp(transform.position, player.transform.position, timer);
                if (timer > 1)
                {
                    player.AddZBucks(value);
                    GameManager.Instance.RemoveZBuck(Index);
                    state = ZBuckState.Dead;
                    gameObject.SetActive(false);
                }
                break;

            case ZBuckState.Dead:
                break;

            default:
                break;
        }

        if (decayTimer >= MAX_TIME)
        {
            GameManager.Instance.RemoveZBuck(Index);
            state = ZBuckState.Dead;
            gameObject.SetActive(false);

        }


        Color col = gameObject.GetComponent<Renderer>().material.color;
        col.a = 0;
        
        gameObject.GetComponent<Renderer>().material.color = Color.Lerp(gameObject.GetComponent<Renderer>().material.color, col, Time.deltaTime/MAX_TIME);
        decayTimer += Time.deltaTime;
    }
}
