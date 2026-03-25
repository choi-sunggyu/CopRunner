using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private float spawnHeight = 1f;
    [SerializeField] private int   maxAttempts = 30;

    public void SpawnOnRoad(List<Vector2> roadPoints)
    {
        StartCoroutine(TrySpawn(roadPoints));
    }

    private IEnumerator TrySpawn(List<Vector2> roadPoints)
    {
        // 기존 Player 오브젝트 찾기
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[PlayerSpawner] ❌ Player 태그 오브젝트 없음!");
            yield break;
        }

        if (roadPoints == null || roadPoints.Count == 0)
        {
            Debug.LogWarning("[PlayerSpawner] ⚠️ 도로 데이터 없음 → 원점 스폰");
            MovePlayer(player, new Vector3(0, spawnHeight, 0));
            yield break;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 roadPoint = roadPoints[Random.Range(0, roadPoints.Count)];
            Vector3 spawnPos  = new Vector3(roadPoint.x, spawnHeight, roadPoint.y);

            // 건물 겹침 체크
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
                MovePlayer(player, spawnPos);
                Debug.Log($"[PlayerSpawner] ✅ 스폰 성공 (시도 {attempt + 1}): {spawnPos}");
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[PlayerSpawner] ⚠️ 안전한 위치 못 찾음 → 원점 스폰");
        MovePlayer(player, new Vector3(0, spawnHeight, 0));
    }

    private void MovePlayer(GameObject player, Vector3 position)
    {
        // Rigidbody 있으면 velocity 초기화
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = position;
    }
}