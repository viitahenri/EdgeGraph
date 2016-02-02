using UnityEngine;
using System;
using System.Collections.Generic;

namespace EdgeGraph
{
    public class Graph : MonoBehaviour
    {
        #region Fields and properties
        [SerializeField]
        private int m_id = -1;
        public int GraphID
        {
            get { return m_id; }
            set { m_id = value; }
        }

        public List<Node> nodes;
        public List<Edge> edges;
        [HideInInspector]
        public List<Primitive> mainPrimitives;
        //[HideInInspector]
        //public List<Primitive> subPrimitives;
        [HideInInspector]
        public List<Graph> subGraphs;

        //Minimal cycle's local lists
        private List<Node> m_nodes;
        private List<Edge> m_edges;
        private List<Primitive> m_primitives;
        #endregion

        #region Editor preferences
        [HideInInspector]
        public int randomSeed = 0;
        [HideInInspector]
        public int _subTargetCount = 5;
        [HideInInspector]
        public float _subEdgeTargetMargin = 1f;
        [HideInInspector]
        public float _subEdgeWidth = 0f;
        [HideInInspector]
        public float _subEdgeAngle = 30f;
        [HideInInspector]
        public float _subEdgeSegmentLength = .5f;
        [HideInInspector]
        public float _subEdgeMinDistance = .3f;
        [HideInInspector]
        public float _subEdgeMaxDistance = 2f;
        [HideInInspector]
        public float _subEdgeNodeCombineRange = .5f;
        [HideInInspector]
        public float _subEdgeEndConnectionRange = 0f;
        #endregion

        [ContextMenu("Reset pivot")]
        void ResetPivot()
        {
            Vector3 nodeCenter = Vector3.zero;

            for (int i = 0; i < nodes.Count; i++)
            {
                nodeCenter += nodes[i].Position;
            }

            nodeCenter /= nodes.Count;

            Vector3 worldPos = transform.position;
            Vector3 nodeWorldCenter = transform.TransformPoint(nodeCenter);

            Vector3 toNodeWorldCenter = nodeWorldCenter - worldPos;

            nodes.ForEach((n) => n.Position -= toNodeWorldCenter);
            transform.position += toNodeWorldCenter;
        }

        #region Node and edge information retrieving
        public Node this[int index]
        {
            get
            {
                if (nodes != null && index >= 0 && index < nodes.Count)
                    return nodes[index];
                return null;
            }
        }

        public Node this[string id]
        {
            get
            {
                if (nodes == null || string.IsNullOrEmpty(id)) return null;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].ID.Equals(id)) return nodes[i];
                }
                return null;
            }
        }

        public Edge GetEdge(string id)
        {
            if (edges == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ID.Equals(id)) return edges[i];
            }
            return null;
        }

        public Vector3 GetEdgePosition(string id, bool localSpace = false)
        {
            Vector3 retval = Vector3.zero;

            Edge e = GetEdge(id);
            if (e == null) return retval;

            Node n1 = this[e.Node1];
            Node n2 = this[e.Node2];

            if (n1 == null || n2 == null) return retval;

            if (localSpace)
                retval = ((n1.Position + n2.Position) / 2f);
            else
                retval = transform.TransformPoint(((n1.Position + n2.Position) / 2f));

            return retval;
        }
        #endregion

        #region Node and edge manipulation
        public void RemoveNode(string id)
        {
            nodes.Remove(this[id]);

            CleanUpEdges();
        }

        /// <summary>
        /// Checks if there are any edges that have invalid nodes and removes them
        /// </summary>
        public void CleanUpEdges()
        {
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                if (this[edges[i].Node1] == null || this[edges[i].Node2] == null)
                    edges.Remove(edges[i]);
            }
        }
        #endregion

        #region Minimal cycle

        public void ProcessMinimalCycles()
        {
            if (GraphID == -1) GraphID = Guid.NewGuid().GetHashCode();
            
            // Use different lists of nodes and edges for processing time, so the serialized lists won't change
            m_edges = new List<Edge>();
            m_nodes = new List<Node>();
            m_primitives = new List<Primitive>();

            UtilityTools.Helper.DestroyChildren(transform);

            EdgeGraphUtility.CopyNodesAndEdges(nodes, edges, out m_nodes, out m_edges);

            EdgeGraphUtility.CheckAdjacentNodes(ref m_nodes, ref m_edges);

            MinimalCycle.Extract(ref m_nodes, ref m_edges, ref m_primitives);

            // Serialize and process primitives
            mainPrimitives = m_primitives;

            try
            {
                ProcessPrimitives();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Graph::ProcessMinimalCycles() - Error while processing primitives: " + e.Message);
            }
        }

        public void ProcessPrimitives(int index = -1)
        {
            if (mainPrimitives != null && mainPrimitives.Count > 0)
            {
                //subPrimitives = new List<Primitive>();
                UtilityTools.Helper.DestroyChildren(transform);
                subGraphs = new List<Graph>();

                if (index == -1)
                {
                    mainPrimitives.ForEach((p) =>
                    {
                        p.Process();
                    });
                }
                else
                {
                    mainPrimitives[index].Process();
                }
            }
        }

        public void ExtractMainPrimitives(float childEdgeWidth)
        {
            if (mainPrimitives != null && mainPrimitives.Count > 0)
            {
                UtilityTools.Helper.DestroyChildren(transform);

                mainPrimitives.ForEach((p) =>
                {
                    GameObject subGraphObj = new GameObject("SubGraph");
                    subGraphObj.transform.SetParent(transform);
                    subGraphObj.transform.localPosition = Vector3.zero;
                    subGraphObj.transform.localScale = Vector3.one;

                    Graph subGraph = subGraphObj.AddComponent<Graph>();
                    subGraph.GraphID = Guid.NewGuid().GetHashCode();
                    subGraphObj.name += subGraph.GraphID;

                    subGraph.nodes = new List<Node>();
                    foreach (var node in p.nodes)
                    {
                        subGraph.nodes.Add(node);
                    }
                    subGraph.edges = new List<Edge>();
                    foreach (var edge in p.edges)
                    {
                        Edge edgeCopy = new Edge(edge.Node1, edge.Node2, childEdgeWidth);
                        subGraph.edges.Add(edgeCopy);
                    }

                    subGraph.ProcessMinimalCycles();

                    subGraphs.Add(subGraph);

                    FootprintPlacer placer = GetComponent<FootprintPlacer>();
                    if (placer != null)
                    {
                        FootprintPlacer _placer = subGraph.gameObject.AddComponent<FootprintPlacer>();
                        _placer.footprintPrefabsOnEdge = placer.footprintPrefabsOnEdge;
                        _placer.footprintPrefabsInside = placer.footprintPrefabsInside;
                        _placer.UpdateData();
                    }
                });
            }
        }

        public void ClearPrimitiveSubPrimitives(int index = -1)
        {
            if (mainPrimitives != null && mainPrimitives.Count > 0)
            {
                if (index == -1)
                {
                    mainPrimitives.ForEach((p) =>
                    {
                        ClearSubPrimitives(p);
                    });
                }
                else
                {
                    ClearSubPrimitives(mainPrimitives[index]);
                }
            }
        }

        void ClearSubPrimitives(Primitive p)
        {
            //for (int i = 0; i < subPrimitives.Count; i++)
            //{
            //    if (subPrimitives[i].parent == p.ID)
            //    {
            //        subPrimitives[i].subNodes = new List<Node>();
            //        subPrimitives[i].subEdges = new List<Edge>();
            //        subPrimitives.RemoveAt(i);
            //        i--;
            //    }
            //}

            for (int i = 0; i < subGraphs.Count; i++)
            {
                if (subGraphs[i] == null)
                {
                    subGraphs.RemoveAt(i);
                    i--;
                    continue;
                }

                if (subGraphs[i].mainPrimitives[0].parent == p.ID)
                {
                    DestroyImmediate(subGraphs[i].gameObject);
                    subGraphs.RemoveAt(i);
                    i--;
                }
            }
        }

        public void GeneratePrimitiveSubEdges(int seed, int index = -1)
        {
            if (mainPrimitives != null && mainPrimitives.Count > 0)
            {
                if (index == -1)
                {
                    foreach (var p in mainPrimitives)
                    {
                        GenerateSubEdges(seed, p);
                    }
                }
                else
                {
                    GenerateSubEdges(seed, mainPrimitives[index]);
                }
            }
        }

        void GenerateSubEdges(int seed, Primitive p)
        {
            p.Generate(seed);
        }

        public void ProcessPrimitiveSubPrimitives(int index = -1)
        {
            if (mainPrimitives != null && mainPrimitives.Count > 0)
            {
                if (index == -1)
                {
                    foreach (var p in mainPrimitives)
                    {
                        ProcessSubPrimitives(p);
                    }
                }
                else
                {
                    ProcessSubPrimitives(mainPrimitives[index]);
                }
            }
        }

        void ProcessSubPrimitives(Primitive p)
        {
            if (p.subEdges == null || p.subEdges.Count <= 0) return;

            // Copy local lists from primitive's sub nodes and edges
            List<Node> _nodes = new List<Node>();
            List<Edge> _edges = new List<Edge>();

            EdgeGraphUtility.CopyNodesAndEdges(p.subNodes, p.subEdges, out _nodes, out _edges);

            EdgeGraphUtility.CheckAdjacentNodes(ref _nodes, ref _edges);

            //subPrimitives = new List<Primitive>();
            List<Primitive> _subPrimitives = new List<Primitive>();

            // Extract primitives inside main primitives
            try
            {
                MinimalCycle.Extract(ref _nodes, ref _edges, ref _subPrimitives);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Graph::GeneratePrimitiveSubPrimitives() - Error while extracting primitives: " + e.Message);
                return;
            }

            _subPrimitives.ForEach((sp) =>
            {
                sp.Process();
            });

            for (int i = _subPrimitives.Count - 1; i >= 0; i--)
            {
                if (!_subPrimitives[i].EvaluationResult)
                    _subPrimitives.RemoveAt(i);
            }

            _subPrimitives.ForEach((sp) =>
            {
                sp.parent = p.ID;

                GameObject subGraphObj = new GameObject("SubGraph");
                subGraphObj.transform.SetParent(transform);
                subGraphObj.transform.localPosition = Vector3.zero;
                subGraphObj.transform.localScale = Vector3.one;

                Graph subGraph = subGraphObj.AddComponent<Graph>();
                subGraph.GraphID = Guid.NewGuid().GetHashCode();
                subGraphObj.name += subGraph.GraphID;

                subGraph.nodes = new List<Node>();
                foreach (var node in sp.nodes)
                {
                    subGraph.nodes.Add(node);
                }
                subGraph.edges = new List<Edge>();
                foreach (var edge in sp.edges)
                {
                    edge.Width = 0f;
                    subGraph.edges.Add(edge);
                }

                subGraph.ProcessMinimalCycles();

                subGraph.mainPrimitives[0].parent = p.ID;

                subGraphs.Add(subGraph);

                //subPrimitives.Add(subGraph.mainPrimitives[0]);

                FacadeBuilder builder = GetComponent<FacadeBuilder>();
                if (builder != null)
                {
                    FacadeBuilder subBuilder = subGraph.gameObject.AddComponent<FacadeBuilder>();
                    subBuilder.inSet = builder.inSet;
                    subBuilder.facadeStretchPrefab = builder.facadeStretchPrefab;
                    subBuilder.facadePrefabs = builder.facadePrefabs;
                    subBuilder.roofMiddleMaterial = builder.roofMiddleMaterial;
                    subBuilder.roofSideMaterial = builder.roofSideMaterial;
                    subBuilder.roofHeight = builder.roofHeight;
                    subBuilder.roofMiddleAddHeight = builder.roofMiddleAddHeight;
                    subBuilder.roofAccentWidth = builder.roofAccentWidth;
                }

                FootprintPlacer placer = GetComponent<FootprintPlacer>();
                if (placer != null)
                {
                    FootprintPlacer _placer = subGraph.gameObject.AddComponent<FootprintPlacer>();
                    _placer.footprintPrefabsOnEdge = placer.footprintPrefabsOnEdge;
                    _placer.footprintPrefabsInside = placer.footprintPrefabsInside;
                    _placer.UpdateData();
                }
            });
        }
        #endregion

        #region Gizmos

        void OnDrawGizmosSelected()
        {
            //Primitives
            if (mainPrimitives != null && mainPrimitives.Count > 0)
            {
                int primitiveCount = 0;
                foreach (var p in mainPrimitives)
                {
                    //Primitive sub edge targets
                    if (p.subEdgeTargets != null && p.subEdgeTargets.Count > 0)
                    {
                        //Gizmos.color = Color.magenta;
                        for (int i = 0; i < p.subEdgeTargets.Count; i++)
                        {
                            Gizmos.color = Color.blue;
                            Vector3 pointPos = transform.TransformPoint(p.subEdgeTargets[i]);
                            Gizmos.DrawSphere(pointPos, .15f);
                            //Gizmos.DrawLine(pointOutside, pointPos);
                        }
                    }

                    //Subedges
                    if (p.subEdges != null && p.subEdges.Count > 0)
                    {
                        Gizmos.color = Color.magenta;

                        // Edges
                        for (int j = 0; j < p.subEdges.Count; j++)
                        {
                            Node n1 = Node.GetNode(p.subNodes, p.subEdges[j].Node1);
                            Node n2 = Node.GetNode(p.subNodes, p.subEdges[j].Node2);

                            if (n1 != null && n2 != null)
                            {
                                Gizmos.DrawLine(transform.TransformPoint(n1.Position), transform.TransformPoint(n2.Position));
                            }
                        }

                        for (int k = 0; k < p.subNodes.Count; k++)
                        {
                            if (p.subNodes[k].adjacents.Count >= 2)
                                Gizmos.color = Color.green;
                            else
                                Gizmos.color = Color.blue;
                            Gizmos.DrawSphere(transform.TransformPoint(p.subNodes[k].Position), .05f);
                        }
                    }

                    primitiveCount++;
                }
            }
        }

        #endregion
    }
}