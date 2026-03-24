using UnityEngine;
using UnityEngine.InputSystem;
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform target;

    [Header("거리 설정")]
    [SerializeField] private float distance    = 6f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 12f;

    [Header("높이 설정")]
    [SerializeField] private float heightOffset = 1.5f;

    [Header("마우스 감도")]
    [SerializeField] private float mouseSpeedX  = 3f;
    [SerializeField] private float mouseSpeedY  = 2f;

    [Header("수직 각도 제한")]
    [SerializeField] private float minYAngle    = -10f;
    [SerializeField] private float maxYAngle    = 60f;

    [Header("부드러움")]
    [SerializeField] private float smoothSpeed  = 8f;

    // 현재 궤도 각도
    private float currentX = 0f;
    private float currentY = 20f;

    // 목표 궤도 각도
    private float targetX  = 0f;
    private float targetY  = 20f;

    // 외부에서 카메라 forward 방향 읽을 수 있게
    public Vector3 Forward => transform.forward;

    private void Start()
    {
        // 마우스 커서 숨기기 + 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleMouseInput();
        UpdateCameraPosition();
    }

    private void HandleMouseInput()
    {
        // New Input System 방식으로 마우스 델타 읽기
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX =  mouseDelta.x * mouseSpeedX * Time.deltaTime;
        float mouseY =  mouseDelta.y * mouseSpeedY * Time.deltaTime;

        targetX += mouseX;
        targetY -= mouseY;

        targetY = Mathf.Clamp(targetY, minYAngle, maxYAngle);

        currentX = Mathf.Lerp(currentX, targetX, smoothSpeed * Time.deltaTime);
        currentY = Mathf.Lerp(currentY, targetY, smoothSpeed * Time.deltaTime);

        // 마우스 휠 줌
        float scroll = Mouse.current.scroll.ReadValue().y;
        distance    -= scroll * 0.01f;
        distance     = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void UpdateCameraPosition()
    {
        // 궤도 회전 계산
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);

        // 플레이어 기준 카메라 위치 계산
        Vector3 targetPos   = target.position + Vector3.up * heightOffset;
        Vector3 offset      = rotation * new Vector3(0f, 0f, -distance);

        // 부드럽게 위치 이동
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos + offset,
            smoothSpeed * Time.deltaTime
        );

        // 항상 플레이어 바라보기
        transform.LookAt(targetPos);
    }

    // ESC로 마우스 커서 토글 (개발 중 편의용)
    private void Update()
    {
        // New Input System 키보드 읽기
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