using UnityEngine;
using System;
using System.Collections.Generic;

namespace EdgeGraph
{
    [Serializable]
    public class Primitive
    {
        #region Fields and properties
        [SerializeField]
        private string id;
        public string ID
        {
            get { return id; }
        }

        public enum PrimitiveType
        {
            None,
            Filament,
            MinimalCycle
        }
        public PrimitiveType type;

        public List<Edge> edges;
        public List<Node> nodes;

        private bool isProcessed = false;

        //Primitive position
        [SerializeField]
        public Vector3 position = Vector3.zero;

        //Boundaries
        [SerializeField]
        public float minX = Mathf.Infinity;
        [SerializeField]
        public float minZ = Mathf.Infinity;
        [SerializeField]
        public float maxX = Mathf.NegativeInfinity;
        [SerializeField]
        public float maxZ = Mathf.NegativeInfinity;

        //Subedge variables
        public string parent = null;
        public int subEdgeRootIndex = 0;
        public List<Vector3> subEdgeTargets;
        public List<Node> subNodes;
        public List<Edge> subEdges;
        public int subEdgeRandomSeed = 42;
        public int subEdgeTargetCount = 20;
        public float subEdgeTargetMargin = 2f;
        public float subEdgeWidth = 0.5f;
        public float subEdgeMinAngle = 35f;
        public float subEdgeSegmentLength = .5f;
        public float subEdgeMinDistance = .3f;
        public float subEdgeMaxDistance = 2f;
        public float subEdgeNodeCombineRange = .5f;
        public float subEdgeEndConnectionRange = 2f;

        //Evaluation conditions
        private bool evaluationResult = true;
        private float minAcceptAngle = 5f;

        public bool EvaluationResult { get { return evaluationResult; } }
        #endregion

        #region Constructors
        public Primitive()
        {
            id = Guid.NewGuid().ToString();
            type = PrimitiveType.None;
            edges = new List<Edge>();
            nodes = new List<Node>();
        }

        public Primitive(PrimitiveType _type)
        {
            id = Guid.NewGuid().ToString();
            type = _type;
            edges = new List<Edge>();
            nodes = new List<Node>();
        }

        public Primitive(Primitive _p)
        {
            id = Guid.NewGuid().ToString();
            type = _p.type;
            edges = _p.edges;
            nodes = _p.nodes;
            CopyNodesAndEdges();
        }
        #endregion

        #region Primitive modification

        public void Process(bool makeNice = false)
        {
            if (isProcessed) return;
            isProcessed = true;

            CopyNodesAndEdges();

            ShiftNodes();

            if (makeNice)
            {
                CutAcuteAngles();

                CombineSubNodes(null, nodes, edges, .5f, false);
            }

            CalculateBounds();

            SortNodes();

            subEdges = new List<Edge>();
            subNodes = new List<Node>();

            Evaluate();
        }

        public void Generate(int seed = 0)
        {
            subEdgeRandomSeed = seed;

            GenerateSubEdgeTargets();

            GenerateSubEdges();
        }

        public bool Evaluate()
        {
            evaluationResult = true;

            // Check smallest angles
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].adjacents.Count != 2) continue;

                Node prevNode = EdgeGraphUtility.GetNode(nodes[i].adjacents[0], ref nodes);
                Node nextNode = EdgeGraphUtility.GetNode(nodes[i].adjacents[1], ref nodes);

                Vector3 dirToPrev = (prevNode.Position - nodes[i].Position).normalized;
                Vector3 dirToNext = (nextNode.Position - nodes[i].Position).normalized;

                if (Vector3.Angle(dirToPrev, dirToNext) < minAcceptAngle)
                    evaluationResult = false;
            }

            return evaluationResult;
        }

        /// <summary>
        /// Makes copies of edges given by the graph for later modifications
        /// New edges will have new IDs and new copies of nodes
        /// </summary>
        void CopyNodesAndEdges()
        {
            List<Node> _newNodes = new List<Node>();
            List<Edge> _newEdges = new List<Edge>();

            EdgeGraphUtility.CopyNodesAndEdges(nodes, edges, out _newNodes, out _newEdges);

            nodes = _newNodes;
            edges = _newEdges;
        }

        void ShiftNodes()
        {
            if (type != PrimitiveType.MinimalCycle) return;

            Dictionary<string, Vector3> newNodePositionDict = new Dictionary<string, Vector3>();

            for (int i = 0; i < nodes.Count; i++)
            {
                position += nodes[i].Position;

                if (nodes[i].adjacents.Count != 2) continue;

                Node prevNode = null;
                Node nextNode = null;

                if (i == 0)
                {
                    prevNode = nodes[nodes.Count - 1];
                    nextNode = nodes[i + 1];
                }
                else if (i == nodes.Count - 1)
                {
                    prevNode = nodes[i - 1];
                    nextNode = nodes[0];
                }
                else
                {
                    prevNode = nodes[i - 1];
                    nextNode = nodes[i + 1];
                }

                Vector3 dirToPrev = (prevNode.Position - nodes[i].Position).normalized;
                Vector3 dirToNext = (nextNode.Position - nodes[i].Position).normalized;

                Edge prevEdge = EdgeGraphUtility.FindEdgeByNodes(nodes[i], prevNode, edges);
                Edge nextEdge = EdgeGraphUtility.FindEdgeByNodes(nodes[i], nextNode, edges);

                Node prevEdgeOtherNode = prevEdge.Node1 == nodes[i].ID ? Node.GetNode(nodes, prevEdge.Node2) : Node.GetNode(nodes, prevEdge.Node1);
                Node nextEdgeOtherNode = nextEdge.Node1 == nodes[i].ID ? Node.GetNode(nodes, nextEdge.Node2) : Node.GetNode(nodes, nextEdge.Node1);

                if (prevEdgeOtherNode == null || nextEdgeOtherNode == null) continue;

                Vector3 prevLeftNormal = Edge.GetLeftPerpendicular(prevEdgeOtherNode.Position, nodes[i].Position);
                Vector3 nextLeftNormal = Edge.GetLeftPerpendicular(nodes[i].Position, nextEdgeOtherNode.Position);

                Vector3 newNodePos = nodes[i].Position;

                // In case the next and edge create a 180 degree angle, just shift this node towards the left normal
                if (Mathf.Approximately(Vector3.Dot(dirToNext, dirToPrev), -1f))
                {
                    float avgEdgeWidth = (prevEdge.Width + nextEdge.Width) / 2f;
                    newNodePos = nodes[i].Position + prevLeftNormal * avgEdgeWidth / 2f;
                }
                else
                {
                    // Shifted edges, streched a bit to ensure the intersection point
                    Vector3 prevEdgeNormalPoint = EdgeGraphUtility.GetEdgePosition(prevEdge.ID, ref nodes, ref edges) + prevLeftNormal * (prevEdge.Width / 2f);
                    float prevEdgeLength = Vector3.Distance(nodes[i].Position, prevEdgeOtherNode.Position);
                    Vector3 prevEdgeFar = prevEdgeNormalPoint + dirToPrev * prevEdgeLength * 2f;
                    Vector3 prevEdgeNear = prevEdgeNormalPoint - dirToPrev * prevEdgeLength * 2f;

                    Vector3 nextEdgeNormalPoint = EdgeGraphUtility.GetEdgePosition(nextEdge.ID, ref nodes, ref edges) + nextLeftNormal * (nextEdge.Width / 2f);
                    float nextEdgeLength = Vector3.Distance(nodes[i].Position, nextEdgeOtherNode.Position);
                    Vector3 nextEdgeFar = nextEdgeNormalPoint + dirToNext * nextEdgeLength * 2f;
                    Vector3 nextEdgeNear = nextEdgeNormalPoint - dirToNext * nextEdgeLength * 2f;

                    // Get intersect point of the shifted edges
                    Vector3 intersectPoint = nodes[i].Position;

                    Vector3 intersectPointXZ;

                    if (UtilityTools.MathHelper.AreIntersecting(out intersectPointXZ, prevEdgeFar, prevEdgeNear, nextEdgeFar, nextEdgeNear) == 1)
                    {
                        intersectPoint = new Vector3(intersectPointXZ.x, nodes[i].Position.y, intersectPointXZ.y);
                    }

                    //Vector3 nodeToNew = (intersectPoint - nodes[i].Position).normalized;
                    //Vector3 prevToNew = (intersectPoint - prevEdgeOtherNode.Position).normalized;
                    //Vector3 nextToNew = (intersectPoint - nextEdgeOtherNode.Position).normalized;
                    
                    newNodePos = intersectPoint;
                }

                newNodePositionDict.Add(nodes[i].ID, newNodePos);
            }

            position /= nodes.Count;

            //Set shifted positions
            for (int i = 0; i < nodes.Count; i++)
            {
                if (newNodePositionDict.ContainsKey(nodes[i].ID))
                    nodes[i].Position = newNodePositionDict[nodes[i].ID];
            }
        }
        
        struct NodePair
        {
            public string oldNode;
            public string newNode;

            public NodePair(string _old, string _new)
            {
                oldNode = _old;
                newNode = _new;
            }
        }

        public void CutAcuteAngles()
        {
            if (type != PrimitiveType.MinimalCycle) return;

            List<Node> _nodesToRemove = new List<Node>();
            List<Node> _newNodes = new List<Node>();
            List<Edge> _newEdges = new List<Edge>();

            // Old edges are kept in the edges list, but node changes are saved here and refreshed to the edges once all angles are checked
            Dictionary<string, List<NodePair>> nodesToSwitchInEdges = new Dictionary<string, List<NodePair>>();

            for (int i = 0; i < nodes.Count; i++)
            {
                Node prevNode = EdgeGraphUtility.GetNode(nodes[i].adjacents[0], ref nodes);
                Node nextNode = EdgeGraphUtility.GetNode(nodes[i].adjacents[1], ref nodes);

                Vector3 dirToPrev = (prevNode.Position - nodes[i].Position).normalized;
                Vector3 dirToNext = (nextNode.Position - nodes[i].Position).normalized;

                Edge prevEdge = EdgeGraphUtility.FindEdgeByNodes(nodes[i], prevNode, edges);
                Edge nextEdge = EdgeGraphUtility.FindEdgeByNodes(nodes[i], nextNode, edges);

                float angle = Vector3.Angle(dirToPrev, dirToNext);
                if (angle < 45f)
                {
                    // Move nodes so that the cut side is 1f wide
                    float distanceToMove = .55f / Mathf.Sin(Mathf.Deg2Rad * angle / 2f);

                    float distToPrev = Vector3.Distance(prevNode.Position, nodes[i].Position);
                    float distToNext = Vector3.Distance(nextNode.Position, nodes[i].Position);

                    if (distanceToMove > distToPrev)
                        distanceToMove = distToPrev * .8f;

                    if (distanceToMove > distToNext)
                        distanceToMove = distToNext * .8f;

                    Vector3 newNodePrevPos = (nodes[i].Position + dirToPrev * distanceToMove);
                    Vector3 newNodeNextPos = (nodes[i].Position + dirToNext * distanceToMove);

                    string oldNodeID = nodes[i].ID;

                    // Remove old node
                    _nodesToRemove.Add(nodes[i]);

                    // Make new nodes
                    Node newNodeToPrev = new Node(newNodePrevPos);

                    Node newNodeToNext = new Node(newNodeNextPos);

                    if (!nodesToSwitchInEdges.ContainsKey(prevEdge.ID))
                        nodesToSwitchInEdges.Add(prevEdge.ID, new List<NodePair>());

                    nodesToSwitchInEdges[prevEdge.ID].Add(new NodePair(oldNodeID, newNodeToPrev.ID));

                    if (!nodesToSwitchInEdges.ContainsKey(nextEdge.ID))
                        nodesToSwitchInEdges.Add(nextEdge.ID, new List<NodePair>());

                    nodesToSwitchInEdges[nextEdge.ID].Add(new NodePair(oldNodeID, newNodeToNext.ID));

                    // Add the new edge
                    Edge newEdge = new Edge(newNodeToNext.ID, newNodeToPrev.ID);

                    _newEdges.Add(newEdge);

                    // Add new nodes to the dict
                    _newNodes.Add(newNodeToPrev);
                    _newNodes.Add(newNodeToNext);
                }
            }

            foreach (var n in _nodesToRemove)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].ID == n.ID)
                    {
                        nodes.RemoveAt(i);
                        i--;
                    }
                }
            }

            foreach (var kvp in nodesToSwitchInEdges)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    if (edges[i].ID == kvp.Key)
                    {
                        Edge e = edges[i];
                        for (int j = 0; j < kvp.Value.Count; j++)
                        {
                            NodePair pair = kvp.Value[j];
                            if (e.Node1 == pair.oldNode) e.Node1 = pair.newNode;
                            if (e.Node2 == pair.oldNode) e.Node2 = pair.newNode;
                        }
                    }
                }
            }

            _newEdges.ForEach((e) => edges.Add(e));
            _newNodes.ForEach((n) => nodes.Add(n));

            EdgeGraphUtility.CleanUpEdges(ref nodes, ref edges);
            EdgeGraphUtility.CheckAdjacentNodes(ref nodes, ref edges);

        }

        void SortNodes()
        {
            List<Node> visited = new List<Node>();

            Node _n0 = nodes[0];
            Node _n1 = MinimalCycle.GetClockwiseMostAdjacent(null, _n0, ref nodes);
            Node prev = _n0;
            Node curr = _n1;

            visited.Add(_n0);

            while (curr != null && curr != _n0 && !visited.Contains(curr))
            {
                visited.Add(curr);
                Node next = MinimalCycle.GetCounterClockwiseMostAdjacent(prev, curr, ref nodes);
                prev = curr;
                curr = next;
            }

            nodes = visited;
        }
        #endregion

        #region Subedge generation

        void GenerateSubEdges()
        {
            // Save hard copies of this primitive's original nodes and edges
            List<Node> nodeCopies = new List<Node>();
            List<Edge> edgeCopies = new List<Edge>();
            EdgeGraphUtility.CopyNodesAndEdges(nodes, edges, out nodeCopies, out edgeCopies, false, false);

            if (subEdgeSegmentLength <= 0f || subEdgeTargets == null || subEdgeTargets.Count <= 0) return;

            if (subEdgeRootIndex >= nodeCopies.Count)
                subEdgeRootIndex = nodeCopies.Count - 1;

            Node rootNode = nodeCopies[subEdgeRootIndex];

            //EdgeBuilder builder = null;

            if (subEdgeSegmentLength == 0f) subEdgeSegmentLength = .5f;

            List<Node> _builtSubNodes = new List<Node>();
            List<Edge> _builtSubEdges = new List<Edge>();

            new EdgeBuilder(rootNode, subEdgeTargets, subEdgeWidth, subEdgeSegmentLength, subEdgeMinAngle, subEdgeMinDistance, subEdgeMaxDistance, (_nodes, _edges) =>
            {
                if (_nodes == null || _nodes.Count <= 0)
                {
                    Debug.Log("Primitive::GenerateSubEdges() - Builder nodes null / empty.");
                    return;
                }

                _builtSubNodes = _nodes;

                if (_edges == null || _edges.Count <= 0)
                {
                    Debug.Log("Primitive::GenerateSubEdges() - Builder edges null / empty.");
                    return;
                }
                _builtSubEdges = _edges;

                CombineSubNodes(rootNode, _builtSubNodes, _builtSubEdges, subEdgeNodeCombineRange);
            });

            nodeCopies.AddRange(_builtSubNodes);
            edgeCopies.AddRange(_builtSubEdges);

            ConnectEndPoints(nodeCopies, edgeCopies);
            CombineSubNodes(rootNode, nodeCopies, edgeCopies, subEdgeNodeCombineRange, false);

            List<Node> _copiedSubNodes = new List<Node>();
            List<Edge> _copiedSubEdges = new List<Edge>();

            EdgeGraphUtility.CopyNodesAndEdges(nodeCopies, edgeCopies, out _copiedSubNodes, out _copiedSubEdges);

            EdgeGraphUtility.CheckAdjacentNodes(ref nodeCopies, ref edgeCopies);

            EdgeGraphUtility.CleanUpEdges(ref nodeCopies, ref edgeCopies);

            subNodes = nodeCopies;
            subEdges = edgeCopies;
        }

        void GenerateSubEdgeTargets()
        {
            UnityEngine.Random.seed = subEdgeRandomSeed;

            int safeCount = 0;
            int pointCount = 0;

            subEdgeTargets = new List<Vector3>();

            while (safeCount < 1000)
            {
                safeCount++;

                //Get random point inside the boundaries
                float randX = UnityEngine.Random.Range(minX, maxX);
                float randZ = UnityEngine.Random.Range(minZ, maxZ);

                Vector3 point = new Vector3(randX, 0f, randZ);

                //If point is inside the polygon, proceed
                if (EdgeGraphUtility.PointIsInside(point, nodes, edges))
                {
                    //Check if the point is inside the margins
                    if (subEdgeTargetMargin > 0)
                    {
                        float toClosest = Mathf.Infinity;
                        //Edge closest;

                        foreach (var edge in edges)
                        {
                            Vector2 closestXZ = Edge.GetClosestPointOnEdge(point, nodes.Find(x => x.ID == edge.Node1).Position, nodes.Find(x => x.ID == edge.Node2).Position);
                            Vector3 closestPoint = new Vector3(closestXZ.x, 0f, closestXZ.y);
                            float dist = Vector3.Distance(point, closestPoint);
                            if (dist < toClosest)
                            {
                                toClosest = dist;
                                //closest = edge;
                            }
                        }

                        if (toClosest < subEdgeTargetMargin)
                            continue;
                    }

                    //If new point is too close to existing one, discard it
                    bool pointIsTooClose = false;
                    foreach (var target in subEdgeTargets)
                    {
                        if (Vector3.Distance(point, target) < subEdgeMinDistance)
                        {
                            pointIsTooClose = true;
                            break;
                        }
                    }

                    if (!pointIsTooClose)
                    {
                        subEdgeTargets.Add(point);
                        pointCount++;
                    }
                }

                if (pointCount >= subEdgeTargetCount)
                    break;
            }
        }

        void CombineSubNodes(Node _rootNode, List<Node> _nodes, List<Edge> _edges, float _combineRange, bool ensureRootEdge = true)
        {
            if (_combineRange > 0f)
            {
                for (int safe = 0; safe < 100; safe++)
                {
                    bool wasCombined = false;

                    for (int i = 0; i < _nodes.Count; i++)
                    {
                        //Get closest node
                        Node closest = Node.GetClosestNode(_nodes, _nodes[i]);
                        if (closest == _nodes[i] || closest == null) continue;

                        if (Vector3.Distance(_nodes[i].Position, closest.Position) < _combineRange)
                        {
                            _nodes[i] = Node.CombineNodes(_nodes[i], closest, _edges, _nodes);
                            _nodes.Remove(closest);
                            wasCombined = true;
                        }
                    }
                    if (!wasCombined)
                        break;
                }
            }

            if (!ensureRootEdge) return;

            // Ensure the edge between first node and root node
            Edge rootEdge = null;
            for (int i = 0; i < _edges.Count; i++)
            {
                if ((_edges[i].Node1 == _rootNode.ID && _edges[i].Node2 == _nodes[0].ID) ||
                    (_edges[i].Node2 == _rootNode.ID && _edges[i].Node1 == _nodes[0].ID))
                {
                    rootEdge = _edges[i];
                }
            }

            if (rootEdge == null)
            {
                rootEdge = new Edge(_rootNode.ID, _nodes[0].ID, subEdgeWidth);
                _edges.Add(rootEdge);
            }
        }

        void ConnectEndPoints(List<Node> _nodes, List<Edge> _edges)
        {
            // Find endpoints
            List<Node> endNodes = new List<Node>();
            for (int i = 1; i < _nodes.Count; i++)
            {
                if (_nodes[i].adjacents.Count == 1)
                    endNodes.Add(_nodes[i]);
            }

            // Connect endpoints to edges or other nodes
            for (int i = 0; i < endNodes.Count; i++)
            {
                // End node direction
                Node adjacent = Node.GetNode(_nodes, endNodes[i].adjacents[0]);
                Vector3 nodeDir = (endNodes[i].Position - adjacent.Position).normalized;

                //Check if there is any nodes in the rough direction of the end node
                float roughAngle = 30f;
                List<Node> nodesInDir = new List<Node>();

                for (int j = 0; j < _nodes.Count; j++)
                {
                    if (_nodes[j] == endNodes[i]) continue;

                    Vector3 dirToNode = (_nodes[j].Position - endNodes[i].Position).normalized;
                    if (Vector3.Angle(nodeDir, dirToNode) < roughAngle && Vector3.Angle(nodeDir, dirToNode) > 30f)
                    {
                        nodesInDir.Add(_nodes[j]);
                    }
                }

                // The node with which this endpoint is connected to
                Node connectTo = null;

                // If there are some nodes in the rough direction, pick the one that is closest
                if (nodesInDir.Count > 0)
                {
                    float toClosest = Mathf.Infinity;
                    Node closest = null;
                    for (int j = 0; j < nodesInDir.Count; j++)
                    {
                        float toCurrent = Vector3.Distance(endNodes[i].Position, nodesInDir[j].Position);
                        if (toCurrent < toClosest)
                        {
                            toClosest = toCurrent;
                            closest = nodesInDir[j];
                        }
                    }

                    if (toClosest < subEdgeEndConnectionRange)
                        connectTo = closest;
                }
                // Else get intersection with closest edge in the direction of this endpoint
                if (connectTo == null)
                {
                    // Ending point for tested segment in the direction of this endpoint
                    Vector3 segmentEnd = endNodes[i].Position + nodeDir * 1000f;

                    // Get the intersection
                    Vector3 intersectPoint = Vector3.zero;
                    // Convert all used points to XZ space
                    Vector3 intersectPointXZ = Vector2.zero;
                    Vector2 endPointXZ = new Vector2(endNodes[i].Position.x, endNodes[i].Position.z);
                    Vector2 segmentEndXZ = new Vector2(segmentEnd.x, segmentEnd.z);

                    List<Edge> intersectedEdges = new List<Edge>();
                    List<Vector3> intersectPoints = new List<Vector3>();

                    // Ignore the edge that starts on this endpoint
                    Edge endEdge = _edges.Find(e => (e.Node1 == endNodes[i].ID || e.Node2 == endNodes[i].ID));

                    for (int j = 0; j < _edges.Count; j++)
                    {
                        if (_edges[j] == endEdge) continue;

                        Node n1 = Node.GetNode(_nodes, _edges[j].Node1);
                        Node n2 = Node.GetNode(_nodes, _edges[j].Node2);

                        Vector2 node1XZ = new Vector2(n1.Position.x, n1.Position.z);
                        Vector2 node2XZ = new Vector2(n2.Position.x, n2.Position.z);

                        if (UtilityTools.MathHelper.AreIntersecting(out intersectPointXZ, endPointXZ, segmentEndXZ, node1XZ, node2XZ) == 1)
                        {
                            intersectPoints.Add(new Vector3(intersectPointXZ.x, n1.Position.y, intersectPointXZ.y));
                            intersectedEdges.Add(_edges[j]);
                        }
                    }

                    // Get closest intersect point
                    float toClosest = Mathf.Infinity;
                    Edge closestIntersectedEdge = null;
                    for (int j = 0; j < intersectPoints.Count; j++)
                    {
                        float toPoint = Vector3.Distance(endNodes[i].Position, intersectPoints[j]);
                        if (toPoint < toClosest)
                        {
                            toClosest = toPoint;
                            intersectPoint = intersectPoints[j];
                            closestIntersectedEdge = intersectedEdges[j];
                        }
                    }

                    // Split the intersected edge on the intersection
                    if (closestIntersectedEdge == null || intersectPoint == Vector3.zero)
                    {
                        Debug.Log("Primitive::ConnectEndPoints() - Intersect point not found.");
                        continue;
                    }
                    else
                        connectTo = Edge.SplitEdge(closestIntersectedEdge, intersectPoint, _nodes, _edges);
                }

                _edges.Add(new Edge(endNodes[i].ID, connectTo.ID, subEdgeWidth));
            }

            // Refresh adjacent nodes after all the endpoint connections
            EdgeGraphUtility.CheckAdjacentNodes(ref _nodes, ref _edges);
        }

        #endregion

        #region Utility

        void CalculateBounds()
        {
            //Primitive bounds
            foreach (var node in nodes)
            {
                if (node.Position.x < minX) minX = node.Position.x;
                if (node.Position.z < minZ) minZ = node.Position.z;

                if (node.Position.x > maxX) maxX = node.Position.x;
                if (node.Position.z > maxZ) maxZ = node.Position.z;
            }
        }

        #endregion
    }
}