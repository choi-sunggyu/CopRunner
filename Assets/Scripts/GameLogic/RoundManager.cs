using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class RoundManager : MonoBehaviour
{
    [Header("라운드 설정")]
    [SerializeField] private float roundDuration    = 180f;
    [SerializeField] private int   countdownSeconds = 3;

    // ✅ [Header] 제거 — property에는 사용 불가
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

        public void StartGame()
    {
        StopAllCoroutines();
        allPlayers.Clear();

        if (CatchDetector.Instance != null)
            CatchDetector.Instance.ResetDetector();

        StartCoroutine(StartRound());
    }

    private void OnDestroy()
    {
        if (CatchDetector.Instance != null)
            CatchDetector.Instance.OnRobberCaught -= HandleRobberCaught;
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

        // 플레이어 스폰 (게임 시작 시)
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();
        spawner?.ResetSpawn();

        AssignRoles();
        yield return StartCoroutine(Countdown());

        // 카운트다운 후 스폰
        if (spawner != null)
            spawner.SpawnOnRoad(new List<Vector2>());

        GameManager.Instance.ChangeState(GameState.Playing);
        RemainingTime = roundDuration;
        OnRoundStart?.Invoke();

        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작!");

        yield return StartCoroutine(RoundTimer());
    }

    private void AssignRoles()
    {
        // 네트워크 플레이어 자동 수집
        PlayerController[] found = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in found)
            if (!allPlayers.Contains(p))
                allPlayers.Add(p);

        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("[RoundManager] 등록된 플레이어 없음");
            return;
        }

        // ✅ 역할 배정 전 CatchDetector 초기화 — 중복 등록 방지
        CatchDetector.Instance?.ResetDetector();

        foreach (PlayerController player in allPlayers)
        {
            // PhotonView로 해당 Photon 플레이어 찾기
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv == null) continue;

            // 해당 플레이어가 선택한 역할 가져오기
            string role = NetworkManager.Instance.GetPlayerRole(pv.Owner);
            bool isCop  = role == "경찰";

            // 1. 역할 설정
            player.SetRole(isCop);

            // 2. 역할 확정 후 등록 (순서 중요)
            if (isCop)
                CatchDetector.Instance?.RegisterCop(player);
            else
                CatchDetector.Instance?.RegisterRobber(player);

            // HUD 역할 텍스트 업데이트 (내 캐릭터만)
            // 3. 내 HUD만 업데이트
            if (pv.IsMine)
                UIManager.Instance?.UpdateRoleText(isCop);

            Debug.Log($"[RoundManager] {player.gameObject.name} → {role}");
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

    private IEnumerator NextRoundDelay()
    {
        yield return new WaitForSeconds(3f);
        StartCoroutine(StartRound());
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
        {
            if (player != null)
                player.ResetPosition();
        }

        Debug.Log("[RoundManager] 초기화 완료");
    }
}