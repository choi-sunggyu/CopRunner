using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] private float buildingHeight = 10f;
    [SerializeField] private Material buildingMaterial;

    private float originLat;
    private float originLon;
    private float metersPerPixel;

    public void Initialize(float lat, float lon, int zoom)
    {
        originLat      = lat;
        originLon      = lon;
        metersPerPixel = OsmCoordConverter.MetersPerPixel(lat, zoom);
    }

    public void GenerateBuildings(OsmParser parser)
    {
        int count   = 0;
        int skipped = 0;

        foreach (List<long> nodeRefs in parser.Buildings)
        {
            List<Vector2> polygon = new();

            foreach (long id in nodeRefs)
            {
                if (!parser.Nodes.ContainsKey(id)) continue;

                Vector2 latLon = parser.Nodes[id];
                Vector2 pos    = OsmCoordConverter.LatLonToUnity(
                    latLon.x, latLon.y,
                    originLat, originLon,
                    metersPerPixel
                );
                polygon.Add(pos);
            }

            if (polygon.Count < 3) continue;

            if (polygon[0] == polygon[polygon.Count - 1])
                polygon.RemoveAt(polygon.Count - 1);

            if (polygon.Count < 3) continue;

            if (Mathf.Abs(PolygonArea(polygon)) < 1f)
            {
                skipped++;
                continue;
            }

            GameObject building = CreateBuilding(polygon, count);
            if (building == null)
            {
                skipped++;
                continue;
            }

            building.transform.SetParent(transform);
            count++;
        }

        Debug.Log($"[BuildingGenerator] ✅ 건물 {count}개 생성 / {skipped}개 스킵");
    }

    private GameObject CreateBuilding(List<Vector2> polygon, int index)
    {
        bool earSuccess;
        List<int> roofTris = EarClipping(polygon, out earSuccess);

        if (!earSuccess)
        {
            Debug.LogWarning($"[BuildingGenerator] Building_{index} Ear Clipping 실패 → 스킵");
            return null;
        }

        GameObject   go = new GameObject($"Building_{index}");
        MeshFilter   mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        // Building 레이어 설정
        go.layer = LayerMask.NameToLayer("Building");

        mr.material = buildingMaterial != null
            ? buildingMaterial
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));

        Mesh mesh     = BuildMesh(polygon, roofTris);
        mf.mesh       = mesh;

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh   = mesh;
        mc.convex       = false; // ← convex 해제

        return go;
    }

    private Mesh BuildMesh(List<Vector2> polygon, List<int> roofTris)
    {
        Mesh mesh = new Mesh();
        int n     = polygon.Count;

        // 바닥 + 천장 꼭짓점 (바깥면 + 안쪽면 각각)
        // 0~n-1        : 바깥 바닥
        // n~2n-1       : 바깥 천장
        // 2n~3n-1      : 안쪽 바닥
        // 3n~4n-1      : 안쪽 천장
        Vector3[] vertices = new Vector3[n * 4];
        for (int i = 0; i < n; i++)
        {
            vertices[i]          = new Vector3(polygon[i].x, 0,             polygon[i].y);
            vertices[i + n]      = new Vector3(polygon[i].x, buildingHeight, polygon[i].y);
            vertices[i + 2 * n]  = new Vector3(polygon[i].x, 0,             polygon[i].y);
            vertices[i + 3 * n]  = new Vector3(polygon[i].x, buildingHeight, polygon[i].y);
        }

        List<int> triangles = new();

        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;

            // 바깥 벽면
            triangles.Add(i);
            triangles.Add(i + n);
            triangles.Add(next + n);

            triangles.Add(i);
            triangles.Add(next + n);
            triangles.Add(next);

            // 안쪽 벽면 (삼각형 순서 반전 → 법선 방향 반대)
            triangles.Add(next + 2 * n);
            triangles.Add(next + 3 * n);
            triangles.Add(i + 3 * n);

            triangles.Add(next + 2 * n);
            triangles.Add(i + 3 * n);
            triangles.Add(i + 2 * n);
        }

        // 지붕 (바깥)
        foreach (int idx in roofTris)
            triangles.Add(idx + n);

        // 지붕 안쪽
        for (int i = 0; i < roofTris.Count; i += 3)
        {
            triangles.Add(roofTris[i + 2] + 3 * n);
            triangles.Add(roofTris[i + 1] + 3 * n);
            triangles.Add(roofTris[i]     + 3 * n);
        }

        mesh.vertices  = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    // out 파라미터로 성공 여부 반환
    private List<int> EarClipping(List<Vector2> polygon, out bool success)
    {
        List<int> result  = new();
        List<int> indices = new();

        for (int i = 0; i < polygon.Count; i++)
            indices.Add(i);

        // 폴리곤 방향 파악 (양수=CCW, 음수=CW)
        float area = PolygonArea(polygon);
        bool isCCW = area > 0;

        int safety = 0;

        while (indices.Count > 3)
        {
            safety++;
            if (safety > 10000)
            {
                success = false;
                return result;
            }

            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                if (!IsEar(polygon, indices, prev, curr, next, isCCW)) continue;

                result.Add(prev);
                result.Add(curr);
                result.Add(next);

                indices.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                success = false;
                return result;
            }
        }

        if (indices.Count == 3)
        {
            result.Add(indices[0]);
            result.Add(indices[1]);
            result.Add(indices[2]);
        }

        success = true;
        return result;
    }

    private bool IsEar(List<Vector2> polygon, List<int> indices,
                       int prev, int curr, int next, bool isCCW)
    {
        Vector2 a = polygon[prev];
        Vector2 b = polygon[curr];
        Vector2 c = polygon[next];

        float cross = Cross(a, b, c);

        // 방향에 따라 볼록 꼭짓점 판단 부호 반전
        if (isCCW && cross <= 0) return false;
        if (!isCCW && cross >= 0) return false;

        // 다른 점이 삼각형 안에 있는지 확인
        foreach (int idx in indices)
        {
            if (idx == prev || idx == curr || idx == next) continue;
            if (PointInTriangle(polygon[idx], a, b, c)) return false;
        }

        return true;
    }

    private float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(a, b, p);
        float d2 = Cross(b, c, p);
        float d3 = Cross(c, a, p);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private float PolygonArea(List<Vector2> polygon)
    {
        float area = 0f;
        int n      = polygon.Count;

        for (int i = 0; i < n; i++)
        {
            Vector2 curr = polygon[i];
            Vector2 next = polygon[(i + 1) % n];
            area += (curr.x * next.y) - (next.x * curr.y);
        }

        return area / 2f;
    }
}