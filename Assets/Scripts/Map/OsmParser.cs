using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

public class OsmParser
{
    public Dictionary<long, Vector2> Nodes     { get; } = new();
    public List<List<long>>          Buildings  { get; } = new();
    public List<long>                RoadNodes  { get; } = new();

    public void Parse(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        foreach (XmlNode node in doc.SelectNodes("/osm/node"))
        {
            long  id  = long.Parse(node.Attributes["id"].Value, CultureInfo.InvariantCulture);
            float lat = float.Parse(node.Attributes["lat"].Value, CultureInfo.InvariantCulture);
            float lon = float.Parse(node.Attributes["lon"].Value, CultureInfo.InvariantCulture);
            Nodes[id] = new Vector2(lat, lon);
        }

        Debug.Log($"[OsmParser] 노드: {Nodes.Count}개");

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

            var nodeRefs = new List<long>();
            foreach (XmlNode nd in way.SelectNodes("nd"))
                nodeRefs.Add(long.Parse(nd.Attributes["ref"].Value, CultureInfo.InvariantCulture));

            if (isBuilding && nodeRefs.Count > 2)
                Buildings.Add(nodeRefs);

            if (isRoad)
                RoadNodes.AddRange(nodeRefs);
        }

        Debug.Log($"[OsmParser] 건물: {Buildings.Count}개 / 도로 노드: {RoadNodes.Count}개");
    }
}
