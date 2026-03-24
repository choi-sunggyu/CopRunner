using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OsmDataLoader : MonoBehaviour
{
    [SerializeField] private GoogleMapLoader mapLoader;
    [SerializeField] private float rangeKm = 0.1f;
    [SerializeField] private int maxRetry = 3;
    [SerializeField] private float retryDelay = 5f;

    // 여러 서버 순서대로 시도
    private string[] servers = new string[]
    {
        "https://overpass.kumi.systems/api/interpreter?data=",
        "https://overpass-api.de/api/interpreter?data=",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter?data="
    };

    private void Start()
    {
        if (mapLoader == null)
            mapLoader = FindAnyObjectByType<GoogleMapLoader>();

        if (mapLoader == null)
        {
            Debug.LogError("[OsmDataLoader] ❌ GoogleMapLoader를 찾을 수 없음!");
            return;
        }

        StartCoroutine(LoadOsmData());
    }

    private IEnumerator LoadOsmData()
    {
        float lat = mapLoader.Latitude;
        float lon = mapLoader.Longitude;

        float delta = rangeKm / 111f;

        float south = lat - delta;
        float north = lat + delta;
        float west  = lon - delta;
        float east  = lon + delta;

        // 건물만 먼저 (쿼리 단순화)
        string query = $"[out:xml][timeout:60];" +
                       $"way[\"building\"]({south},{west},{north},{east});" +
                       $"(._;>;);" +
                       $"out body;";

        foreach (string server in servers)
        {
            string url = server + UnityWebRequest.EscapeURL(query);
            int retry = 0;

            while (retry < maxRetry)
            {
                Debug.Log($"[OsmDataLoader] 서버: {server} (시도 {retry + 1}/{maxRetry})");

                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 60; // 30 → 60초
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[OsmDataLoader] ✅ 데이터 수신 완료");
                    
                    // 파싱 시작
                    OsmParser parser = new OsmParser();
                    parser.Parse(request.downloadHandler.text);

                    Debug.Log($"[OsmDataLoader] 건물 {parser.Buildings.Count}개 파싱 완료");
                    yield break;

                }

                retry++;
                Debug.LogWarning($"[OsmDataLoader] ⚠️ {request.error} → {retryDelay}초 후 재시도");
                yield return new WaitForSeconds(retryDelay);
            }

            Debug.LogWarning($"[OsmDataLoader] 서버 포기 → 다음 서버 시도");
        }

        Debug.LogError("[OsmDataLoader] ❌ 모든 서버 실패. 브라우저로 직접 확인해봐.");
        Debug.LogError($"[OsmDataLoader] 테스트 URL: https://overpass-api.de/api/interpreter?data={UnityWebRequest.EscapeURL(query)}");
    }
}