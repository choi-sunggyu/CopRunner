using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

        AssignRoles();

        yield return StartCoroutine(Countdown());

        GameManager.Instance.ChangeState(GameState.Playing);
        RemainingTime = roundDuration;
        OnRoundStart?.Invoke();

        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작!");

        yield return StartCoroutine(RoundTimer());
    }

    private void AssignRoles()
    {
        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("[RoundManager] 등록된 플레이어 없음");
            return;
        }

        for (int i = 0; i < allPlayers.Count; i++)
        {
            bool isCop = (i == 0);
            allPlayers[i].SetRole(isCop);

            if (isCop)
                CatchDetector.Instance?.RegisterCop(allPlayers[i]);
            else
                CatchDetector.Instance?.RegisterRobber(allPlayers[i]);

            Debug.Log($"[RoundManager] {allPlayers[i].gameObject.name} " +
                      $"→ {(isCop ? "경찰" : "도둑")}");
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
        ResetRound();
        StartCoroutine(StartRound());
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