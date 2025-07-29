using UnityEngine;

public class HorizontalMover : MonoBehaviour
{
    [Header("左右に動く距離")]
    public float moveDistance = 3.0f;

    [Header("1往復にかかる秒数")]
    public float moveDuration = 2.0f;

    private Vector3 startPos;
    private Vector3 leftTarget;
    private Vector3 rightTarget;

    private bool goingRight = true;
    private float timer = 0f;

    void Start()
    {
        startPos = transform.position;
        leftTarget = startPos + Vector3.left * moveDistance * 0.5f;
        rightTarget = startPos + Vector3.right * moveDistance * 0.5f;
        transform.position = leftTarget;
    }

    void Update()
    {
        timer += Time.deltaTime / (moveDuration * 0.5f); // 片道の時間
        if (goingRight)
        {
            transform.position = Vector3.Lerp(leftTarget, rightTarget, timer);
        }
        else
        {
            transform.position = Vector3.Lerp(rightTarget, leftTarget, timer);
        }

        if (timer >= 1f)
        {
            timer = 0f;
            goingRight = !goingRight;
        }
    }
}
