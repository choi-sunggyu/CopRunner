using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class OsmParser
{
    public Dictionary<long, Vector2> Nodes     = new();
    public List<List<long>>          Buildings  = new();
    public List<long>                RoadNodes  = new(); // 도로 노드 추가

    public void Parse(string xml)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);

        // 1. 노드 파싱
        foreach (XmlNode node in doc.SelectNodes("/osm/node"))
        {
            long  id  = long.Parse(node.Attributes["id"].Value);
            float lat = float.Parse(node.Attributes["lat"].Value);
            float lon = float.Parse(node.Attributes["lon"].Value);
            Nodes[id] = new Vector2(lat, lon);
        }

        Debug.Log($"[OsmParser] 노드 수: {Nodes.Count}");

        // 2. 건물 파싱
        foreach (XmlNode way in doc.SelectNodes("/osm/way"))
        {
            bool isBuilding = false;
            bool isRoad     = false;

            foreach (XmlNode tag in way.SelectNodes("tag"))
            {
                string k = tag.Attributes["k"].Value;
                if (k == "building") isBuilding = true;
                if (k == "highway")  isRoad     = true;
            }

            List<long> nodeRefs = new();
            foreach (XmlNode nd in way.SelectNodes("nd"))
                nodeRefs.Add(long.Parse(nd.Attributes["ref"].Value));

            if (isBuilding && nodeRefs.Count > 2)
                Buildings.Add(nodeRefs);

            // 도로 노드 수집
            if (isRoad)
                RoadNodes.AddRange(nodeRefs);
        }

        Debug.Log($"[OsmParser] 건물 수: {Buildings.Count}");
        Debug.Log($"[OsmParser] 도로 노드 수: {RoadNodes.Count}");
    }
}