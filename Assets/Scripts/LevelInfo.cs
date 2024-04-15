using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelInfo : MonoBehaviour
{
    public Vector2Int pos;
    public string levelName;
    public bool isComplete;

    private void Awake()
    {
        pos = new Vector2Int((int)transform.position.x, (int)transform.position.y);
    }
}
