using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed   = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float rotateSpeed = 10f;

    private Rigidbody  rb;
    private Camera     mainCamera;
    private PhotonView photonViewCached;

    private Vector2 moveInput;
    private bool    isSprinting;
    private Vector3 startPosition;
    private bool isCaught = false;
    public bool IsCaught => isCaught;

    public float CurrentSpeed { get; private set; }

    [SerializeField] private bool isCop = false;
    public bool IsCop => isCop;

    public void SetRole(bool cop)
    {
        isCop = cop;
        Debug.Log($"[PlayerController] {gameObject.name} 역할: {(cop ? "경찰" : "도둑")}");

        // ✅ 등록 로직 없음 — RoundManager.AssignRoles()가 담당
    }

    public void SetCaught()
    {
        isCaught = true;
        Debug.Log($"[PlayerController] {gameObject.name} 체포됨 — 이동 불가");
    }

    private void Awake()
    {
        rb                = GetComponent<Rigidbody>();
        mainCamera        = Camera.main;
        photonViewCached  = GetComponent<PhotonView>();
    }

    private void Start()
    {
        startPosition = transform.position;
        StartCoroutine(RegisterWithDelay());
    }

    public void ResetPosition()
    {
        transform.position = startPosition;
        rb.linearVelocity        = Vector3.zero; // ✅ 수정
        Debug.Log($"[PlayerController] {gameObject.name} 위치 초기화");
    }

    private System.Collections.IEnumerator RegisterWithDelay()
    {
        yield return null;

        RoundManager.Instance?.RegisterPlayer(this);
    }

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
        if (photonViewCached != null && !photonViewCached.IsMine) return;

        // ✅ 잡혔으면 이동 불가
        if (isCaught) return;

        // Playing 상태일 때만 이동 허용
        if (GameManager.Instance.CurrentState != GameState.Playing)
        {
            rb.linearVelocity  = new Vector3(0f, rb.linearVelocity.y, 0f); // ✅ 수정
            CurrentSpeed = 0f;
            return;
        }

        if (moveInput.magnitude < 0.1f)
        {
            rb.linearVelocity  = new Vector3(0f, rb.linearVelocity.y, 0f); // ✅ 수정
            CurrentSpeed = 0f;
            return;
        }

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

        Vector3 velocity = moveDirection * targetSpeed;
        velocity.y       = rb.linearVelocity.y;  // ✅ 수정
        rb.linearVelocity      = velocity;        // ✅ 수정

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