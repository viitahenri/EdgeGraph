/***
Prototype class for EdgeGraph utilization
***/

using UnityEngine;
using System.Collections.Generic;
using EdgeGraph;

[ExecuteInEditMode]
public class Footprint : MonoBehaviour
{
    public List<Node> nodes;
    public List<Edge> edges;

    public int ID { get; set; }

    [SerializeField]
    private bool m_modified = false;
    public bool Modified
    {
        get
        {
            return m_modified;
        }

        set
        {
            if (!value)
            {
                cachedPos = transform.position;
                cachedRot = transform.rotation;
            }

            m_modified = false;
        }
    }

    [HideInInspector]
    public FootprintPlacer placer;
    [HideInInspector]
    public bool onEdge;

    [HideInInspector]
    [SerializeField]
    private Vector3 cachedPos;

    [HideInInspector]
    [SerializeField]
    private Quaternion cachedRot;

    void OnDestroy()
    {
        if (placer != null) placer.InstantiatedFootprintDestroyed(this);
    }

    void Update()
    {
        if (transform.position != cachedPos || transform.rotation != cachedRot)
            m_modified = true;
    }

    public void EnsureEdges()
    {
        if (edges == null || edges.Count == 0)
        {
            CreateEdges();
        }
    }

    public void CreateEdges()
    {
        edges = new List<Edge>();

        for (int i = 0; i < nodes.Count; i++)
        {
            Node cur = nodes[i];
            Node other;

            if (i == nodes.Count - 1)
                other = nodes[0];
            else
                other = nodes[i + 1];

            edges.Add(new Edge(cur.ID, other.ID));
        }
    }
}
