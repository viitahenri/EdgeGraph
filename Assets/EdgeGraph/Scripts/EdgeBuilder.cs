using UnityEngine;
using System;
using System.Collections.Generic;

namespace EdgeGraph
{
    /// <summary>
    /// Builds edges utilizing space colonization
    /// </summary>
    [Serializable]
    public class EdgeBuilder
    {
        class Target
        {
            public Vector3 position;
            public Node closestNode;

            public Target(Vector3 pos)
            {
                position = pos;
                closestNode = null;
            }
        }

        public List<Node> nodes;
        public List<Edge> edges;

        List<Target> m_targets;
        Stack<Node> m_nodeStack;
        List<Target> m_visitedTargets;

        bool m_isFinished = false;
        Node m_rootNode;
        Node m_currentNode;
        float m_edgeWidth;
        float m_segmentLength;
        float m_minAngle;
        float m_maxDistance;
        float m_minDistance;
        Dictionary<string, int> m_nodeGrowCounts;
        Dictionary<string, Vector3> m_nodeGrowDirections;

        public EdgeBuilder(Node _root, List<Vector3> _targets, float _edgeWidth, float _segment, float _minAngle, float _minDistance, float _maxDistance, Action<List<Node>, List<Edge>> onFinished)
        {
            //m_rootNode = new Node(_root.Position);
            m_rootNode = _root;
            m_edgeWidth = _edgeWidth;
            m_segmentLength = _segment;
            m_minAngle = _minAngle;
            m_minDistance = _minDistance;
            m_maxDistance = _maxDistance;

            edges = new List<Edge>();
            nodes = new List<Node>();
            m_nodeGrowCounts = new Dictionary<string, int>();
            m_nodeGrowDirections = new Dictionary<string, Vector3>();
            m_nodeStack = new Stack<Node>();
            m_visitedTargets = new List<Target>();

            m_targets = new List<Target>();
            if (_targets.Count > 0)
            {
                _targets.ForEach((t) => m_targets.Add(new Target(t)));
                StartBuild(onFinished);
            }

        }

        void StartBuild(Action<List<Node>, List<Edge>> onFinished)
        {
            // Create first edge from root to first target
            float toClosest;
            Target initTarget = GetClosestNonVisitedTarget(m_rootNode, out toClosest, false);
            m_visitedTargets.Add(initTarget);

            m_currentNode = new Node(initTarget.position);
            nodes.Add(m_currentNode);
            m_nodeStack.Push(m_currentNode);

            m_currentNode.adjacents.Add(m_rootNode.ID);
            m_rootNode.adjacents.Add(m_currentNode.ID);

            edges.Add(new Edge(m_rootNode.ID, m_currentNode.ID, m_edgeWidth));

            // Get next target and advance to it
            Target currentTarget = GetClosestNonVisitedTarget(m_currentNode, out toClosest, false);

            AdvanceUntilClosest(ref m_currentNode, currentTarget, toClosest);

            // Build other edges
            BuildEdgesRecursive();

            EdgeGraphUtility.FixIntersectingEdges(ref nodes, ref edges);

            if (onFinished != null)
                onFinished(nodes, edges);
        }

        /// <summary>
        /// Get closest non visited target
        /// </summary>
        Target GetClosestNonVisitedTarget(Node node, out float toClosest, bool checkMaxDistance = true)
        {
            Vector3 nodePos = node.Position;
            Target closest = null;
            float _toClosest = Mathf.Infinity;
            foreach (var t in m_targets)
            {
                float toTarget = Vector3.Distance(nodePos, t.position);
                if (toTarget < _toClosest && !m_visitedTargets.Contains(t))
                {
                    if (checkMaxDistance && toTarget > m_maxDistance)
                        continue;

                    closest = t;
                    _toClosest = Vector3.Distance(nodePos, closest.position);
                }
            }

            toClosest = _toClosest;
            return closest;
        }

        Node GetClosestNodeToTarget(Target target)
        {
            Node closest = null;
            float toClosest = Mathf.Infinity;
            float toCurrent = Mathf.Infinity;
            for (int i = 0; i < nodes.Count; i++)
            {
                toCurrent = Vector3.Distance(nodes[i].Position, target.position);
                if (toCurrent < toClosest)
                {
                    closest = nodes[i];
                    toClosest = toCurrent;
                }
            }

            return closest;
        }

        void AdvanceUntilClosest(ref Node currentNode, Target closest, float toClosest)
        {
            Vector3 currentDir = (closest.position - currentNode.Position).normalized;

            int counter = 0;
            float lastToClosest = 0f;

            while (counter < 100)
            {
                if (toClosest > m_minDistance)
                {
                    currentDir = (closest.position - currentNode.Position).normalized;

                    if (Advance(ref currentNode, currentDir))
                        m_nodeStack.Push(currentNode);

                    lastToClosest = toClosest;
                    toClosest = Vector3.Distance(currentNode.Position, closest.position);

                    // If we are suddenly going away from the closest node, end
                    if (toClosest > lastToClosest)
                        break;
                }
                else
                    break;
                counter++;
            }

            m_visitedTargets.Add(closest);
        }

        //Round orientation to closest multiply of angle
        //void RoundOrientation(Vector3 _currentPos, ref Vector3 _currentDir)
        //{
        //    if (m_minAngle == 0f) return;

        //    float currentOrientation = Vector3.Angle(Vector3.forward, _currentDir);

        //    float a = currentOrientation / m_minAngle;
        //    a = Mathf.Floor(a);
        //    currentOrientation = a * m_minAngle;

        //    //Angle variance
        //    currentOrientation = UnityEngine.Random.Range(currentOrientation - m_angleVariance, currentOrientation + m_angleVariance);

        //    float newX = _currentPos.x + Mathf.Cos(Mathf.Deg2Rad * currentOrientation);
        //    float newZ = _currentPos.z + Mathf.Sin(Mathf.Deg2Rad * currentOrientation);

        //    _currentDir = (new Vector3(newX, 0f, newZ) - _currentPos).normalized;
        //}

        /// <summary>
        /// Advances forward by segmentLength from current node
        /// </summary>
        /// <param name="_node"></param>
        /// <param name="_dir"></param>
        /// <returns>True if new node was made</returns>
        bool Advance(ref Node _node, Vector3 _dir)
        {
            bool retval = false;

            Vector3 newPos = _node.Position + _dir * m_segmentLength;

            if (newPos == _node.Position)
                return retval;

            bool addNew = false;

            if (_node.adjacents.Count > 0)
            {
                Node adj = Node.GetNode(nodes, _node.adjacents[0]);
                if (adj != null)
                {
                    Vector3 dirToPrevious = (adj.Position - _node.Position).normalized;
                    // If direction stays the same, just move current node
                    float dot = Vector3.Dot(_dir.normalized, dirToPrevious);

                    if (Mathf.Approximately(dot, -1f))
                    {
                        _node.Position = newPos;
                    }
                    else
                    {
                        // If resulting angle is too sharp, make a 90 degree turn
                        Vector3 referenceRight = Vector3.Cross(Vector3.up, dirToPrevious);
                        // Pick adjacent with which current direction creates the smallest angle
                        float angle = Mathf.Infinity;
                        for (int i = 0; i < _node.adjacents.Count; i++)
                        {
                            adj = Node.GetNode(nodes, _node.adjacents[0]);
                            Vector3 dirToCur = (adj.Position - _node.Position).normalized;
                            float toCur = Vector3.Angle(dirToCur, _dir);
                            if (toCur < angle)
                            {
                                angle = toCur;
                                dirToPrevious = dirToCur;
                            }
                        }
                        if (angle < m_minAngle)
                        {
                            //Check if current direction points left or right from previous and make a 90 degree turn to that direction
                            if (Vector3.Dot(referenceRight, _dir) > 0) // Right
                                _dir = Vector3.Cross(Vector3.up, dirToPrevious);
                            else
                                _dir = Vector3.Cross(dirToPrevious, Vector3.up);

                            newPos = _node.Position + _dir * m_segmentLength;
                        }

                        addNew = true;
                    }
                }
                else
                    addNew = true;
            }
            else
                addNew = true;

            if (addNew)
            {
                Node newNode = new Node(newPos);
                nodes.Add(newNode);

                newNode.adjacents.Add(_node.ID);
                _node.adjacents.Add(newNode.ID);

                edges.Add(new Edge(_node.ID, newNode.ID, m_edgeWidth));

                _node = newNode;

                retval = true;
            }

            return retval;
        }

        void BuildEdgesRecursive(int limit = 0)
        {
            limit++;
            if (limit >= 1000 || m_isFinished) return;
            // If all targets have been visited, we are finished
            else if (m_visitedTargets.Count == m_targets.Count)
            {
                m_isFinished = true;
                return;
            }
            else if (m_visitedTargets.Count < m_targets.Count)
            {
                BuildEdges();
                BuildEdgesRecursive(limit);
            }

            return;
        }

        void BuildEdges()
        {
            float toClosest;
            Target currentTarget = GetClosestNonVisitedTarget(m_currentNode, out toClosest);

            //If no target was found, trace back node stack
            if (currentTarget == null)
            {
                for (int i = 0; i < m_nodeStack.Count; i++)
                {
                    Node n = m_nodeStack.Pop();
                    currentTarget = GetClosestNonVisitedTarget(n, out toClosest);
                    if (currentTarget != null)
                    {
                        m_currentNode = n;
                        break;
                    }
                }

                /// If no target was found but there is still unvisited targets,
                /// connect one of these targets to the closest node from it
                if (currentTarget == null && m_visitedTargets.Count < m_targets.Count)
                {
                    //Empty the stack as we sort of start a new progress
                    m_nodeStack = new Stack<Node>();

                    Target nonVisitedTarget = null;
                    for (int j = 0; j < m_targets.Count; j++)
                    {
                        if (!m_visitedTargets.Contains(m_targets[j]))
                        {
                            nonVisitedTarget = m_targets[j];
                            break;
                        }
                    }

                    //Find closest node to this non-visited target
                    if (nonVisitedTarget != null)
                    {
                        Node closest = GetClosestNodeToTarget(nonVisitedTarget);
                        if (closest != null)
                        {
                            m_currentNode = closest;
                            //Get closest target to this node
                            currentTarget = GetClosestNonVisitedTarget(m_currentNode, out toClosest, false);
                        }
                    }
                }

                /// If current target is still null, we are finished
                if (currentTarget == null)
                {
                    m_isFinished = true;
                    return;
                }
            }

            AdvanceUntilClosest(ref m_currentNode, currentTarget, toClosest);
        }

        ///// <summary>
        ///// Goes through all the built edges and finds intersections
        ///// If intersections are found, splits both edges in the intersection point
        ///// </summary>
        //void FixIntersectingEdges()
        //{
        //    EdgeGraphUtility.CleanUpEdges(ref nodes, ref edges);

        //    int limit = edges.Count;
        //    for (int i = 0; i < edges.Count; i++)
        //    {
        //        if (i > limit) break;

        //        Vector3 intersectPoint = Vector3.zero;

        //        Node n1 = Node.GetNode(nodes, edges[i].Node1);
        //        Node n2 = Node.GetNode(nodes, edges[i].Node2);

        //        if (n1 == null || n2 == null) continue;

        //        Vector2 node1XZ = new Vector2(n1.Position.x, n1.Position.z);
        //        Vector2 node2XZ = new Vector2(n2.Position.x, n2.Position.z);

        //        for (int j = 0; j < edges.Count; j++)
        //        {
        //            if (i == j) continue;

        //            Node otherN1 = Node.GetNode(nodes, edges[j].Node1);
        //            Node otherN2 = Node.GetNode(nodes, edges[j].Node2);

        //            if (otherN1 == null || otherN2 == null || 
        //                otherN1.adjacents.Contains(n1.ID) || 
        //                otherN2.adjacents.Contains(n1.ID) || 
        //                otherN1.adjacents.Contains(n2.ID) || 
        //                otherN2.adjacents.Contains(n2.ID))
        //            {
        //                continue;
        //            }

        //            Vector2 otherN1XZ = new Vector2(otherN1.Position.x, otherN1.Position.z);
        //            Vector2 otherN2XZ = new Vector2(otherN2.Position.x, otherN2.Position.z);

        //            Vector3 intersectPointXZ;

        //            if (MentalTools.LineHelper.AreIntersecting(out intersectPointXZ, node1XZ, node2XZ, otherN1XZ, otherN2XZ) == 1)
        //            {
        //                intersectPoint = new Vector3(intersectPointXZ.x, otherN1.Position.y, intersectPointXZ.y);
        //                Node intersectNode = Edge.SplitEdge(edges[i], intersectPoint, nodes, edges);
        //                Edge.SplitEdge(edges[j], intersectPoint, nodes, edges, intersectNode);
        //                break;
        //            }
        //        }
        //    }
        //}

        #region Colonization

        void ColonizeSpaceRecursive(int limit = 0)
        {
            limit++;
            if (m_isFinished || limit > 100)
            {
                Debug.Log("EdgeBuilder::BuildEdgesRecursive() - Finished or limit reached, node count: " + nodes.Count + ", edge count: " + edges.Count);
                return;
            }
            else if (m_targets.Count == 0)
            {
                Debug.Log("EdgeBuilder::BuildEdgesRecursive() - No targets left, node count: " + nodes.Count + ", edge count: " + edges.Count);
                m_isFinished = true;
                return;
            }
            else if (m_targets.Count > 0)
            {
                ColonizeSpace();
                ColonizeSpaceRecursive(limit);
            }

            return;
        }

        void ColonizeSpace()
        {
            //Process targets
            for (int i = 0; i < m_targets.Count; i++)
            {
                bool targetRemoved = false;

                m_targets[i].closestNode = null;
                Vector3 direction = Vector3.zero;

                //Find closest node for this target
                for (int j = 0; j < nodes.Count; j++)
                {
                    direction = m_targets[i].position - nodes[j].Position;
                    float distance = direction.magnitude;
                    direction.Normalize();

                    //If min distance is reached, we remove the target
                    if (distance <= m_minDistance)
                    {
                        m_targets.RemoveAt(i);
                        i--;
                        targetRemoved = true;
                        break;
                    }
                    //If target is in range, check if it is closest
                    else if (distance <= m_maxDistance)
                    {
                        if (m_targets[i].closestNode == null)
                            m_targets[i].closestNode = nodes[j];
                        else if ((m_targets[i].position - m_targets[i].closestNode.Position).magnitude > distance)
                            m_targets[i].closestNode = nodes[j];
                    }

                    //If target is not removed, set the grow parameters on all the closest nodes that are in range
                    if (!targetRemoved && m_targets[i].closestNode != null)
                    {
                        Vector3 dir = (m_targets[i].position - m_targets[i].closestNode.Position).normalized;
                        string closestID = m_targets[i].closestNode.ID;
                        if (m_nodeGrowDirections.ContainsKey(closestID))
                            m_nodeGrowDirections[closestID] += dir;
                        else
                            m_nodeGrowDirections.Add(closestID, dir);

                        if (m_nodeGrowCounts.ContainsKey(closestID))
                            m_nodeGrowCounts[closestID]++;
                        else
                            m_nodeGrowCounts.Add(closestID, 1);
                    }
                }
            }

            //Advance nodes towards nearest targets
            bool nodeAdded = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                string nodeID = nodes[i].ID;
                //If at least one target is affecting the current node
                if (m_nodeGrowCounts.ContainsKey(nodeID) && m_nodeGrowCounts[nodeID] > 0)
                {
                    Vector3 avgDirection = m_nodeGrowDirections[nodeID] / (float)m_nodeGrowCounts[nodeID];
                    avgDirection.Normalize();

                    //RoundOrientation(nodes[i].Position, ref avgDirection);

                    Node newNode = new Node(nodes[i].Position);
                    nodes.Add(newNode);
                    if (Advance(ref newNode, avgDirection))
                    {
                        m_nodeGrowCounts[nodeID] = 0;
                        m_nodeGrowDirections[nodeID] = Vector3.zero;
                        nodeAdded = true;
                    }
                }
            }

            //If no nodes were added, we are done
            if (!nodeAdded)
                m_isFinished = true;
        }

        #endregion
    }
}