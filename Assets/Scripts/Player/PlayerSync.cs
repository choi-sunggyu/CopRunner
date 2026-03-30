using UnityEngine;
using Photon.Pun;

public class PlayerSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private float smoothSpeed = 15f;

    private Vector3    networkPosition;
    private Vector3    prevNetworkPosition;
    private Quaternion networkRotation;
    private bool       firstReceive = true;

    private void Update()
    {
        if (photonView.IsMine) return;

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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            prevNetworkPosition = firstReceive ? transform.position : networkPosition;
            networkPosition     = (Vector3)stream.ReceiveNext();
            networkRotation     = (Quaternion)stream.ReceiveNext();
            firstReceive        = false;

            // 수신된 패킷은 lag초 전 위치 — 예상 속도로 현재 위치를 추정
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            float dt  = PhotonNetwork.SerializationRate > 0
                ? 1f / PhotonNetwork.SerializationRate
                : 0.1f;

            Vector3 estimatedVelocity = (networkPosition - prevNetworkPosition) / dt;
            networkPosition += estimatedVelocity * lag;
        }
    }
}
