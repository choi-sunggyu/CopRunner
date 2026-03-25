using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform target;

    [Header("거리 설정")]
    [SerializeField] private float distance    = 6f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 12f;

    [Header("높이 설정")]
    [SerializeField] private float heightOffset = 1.5f;

    [Header("마우스 감도")]
    [SerializeField] private float mouseSpeedX = 3f;
    [SerializeField] private float mouseSpeedY = 2f;

    [Header("수직 각도 제한")]
    [SerializeField] private float minYAngle = -10f;
    [SerializeField] private float maxYAngle = 60f;

    [Header("부드러움")]
    [SerializeField] private float smoothSpeed = 8f;

    [Header("벽 통과 방지")]
    [SerializeField] private LayerMask collisionMask;

    private float currentX       = 0f;
    private float currentY       = 20f;
    private float targetX        = 0f;
    private float targetY        = 20f;
    private float currentDistance;

    public Vector3 Forward => transform.forward;

    private void Start()
    {
        currentDistance = distance;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        HandleMouseInput();
        UpdateCameraPosition();
    }

    private void HandleMouseInput()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        targetX += mouseDelta.x * mouseSpeedX * Time.deltaTime;
        targetY -= mouseDelta.y * mouseSpeedY * Time.deltaTime;
        targetY  = Mathf.Clamp(targetY, minYAngle, maxYAngle);

        currentX = Mathf.Lerp(currentX, targetX, smoothSpeed * Time.deltaTime);
        currentY = Mathf.Lerp(currentY, targetY, smoothSpeed * Time.deltaTime);

        float scroll = Mouse.current.scroll.ReadValue().y;
        distance     = Mathf.Clamp(distance - scroll * 0.01f, minDistance, maxDistance);
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation   = Quaternion.Euler(currentY, currentX, 0f);
        Vector3    targetPos  = target.position + Vector3.up * heightOffset;
        Vector3    direction  = rotation * Vector3.back;
        Vector3    desiredPos = targetPos + direction * distance;

        RaycastHit hit;
        if (Physics.Linecast(targetPos, desiredPos, out hit, collisionMask))
        {
            currentDistance = Mathf.Max(hit.distance - 0.1f, 0.1f);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, distance, smoothSpeed * Time.deltaTime);
        }

        transform.position = targetPos + direction * currentDistance;
        transform.LookAt(targetPos);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }
    }
}