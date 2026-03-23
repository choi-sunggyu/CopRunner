using UnityEngine;
using System.Collections.Generic;

public class CatchDetector : MonoBehaviour
{
    [Header("체포 설정")]
    [SerializeField] private float catchRadius     = 1.5f;  // 체포 가능 거리
    [SerializeField] private float checkInterval   = 0.1f;  // 판정 주기 (초)

    [Header("디버그")]
    [SerializeField] private bool showGizmos = true;        // 씬 뷰에서 범위 시각화

    // 등록된 경찰/도둑 목록
    private List<PlayerController> cops    = new List<PlayerController>();
    private List<PlayerController> robbers = new List<PlayerController>();

    // 체포 이벤트 (외부에서 구독 가능)
    public event System.Action<PlayerController> OnRobberCaught;

    // 싱글톤
    public static CatchDetector Instance { get; private set; }

    private float timer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // 매 프레임 체크하면 부하 — checkInterval마다 체크
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        CheckCatch();
    }

    // 경찰/도둑 등록 메서드
    public void RegisterCop(PlayerController cop)
    {
        if (!cops.Contains(cop))
        {
            cops.Add(cop);
            Debug.Log($"[CatchDetector] 경찰 등록: {cop.gameObject.name}");
        }
    }

    public void RegisterRobber(PlayerController robber)
    {
        if (!robbers.Contains(robber))
        {
            robbers.Add(robber);
            Debug.Log($"[CatchDetector] 도둑 등록: {robber.gameObject.name}");
        }
    }

    // 체포 판정 핵심 로직
    private void CheckCatch()
    {
        // 체포할 도둑 목록 따로 수집
        List<PlayerController> caughtRobbers = new List<PlayerController>();

        foreach (var cop in cops)
        {
            if (cop == null) continue;

            foreach (var robber in robbers)
            {
                if (robber == null) continue;

                float distance = Vector3.Distance(
                    cop.transform.position,
                    robber.transform.position
                );

                if (distance <= catchRadius)
                {
                    // 바로 제거하지 않고 목록에 추가
                    if (!caughtRobbers.Contains(robber))
                        caughtRobbers.Add(robber);
                }
            }
        }

        // foreach 끝난 후 제거
        foreach (var robber in caughtRobbers)
        {
            CatchRobber(robber);
        }
    }

    // cop 파라미터 제거 — 로그용으로만 쓰던 것
    private void CatchRobber(PlayerController robber)
    {
        robbers.Remove(robber);

        Debug.Log($"[CatchDetector] 체포! (남은 도둑: {robbers.Count}명)");

        OnRobberCaught?.Invoke(robber);

        if (robbers.Count == 0)
        {
            Debug.Log("[CatchDetector] 모든 도둑 체포 완료!");
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
    }

        // 씬 뷰에서 체포 범위 시각화
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        foreach (var cop in cops)
        {
            if (cop != null)
                Gizmos.DrawWireSphere(cop.transform.position, catchRadius);
        }
    }
}