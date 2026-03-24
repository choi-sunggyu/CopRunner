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
        int count = 0;

        foreach (List<long> nodeRefs in parser.Buildings)
        {
            // 1. node id → Unity XZ 좌표 변환
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

            // 2. 메쉬 생성
            GameObject building = CreateBuilding(polygon, count);
            building.transform.SetParent(transform);
            count++;
        }

        Debug.Log($"[BuildingGenerator] ✅ 건물 {count}개 생성 완료");
    }

    private GameObject CreateBuilding(List<Vector2> polygon, int index)
    {
        GameObject go  = new GameObject($"Building_{index}");
        MeshFilter mf  = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        mr.material = buildingMaterial != null
            ? buildingMaterial
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));

        mf.mesh = BuildMesh(polygon);
        return go;
    }

    private Mesh BuildMesh(List<Vector2> polygon)
    {
        Mesh mesh      = new Mesh();
        int n          = polygon.Count - 1; // 마지막은 첫점과 동일하므로 제외

        // 꼭짓점: 바닥(n개) + 천장(n개)
        Vector3[] vertices  = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            vertices[i]     = new Vector3(polygon[i].x, 0,             polygon[i].y);
            vertices[i + n] = new Vector3(polygon[i].x, buildingHeight, polygon[i].y);
        }

        // 삼각형: 벽면(n개 면 × 2삼각형)
        List<int> triangles = new();
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;

            // 벽 앞면
            triangles.Add(i);
            triangles.Add(i + n);
            triangles.Add(next + n);

            triangles.Add(i);
            triangles.Add(next + n);
            triangles.Add(next);
        }

        mesh.vertices  = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}