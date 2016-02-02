/***
Prototype class for EdgeGraph utilization
***/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using EdgeGraph;

public class FootprintPlacer : MonoBehaviour
{
    public bool handPlacementEnabled = false;
    public bool handPlacementOnEdge = true;

    public Graph graph;

    public List<GameObject> footprintPrefabsOnEdge;
    public List<GameObject> footprintPrefabsInside;

    [HideInInspector]
    [SerializeField]
    private GameObject footprintParent = null;

    //[HideInInspector]
    [SerializeField]
    private List<Footprint> instantiatedFootprints;

    public void UpdateData()
    {
        if (graph == null) graph = GetComponent<Graph>();
        if (graph == null) graph = GetComponentInParent<Graph>();

        if (instantiatedFootprints == null) instantiatedFootprints = new List<Footprint>();

        Random.seed = System.Guid.NewGuid().GetHashCode();

        Footprint fp;

        for (int i = 0; i < footprintPrefabsOnEdge.Count; i++)
        {
            fp = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(footprintPrefabsOnEdge[i]);
            if (fp == null)
            {
                footprintPrefabsOnEdge.RemoveAt(i);
                i--;
            }
            else
                fp.EnsureEdges();
        }

        for (int i = 0; i < footprintPrefabsInside.Count; i++)
        {
            fp = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(footprintPrefabsInside[i]);
            if (fp == null)
            {
                footprintPrefabsInside.RemoveAt(i);
                i--;
            }
            else
                fp.EnsureEdges();
        }
    }

    public int GetRandomEdgeFootPrintIdx()
    {
        if (footprintPrefabsOnEdge != null && footprintPrefabsOnEdge.Count > 0)
        {
            return Random.Range(0, footprintPrefabsOnEdge.Count);
        }
        else
            return -1;
    }

    public int GetRandomInsideFootPrintIdx()
    {
        if (footprintPrefabsInside != null && footprintPrefabsInside.Count > 0)
        {
            return Random.Range(0, footprintPrefabsInside.Count);
        }
        else
            return -1;
    }

    public GameObject PlaceFootprint(Vector3 worldPos, Quaternion rotation, int index, bool onEdge, bool modified = false)
    {
        GameObject container = GameObject.Find("FootprintPlacerContainer");
        if (container == null) container = new GameObject("FootprintPlacerContainer");

        if (footprintParent == null)
            footprintParent = GameObject.Find("FootprintObjectParent" + graph.GraphID);
        if (footprintParent == null)
        {
            footprintParent = new GameObject("FootprintObjectParent" + graph.GraphID);
            footprintParent.transform.SetParent(container.transform);
        }

        GameObject obj;
        if (onEdge)
            obj = PrefabUtility.InstantiatePrefab(footprintPrefabsOnEdge[index]) as GameObject;
        else
            obj = PrefabUtility.InstantiatePrefab(footprintPrefabsInside[index]) as GameObject;

        obj.transform.SetParent(footprintParent.transform);
        obj.transform.position = worldPos;
        obj.transform.rotation = rotation;

        Footprint fp = obj.GetComponentInChildren<Footprint>();
        fp.placer = this;
        fp.onEdge = onEdge;
        fp.Modified = modified;
        instantiatedFootprints.Add(fp);

        return obj;
    }

    /// <summary>
    /// Gets position and rotation in given target position according to closest edge
    /// </summary>
    /// <param name="target"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    /// <param name="index"></param>
    /// <param name="placeInside">Is footprint placed inside or outside the edge?</param>
    /// <param name="placeOnEdge">Is footprint placed on the edge or on target?</param>
    public void GetFootprintPosAndRotAtTarget(Vector3 target, out Vector3 pos, out Quaternion rot, out Vector3 closestPoint, int index, bool placeOnEdge = true, bool placeInside = true)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        closestPoint = Vector3.zero;

        if (placeOnEdge)
        {
            // Line the footprint with closest edge
            Edge edge = null;
            closestPoint = EdgeGraphUtility.GetClosestPointOnEdge(target, graph.mainPrimitives[0].nodes, graph.mainPrimitives[0].edges, transform, out edge);

            Node n1 = EdgeGraphUtility.GetNode(edge.Node1, ref graph.mainPrimitives[0].nodes);
            Vector3 eN1Pos = n1.Position;
            Vector3 eN2Pos = EdgeGraphUtility.GetNode(edge.Node2, ref graph.mainPrimitives[0].nodes).Position;

            eN2Pos.y = eN1Pos.y;
            Footprint fp = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(footprintPrefabsOnEdge[index]);
            if (fp != null)
            {
                Vector3 fpN1Pos = fp.nodes[0].Position;
                Vector3 fpN2Pos = fp.nodes[1].Position;
                Vector3 fpEdgePoint = Edge.GetClosestPointOnEdge(Vector3.zero, fpN1Pos, fpN2Pos);

                //Vector3 nodeDir = (fpN2Pos - fpN1Pos).normalized;
                Vector3 edgeDir = (eN2Pos - eN1Pos).normalized;

                Vector3 edgeNormal = UtilityTools.MathHelper.LeftSideNormal(edgeDir);
                // Check that edge normal points outside (building will be placed inside the edge)
                if (Vector3.Dot(n1.dirToInside, edgeNormal) > 0)
                    edgeNormal = -edgeNormal;

                // Place the building outside
                if (!placeInside)
                    edgeNormal = -edgeNormal;

                pos = closestPoint - edgeNormal * (fpEdgePoint.magnitude + .05f);

                rot = Quaternion.LookRotation((pos - closestPoint), Vector3.up);
            }
            else
                Debug.Log("FootprintPlacer::GetFootprintPosAndRotAtTarget() - No footprint found in prefab.");
        }
        else
        {
            pos = target;
            rot = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
        }
    }

    public void FillWithEdgesWithFootprints()
    {
        ClearUnmodifiedFootprints(true);

        //List<Footprint> instantiatedFootprints = new List<Footprint>();

        Vector3 target;
        Vector3 bPos;
        Vector3 closestPoint;
        Quaternion bRot;
        Matrix4x4 footprintTRSMatrix;
        Footprint footprint;
        Primitive primitive = graph.mainPrimitives[0];

        List<Node> nodes = primitive.nodes;
        List<Edge> edges = primitive.edges;

        // Two passes are made, first one places random footprints, the second tries every footprint on every position so smaller gaps left are filled
        for (int pass = 1; pass <= 2; pass++)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Node n1 = EdgeGraphUtility.GetNode(edges[i].Node1, ref nodes);
                Node n2 = EdgeGraphUtility.GetNode(edges[i].Node2, ref nodes);

                Vector3 n1Pos = transform.TransformPoint(n1.Position);
                Vector3 n2Pos = transform.TransformPoint(n2.Position);
                //Vector3 n1n2 = (n2Pos - n1Pos);

                int footprintIdx = GetRandomEdgeFootPrintIdx();
                for (float t = 0f; t < 1f; t += .005f)
                {
                    target = Vector3.Lerp(n1Pos, n2Pos, t);
                    GetFootprintPosAndRotAtTarget(target, out bPos, out bRot, out closestPoint, footprintIdx);

                    if (pass == 1)
                    {
                        footprint = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(footprintPrefabsOnEdge[footprintIdx]);
                        footprintTRSMatrix = Matrix4x4.TRS(bPos, bRot, Vector3.one);
                        if (!FootprintOverlapsOthers(footprint, footprintTRSMatrix) && FootprintIsInsideGraph(footprint, footprintTRSMatrix))
                        {
                            PlaceFootprint(bPos, bRot, footprintIdx, true);
                            footprintIdx = GetRandomEdgeFootPrintIdx();
                        }
                    }
                    else
                    {
                        for (int j = 0; j < footprintPrefabsOnEdge.Count; j++)
                        {
                            footprint = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(footprintPrefabsOnEdge[j]);
                            footprintTRSMatrix = Matrix4x4.TRS(bPos, bRot, Vector3.one);
                            if (!FootprintOverlapsOthers(footprint, footprintTRSMatrix) && FootprintIsInsideGraph(footprint, footprintTRSMatrix))
                            {
                                PlaceFootprint(bPos, bRot, j, true);
                            }
                        }
                    }
                }
            }
        }
    }

    public void FillInsideWithFootprints()
    {
        ClearUnmodifiedFootprints(false);

        //List<Footprint> instantiatedFootprints = new List<Footprint>();

        Vector3 bPos;
        Quaternion bRot;
        Vector3 closestPoint;
        //GameObject footprintObj;
        Footprint footprint;
        Primitive primitive = graph.mainPrimitives[0];

        List<Node> nodes = primitive.nodes;
        List<Edge> edges = primitive.edges;

        int failedPosCount = 0;
        int failedPosMax = 1000;
        int safeCount = 0;

        Vector3 point;
        Vector3 pointWorld;

        while (failedPosCount < failedPosMax && safeCount < 1000)
        {
            safeCount++;

            //Get random point inside the boundaries
            float randX = UnityEngine.Random.Range(primitive.minX, primitive.maxX);
            float randZ = UnityEngine.Random.Range(primitive.minZ, primitive.maxZ);

            point = new Vector3(randX, 0f, randZ);

            //If point is inside the polygon, proceed
            if (EdgeGraphUtility.PointIsInside(point, nodes, edges))
            {
                pointWorld = transform.TransformPoint(point);
                int footprintIdx = GetRandomInsideFootPrintIdx();
                GetFootprintPosAndRotAtTarget(pointWorld, out bPos, out bRot, out closestPoint, footprintIdx, false);
                footprint = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(footprintPrefabsOnEdge[footprintIdx]);
                Matrix4x4 footprintTRSMatrix = Matrix4x4.TRS(bPos, bRot, Vector3.one);
                if (!FootprintOverlapsOthers(footprint, footprintTRSMatrix) && FootprintIsInsideGraph(footprint, footprintTRSMatrix))
                {
                    PlaceFootprint(bPos, bRot, footprintIdx, false);
                }
                else
                {
                    failedPosCount++;
                }
            }

            if (failedPosCount >= failedPosMax || safeCount >= 1000)
                break;
        }
    }

    public void InstantiatedFootprintDestroyed(Footprint fp)
    {
        if (instantiatedFootprints.Contains(fp))
            instantiatedFootprints.Remove(fp);
    }

    public void ClearUnmodifiedFootprints(bool onEdge)
    {
        if (instantiatedFootprints == null) return;

        for (int i = 0; i < instantiatedFootprints.Count; i++)
        {
            if (!instantiatedFootprints[i].Modified && instantiatedFootprints[i].onEdge == onEdge)
            {
                DestroyImmediate(instantiatedFootprints[i].transform.parent.gameObject);
                i--;
            }
        }
    }

    bool FootprintOverlapsOthers(Footprint footprint, Matrix4x4 footprintTRSMatrix)
    {
        // Check if any footprint edges cross each other
        for (int i = 0; i < footprint.edges.Count; i++)
        {
            Vector3 n1Pos = footprintTRSMatrix.MultiplyPoint3x4(EdgeGraphUtility.GetNode(footprint.edges[i].Node1, ref footprint.nodes).Position);
            Vector3 n2Pos = footprintTRSMatrix.MultiplyPoint3x4(EdgeGraphUtility.GetNode(footprint.edges[i].Node2, ref footprint.nodes).Position);

            for (int j = 0; j < instantiatedFootprints.Count; j++)
            {
                List<Node> placedFootprintWorldNodes = new List<Node>();
                instantiatedFootprints[j].nodes.ForEach((node) =>
                {
                    Node newNode = new Node(node);
                    newNode.Position = instantiatedFootprints[j].transform.TransformPoint(node.Position);
                    placedFootprintWorldNodes.Add(newNode);
                });

                for (int k = 0; k < instantiatedFootprints[j].edges.Count; k++)
                {
                    Vector3 n3Pos = instantiatedFootprints[j].transform.TransformPoint(EdgeGraphUtility.GetNode(instantiatedFootprints[j].edges[k].Node1, ref instantiatedFootprints[j].nodes).Position);
                    Vector3 n4Pos = instantiatedFootprints[j].transform.TransformPoint(EdgeGraphUtility.GetNode(instantiatedFootprints[j].edges[k].Node2, ref instantiatedFootprints[j].nodes).Position);

                    Vector3 intersect;
                    if (UtilityTools.MathHelper.AreIntersecting(out intersect, n1Pos, n2Pos, n3Pos, n4Pos) == 1)
                    {
                        return true;
                    }
                }
            }
        }

        // Check if footprint is inside placed footprint
        Vector3 nodePosWorld;
        for (int i = 0; i < footprint.nodes.Count; i++)
        {
            nodePosWorld = footprintTRSMatrix.MultiplyPoint3x4(footprint.nodes[i].Position);
            for (int j = 0; j < instantiatedFootprints.Count; j++)
            {
                List<Node> placedFootprintWorldNodes = new List<Node>();
                instantiatedFootprints[j].nodes.ForEach((node) =>
                {
                    Node newNode = new Node(node);
                    newNode.Position = instantiatedFootprints[j].transform.TransformPoint(node.Position);
                    placedFootprintWorldNodes.Add(newNode);
                });

                if (EdgeGraphUtility.PointIsInside(nodePosWorld, placedFootprintWorldNodes, instantiatedFootprints[j].edges))
                {
                    return true;
                }
            }
        }

        // Check if any placed footprint is inside the footprint
        List<Node> footprintWorldNodes = new List<Node>();
        footprint.nodes.ForEach((node) =>
        {
            Node newNode = new Node(node);
            newNode.Position = footprintTRSMatrix.MultiplyPoint3x4(node.Position);
            footprintWorldNodes.Add(newNode);
        });

        for (int i = 0; i < instantiatedFootprints.Count; i++)
        {
            List<Node> placedFootprintWorldNodes = new List<Node>();
            instantiatedFootprints[i].nodes.ForEach((node) =>
            {
                Node newNode = new Node(node);
                newNode.Position = instantiatedFootprints[i].transform.TransformPoint(node.Position);
                placedFootprintWorldNodes.Add(newNode);
            });

            for (int j = 0; j < placedFootprintWorldNodes.Count; j++)
            {
                if (EdgeGraphUtility.PointIsInside(placedFootprintWorldNodes[j].Position, footprintWorldNodes, footprint.edges))
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool FootprintIsInsideGraph(Footprint footprint, Matrix4x4 footprintTRSMatrix)
    {
        for (int i = 0; i < footprint.edges.Count; i++)
        {
            Vector3 n1PosWorld = footprintTRSMatrix.MultiplyPoint3x4(EdgeGraphUtility.GetNode(footprint.edges[i].Node1, ref footprint.nodes).Position);
            Vector3 n2PosWorld = footprintTRSMatrix.MultiplyPoint3x4(EdgeGraphUtility.GetNode(footprint.edges[i].Node2, ref footprint.nodes).Position);

            Vector3 n1PosGraph = graph.transform.InverseTransformPoint(n1PosWorld);
            Vector3 n2PosGraph = graph.transform.InverseTransformPoint(n2PosWorld);

            if (!EdgeGraphUtility.PointIsInside(n1PosGraph, graph.mainPrimitives[0].nodes, graph.mainPrimitives[0].edges) ||
                !EdgeGraphUtility.PointIsInside(n2PosGraph, graph.mainPrimitives[0].nodes, graph.mainPrimitives[0].edges))
            {
                return false;
            }
        }

        return true;
    }
}
