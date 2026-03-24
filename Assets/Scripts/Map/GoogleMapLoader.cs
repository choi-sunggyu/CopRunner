using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GoogleMapLoader : MonoBehaviour
{
    [Header("지도 설정")]
    [SerializeField] private float  latitude   = 37.5665f;
    [SerializeField] private float  longitude  = 126.9780f;
    [SerializeField] private int    zoom       = 16;
    [SerializeField] private int    mapWidth   = 600;
    [SerializeField] private int    mapHeight  = 400;
    [SerializeField] private string mapType    = "roadmap"; // roadmap, satellite, hybrid

    [Header("렌더링 대상")]
    [SerializeField] private Renderer targetRenderer;

    // 외부에서 좌표 변경 가능
    public float Latitude  { get => latitude;  set => latitude  = value; }
    public float Longitude { get => longitude; set => longitude = value; }

    private void Start()
    {
        StartCoroutine(LoadMap());
    }

    private IEnumerator LoadMap()
    {
        string url = BuildUrl();
        Debug.Log($"[GoogleMapLoader] 요청 URL: {url}");

        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            ApplyTexture(texture);
            Debug.Log("[GoogleMapLoader] ✅ 지도 로드 성공!");
        }
        else
        {
            Debug.LogError($"[GoogleMapLoader] ❌ 실패: {request.error}");
            Debug.LogError($"[GoogleMapLoader] 응답: {request.downloadHandler.text}");
            LoadTempMap();
        }
    }

    private string BuildUrl()
    {
        return $"https://maps.googleapis.com/maps/api/staticmap" +
               $"?center={latitude},{longitude}" +
               $"&zoom={zoom}" +
               $"&size={mapWidth}x{mapHeight}" +
               $"&maptype={mapType}" +
               $"&key={ApiKeys.GoogleMapsApiKey}";
    }

    private void ApplyTexture(Texture2D texture)
    {
        if (targetRenderer != null)
            targetRenderer.material.mainTexture = texture;
        else
            Debug.LogWarning("[GoogleMapLoader] targetRenderer 없음!");
    }

    // 실패 시 임시 텍스처
    private void LoadTempMap()
    {
        Texture2D temp   = new Texture2D(mapWidth, mapHeight);
        Color[]   pixels = new Color[mapWidth * mapHeight];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(0.6f, 0.8f, 0.6f);
        temp.SetPixels(pixels);
        temp.Apply();

        if (targetRenderer != null)
            targetRenderer.material.mainTexture = temp;

        Debug.LogWarning("[GoogleMapLoader] 임시 텍스처 사용 중");
    }

    // 외부에서 좌표 변경 후 재로드
    public void ReloadMap(float lat, float lon)
    {
        latitude  = lat;
        longitude = lon;
        StartCoroutine(LoadMap());
    }
}