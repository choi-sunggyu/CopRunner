using UnityEngine;
using Photon.Pun;

// PhotonView와 함께 작동하는 위치 동기화
public class PlayerSync : MonoBehaviourPun, IPunObservable
{
    private Vector3    networkPosition;
    private Quaternion networkRotation;
    private float      lag;

    [SerializeField] private float smoothSpeed = 15f;

    private void Update()
    {
        // 내 캐릭터가 아니면 네트워크 위치로 부드럽게 이동
        if (!photonView.IsMine)
        {
            // 거리 멀면 즉시 이동, 가까우면 부드럽게
            float dist = Vector3.Distance(transform.position, networkPosition);
            if (dist > 5f)
                transform.position = networkPosition;
            else
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    networkPosition,
                    smoothSpeed * Time.deltaTime
                );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                networkRotation,
                smoothSpeed * 100f * Time.deltaTime
            );
        }
    }

    // 위치 데이터 송수신
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 위치를 다른 플레이어에게 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 다른 플레이어 위치 수신
            networkPosition = (Vector3)    stream.ReceiveNext();
            networkRotation = (Quaternion) stream.ReceiveNext();

            // 네트워크 지연 보정
            lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += (networkPosition - transform.position) * lag;
        }
    }
}