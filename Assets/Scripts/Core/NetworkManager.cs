using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("방 설정")]
    [SerializeField] private int    maxPlayersPerRoom = 8;
    [SerializeField] private string gameVersion       = "1.0";

    public static NetworkManager Instance { get; private set; }

    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsInRoom    => PhotonNetwork.InRoom;

    public const string READY_KEY     = "IsReady";
    public const string ROLE_KEY      = "Role";
    public const string MAP_READY_KEY = "IsMapReady";  // ✅ 추가

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;

        // ✅ OsmDataLoader 맵 완료 이벤트 구독
        OsmDataLoader.OnMapReady += OnLocalMapReady;
    }

    private void OnDestroy()
    {
        OsmDataLoader.OnMapReady -= OnLocalMapReady;
    }

    // ✅ 내 클라이언트 맵 완료 → Photon에 신호
    private void OnLocalMapReady()
    {
        Hashtable props = new Hashtable { { MAP_READY_KEY, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log("[NetworkManager] ✅ 맵 준비 완료 신호 전송");
    }

    // ✅ MasterClient가 호출 — 전원 맵 준비됐는지 확인
    public bool AllPlayersMapReady()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            bool ready = (bool)(player.CustomProperties[MAP_READY_KEY] ?? false);
            if (!ready) return false;
        }
        return true;
    }

    // ✅ 라운드 재시작 전 초기화
    public void ResetMapReadyState()
    {
        Hashtable props = new Hashtable { { MAP_READY_KEY, false } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected) return;
        Debug.Log("[NetworkManager] Photon 서버 연결 중...");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void SetNickname(string nickname)
    {
        PhotonNetwork.NickName = nickname;
    }

    public void CreateRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected) return;
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayersPerRoom,
            IsVisible  = true,
            IsOpen     = true
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected) return;
        PhotonNetwork.JoinRoom(roomName);
    }

    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnected) return;
        PhotonNetwork.JoinRandomRoom();
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void SetReady(bool isReady)
    {
        Hashtable props = new Hashtable { { READY_KEY, isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool IsAllReady()
    {
        if (PhotonNetwork.CurrentRoom == null) return false;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return false;

        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!player.CustomProperties.TryGetValue(READY_KEY, out object isReady))
                return false;
            if (!(bool)isReady)
                return false;
        }
        return true;
    }

    public void SetRole(PlayerRole role)
    {
        Hashtable props = new Hashtable { { ROLE_KEY, (int)role } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[NetworkManager] 역할 설정: {role}");
    }

    public PlayerRole GetPlayerRole(Player player)
    {
        if (player.CustomProperties.TryGetValue(ROLE_KEY, out object role) && role is int roleInt)
            return (PlayerRole)roleInt;
        return PlayerRole.None;
    }

    // ── Photon 콜백 ──────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkManager] ✅ Photon 연결 성공");
        LobbyManager.Instance?.OnConnected();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[NetworkManager] ❌ 연결 끊김: {cause}");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[NetworkManager] ✅ 방 생성: {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkManager] ✅ 방 참가 | 인원: {PhotonNetwork.CurrentRoom.PlayerCount}명");
        LobbyManager.Instance?.OnJoinedRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom($"Room_{Random.Range(1000, 9999)}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[NetworkManager] 입장: {newPlayer.NickName}");
        LobbyManager.Instance?.RefreshRoomUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[NetworkManager] 퇴장: {otherPlayer.NickName}");
        LobbyManager.Instance?.RefreshRoomUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(READY_KEY) || changedProps.ContainsKey(ROLE_KEY))
            LobbyManager.Instance?.RefreshRoomUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[NetworkManager] 방장 변경: {newMasterClient.NickName}");
        LobbyManager.Instance?.RefreshRoomUI();
    }
}