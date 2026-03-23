using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed   = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float rotateSpeed = 10f;

    private Rigidbody rb;
    private Camera    mainCamera;

    private Vector2 moveInput;
    private bool    isSprinting;

    public float CurrentSpeed { get; private set; }
    public bool  IsCop        { get; set; } = false;

    private void Awake()
    {
        rb         = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
    }

    // New Input System 이벤트 콜백 — PlayerInput이 자동 호출
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        if (moveInput.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            CurrentSpeed = 0f;
            return;
        }

        // CameraFollow에서 카메라 방향 가져오기
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight   = mainCamera.transform.right;

        camForward.y = 0;
        camRight.y   = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection =
            (camForward * moveInput.y + camRight * moveInput.x).normalized;

        float targetSpeed = (!IsCop && isSprinting) ? sprintSpeed : moveSpeed;
        CurrentSpeed      = targetSpeed;

        Vector3 velocity  = moveDirection * targetSpeed;
        velocity.y        = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        // 플레이어는 이동 방향으로만 회전 (카메라와 독립)
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );
        }
    }
}