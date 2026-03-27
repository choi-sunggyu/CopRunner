using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("메인메뉴 패널")]
    [SerializeField] private GameObject      mainMenuPanel;
    [SerializeField] private TMP_InputField  nicknameInput;
    [SerializeField] private TMP_InputField  roomNameInput;
    [SerializeField] private Button          createRoomButton;
    [SerializeField] private Button          joinRoomButton;
    [SerializeField] private Button          quickJoinButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("방 패널")]
    [SerializeField] private GameObject      roomPanel;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private Transform       playerListContent;
    [SerializeField] private GameObject      playerEntryPrefab;
    [SerializeField] private Button          readyButton;
    [SerializeField] private Button          startButton;
    [SerializeField] private Button          leaveRoomButton;

    [Header("역할 선택")]
    [SerializeField] private Button          copButton;
    [SerializeField] private Button          robberButton;
    [SerializeField] private TextMeshProUGUI selectedRoleText;

    private string selectedRole = "미선택";

    private bool isReady = false;
    private List<GameObject> playerEntries = new(); // 항목 직접 관리

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
        if (nicknameInput != null) nicknameInput.characterLimit = 10;
        if (roomNameInput  != null) roomNameInput.characterLimit  = 20;

        createRoomButton?.onClick.AddListener(OnCreateRoom);
        joinRoomButton?.onClick.AddListener(OnJoinRoom);
        quickJoinButton?.onClick.AddListener(OnQuickJoin);
        readyButton?.onClick.AddListener(OnReadyToggle);
        startButton?.onClick.AddListener(OnStartGame);
        leaveRoomButton?.onClick.AddListener(OnLeaveRoom);
        copButton?.onClick.AddListener(() => OnRoleSelect("경찰"));
        robberButton?.onClick.AddListener(() => OnRoleSelect("도둑"));

        ShowMainMenu();
        SetStatus("서버 연결 중...");
    }

    // ── 패널 전환 ──────────────────────────────

    public void ShowMainMenu()
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

    // 게임 시작 시 모든 UI 숨기기 (PC1, PC2 모두 호출)
    public void HideAllLobbyUI()
    {
        mainMenuPanel?.SetActive(false);
        roomPanel?.SetActive(false);

        // 플레이어 목록 항목 전부 삭제
        foreach (GameObject entry in playerEntries)
            if (entry != null) DestroyImmediate(entry);
        playerEntries.Clear();
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

        TextMeshProUGUI btnText = readyButton?.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = isReady ? "레디 취소" : "✅ 레디";

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

        // 경찰 1명 이상 있는지 확인
        bool hasCop = false;
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            string role = NetworkManager.Instance.GetPlayerRole(player);
            if (role == "경찰") { hasCop = true; break; }
        }

        if (!hasCop)
        {
            SetStatus("경찰이 최소 1명 필요합니다!");
            return;
        }

        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
            pv.RPC("RPC_StartGame", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_StartGame()
    {
        Debug.Log("[LobbyManager] RPC_StartGame 수신 → UI 숨기고 게임 시작");
        HideAllLobbyUI();
        UIManager.Instance?.HideLobbyUI();
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

    // 방 UI 갱신 — 기존 항목 즉시 삭제 방식
    public void RefreshRoomUI()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (roomNameText != null)
            roomNameText.text = $"방: {PhotonNetwork.CurrentRoom.Name}";

        if (playerListContent != null)
        {
            // 기존 항목 즉시 삭제 (Destroy 대신 DestroyImmediate 사용)
            foreach (GameObject entry in playerEntries)
                if (entry != null) DestroyImmediate(entry);
            playerEntries.Clear();

            // 새 항목 생성
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                GameObject entry = Instantiate(playerEntryPrefab, playerListContent);
                playerEntries.Add(entry);

                TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    object isReadyObj;
                    player.CustomProperties.TryGetValue(NetworkManager.READY_KEY, out isReadyObj);
                    bool ready = isReadyObj != null && (bool)isReadyObj;

                    // 역할 가져오기
                    string role = NetworkManager.Instance.GetPlayerRole(player);

                    string masterTag = player.IsMasterClient ? "[방장] " : "";
                    string readyTag  = ready ? " ✅" : " ⬜";
                    string roleTag   = role != "미선택" ? $" [{role}]" : " [미선택]";

                    text.text = $"{masterTag}{player.NickName}{roleTag}{readyTag}";
                }
            }
        }

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

    private void OnRoleSelect(string role)
    {
        selectedRole = role;
        NetworkManager.Instance.SetRole(role);

        // 버튼 색상으로 선택 상태 표시
        if (copButton != null)
        {
            ColorBlock cb = copButton.colors;
            cb.normalColor = role == "경찰"
                ? new Color(0.2f, 0.4f, 0.8f)  // 선택됨 (파랑)
                : new Color(0.5f, 0.5f, 0.5f); // 미선택 (회색)
            copButton.colors = cb;
        }

        if (robberButton != null)
        {
            ColorBlock cb = robberButton.colors;
            cb.normalColor = role == "도둑"
                ? new Color(0.8f, 0.2f, 0.2f)  // 선택됨 (빨강)
                : new Color(0.5f, 0.5f, 0.5f); // 미선택 (회색)
            robberButton.colors = cb;
        }

        if (selectedRoleText != null)
            selectedRoleText.text = $"선택: {role}";

        RefreshRoomUI();
    }
}