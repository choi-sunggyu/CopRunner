using UnityEngine;

public enum GameState
{
    Lobby,
    Countdown,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴 — 게임 어디서든 GameManager.Instance로 접근 가능
    public static GameManager Instance { get; private set; }

    // 현재 게임 상태
    public GameState CurrentState { get; private set; }

    // 라운드 제한 시간 (초)
    [SerializeField] private float roundDuration = 180f;

    // 현재 남은 시간
    public float RemainingTime { get; private set; }
    
    private void Awake()
    {
        // 싱글톤 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환해도 유지 (기본 동작: 이전 씬의 모든 GameObject 삭제 / 타이머, 점수 유지 필요)
    }

    private void Start()
    {
        ChangeState(GameState.Playing);
        //ChangeState(GameState.Lobby);
    }

    private void Update()
    {
        // 게임 중일 때만 타이머 작동
        if (CurrentState == GameState.Playing)
        {
            RemainingTime -= Time.deltaTime;

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                ChangeState(GameState.GameOver);
            }
        }
    }

    // 상태 변경 메서드
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        RemainingTime = roundDuration;

        Debug.Log($"[GameManager] 상태 변경: {newState}");

        switch (newState)
        {
            case GameState.Lobby:
            case GameState.GameOver:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
                break;
            case GameState.Countdown:
            case GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
                break;
        }

        switch (newState)
        {
            case GameState.Lobby:      OnEnterLobby();     break;
            case GameState.Countdown:  OnEnterCountdown(); break;
            case GameState.Playing:    OnEnterPlaying();   break;
            case GameState.GameOver:   OnEnterGameOver();  break;
        }
    }

     private void OnEnterLobby()
    {
        Debug.Log("[GameManager] 로비 진입 — 플레이어 대기 중");
    }

    private void OnEnterCountdown()
    {
        Debug.Log("[GameManager] 카운트다운 시작");
    }

    private void OnEnterPlaying()
    {
        Debug.Log("[GameManager] 게임 시작!");
    }

    private void OnEnterGameOver()
    {
        Debug.Log("[GameManager] 게임 오버");
    }
}
