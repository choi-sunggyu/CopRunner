using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class OsmParser
{
    // node id → 위도경도
    public Dictionary<long, Vector2> Nodes = new();

    // 건물 외곽선 (node id 리스트들)
    public List<List<long>> Buildings = new();

    public void Parse(string xml)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);

        // 1. 모든 node 파싱
        foreach (XmlNode node in doc.SelectNodes("/osm/node"))
        {
            long id  = long.Parse(node.Attributes["id"].Value);
            float lat = float.Parse(node.Attributes["lat"].Value);
            float lon = float.Parse(node.Attributes["lon"].Value);
            Nodes[id] = new Vector2(lat, lon);
        }

        Debug.Log($"[OsmParser] 노드 수: {Nodes.Count}");

        // 2. building 태그 있는 way만 파싱
        foreach (XmlNode way in doc.SelectNodes("/osm/way"))
        {
            bool isBuilding = false;
            foreach (XmlNode tag in way.SelectNodes("tag"))
            {
                if (tag.Attributes["k"].Value == "building")
                {
                    isBuilding = true;
                    break;
                }
            }

            if (!isBuilding) continue;

            List<long> nodeRefs = new();
            foreach (XmlNode nd in way.SelectNodes("nd"))
            {
                long refId = long.Parse(nd.Attributes["ref"].Value);
                nodeRefs.Add(refId);
            }

            if (nodeRefs.Count > 2)
                Buildings.Add(nodeRefs);
        }

        Debug.Log($"[OsmParser] 건물 수: {Buildings.Count}");
    }
}