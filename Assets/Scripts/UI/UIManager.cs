using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 싱글톤
    public static UIManager Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("HUD 요소")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    [Header("카운트다운")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("결과 화면")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultDetailText;
    [SerializeField] private Button          restartButton;

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
        // RoundManager 이벤트 구독
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnCountdown  += ShowCountdown;
            RoundManager.Instance.OnRoundStart += ShowHUD;
            RoundManager.Instance.OnRoundEnd   += ShowResult;
        }

        // 버튼 이벤트
        restartButton?.onClick.AddListener(OnRestartClicked);

        // 초기 상태
        ShowLobby();
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnCountdown  -= ShowCountdown;
            RoundManager.Instance.OnRoundStart -= ShowHUD;
            RoundManager.Instance.OnRoundEnd   -= ShowResult;
        }
    }

    private void Update()
    {
        // HUD 타이머 업데이트
        if (hudPanel.activeSelf && RoundManager.Instance != null)
        {
            UpdateTimer(RoundManager.Instance.RemainingTime);
        }
    }

    // ── 패널 전환 ──────────────────────────────

    public void ShowLobby()
    {
        SetAllPanelsInactive();
        lobbyPanel.SetActive(true);
    }

    public void ShowHUD()
    {
        SetAllPanelsInactive();
        hudPanel.SetActive(true);
    }

    public void ShowCountdown(int count)
    {
        SetAllPanelsInactive();
        countdownPanel.SetActive(true);
        StartCoroutine(AnimateCountdown(count));
    }

    public void ShowResult(bool copWin)
    {
        SetAllPanelsInactive();
        resultPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        resultTitleText.text = copWin ? "경찰 승리!" : "도둑 승리!";
        resultDetailText.text = copWin
            ? "모든 도둑이 체포됐습니다!"
            : "시간이 초과됐습니다!";
    }

    private void SetAllPanelsInactive()
    {
        lobbyPanel?    .SetActive(false);
        countdownPanel?.SetActive(false);
        hudPanel?      .SetActive(false);
        resultPanel?   .SetActive(false);
    }

    // ── HUD 업데이트 ───────────────────────────

    private void UpdateTimer(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        // 30초 이하 빨간색 경고
        timerText.color = remainingTime <= 30f ? Color.red : Color.white;
    }

    public void UpdateRoleText(bool isCop)
    {
        roleText.text  = isCop ? "경찰" : "도둑";
        roleText.color = isCop ? Color.blue : Color.red;
    }

    public void UpdatePlayerCount(int count)
    {
        playerCountText.text = $"플레이어: {count}명";
    }

    // ── 카운트다운 애니메이션 ──────────────────

    private IEnumerator AnimateCountdown(int count)
    {
        countdownText.text = count > 0 ? count.ToString() : "출발!";

        // 크게 시작
        countdownText.transform.localScale = Vector3.one * 1.5f;

        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 점점 작아지면서 사라짐
            countdownText.transform.localScale =
                Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);

            countdownText.color = new Color(1f, 1f, 1f, 1f - t * 0.3f);
            yield return null;
        }
    }

    // ── 버튼 이벤트 ───────────────────────────

    private void OnRestartClicked()
    {
        // 게임 시작 시 마우스 다시 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        Debug.Log("[UIManager] 재시작 버튼 클릭");
        RoundManager.Instance?.RestartRound();
    }
}