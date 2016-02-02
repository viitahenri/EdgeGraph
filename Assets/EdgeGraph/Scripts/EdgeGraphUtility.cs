using UnityEngine;
using System.Collections.Generic;

namespace EdgeGraph
{
    public class EdgeGraphUtility
    {
        #region Node and edge methods
        public static Node GetNode(int index, ref List<Node> _nodes)
        {
            if (_nodes != null && index >= 0 && index < _nodes.Count)
                return _nodes[index];
            return null;
        }

        public static Node GetNode(string id, ref List<Node> _nodes)
        {
            if (_nodes == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].ID.Equals(id)) return _nodes[i];
            }
            return null;
        }

        public static Edge GetEdge(string id, ref List<Edge> _edges)
        {
            if (_edges == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _edges.Count; i++)
            {
                if (_edges[i].ID.Equals(id)) return _edges[i];
            }
            return null;
        }

        public static Vector3 GetEdgePosition(string id, ref List<Node> _nodes, ref List<Edge> _edges, Transform transform = null)
        {
            Vector3 retval = Vector3.zero;

            Edge e = GetEdge(id, ref _edges);
            if (e == null) return retval;

            Node n1 = GetNode(e.Node1, ref _nodes);
            Node n2 = GetNode(e.Node2, ref _nodes);

            if (n1 == null || n2 == null) return retval;

            if (transform == null)
                retval = ((n1.Position + n2.Position) / 2f);
            else
                retval = transform.TransformPoint(((n1.Position + n2.Position) / 2f));

            return retval;
        }

        public static Edge FindEdgeByNodes(Node n1, Node n2, List<Edge> _edges)
        {
            if (n1 == null || n2 == null) return null;

            for (int i = 0; i < _edges.Count; i++)
            {
                if (_edges[i] == null) continue;

                if ((_edges[i].Node1 == n1.ID && _edges[i].Node2 == n2.ID)
                    || (_edges[i].Node1 == n2.ID && _edges[i].Node2 == n1.ID))
                {
                    return _edges[i];
                }
            }

            return null;
        }

        public static Edge FindEdgeByNodes(string n1, string n2, List<Edge> _edges)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                if ((_edges[i].Node1 == n1 && _edges[i].Node1 == n2)
                    || (_edges[i].Node1 == n2 && _edges[i].Node1 == n1))
                {
                    return _edges[i];
                }
            }

            return null;
        }

        public static void RemoveNodeAndCleanAdjacents(Node n, ref List<Node> _nodes, ref List<Edge> _edges)
        {
            if (_nodes.Remove(n))
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    if (_nodes[i] != n && _nodes[i].adjacents.Contains(n.ID))
                        _nodes[i].adjacents.Remove(n.ID);
                }
            }
        }

        public static Edge RemoveEdgeAndCleanAdjacents(Node _n0, Node _n1, ref List<Node> _nodes, ref List<Edge> _edges)
        {
            Edge e = FindEdgeByNodes(_n0, _n1, _edges);
            if (e != null && _edges.Contains(e))
            {
                if (_edges.Remove(e))
                {
                    CheckAdjacentNodes(ref _nodes, ref _edges);
                }
            }
            return e;
        }

        public static void CheckAdjacentNodes(ref List<Node> _nodes, ref List<Edge> _edges)
        {
            //Assign adjacents
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].CheckAdjacents(_edges);
            }
        }

        public static void CleanUpEdges(ref List<Node> nodes, ref List<Edge> edges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                // Make sure the edge is still an existing one
                Node n1 = Node.GetNode(nodes, edges[i].Node1);
                Node n2 = Node.GetNode(nodes, edges[i].Node2);

                if (n1 == null || n2 == null)
                {
                    edges.RemoveAt(i);
                    i--;
                    continue;
                }

                // Remove edges that have the same node twice
                if (edges[i].Node1 == edges[i].Node2)
                {
                    edges.RemoveAt(i);
                    i--;
                    continue;
                }

                // Remove duplicate edges
                for (int j = 0; j < edges.Count; j++)
                {
                    if (j != i && j < edges.Count - 1 && i < edges.Count - 1 && i > 0 && j > 0)
                    {
                        if ((edges[i].Node1 == edges[j].Node1 && edges[i].Node2 == edges[j].Node2) ||
                            (edges[i].Node1 == edges[j].Node2 && edges[i].Node2 == edges[j].Node1))
                        {
                            edges.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns closest node to the world point. Node positions are changed to world coordinates in reference to refTransform.
        /// </summary>
        public static Node GetClosestNode(Vector3 point, List<Node> nodes, Transform refTransform)
        {
            Node retval = nodes[0];
            float toClosest = Mathf.Infinity;
            for (int i = 0; i < nodes.Count; i++)
            {
                Vector3 currentPos = refTransform.TransformPoint(nodes[i].Position);
                float toCurrent = Vector3.Distance(currentPos, point);
                Vector3 closestPos = refTransform.TransformPoint(retval.Position);
                toClosest = Vector3.Distance(closestPos, point);

                if (toCurrent < toClosest)
                {
                    retval = nodes[i];
                    toClosest = toCurrent;
                }
            }

            return retval;
        }

        /// <summary>
        /// Returns closest edge to the world point. Edge positions are changed to world coordinates in reference to refTransform.
        /// </summary>
        public static Edge GetClosestEdge(Vector3 point, List<Node> nodes, List<Edge> edges, Transform refTransform)
        {
            Edge retval = edges[0];
            float toClosest = Mathf.Infinity;

            for (int i = 0; i < edges.Count; i++)
            {
                Vector3 currentPos = GetEdgePosition(edges[i].ID, ref nodes, ref edges, refTransform);
                float toCurrent = Vector3.Distance(currentPos, point);
                Vector3 closestPos = GetEdgePosition(retval.ID, ref nodes, ref edges, refTransform);
                toClosest = Vector3.Distance(closestPos, point);

                if (toCurrent < toClosest)
                {
                    retval = edges[i];
                    toClosest = toCurrent;
                }
            }

            return retval;
        }

        /// <summary>
        /// Returns closest point on the closest point with given point
        /// </summary>
        public static Vector3 GetPointOnClosestEdge(Vector3 point, List<Node> nodes, List<Edge> edges, Transform refTransform)
        {
            Edge closestEdge = GetClosestEdge(point, nodes, edges, refTransform);

            if (closestEdge == null) return Vector3.zero;

            Vector2 splitPointXZ;

            Node node1 = EdgeGraphUtility.GetNode(closestEdge.Node1, ref nodes);
            if (node1 == null) return Vector3.zero;

            Vector3 n1Pos = node1.Position;
            n1Pos = refTransform.TransformPoint(n1Pos);

            Node node2 = EdgeGraphUtility.GetNode(closestEdge.Node2, ref nodes);
            if (node2 == null) return Vector3.zero;

            Vector3 n2Pos = node2.Position;
            n2Pos = refTransform.TransformPoint(n2Pos);

            splitPointXZ = Edge.GetClosestPointOnEdge(point, n1Pos, n2Pos);

            return new Vector3(splitPointXZ.x, n1Pos.y, splitPointXZ.y);
        }

        /// <summary>
        /// Returns closest point on all the given edges. Edge positions are changed to world coordinates in reference to refTransform.
        /// </summary>
        public static Vector3 GetClosestPointOnEdge(Vector3 point, List<Node> nodes, List<Edge> edges, Transform refTransform, out Edge closestEdge)
        {
            Vector3 pointOnEdge = Vector3.zero;
            closestEdge = edges[0];
            float closestDist = Mathf.Infinity;

            for (int i = 0; i < edges.Count; i++)
            {
                Vector2 splitPointXZ;

                Node node1 = EdgeGraphUtility.GetNode(edges[i].Node1, ref nodes);
                if (node1 == null) continue;

                Vector3 n1Pos = node1.Position;
                n1Pos = refTransform.TransformPoint(n1Pos);

                Node node2 = EdgeGraphUtility.GetNode(edges[i].Node2, ref nodes);
                if (node2 == null) continue;

                Vector3 n2Pos = node2.Position;
                n2Pos = refTransform.TransformPoint(n2Pos);

                splitPointXZ = Edge.GetClosestPointOnEdge(point, n1Pos, n2Pos);

                Vector3 splitPoint = new Vector3(splitPointXZ.x, n1Pos.y, splitPointXZ.y);

                if (Vector3.Distance(point, splitPoint) < closestDist)
                {
                    closestEdge = edges[i];
                    pointOnEdge = splitPoint;
                    closestDist = Vector3.Distance(point, splitPoint);
                }
            }

            return pointOnEdge;
        }

        /// <summary>
        /// Checks if given point is inside the edges. The point must be in the same space than node positions
        /// </summary>
        public static bool PointIsInside(Vector3 point, List<Node> nodes, List<Edge> edges)
        {
            Vector3 position = Vector3.zero;

            for (int i = 0; i < nodes.Count; i++)
            {
                position += nodes[i].Position;
            }

            position /= nodes.Count;

            Vector2 posInXZSpace = new Vector2(position.x, position.z);
            Vector2 pointOutside = posInXZSpace + Vector2.right * 10000f;

            Vector2 pointInXZSpace = new Vector2(point.x, point.z);

            int intersectedEdgeCount = 0;

            foreach (var edge in edges)
            {
                //Node n1 = nodes.Find(x => x.ID == edge.Node1);
                //Node n2 = nodes.Find(x => x.ID == edge.Node2);

                Node node1 = EdgeGraphUtility.GetNode(edge.Node1, ref nodes);
                if (node1 == null) continue;

                Vector3 n1Pos = node1.Position;
                Vector3 n1PosXZ = new Vector2(n1Pos.x, n1Pos.z);

                Node node2 = EdgeGraphUtility.GetNode(edge.Node2, ref nodes);
                if (node2 == null) continue;

                Vector3 n2Pos = node2.Position;
                Vector3 n2PosXZ = new Vector2(n2Pos.x, n2Pos.z);

                if (UtilityTools.MathHelper.AreIntersecting(pointOutside, pointInXZSpace, n1PosXZ, n2PosXZ, 0f) == 1)
                {
                    intersectedEdgeCount++;
                }
            }

            return intersectedEdgeCount % 2 != 0;
        }
        #endregion

        /// <summary>
        /// Makes copies of edges and nodes
        /// New edges will have new IDs and new copies of nodes
        /// </summary>
        public static void CopyNodesAndEdges(List<Node> _nodes, List<Edge> _edges, out List<Node> _newNodes, out List<Edge> _newEdges, bool adjacentCheck = true, bool cleanUp = true)
        {
            _newNodes = new List<Node>();
            _newEdges = new List<Edge>();

            //Refresh adjacent nodes
            if (adjacentCheck)
                CheckAdjacentNodes(ref _nodes, ref _edges);
            if (cleanUp)
                CleanUpEdges(ref _nodes, ref _edges);

            //Calculate angle between adjacent nodes
            for (int i = 0; i < _nodes.Count; i++)
            {
                Node adj1 = null;
                Node adj2 = null;

                if (i == 0)
                {
                    adj1 = _nodes[_nodes.Count - 1];
                    adj2 = _nodes[i + 1];
                }
                else if (i == _nodes.Count - 1)
                {
                    adj1 = _nodes[i - 1];
                    adj2 = _nodes[0];
                }
                else
                {
                    adj1 = _nodes[i - 1];
                    adj2 = _nodes[i + 1];
                }

                Vector3 dirToNode1 = (adj1.Position - _nodes[i].Position).normalized;
                Vector3 dirToNode2 = (adj2.Position - _nodes[i].Position).normalized;

                Vector3 referenceForward = -dirToNode1;
                Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);

                float angleBetweenAdjs = Vector3.Angle(dirToNode1, dirToNode2);
                float angle = Vector3.Angle(dirToNode2, referenceForward);

                //Angles are in counter clockwise order, so positive dot product with right reference means angle is more than 180 degrees
                float sign = Mathf.Sign(Vector3.Dot(dirToNode2, referenceRight));

                //Center position of this node and adjacent nodes
                //Vector3 center = (adj1.Position + adj2.Position + _nodes[i].Position) / 3f;
                Vector3 center = _nodes[i].Position + (dirToNode1 + dirToNode2).normalized;

                Vector3 dirToCenter = (center - _nodes[i].Position).normalized;

                if (sign > 0)
                {
                    _nodes[i].Angle = 180f + angle;
                    _nodes[i].dirToInside = -dirToCenter;
                }
                else
                {
                    _nodes[i].Angle = angleBetweenAdjs;
                    _nodes[i].dirToInside = dirToCenter;
                }
            }

            //Save old and new IDs of nodes in order <old, new>
            Dictionary<string, string> nodeIDPairs = new Dictionary<string, string>();

            //Make new nodes and save the link to the old one
            for (int i = 0; i < _nodes.Count; i++)
            {
                Node oldNode = _nodes[i];
                Node newNode = new Node(oldNode.Position, oldNode.Angle);
                newNode.dirToInside = oldNode.dirToInside;

                if (!nodeIDPairs.ContainsKey(oldNode.ID))
                {
                    nodeIDPairs.Add(oldNode.ID, newNode.ID);
                    _newNodes.Add(newNode);
                }
            }

            //Now that we have all the links made, we can refresh adjacents for new nodes
            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = 0; j < _nodes[i].adjacents.Count; j++)
                {
                    string oldAdj = _nodes[i].adjacents[j];
                    Node newNode = _newNodes.Find(n => n.ID == nodeIDPairs[_nodes[i].ID]);
                    if (nodeIDPairs.ContainsKey(oldAdj))
                        newNode.adjacents.Add(nodeIDPairs[oldAdj]);
                    else
                        newNode.adjacents.Add(oldAdj);
                }
            }

            //Make new edges
            for (int i = 0; i < _edges.Count; i++)
            {
                Edge e = _edges[i];
                if (e == null) continue;

                //New nodes retrieved by using old node IDs
                Node n1 = _newNodes.Find(n => n.ID == nodeIDPairs[e.Node1]);
                Node n2 = _newNodes.Find(n => n.ID == nodeIDPairs[e.Node2]);

                Edge newE = new Edge(n1.ID, n2.ID, e.Width);

                _newEdges.Add(newE);
            }
        }

        /// Goes through all the edges and finds intersections
        /// If intersections are found, splits both edges in the intersection point
        /// </summary>
        /// <returns>True if an edge was fixed</returns>
        public static bool FixIntersectingEdges(ref List<Node> nodes, ref List<Edge> edges)
        {
            bool retval = false;

            CleanUpEdges(ref nodes, ref edges);

            int limit = edges.Count;
            for (int i = 0; i < edges.Count; i++)
            {
                if (i > limit) break;

                Vector3 intersectPoint = Vector3.zero;

                Node n1 = Node.GetNode(nodes, edges[i].Node1);
                Node n2 = Node.GetNode(nodes, edges[i].Node2);

                if (n1 == null || n2 == null) continue;

                Vector2 node1XZ = new Vector2(n1.Position.x, n1.Position.z);
                Vector2 node2XZ = new Vector2(n2.Position.x, n2.Position.z);

                for (int j = 0; j < edges.Count; j++)
                {
                    if (i == j) continue;

                    Node otherN1 = Node.GetNode(nodes, edges[j].Node1);
                    Node otherN2 = Node.GetNode(nodes, edges[j].Node2);

                    if (otherN1 == null || otherN2 == null ||
                        otherN1.adjacents.Contains(n1.ID) ||
                        otherN2.adjacents.Contains(n1.ID) ||
                        otherN1.adjacents.Contains(n2.ID) ||
                        otherN2.adjacents.Contains(n2.ID))
                    {
                        continue;
                    }

                    Vector2 otherN1XZ = new Vector2(otherN1.Position.x, otherN1.Position.z);
                    Vector2 otherN2XZ = new Vector2(otherN2.Position.x, otherN2.Position.z);

                    Vector3 intersectPointXZ;

                    if (UtilityTools.MathHelper.AreIntersecting(out intersectPointXZ, node1XZ, node2XZ, otherN1XZ, otherN2XZ) == 1)
                    {
                        intersectPoint = new Vector3(intersectPointXZ.x, otherN1.Position.y, intersectPointXZ.y);
                        Node intersectNode = Edge.SplitEdge(edges[i], intersectPoint, nodes, edges);
                        Edge.SplitEdge(edges[j], intersectPoint, nodes, edges, intersectNode);

                        retval = true;
                        break;
                    }
                }
            }

            return retval;
        }
    }
}