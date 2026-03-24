using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoundManager : MonoBehaviour
{
    [Header("라운드 설정")]
    [SerializeField] private float roundDuration    = 180f; // 180초
    [SerializeField] private int   countdownSeconds = 3;

    [Header("상태")]
    public float RemainingTime  { get; private set; }
    public int   CurrentRound   { get; private set; } = 1;
    public int   MaxRounds      { get; private set; } = 3;

    // 싱글톤
    public static RoundManager Instance { get; private set; }

    // 이벤트
    public event System.Action<int>   OnCountdown;   // 카운트다운 숫자
    public event System.Action        OnRoundStart;  // 라운드 시작
    public event System.Action<bool>  OnRoundEnd;    // 라운드 종료 (경찰 승리 여부)

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
        // CatchDetector 체포 이벤트 구독
        if (CatchDetector.Instance != null)
            CatchDetector.Instance.OnRobberCaught += HandleRobberCaught;

        // 게임 시작
        StartCoroutine(StartRound());
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (CatchDetector.Instance != null)
            CatchDetector.Instance.OnRobberCaught -= HandleRobberCaught;
    }

    // 플레이어 등록
    public void RegisterPlayer(PlayerController player)
    {
        if (!allPlayers.Contains(player))
            allPlayers.Add(player);
    }

    // 라운드 시작 코루틴
    private IEnumerator StartRound()
    {
        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작 준비");
        GameManager.Instance.ChangeState(GameState.Lobby);

        // 역할 배정
        AssignRoles();

        // 카운트다운
        yield return StartCoroutine(Countdown());

        // 게임 시작
        GameManager.Instance.ChangeState(GameState.Playing);
        RemainingTime = roundDuration;
        OnRoundStart?.Invoke();

        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작!");

        // 타이머
        yield return StartCoroutine(RoundTimer());
    }

    // 역할 자동 배정
    private void AssignRoles()
    {
        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("[RoundManager] 등록된 플레이어 없음");
            return;
        }

        // 첫 번째 플레이어를 경찰로
        for (int i = 0; i < allPlayers.Count; i++)
        {
            bool isCop = (i == 0);
            // SerializedField isCop은 직접 접근 불가 → 메서드로 설정
            allPlayers[i].SetRole(isCop);

            // CatchDetector에 재등록
            if (isCop)
                CatchDetector.Instance?.RegisterCop(allPlayers[i]);
            else
                CatchDetector.Instance?.RegisterRobber(allPlayers[i]);

            Debug.Log($"[RoundManager] {allPlayers[i].gameObject.name} " +
                      $"→ {(isCop ? "경찰" : "도둑")}");
        }
    }

    // 카운트다운 코루틴
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

    // 타이머 코루틴
    private IEnumerator RoundTimer()
    {
        while (RemainingTime > 0)
        {
            // GameOver 상태면 즉시 중단
            if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                Debug.Log("[RoundManager] 게임 오버 — 타이머 중단");
                yield break;
            }

            RemainingTime -= Time.deltaTime;
            yield return null;
        }

        // 시간 초과 → 도둑 승리
        Debug.Log("[RoundManager] 시간 초과 — 도둑 승리!");
        EndRound(copWin: false);
    }

    // 도둑 체포 시 호출
    private void HandleRobberCaught(PlayerController robber)
    {
        Debug.Log($"[RoundManager] 도둑 체포됨: {robber.gameObject.name}");
    }

    // 라운드 종료
    public void EndRound(bool copWin)
    {
        StopAllCoroutines();
        GameManager.Instance.ChangeState(GameState.GameOver);

        string result = copWin ? "경찰 승리!" : "도둑 승리!";
        Debug.Log($"[RoundManager] {CurrentRound}라운드 종료 — {result}");

        OnRoundEnd?.Invoke(copWin);

        // ❌ 자동 재시작 제거
        // if (CurrentRound < MaxRounds) ...
    }

    private IEnumerator NextRoundDelay()
    {
        yield return new WaitForSeconds(3f);
        StartCoroutine(StartRound());
    }

    // 재시작 메서드 추가 — UIManager 버튼에서 호출
    public void RestartRound()
    {
        Debug.Log("[RoundManager] 재시작 요청");
        ResetRound();
        StartCoroutine(StartRound());
    }

    // 초기화 메서드 추가
    private void ResetRound()
    {
        // 타이머 초기화
        RemainingTime = roundDuration;

        // CatchDetector 리스트 초기화
        if (CatchDetector.Instance != null)
            CatchDetector.Instance.ResetDetector();

        // 플레이어 위치 초기화
        foreach (var player in allPlayers)
        {
            if (player != null)
                player.ResetPosition();
        }

        Debug.Log("[RoundManager] 초기화 완료");        
    }
}