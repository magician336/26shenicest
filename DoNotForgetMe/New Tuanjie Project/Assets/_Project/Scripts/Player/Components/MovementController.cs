using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(float horizontalInput)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    public void ApplySettings(float speed)
    {
        moveSpeed = speed;
    }

    public void Stop()
    {
        if (rb == null) return;
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }
}
