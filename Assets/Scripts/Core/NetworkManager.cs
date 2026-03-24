using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("방 설정")]
    [SerializeField] private int   maxPlayersPerRoom = 8;
    [SerializeField] private string gameVersion      = "1.0";

    // 싱글톤
    public static NetworkManager Instance { get; private set; }

    // 상태
    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsInRoom    => PhotonNetwork.InRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 씬 전환 시 자동 동기화
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    // Photon 서버 연결
    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected) return;

        Debug.Log("[NetworkManager] Photon 서버 연결 중...");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    // 방 생성
    public void CreateRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected) return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers    = (byte)maxPlayersPerRoom,
            IsVisible     = true,
            IsOpen        = true
        };

        PhotonNetwork.CreateRoom(roomName, options);
        Debug.Log($"[NetworkManager] 방 생성 중: {roomName}");
    }

    // 방 참가
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected) return;

        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"[NetworkManager] 방 참가 중: {roomName}");
    }

    // 빠른 참가 (아무 방이나)
    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnected) return;

        PhotonNetwork.JoinRandomRoom();
        Debug.Log("[NetworkManager] 랜덤 방 참가 중...");
    }

    // 방 나가기
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        Debug.Log("[NetworkManager] 방 나감");
    }

    //  ── Photon 콜백 ──────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkManager] ✅ Photon 서버 연결 성공!");
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
        Debug.Log($"[NetworkManager] ✅ 방 참가 완료! " +
                  $"플레이어: {PhotonNetwork.CurrentRoom.PlayerCount}명");

        // 방장이면 게임 시작 준비
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[NetworkManager] 방장으로 입장 — 게임 시작 권한 있음");
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[NetworkManager] 참가할 방 없음 — 새 방 생성");
        CreateRoom($"Room_{Random.Range(1000, 9999)}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[NetworkManager] 새 플레이어 입장: {newPlayer.NickName} " +
                  $"(총 {PhotonNetwork.CurrentRoom.PlayerCount}명)");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[NetworkManager] 플레이어 퇴장: {otherPlayer.NickName}");
    }
}