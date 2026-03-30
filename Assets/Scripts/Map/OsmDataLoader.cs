using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class OsmDataLoader : MonoBehaviourPun
{
    public static event System.Action OnMapReady;

    [SerializeField] private GoogleMapLoader   mapLoader;
    [SerializeField] private BuildingGenerator buildingGenerator;
    [SerializeField] private PlayerSpawner     playerSpawner;
    [SerializeField] private float rangeKm    = 0.1f;
    [SerializeField] private int   maxRetry   = 3;
    [SerializeField] private float retryDelay = 5f;

    private static readonly string[] Servers =
    {
        "https://overpass.kumi.systems/api/interpreter?data=",
        "https://overpass-api.de/api/interpreter?data=",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter?data=",
    };

    private void Start()
    {
        if (mapLoader         == null) mapLoader         = FindAnyObjectByType<GoogleMapLoader>();
        if (buildingGenerator == null) buildingGenerator = FindAnyObjectByType<BuildingGenerator>();
        if (playerSpawner     == null) playerSpawner     = FindAnyObjectByType<PlayerSpawner>();

        if (mapLoader == null)
        {
            Debug.LogError("[OsmDataLoader] GoogleMapLoader 없음!");
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

        foreach (string server in Servers)
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

                    Debug.Log($"[OsmDataLoader] 데이터 수신 성공 ({text.Length} bytes)");

                    yield return StartCoroutine(ProcessOsmData(text));
                    photonView.RPC("RPC_ReceiveOsmData", RpcTarget.Others, text);
                    yield break;
                }

                retry++;
                yield return new WaitForSeconds(retryDelay);
            }
        }

        Debug.LogError("[OsmDataLoader] 모든 서버 실패 — 맵 없이 진행");
        OnMapReady?.Invoke();
    }

    [PunRPC]
    private void RPC_ReceiveOsmData(string osmText)
    {
        Debug.Log("[OsmDataLoader] MasterClient로부터 OSM 데이터 수신");
        StartCoroutine(ProcessOsmData(osmText));
    }

    private IEnumerator ProcessOsmData(string text)
    {
        var parser = new OsmParser();
        parser.Parse(text);

        if (buildingGenerator != null)
        {
            buildingGenerator.Initialize(mapLoader.Latitude, mapLoader.Longitude, 16);
            buildingGenerator.GenerateBuildings(parser);
        }

        // MeshCollider 빌드 대기 (3프레임)
        yield return null;
        yield return null;
        yield return null;

        if (playerSpawner != null)
        {
            float   mpp           = OsmCoordConverter.MetersPerPixel(mapLoader.Latitude, 16);
            var     roadPositions = new List<Vector2>(parser.RoadNodes.Count);

            foreach (long id in parser.RoadNodes)
            {
                if (!parser.Nodes.ContainsKey(id)) continue;
                Vector2 latLon = parser.Nodes[id];
                roadPositions.Add(OsmCoordConverter.LatLonToUnity(
                    latLon.x, latLon.y,
                    mapLoader.Latitude, mapLoader.Longitude,
                    mpp
                ));
            }

            playerSpawner.CacheRoadPoints(roadPositions);
            Debug.Log($"[OsmDataLoader] 도로 노드 {roadPositions.Count}개 캐싱 완료");
        }

        Debug.Log("[OsmDataLoader] 맵 준비 완료");
        OnMapReady?.Invoke();
    }
}