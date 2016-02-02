using UnityEngine;
using System.Collections.Generic;

namespace EdgeGraph
{
    public class MinimalCycle
    {
        #region Primitive extraction

        /// <summary>
        /// Sort nodes by x-axis
        /// </summary>
        static void SortNodes(ref List<Node> _nodes)
        {
            _nodes.Sort((n0, n1) =>
            {
                if ((n0.Position.x == n1.Position.x && n0.Position.z < n1.Position.z) || n0.Position.x < n1.Position.x)
                    return -1;
                if (n0.Position.x == n1.Position.x && n0.Position.z == n1.Position.z)
                    return 0;
                if ((n0.Position.x == n1.Position.x && n0.Position.z > n1.Position.z) || n0.Position.x > n1.Position.x)
                    return 1;

                return 0;
            });
        }

        public static void Extract(ref List<Node> _nodes, ref List<Edge> _edges, ref List<Primitive> _primitives)
        {
            SortNodes(ref _nodes);

            ExtractPrimitivesRecursive(ref _nodes, ref _edges, ref _primitives);
        }

        static void ExtractPrimitivesRecursive(ref List<Node> _nodes, ref List<Edge> _edges, ref List<Primitive> _primitives, int limit = 0)
        {
            limit++;
            if (limit > 1000) return;
            if (_nodes.Count > 0)
            {
                ExtractPrimitive(_nodes[0], ref _nodes, ref _edges, ref _primitives);
                ExtractPrimitivesRecursive(ref _nodes, ref _edges, ref _primitives, limit);
            }

            return;
        }

        // Algorithms from http://www.geometrictools.com/Documentation/MinimalCycleBasis.pdf
        // The Minimal Cycle Basis for a Planar Graph by David Eberly

        /// <summary>
        /// Attempts to find minimal cycles
        /// </summary>
        public static void ExtractPrimitive(Node _n0, ref List<Node> _nodes, ref List<Edge> _edges, ref List<Primitive> _primitives)
        {
            List<Node> visited = new List<Node>();
            List<Node> sequence = new List<Node>();

            EdgeGraphUtility.CheckAdjacentNodes(ref _nodes, ref _edges);

            if (_n0.adjacents.Count == 0)
            {
                EdgeGraphUtility.RemoveNodeAndCleanAdjacents(_n0, ref _nodes, ref _edges);
                return;
            }

            sequence.Add(_n0);
            Node _n1 = GetClockwiseMostAdjacent(null, _n0, ref _nodes);
            Node prev = _n0;
            Node curr = _n1;

            while (curr != null && curr != _n0 && !visited.Contains(curr))
            {
                sequence.Add(curr);
                visited.Add(curr);
                Node next = GetCounterClockwiseMostAdjacent(prev, curr, ref _nodes);
                prev = curr;
                curr = next;
            }

            if (curr == null)
            {
                // Filament found, not necessarily rooted at prev
                ExtractFilament(prev, EdgeGraphUtility.GetNode(prev.adjacents[0], ref _nodes), ref _nodes, ref _edges, ref _primitives);
            }
            else if (curr == _n0)
            {
                // Minimal cycle found
                Primitive primitive = new Primitive(Primitive.PrimitiveType.MinimalCycle);
                primitive.nodes.AddRange(sequence);

                for (int i = 0; i < sequence.Count; i++)
                {
                    Node n1;
                    Node n2;
                    if (i == sequence.Count - 1)
                    {
                        n1 = sequence[i];
                        n2 = sequence[0];
                    }
                    else
                    {
                        n1 = sequence[i];
                        n2 = sequence[i + 1];
                    }
                    Edge e = EdgeGraphUtility.FindEdgeByNodes(n1, n2, _edges);
                    if (e != null)
                    {
                        primitive.edges.Add(e);
                        e.isPartOfCycle = true;
                    }
                }

                EdgeGraphUtility.RemoveEdgeAndCleanAdjacents(_n0, _n1, ref _nodes, ref _edges);

                if (_n0.adjacents.Count == 1)
                {
                    // Remove the filament rooted at v0
                    ExtractFilament(_n0, EdgeGraphUtility.GetNode(_n0.adjacents[0], ref _nodes), ref _nodes, ref _edges, ref _primitives);
                }

                if (_n1.adjacents.Count == 1)
                {
                    // Remove the filament rooted at v1
                    ExtractFilament(_n1, EdgeGraphUtility.GetNode(_n1.adjacents[0], ref _nodes), ref _nodes, ref _edges, ref _primitives);
                }

                _primitives.Add(primitive);
            }
            else   // curr was visited earlier
            {
                // A cycle has been found, but is not guaranteed to be a minimal cycle.
                // This implies v0 is part of a filament
                // Locate the starting point for the filament by traversing from v0 away from the initial v1

                while (_n0.adjacents.Count == 2)
                {
                    if (_n0.adjacents[0] != _n1.ID)
                    {
                        _n1 = _n0;
                        _n0 = EdgeGraphUtility.GetNode(_n0.adjacents[0], ref _nodes);
                    }
                    else
                    {
                        _n1 = _n0;
                        _n0 = EdgeGraphUtility.GetNode(_n0.adjacents[1], ref _nodes);
                    }
                }

                ExtractFilament(_n0, _n1, ref _nodes, ref _edges, ref _primitives);
            }
        }

        /// <summary>
        /// Extracts filament consisting of nodes and edges
        /// </summary>
        public static void ExtractFilament(Node _n0, Node _n1, ref List<Node> _nodes, ref List<Edge> _edges, ref List<Primitive> _primitives)
        {
            Edge e = EdgeGraphUtility.FindEdgeByNodes(_n0, _n1, _edges);
            if (e != null && e.isPartOfCycle)
            {
                if (_n0.adjacents.Count >= 3)
                {
                    EdgeGraphUtility.RemoveEdgeAndCleanAdjacents(_n0, _n1, ref _nodes, ref _edges);
                    _n0 = _n1;
                    if (_n0.adjacents.Count == 1) _n1 = EdgeGraphUtility.GetNode(_n0.adjacents[0], ref _nodes);
                }

                while (_n0.adjacents.Count == 1)
                {
                    _n1 = EdgeGraphUtility.GetNode(_n0.adjacents[0], ref _nodes);
                    Edge ee = EdgeGraphUtility.FindEdgeByNodes(_n0, _n1, _edges);
                    if (ee != null && e.isPartOfCycle)
                    {
                        EdgeGraphUtility.RemoveNodeAndCleanAdjacents(_n0, ref _nodes, ref _edges);
                        EdgeGraphUtility.RemoveEdgeAndCleanAdjacents(_n0, _n1, ref _nodes, ref _edges);
                    }
                    else
                        break;
                }

                if (_n0.adjacents.Count == 0)
                {
                    EdgeGraphUtility.RemoveNodeAndCleanAdjacents(_n0, ref _nodes, ref _edges);
                }
            }
            else
            {
                Primitive primitive = new Primitive(Primitive.PrimitiveType.Filament);

                if (_n0.adjacents.Count >= 3)
                {
                    primitive.nodes.Add(_n0);
                    primitive.edges.Add(e);
                    EdgeGraphUtility.RemoveEdgeAndCleanAdjacents(_n0, _n1, ref _nodes, ref _edges);
                    _n0 = _n1;
                    if (_n0.adjacents.Count == 1) _n1 = EdgeGraphUtility.GetNode(_n0.adjacents[0], ref _nodes);
                }

                while (_n0.adjacents.Count == 1)
                {
                    primitive.nodes.Add(_n0);
                    _n1 = EdgeGraphUtility.GetNode(_n0.adjacents[0], ref _nodes);
                    EdgeGraphUtility.RemoveNodeAndCleanAdjacents(_n0, ref _nodes, ref _edges);
                    Edge _e = EdgeGraphUtility.RemoveEdgeAndCleanAdjacents(_n0, _n1, ref _nodes, ref _edges);
                    if (_e != null) primitive.edges.Add(_e);
                    _n0 = _n1;
                }

                primitive.nodes.Add(_n0);
                if (_n0.adjacents.Count == 0)
                {
                    EdgeGraphUtility.RemoveNodeAndCleanAdjacents(_n0, ref _nodes, ref _edges);
                }

                _primitives.Add(primitive);
            }
        }

        public static Node GetClockwiseMostAdjacent(Node prev, Node curr, ref List<Node> _nodes)
        {
            if (curr.adjacents == null || curr.adjacents.Count == 0) return null;

            Vector3 dirCurr = Vector3.zero;

            if (prev != null)
                dirCurr = curr.Position - prev.Position;
            else
                dirCurr = new Vector3(0, 0, -1);

            Node next = null;
            if (prev != null)
                next = EdgeGraphUtility.GetNode(curr.GetAdjacent(prev.ID), ref _nodes);
            else
                next = EdgeGraphUtility.GetNode(curr.adjacents[0], ref _nodes);

            if (next == null) return null;

            Vector3 dirNext = next.Position - curr.Position;

            bool currIsConvex = DotPerp(dirNext, dirCurr) <= 0;

            foreach (var a in curr.adjacents)
            {
                var adj = EdgeGraphUtility.GetNode(a, ref _nodes);
                Vector3 dirAdj = adj.Position - curr.Position;
                if (currIsConvex)
                {
                    if (DotPerp(dirCurr, dirAdj) < 0 || DotPerp(dirNext, dirAdj) < 0)
                    {
                        next = adj;
                        dirNext = dirAdj;
                        currIsConvex = DotPerp(dirNext, dirCurr) <= 0;
                    }
                }
                else
                {
                    if (DotPerp(dirCurr, dirAdj) < 0 && DotPerp(dirNext, dirAdj) < 0)
                    {
                        next = adj;
                        dirNext = dirAdj;
                        currIsConvex = DotPerp(dirNext, dirCurr) <= 0;
                    }
                }
            }

            return next;
        }

        public static Node GetCounterClockwiseMostAdjacent(Node prev, Node curr, ref List<Node> _nodes)
        {
            if (curr.adjacents == null || curr.adjacents.Count == 0) return null;

            Vector3 dirCurr = Vector3.zero;

            dirCurr = curr.Position - prev.Position;

            Node next = EdgeGraphUtility.GetNode(curr.GetAdjacent(prev.ID), ref _nodes);
            if (next == null) return null;

            Vector3 dirNext = next.Position - curr.Position;

            bool currIsConvex = DotPerp(dirNext, dirCurr) <= 0;

            foreach (var a in curr.adjacents)
            {
                var adj = EdgeGraphUtility.GetNode(a, ref _nodes);
                Vector3 dirAdj = adj.Position - curr.Position;
                if (currIsConvex)
                {
                    if (DotPerp(dirCurr, dirAdj) > 0 && DotPerp(dirNext, dirAdj) > 0)
                    {
                        next = adj;
                        dirNext = dirAdj;
                        currIsConvex = DotPerp(dirNext, dirCurr) <= 0;
                    }
                }
                else
                {
                    if (DotPerp(dirCurr, dirAdj) > 0 || DotPerp(dirNext, dirAdj) > 0)
                    {
                        next = adj;
                        dirNext = dirAdj;
                        currIsConvex = DotPerp(dirNext, dirCurr) <= 0;
                    }
                }
            }

            return next;
        }

        //2D perp dot product in x-z space
        public static float DotPerp(Vector3 v0, Vector3 v1)
        {
            Vector3 perpv1 = new Vector3(v1.z, 0f, -v1.x);
            return Vector3.Dot(v0, perpv1);
        }

        #endregion
    }
}