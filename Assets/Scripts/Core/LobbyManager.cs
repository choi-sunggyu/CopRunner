using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("메인메뉴 패널")]
    [SerializeField] private GameObject    mainMenuPanel;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button         createRoomButton;
    [SerializeField] private Button         joinRoomButton;
    [SerializeField] private Button         quickJoinButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("방 패널")]
    [SerializeField] private GameObject      roomPanel;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private Transform       playerListContent; // Scroll View Content
    [SerializeField] private GameObject      playerEntryPrefab; // 플레이어 목록 항목
    [SerializeField] private Button          readyButton;
    [SerializeField] private Button          startButton;       // 방장만 활성화
    [SerializeField] private Button          leaveRoomButton;

    private bool isReady = false;

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
        // 닉네임 최대 10자 제한
        if (nicknameInput != null)
            nicknameInput.characterLimit = 10;

        // 방 이름 최대 20자 제한
        if (roomNameInput != null)
            roomNameInput.characterLimit = 20;

        // 버튼 이벤트 연결
        createRoomButton?.onClick.AddListener(OnCreateRoom);
        joinRoomButton?.onClick.AddListener(OnJoinRoom);
        quickJoinButton?.onClick.AddListener(OnQuickJoin);
        readyButton?.onClick.AddListener(OnReadyToggle);
        startButton?.onClick.AddListener(OnStartGame);
        leaveRoomButton?.onClick.AddListener(OnLeaveRoom);

        ShowMainMenu();
        SetStatus("서버 연결 중...");
    }

    // ── 패널 전환 ──────────────────────────────

    private void ShowMainMenu()
    {
        mainMenuPanel?.SetActive(true);
        roomPanel?.SetActive(false);
    }

    private void ShowRoomPanel()
    {
        mainMenuPanel?.SetActive(false);
        roomPanel?.SetActive(true);
        RefreshRoomUI();
    }

    // ── 버튼 이벤트 ───────────────────────────

    private void OnCreateRoom()
    {
            if (NetworkManager.Instance == null)
            {
                SetStatus("서버 연결 중... 잠시 후 시도해주세요.");
                return;
            }

            string nickname = nicknameInput?.text.Trim();
            string roomName = roomNameInput?.text.Trim();

            if (string.IsNullOrEmpty(nickname))
            {
                SetStatus("닉네임을 입력해주세요!");
                return;
            }

            NetworkManager.Instance.SetNickname(nickname);
            string finalRoomName = string.IsNullOrEmpty(roomName)
                ? $"Room_{Random.Range(1000, 9999)}"
                : roomName;

            NetworkManager.Instance.CreateRoom(finalRoomName);
            SetStatus("방 생성 중...");
    }

    private void OnJoinRoom()
    {
        if (NetworkManager.Instance == null)
        {
            SetStatus("서버 연결 중... 잠시 후 시도해주세요.");
            return;
        }

        string nickname = nicknameInput?.text.Trim();
        string roomName = roomNameInput?.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            SetStatus("닉네임을 입력해주세요!");
            return;
        }

        if (string.IsNullOrEmpty(roomName))
        {
            SetStatus("방 이름을 입력해주세요!");
            return;
        }

        NetworkManager.Instance.SetNickname(nickname);
        NetworkManager.Instance.JoinRoom(roomName);
        SetStatus("방 참가 중...");
    }

    private void OnQuickJoin()
    {
        if (NetworkManager.Instance == null)
        {
            SetStatus("서버 연결 중... 잠시 후 시도해주세요.");
            return;
        }
        
        string nickname = nicknameInput?.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            SetStatus("닉네임을 입력해주세요!");
            return;
        }

        NetworkManager.Instance.SetNickname(nickname);
        NetworkManager.Instance.JoinRandomRoom();
        SetStatus("방 찾는 중...");
    }

    private void OnReadyToggle()
    {
        isReady = !isReady;
        NetworkManager.Instance.SetReady(isReady);

        // 버튼 텍스트 변경
        TextMeshProUGUI btnText = readyButton?.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = isReady ? "레디 취소" : "레디";

        RefreshRoomUI();
    }

    private void OnStartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!NetworkManager.Instance.IsAllReady())
        {
            SetStatus("모든 플레이어가 레디해야 합니다!");
            return;
        }

        Debug.Log("[LobbyManager] 게임 시작!");
        RoundManager.Instance?.StartGame();
    }
    

    private void OnLeaveRoom()
    {
        isReady = false;
        NetworkManager.Instance.LeaveRoom();
        ShowMainMenu();
    }

    // ── 외부 콜백 ─────────────────────────────

    public void OnConnected()
    {
        SetStatus("서버 연결 완료!");
    }

    public void OnJoinedRoom()
    {
        isReady = false;
        ShowRoomPanel();
    }

    // 방 UI 갱신
    public void RefreshRoomUI()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        // 방 이름
        if (roomNameText != null)
            roomNameText.text = $"방: {PhotonNetwork.CurrentRoom.Name}";

        // 플레이어 목록 갱신
        if (playerListContent != null)
        {
            // 기존 항목 삭제
            foreach (Transform child in playerListContent)
                Destroy(child.gameObject);

            // 플레이어 항목 생성
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                GameObject entry = Instantiate(playerEntryPrefab, playerListContent);
                TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    object isReady;
                    player.CustomProperties.TryGetValue(NetworkManager.READY_KEY, out isReady);
                    bool ready = isReady != null && (bool)isReady;

                    string masterTag = player.IsMasterClient ? "[방장] " : "";
                    string readyTag  = ready ? " ✅" : " ⬜";
                    text.text = $"{masterTag}{player.NickName}{readyTag}";
                }
            }
        }

        // 시작 버튼: 방장만 + 모두 레디일 때 활성화
        if (startButton != null)
        {
            bool canStart = PhotonNetwork.IsMasterClient && NetworkManager.Instance.IsAllReady();
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            startButton.interactable = canStart;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[LobbyManager] {message}");
    }
}