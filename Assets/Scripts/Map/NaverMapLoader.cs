using UnityEngine;

public class NaverMapLoader : MonoBehaviour
{
    [Header("임시 지도 설정")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color mapColor = new Color(0.6f, 0.8f, 0.6f);

    private void Start()
    {
        LoadTempMap();
    }

    private void LoadTempMap()
    {
        // 임시 단색 텍스처 생성 (나중에 실제 API로 교체)
        Texture2D tempTexture = new Texture2D(600, 400);
        Color[] pixels = new Color[600 * 400];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = mapColor;

        tempTexture.SetPixels(pixels);
        tempTexture.Apply();

        if (targetRenderer != null)
        {
            targetRenderer.material.mainTexture = tempTexture;
            Debug.Log("[NaverMapLoader] 임시 지도 텍스처 로드 완료 (API 연동 전)");
        }
        else
        {
            Debug.LogWarning("[NaverMapLoader] targetRenderer가 없습니다!");
        }
    }

    // 나중에 실제 API 연동 시 이 메서드만 교체하면 됨
    public void ReloadMap(double lat, double lon)
    {
        Debug.Log($"[NaverMapLoader] 좌표 업데이트: {lat}, {lon} (API 연동 후 활성화)");
    }
}