using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate() // 注意是 FixedUpdate，用于物理操作！
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;

        Vector3 targetVelocity = input * moveSpeed;
        targetVelocity.y = rb.velocity.y; // 保留 Y 轴速度（0）

        rb.velocity = targetVelocity;
    }
}
