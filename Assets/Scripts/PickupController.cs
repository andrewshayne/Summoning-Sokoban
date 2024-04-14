using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class PickupController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;
    private float randOscillationStart;

    private float speed = 2.5f;

    // Start is called before the first frame update
    void Start()
    {
        randOscillationStart = Random.Range(0, 2f * Mathf.PI);
    }

    // Update is called once per frame
    void Update()
    {
        float offset = Mathf.Sin(randOscillationStart + Time.timeSinceLevelLoad * speed) * 0.1f;
        Vector3 targetPos = new Vector3(gridPosition.x, gridPosition.y + offset, 0);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime);
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public int GetID()
    {
        return id;
    }

    public void SetID(int id)
    {
        this.id = id;
    }

    public Vector2 GetTruePosition()
    {
        return position;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;

        transform.position = new Vector3(pos.x, pos.y, 0);
    }

    public Tag GetTag()
    {
        return Tag.Pickup;
    }
}
