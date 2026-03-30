using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class OsmDataLoader : MonoBehaviourPun
{
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
            Debug.LogError("[OsmDataLoader] ❌ GoogleMapLoader 없음!");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // ✅ MasterClient만 API 호출
            Debug.Log("[OsmDataLoader] MasterClient → OSM 데이터 로드 시작");
            StartCoroutine(LoadOsmData());
        }
        else
        {
            // ✅ 나머지는 MasterClient가 보내줄 때까지 대기
            Debug.Log("[OsmDataLoader] 클라이언트 → MasterClient 데이터 수신 대기");
        }
    }

    private IEnumerator LoadOsmData()
    {
        float lat   = mapLoader.Latitude;
        float lon   = mapLoader.Longitude;
        float delta = rangeKm / 111f;

        string query = $"[out:xml][timeout:60];" +
                       $"(" +
                       $"way[\"building\"]({lat-delta},{lon-delta},{lat+delta},{lon+delta});" +
                       $"way[\"highway\"]({lat-delta},{lon-delta},{lat+delta},{lon+delta});" +
                       $");" +
                       $"(._;>;);" +
                       $"out body;";

        foreach (string server in servers)
        {
            string url   = server + UnityWebRequest.EscapeURL(query);
            int    retry = 0;

            while (retry < maxRetry)
            {
                Debug.Log($"[OsmDataLoader] 서버: {server} ({retry+1}/{maxRetry})");

                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 60;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string text = request.downloadHandler.text;

                    if (!text.Contains("<osm"))
                    {
                        retry++;
                        yield return new WaitForSeconds(retryDelay);
                        continue;
                    }

                    Debug.Log($"[OsmDataLoader] ✅ 데이터 수신 성공 ({text.Length} bytes)");

                    // ✅ 로컬 처리
                    yield return StartCoroutine(ProcessOsmData(text));

                    // ✅ 다른 클라이언트에게 RPC로 전송
                    photonView.RPC("RPC_ReceiveOsmData", RpcTarget.Others, text);

                    yield break;
                }

                retry++;
                yield return new WaitForSeconds(retryDelay);
            }
        }

        Debug.LogError("[OsmDataLoader] ❌ 모든 서버 실패");
        // 실패해도 신호는 보냄
        OnMapReady?.Invoke();
    }

    [PunRPC]
    private void RPC_ReceiveOsmData(string osmText)
    {
        Debug.Log("[OsmDataLoader] ✅ MasterClient로부터 OSM 데이터 수신");
        StartCoroutine(ProcessOsmData(osmText));
    }

    private IEnumerator ProcessOsmData(string text)
    {
        // 1. 파싱
        OsmParser parser = new OsmParser();
        parser.Parse(text);
        Debug.Log($"[OsmDataLoader] 건물 {parser.Buildings.Count}개 파싱");

        // 2. 건물 생성
        BuildingGenerator generator = FindAnyObjectByType<BuildingGenerator>();
        if (generator != null)
        {
            generator.Initialize(mapLoader.Latitude, mapLoader.Longitude, 16);
            generator.GenerateBuildings(parser);
        }

        // 3. MeshCollider 활성화 대기
        yield return null;
        yield return null;
        yield return null;

        // 4. 도로 노드 캐싱
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
            Debug.Log($"[OsmDataLoader] 도로 {roadPositions.Count}개 캐싱 완료");
        }

        // 5. 완료 신호
        Debug.Log("[OsmDataLoader] ✅ 맵 준비 완료");
        OnMapReady?.Invoke();
    }
}