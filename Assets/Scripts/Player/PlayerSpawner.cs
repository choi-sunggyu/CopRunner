using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private float spawnHeight = 1f;
    [SerializeField] private int   maxAttempts = 30;

    private List<Vector2> cachedRoadPoints = new();
    private bool isSpawned = false;

    // OSM 로드 완료 시 도로 데이터 캐싱
    public void CacheRoadPoints(List<Vector2> roadPoints)
    {
        cachedRoadPoints = roadPoints;
        Debug.Log($"[PlayerSpawner] 도로 데이터 캐싱: {roadPoints.Count}개");
    }

    // RoundManager에서 게임 시작 시 호출
    public void SpawnOnRoad(List<Vector2> roadPoints)
    {
        if (isSpawned) return;
        isSpawned = true;
        StartCoroutine(TrySpawn(roadPoints.Count > 0 ? roadPoints : cachedRoadPoints));
    }

    public void ResetSpawn()
    {
        isSpawned = false;
    }

    private IEnumerator TrySpawn(List<Vector2> roadPoints)
    {
        if (roadPoints == null || roadPoints.Count == 0)
        {
            Debug.LogWarning("[PlayerSpawner] ⚠️ 도로 데이터 없음 → 원점 스폰");
            SpawnPlayer(new Vector3(0, spawnHeight, 0));
            yield break;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 roadPoint = roadPoints[Random.Range(0, roadPoints.Count)];
            Vector3 spawnPos  = new Vector3(roadPoint.x, spawnHeight, roadPoint.y);

            Collider[] overlaps = Physics.OverlapSphere(spawnPos, 0.5f);
            bool isBlocked = false;

            foreach (Collider col in overlaps)
            {
                if (col.gameObject.name.StartsWith("Building_"))
                {
                    isBlocked = true;
                    break;
                }
            }

            if (!isBlocked)
            {
                SpawnPlayer(spawnPos);
                Debug.Log($"[PlayerSpawner] ✅ 스폰 성공 (시도 {attempt + 1}): {spawnPos}");
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[PlayerSpawner] ⚠️ 안전한 위치 못 찾음 → 원점 스폰");
        SpawnPlayer(new Vector3(0, spawnHeight, 0));
    }

    private void SpawnPlayer(Vector3 position)
    {
        GameObject player = PhotonNetwork.Instantiate(
            "Player",
            position,
            Quaternion.identity
        );

        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.SetTarget(player.transform);

        Debug.Log($"[PlayerSpawner] 플레이어 생성: {player.name}");
    }
}