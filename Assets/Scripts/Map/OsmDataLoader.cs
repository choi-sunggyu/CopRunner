using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class OsmDataLoader : MonoBehaviour
{
    [SerializeField] private GoogleMapLoader mapLoader;
    [SerializeField] private float rangeKm = 0.1f;
    [SerializeField] private int maxRetry = 3;
    [SerializeField] private float retryDelay = 5f;

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

        // 건물 + 도로 함께 요청
        string query = $"[out:xml][timeout:60];" +
                       $"(" +
                       $"way[\"building\"]({south},{west},{north},{east});" +
                       $"way[\"highway\"]({south},{west},{north},{east});" +
                       $");" +
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
                request.timeout = 60;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string text = request.downloadHandler.text;
                    Debug.Log($"[OsmDataLoader] 데이터 길이: {text.Length}");
                    Debug.Log($"[OsmDataLoader] 내용 앞부분: {text[..Mathf.Min(500, text.Length)]}");

                    if (!text.Contains("<osm"))
                    {
                        retry++;
                        Debug.LogWarning($"[OsmDataLoader] ⚠️ OSM 데이터 아님 → {retryDelay}초 후 재시도");
                        yield return new WaitForSeconds(retryDelay);
                        continue;
                    }

                    OsmParser parser = new OsmParser();
                    parser.Parse(text);
                    Debug.Log($"[OsmDataLoader] 건물 {parser.Buildings.Count}개 파싱 완료");

                    // 건물 생성 (건물보다 스폰이 먼저 되면 충돌 체크가 안 됨)
                    BuildingGenerator generator = FindAnyObjectByType<BuildingGenerator>();
                    if (generator != null)
                    {
                        generator.Initialize(mapLoader.Latitude, mapLoader.Longitude, 16);
                        generator.GenerateBuildings(parser);
                    }
                    else
                    {
                        Debug.LogError("[OsmDataLoader] ❌ BuildingGenerator를 찾을 수 없음!");
                    }

                    // 건물 생성 후 한 프레임 대기 → 콜라이더 활성화 기다림
                    yield return null;

                    // 플레이어 스폰
                    PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();
                    if (spawner != null)
                    {
                        List<Vector2> roadPositions = new();
                        foreach (long id in parser.RoadNodes)
                        {
                            if (!parser.Nodes.ContainsKey(id)) continue;
                            Vector2 latLon = parser.Nodes[id];
                            Vector2 pos    = OsmCoordConverter.LatLonToUnity(
                                latLon.x, latLon.y,
                                mapLoader.Latitude, mapLoader.Longitude,
                                OsmCoordConverter.MetersPerPixel(mapLoader.Latitude, 16)
                            );
                            roadPositions.Add(pos);
                        }

                        Debug.Log($"[OsmDataLoader] 도로 노드 {roadPositions.Count}개 전달");
                        spawner.SpawnOnRoad(roadPositions);
                    }
                    else
                    {
                        Debug.LogError("[OsmDataLoader] ❌ PlayerSpawner를 찾을 수 없음!");
                    }

                    yield break;
                }

                retry++;
                Debug.LogWarning($"[OsmDataLoader] ⚠️ {request.error} → {retryDelay}초 후 재시도");
                yield return new WaitForSeconds(retryDelay);
            }

            Debug.LogWarning($"[OsmDataLoader] 서버 포기 → 다음 서버 시도");
        }

        Debug.LogError("[OsmDataLoader] ❌ 모든 서버 실패.");
    }
}