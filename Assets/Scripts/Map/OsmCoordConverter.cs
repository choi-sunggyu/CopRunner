using UnityEngine;

public static class OsmCoordConverter
{
    // 줌 레벨 16 기준 1픽셀 = 몇 미터인지
    // 위도에 따라 달라짐 (Mercator 특성)
    public static float MetersPerPixel(float latitude, int zoom)
    {
        return 156543.03392f 
               * Mathf.Cos(latitude * Mathf.Deg2Rad) 
               / Mathf.Pow(2, zoom);
    }

    // 위도/경도 → Unity XZ 좌표
    public static Vector2 LatLonToUnity(
        float lat, float lon,
        float originLat, float originLon,
        float metersPerPixel)
    {
        float x = (lon - originLon) * 111320f 
                  * Mathf.Cos(originLat * Mathf.Deg2Rad);
        float z = (lat - originLat) * 110540f;

        return new Vector2(x / metersPerPixel, z / metersPerPixel);
    }
}