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
        // 방장만 체포 판정
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
        Debug.Log($"[CatchDetector] 경찰 등록: {cop.gameObject.name}");
    }

    public void RegisterRobber(PlayerController robber)
    {
        if (!robbers.Contains(robber)) robbers.Add(robber);
        Debug.Log($"[CatchDetector] 도둑 등록: {robber.gameObject.name}");
    }

    private void CheckCatch()
    {
        List<PlayerController> caughtRobbers = new();

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
                    if (!caughtRobbers.Contains(robber))
                        caughtRobbers.Add(robber);
                }
            }
        }

        foreach (var robber in caughtRobbers)
        {
            // 방장이 RPC로 모든 클라이언트에 체포 알림
            PhotonView rv = robber.GetComponent<PhotonView>();
            if (rv != null)
                photonView.RPC("RPC_CatchRobber", RpcTarget.All, rv.ViewID);
        }
    }

    [PunRPC]
    private void RPC_CatchRobber(int robberViewID)
    {
        PlayerController robber = null;
        foreach (var r in robbers)
        {
            PhotonView pv = r.GetComponent<PhotonView>();
            if (pv != null && pv.ViewID == robberViewID)
            {
                robber = r;
                break;
            }
        }

        if (robber == null) return;

        robbers.Remove(robber);
        Debug.Log($"[CatchDetector] 체포! (남은 도둑: {robbers.Count}명)");
        OnRobberCaught?.Invoke(robber);

        // 잡힌 도둑 비활성화
        PhotonView robberPV = robber.GetComponent<PhotonView>();
        if (robberPV != null && robberPV.IsMine)
        {
            // 내가 잡힌 도둑이면 스펙테이터 모드로
            UIManager.Instance?.ShowSpectator();
        }

        if (robbers.Count == 0)
        {
            Debug.Log("[CatchDetector] 모든 도둑 체포!");
            if (PhotonNetwork.IsMasterClient)
                RoundManager.Instance?.EndRound(copWin: true);
        }
    }

    public void ResetDetector()
    {
        cops.Clear();
        robbers.Clear();
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