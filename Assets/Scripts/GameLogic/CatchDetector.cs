using UnityEngine;
using System.Collections.Generic;

public class CatchDetector : MonoBehaviour
{
    [Header("체포 설정")]
    [SerializeField] private float catchRadius   = 0.8f;
    [SerializeField] private float checkInterval = 0.1f;

    [Header("디버그")]
    [SerializeField] private bool showGizmos = true;

    private List<PlayerController> cops    = new List<PlayerController>();
    private List<PlayerController> robbers = new List<PlayerController>();

    public event System.Action<PlayerController> OnRobberCaught;
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
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        CheckCatch();
    }

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

    private void CheckCatch()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        List<PlayerController> caughtRobbers = new List<PlayerController>();

        foreach (var cop in cops)
        {
            if (cop == null) continue;

            foreach (var robber in robbers)
            {
                if (robber == null) continue;

                // 캡슐 콜라이더 기준 실제 거리 계산
                CapsuleCollider copCol    = cop.GetComponent<CapsuleCollider>();
                CapsuleCollider robberCol = robber.GetComponent<CapsuleCollider>();

                float distance = Vector3.Distance(
                    cop.transform.position,
                    robber.transform.position
                );

                // 두 콜라이더 반지름 합산
                float combinedRadius = catchRadius;
                if (copCol != null && robberCol != null)
                    combinedRadius = copCol.radius + robberCol.radius + 0.1f;

                if (distance <= combinedRadius)
                {
                    if (!caughtRobbers.Contains(robber))
                        caughtRobbers.Add(robber);
                }
            }
        }

        foreach (var robber in caughtRobbers)
            CatchRobber(robber);
    }

    private void CatchRobber(PlayerController robber)
    {
        robbers.Remove(robber);
        Debug.Log($"[CatchDetector] 체포! (남은 도둑: {robbers.Count}명)");
        OnRobberCaught?.Invoke(robber);

        if (robbers.Count == 0)
        {
            Debug.Log("[CatchDetector] 모든 도둑 체포 완료!");
            GameManager.Instance.ChangeState(GameState.GameOver);
            RoundManager.Instance?.EndRound(copWin: true);
        }
    }

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

    // 리스트 초기화 메서드 추가
    public void ResetDetector()
    {
        cops.Clear();
        robbers.Clear();
        Debug.Log("[CatchDetector] 경찰/도둑 리스트 초기화");
    }
}