using System;
using UnityEngine;

[Serializable]
public class RobotSpawnZone
{
    public Vector2 position;
    public Vector2 size;

    public Vector3 GetRandomPointInZone()
    {
        Vector3 point = new Vector3(UnityEngine.Random.Range(-size.x, size.x), GameManager.CONSTANT_Y_POS, UnityEngine.Random.Range(-size.y, size.y));
        point.x += position.x;
        point.z += position.y;
        return point;
    }
}
