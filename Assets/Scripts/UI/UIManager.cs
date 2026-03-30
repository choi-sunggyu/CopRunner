using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Photon.Pun;

public class UIManager : MonoBehaviour
{
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
    [SerializeField] private Button          restartButton;   // 방장만
    [SerializeField] private Button          leaveButton;     // 모두

    [Header("스펙테이터")]
    [SerializeField] private GameObject spectatorPanel;
    [SerializeField] private Button     spectatorLeaveButton;

    public void ShowSpectator()
    {
        SetAllPanelsInactive();
        spectatorPanel?.SetActive(true);
    }

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
        // 시작 시 모든 패널 비활성화
        SetAllPanelsInactive();

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnCountdown  += ShowCountdown;
            RoundManager.Instance.OnRoundStart += ShowHUD;
            RoundManager.Instance.OnRoundEnd   += ShowResult;
        }

        leaveButton?.onClick.AddListener(() => {
            NetworkManager.Instance?.LeaveRoom();
            LobbyManager.Instance?.ShowMainMenu();
            SetAllPanelsInactive();
        });

        spectatorLeaveButton?.onClick.AddListener(() =>
        {
            NetworkManager.Instance?.LeaveRoom();
            LobbyManager.Instance?.ShowMainMenu();
            SetAllPanelsInactive();
        });

        restartButton?.onClick.AddListener(OnRestartClicked);
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
        if (hudPanel != null && hudPanel.activeSelf && RoundManager.Instance != null)
            UpdateTimer(RoundManager.Instance.RemainingTime);
    }

    // ── 패널 전환 ──────────────────────────────

    public void HideLobbyUI()
    {
        SetAllPanelsInactive();
        Debug.Log("[UIManager] 모든 UI 숨김");
    }

    public void ShowHUD()
    {
        SetAllPanelsInactive();
        hudPanel?.SetActive(true);

        // RoundManager에서 역할 가져와서 업데이트
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController pc in players)
        {
            PhotonView pv = pc.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                UpdateRoleText(pc.IsCop);
                break;
            }
        }

        UpdatePlayerCount(PhotonNetwork.CurrentRoom?.PlayerCount ?? 0);
    }

    public void ShowCountdown(int count)
    {
        SetAllPanelsInactive();
        countdownPanel?.SetActive(true);
        StartCoroutine(AnimateCountdown(count));
    }

    public void ShowResult(bool copWin)
    {
        SetAllPanelsInactive();
        resultPanel?.SetActive(true);

        if (resultTitleText  != null)
            resultTitleText.text  = copWin ? "경찰 승리!" : "도둑 승리!";
        if (resultDetailText != null)
            resultDetailText.text = copWin
                ? "모든 도둑이 체포됐습니다!"
                : "시간이 초과됐습니다!";

        // 방장만 재시작 버튼 표시
        if (restartButton != null)
            restartButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // 나가기 버튼 표시
        if (leaveButton != null)
            leaveButton.gameObject.SetActive(true);
    }

    private void SetAllPanelsInactive()
    {
        lobbyPanel?.SetActive(false);
        countdownPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        resultPanel?.SetActive(false);
        spectatorPanel?.SetActive(false);
    }

    // ── HUD 업데이트 ───────────────────────────

    private void UpdateTimer(float remainingTime)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text  = $"{minutes:00}:{seconds:00}";
        timerText.color = remainingTime <= 30f ? Color.red : Color.white;
    }

    public void UpdateRoleText(bool isCop)
    {
        if (roleText == null) return;
        roleText.text  = isCop ? "경찰" : "도둑";
        roleText.color = isCop ? Color.blue : Color.red;
    }

    public void UpdatePlayerCount(int count)
    {
        if (playerCountText != null)
            playerCountText.text = $"플레이어: {count}명";
    }

    // ── 카운트다운 애니메이션 ──────────────────

    private IEnumerator AnimateCountdown(int count)
    {
        if (countdownText == null) yield break;

        countdownText.text = count > 0 ? count.ToString() : "출발!";
        countdownText.transform.localScale = Vector3.one * 1.5f;

        float elapsed  = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            countdownText.transform.localScale =
                Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
            countdownText.color = new Color(1f, 1f, 1f, 1f - t * 0.3f);
            yield return null;
        }
    }

    // ── 버튼 이벤트 ───────────────────────────

    private void OnRestartClicked()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
            pv.RPC("RPC_RestartGame", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_RestartGame()
    {
        SetAllPanelsInactive();
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();
        spawner?.ResetSpawn();
        RoundManager.Instance?.RestartRound();
    }
}