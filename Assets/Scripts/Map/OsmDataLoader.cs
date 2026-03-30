using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class OsmDataLoader : MonoBehaviour
{
    // ✅ 맵 생성 완료 신호 — NetworkReadyTracker가 구독
    public static event System.Action OnMapReady;

    [SerializeField] private GoogleMapLoader mapLoader;
    [SerializeField] private float rangeKm    = 0.1f;
    [SerializeField] private int   maxRetry   = 3;
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

        string query = $"[out:xml][timeout:60];" +
                       $"(" +
                       $"way[\"building\"]({south},{west},{north},{east});" +
                       $"way[\"highway\"]({south},{west},{north},{east});" +
                       $");" +
                       $"(._;>;);" +
                       $"out body;";

        foreach (string server in servers)
        {
            string url   = server + UnityWebRequest.EscapeURL(query);
            int    retry = 0;

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

                    // 1. 파싱
                    OsmParser parser = new OsmParser();
                    parser.Parse(text);
                    Debug.Log($"[OsmDataLoader] 건물 {parser.Buildings.Count}개 파싱 완료");

                    // 2. 건물 생성
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

                    // ✅ 건물 MeshCollider 활성화 대기 (1프레임으로 부족할 수 있어서 3프레임 대기)
                    yield return null;
                    yield return null;
                    yield return null;

                    // 3. 도로 노드 캐싱
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

                        spawner.CacheRoadPoints(roadPositions);
                        Debug.Log($"[OsmDataLoader] 도로 노드 {roadPositions.Count}개 캐싱 완료");
                    }
                    else
                    {
                        Debug.LogError("[OsmDataLoader] ❌ PlayerSpawner를 찾을 수 없음!");
                    }

                    // ✅ 모든 준비 완료 → 신호 발송
                    Debug.Log("[OsmDataLoader] ✅ 맵 준비 완료 신호 발송");
                    OnMapReady?.Invoke();

                    yield break;
                }

                retry++;
                Debug.LogWarning($"[OsmDataLoader] ⚠️ {request.error} → {retryDelay}초 후 재시도");
                yield return new WaitForSeconds(retryDelay);
            }

            Debug.LogWarning($"[OsmDataLoader] 서버 포기 → 다음 서버 시도");
        }

        // ✅ 모든 서버 실패해도 신호는 보내야 게임이 멈추지 않음
        Debug.LogError("[OsmDataLoader] ❌ 모든 서버 실패. 빈 맵으로 진행");
        OnMapReady?.Invoke();
    }
}