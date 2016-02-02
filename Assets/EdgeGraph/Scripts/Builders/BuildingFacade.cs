/***
Prototype class for EdgeGraph utilization
***/

using UnityEngine;
using System.Collections.Generic;
using EdgeGraph;

public class BuildingFacade : MonoBehaviour
{
    public List<Node> nodes;
    public List<Edge> edges;

    public int ID { get; set; }

    public bool isStretchable = false;
    [HideInInspector]
    public bool isFirst = false;
    [HideInInspector]
    public bool combine = false;
}
