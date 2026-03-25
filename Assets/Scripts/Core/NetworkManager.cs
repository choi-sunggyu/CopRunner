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

    // 레디 상태 키
    public const string READY_KEY = "IsReady";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
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
        Debug.Log($"[NetworkManager] 방 생성 중: {roomName}");
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

    // 레디 상태 설정
    public void SetReady(bool isReady)
    {
        Hashtable props = new Hashtable { { READY_KEY, isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[NetworkManager] 레디 상태: {isReady}");
    }

    // 모든 플레이어 레디 여부 확인
    public bool IsAllReady()
    {
        if (PhotonNetwork.CurrentRoom == null) return false;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return false;

        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            object isReady;
            if (!player.CustomProperties.TryGetValue(READY_KEY, out isReady))
                return false;
            if (!(bool)isReady)
                return false;
        }
        return true;
    }

    // ── Photon 콜백 ──────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkManager] ✅ Photon 서버 연결 성공!");
        LobbyManager.Instance?.OnConnected();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[NetworkManager] ❌ 연결 끊김: {cause}");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[NetworkManager] ✅ 방 생성 완료: {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkManager] ✅ 방 참가 완료! 플레이어: {PhotonNetwork.CurrentRoom.PlayerCount}명");
        LobbyManager.Instance?.OnJoinedRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[NetworkManager] 참가할 방 없음 — 새 방 생성");
        CreateRoom($"Room_{Random.Range(1000, 9999)}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[NetworkManager] 새 플레이어 입장: {newPlayer.NickName}");
        LobbyManager.Instance?.RefreshRoomUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[NetworkManager] 플레이어 퇴장: {otherPlayer.NickName}");
        LobbyManager.Instance?.RefreshRoomUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 레디 상태 변경 시 UI 갱신
        if (changedProps.ContainsKey(READY_KEY))
            LobbyManager.Instance?.RefreshRoomUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[NetworkManager] 방장 변경: {newMasterClient.NickName}");
        LobbyManager.Instance?.RefreshRoomUI();
    }
}