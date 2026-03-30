using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class CatchDetector : MonoBehaviourPun
{
    [Header("체포 설정")]
    [SerializeField] private float catchRadius   = 1.5f;
    [SerializeField] private float checkInterval = 0.1f;

    [Header("디버그")]
    [SerializeField] private bool showGizmos = true;

    private List<PlayerController> cops    = new();
    private List<PlayerController> robbers = new();

    // 중복 체포 방지용
    private HashSet<int> caughtViewIDs = new();

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
        if (!PhotonNetwork.IsMasterClient) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        CheckCatch();
    }

    public void RegisterCop(PlayerController cop)
    {
        if (!cops.Contains(cop)) cops.Add(cop);
        Debug.Log($"[CatchDetector] 경찰 등록: {cop.gameObject.name} | 총 {cops.Count}명");
    }

    public void RegisterRobber(PlayerController robber)
    {
        if (!robbers.Contains(robber)) robbers.Add(robber);
        Debug.Log($"[CatchDetector] 도둑 등록: {robber.gameObject.name} | 총 {robbers.Count}명");
    }

    private void CheckCatch()
    {
        foreach (var cop in cops)
        {
            if (cop == null) continue;

            foreach (var robber in robbers)
            {
                if (robber == null) continue;

                PhotonView rv = robber.GetComponent<PhotonView>();
                if (rv == null) continue;

                // ✅ 이미 체포된 도둑은 건너뜀
                if (caughtViewIDs.Contains(rv.ViewID)) continue;

                float distance = Vector3.Distance(
                    cop.transform.position,
                    robber.transform.position
                );

                if (distance <= catchRadius)
                {
                    // ✅ 즉시 등록해서 같은 프레임에 중복 RPC 방지
                    caughtViewIDs.Add(rv.ViewID);
                    photonView.RPC("RPC_CatchRobber", RpcTarget.All, rv.ViewID);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_CatchRobber(int robberViewID)
    {
        // ✅ ViewID로 직접 찾기 — robbers 리스트 의존 없음
        PhotonView robberPV = PhotonView.Find(robberViewID);
        if (robberPV == null)
        {
            Debug.LogWarning($"[CatchDetector] ViewID {robberViewID} 못 찾음");
            return;
        }

        PlayerController robber = robberPV.GetComponent<PlayerController>();
        if (robber == null) return;

        // 로컬 리스트에서 제거
        robbers.Remove(robber);
        Debug.Log($"[CatchDetector] 체포 확정! ViewID:{robberViewID} | 남은 도둑: {robbers.Count}명");

        OnRobberCaught?.Invoke(robber);

        // ✅ 잡힌 게 내 플레이어면 스펙테이터 전환
        if (robberPV.IsMine)
        {
            robber.SetCaught();  // ← PlayerController에 추가 필요 (아래 참고)
            UIManager.Instance?.ShowSpectator();
        }

        // ✅ MasterClient만 종료 판정
        if (PhotonNetwork.IsMasterClient && robbers.Count == 0)
        {
            Debug.Log("[CatchDetector] 모든 도둑 체포 → 경찰 승리");
            RoundManager.Instance?.EndRound(copWin: true);
        }
    }

    public void ResetDetector()
    {
        cops.Clear();
        robbers.Clear();
        caughtViewIDs.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.red;
        foreach (var cop in cops)
            if (cop != null)
                Gizmos.DrawWireSphere(cop.transform.position, catchRadius);
    }
}