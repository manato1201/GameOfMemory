using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    [Header("移動するポイント（順番に追加：始点→中間点→終点）")]
    public Transform[] waypoints;

    [Header("移動速度")]
    public float moveSpeed = 3.0f;

    [Header("到達とみなす距離")]
    public float arrivalThreshold = 0.05f;

    private int currentTarget = 0;

    void Start()
    {
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentTarget = 1 % waypoints.Length;
        }
    }

    void Update()
    {
        if (waypoints.Length < 2) return;

        Vector3 targetPos = waypoints[currentTarget].position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < arrivalThreshold)
        {
            currentTarget++;
            if (currentTarget >= waypoints.Length)
            {
                // 終点に着いたら始点に戻る
                currentTarget = 0;
            }
        }
    }
}