using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class RoundManager : MonoBehaviour
{
    [Header("라운드 설정")]
    [SerializeField] private float roundDuration    = 180f;
    [SerializeField] private int   countdownSeconds = 3;

    public float RemainingTime { get; private set; }
    public int   CurrentRound  { get; private set; } = 1;
    public int   MaxRounds     { get; private set; } = 3;

    public static RoundManager Instance { get; private set; }

    public event System.Action<int>  OnCountdown;
    public event System.Action       OnRoundStart;
    public event System.Action<bool> OnRoundEnd;

    private List<PlayerController> allPlayers = new List<PlayerController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (CatchDetector.Instance != null)
            CatchDetector.Instance.OnRobberCaught += HandleRobberCaught;
    }

    private void OnDestroy()
    {
        if (CatchDetector.Instance != null)
            CatchDetector.Instance.OnRobberCaught -= HandleRobberCaught;
    }

    public void StartGame()
    {
        StopAllCoroutines();
        allPlayers.Clear();

        if (CatchDetector.Instance != null)
            CatchDetector.Instance.ResetDetector();

        StartCoroutine(StartRound());
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (!allPlayers.Contains(player))
            allPlayers.Add(player);
    }

    private IEnumerator StartRound()
    {
        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작 준비");
        GameManager.Instance.ChangeState(GameState.Lobby);

        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();
        spawner?.ResetSpawn();

        // ✅ 맵 대기 제거 — 이미 Room 단계에서 로딩 완료
        // MasterClient 기준 전원 준비 확인만 유지
        if (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitUntil(() => NetworkManager.Instance.AllPlayersMapReady());
            Debug.Log("[RoundManager] ✅ 전원 맵 준비 완료");
        }

        yield return StartCoroutine(Countdown());

        if (spawner != null)
            spawner.SpawnOnRoad(new List<Vector2>());

        // 스폰된 내 플레이어 감지 대기 (최대 3초)
        float waitTime = 0f;
        while (waitTime < 3f)
        {
            PlayerController[] found =
                FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            bool myPlayerSpawned = false;
            foreach (var p in found)
            {
                PhotonView pv = p.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) { myPlayerSpawned = true; break; }
            }
            if (myPlayerSpawned) break;

            waitTime += Time.deltaTime;
            yield return null;
        }

        AssignRoles();

        GameManager.Instance.ChangeState(GameState.Playing);
        RemainingTime = roundDuration;
        OnRoundStart?.Invoke();

        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작!");
        yield return StartCoroutine(RoundTimer());
    }

    private void AssignRoles()
    {
        PlayerController[] found =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var p in found)
            if (!allPlayers.Contains(p))
                allPlayers.Add(p);

        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("[RoundManager] 등록된 플레이어 없음");
            return;
        }

        // 역할 배정 전 초기화 — 중복 등록 방지
        CatchDetector.Instance?.ResetDetector();

        foreach (PlayerController player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv == null) continue;

            // CustomProperties 직접 참조
            string role = pv.Owner?.CustomProperties[NetworkManager.ROLE_KEY] as string ?? "";
            Debug.Log($"[RoundManager] {player.name} | Role: '{role}'");

            bool isCop = role == "경찰";

            player.SetRole(isCop);

            if (isCop)
                CatchDetector.Instance?.RegisterCop(player);
            else
                CatchDetector.Instance?.RegisterRobber(player);

            if (pv.IsMine)
                UIManager.Instance?.UpdateRoleText(isCop);

            Debug.Log($"[RoundManager] {player.name} → {(isCop ? "경찰" : "도둑")} 등록 완료");
        }
    }

    private IEnumerator Countdown()
    {
        GameManager.Instance.ChangeState(GameState.Countdown);

        for (int i = countdownSeconds; i > 0; i--)
        {
            Debug.Log($"[RoundManager] {i}...");
            OnCountdown?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("[RoundManager] 출발!");
    }

    private IEnumerator RoundTimer()
    {
        while (RemainingTime > 0)
        {
            if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                Debug.Log("[RoundManager] 게임 오버 — 타이머 중단");
                yield break;
            }

            RemainingTime -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("[RoundManager] 시간 초과 — 도둑 승리!");
        EndRound(copWin: false);
    }

    private void HandleRobberCaught(PlayerController robber)
    {
        Debug.Log($"[RoundManager] 도둑 체포됨: {robber.gameObject.name}");
    }

    public void EndRound(bool copWin)
    {
        StopAllCoroutines();
        GameManager.Instance.ChangeState(GameState.GameOver);

        string result = copWin ? "경찰 승리!" : "도둑 승리!";
        Debug.Log($"[RoundManager] {CurrentRound}라운드 종료 — {result}");

        OnRoundEnd?.Invoke(copWin);
    }

    public void RestartRound()
    {
        Debug.Log("[RoundManager] 재시작 요청");
        StartGame();
    }

    private void ResetRound()
    {
        RemainingTime = roundDuration;

        if (CatchDetector.Instance != null)
            CatchDetector.Instance.ResetDetector();

        foreach (var player in allPlayers)
            if (player != null)
                player.ResetPosition();

        Debug.Log("[RoundManager] 초기화 완료");
    }

    private IEnumerator NextRoundDelay()
    {
        yield return new WaitForSeconds(3f);
        StartCoroutine(StartRound());
    }
}