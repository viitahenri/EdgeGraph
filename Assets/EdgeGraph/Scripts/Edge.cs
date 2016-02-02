using UnityEngine;
using System;
using System.Collections.Generic;

namespace EdgeGraph
{
    [Serializable]
    public class Edge
    {
        #region Fields and properties
        [SerializeField]
        private string id;
        public string ID
        {
            get { return id; }
        }

        [SerializeField]
        private string node1;
        public string Node1
        {
            get { return node1; }
            set { node1 = value; }
        }

        [SerializeField]
        private string node2;
        public string Node2
        {
            get { return node2; }
            set { node2 = value; }
        }

        [SerializeField]
        private float width;
        public float Width
        {
            get { return width; }
            set { width = value; }
        }

        [System.NonSerialized]
        public bool isPartOfCycle = false;
        #endregion

        #region Constructors
        public Edge()
        {
            id = Guid.NewGuid().ToString();
            width = 0f;
        }

        public Edge(string _node1, string _node2)
        {
            id = Guid.NewGuid().ToString();
            node1 = _node1;
            node2 = _node2;
            width = 0f;
        }

        public Edge(string _node1, string _node2, float _width)
        {
            id = Guid.NewGuid().ToString();
            node1 = _node1;
            node2 = _node2;
            width = _width;
        }

        public Edge(Edge edge)
        {
            id = edge.ID;
            node1 = edge.Node1;
            node2 = edge.Node2;
            width = edge.Width;
        }
        #endregion

        #region Utility

        public string GetAdjacentNode(Node current)
        {
            if (current.ID == node1)
                return node2;
            if (current.ID == node2)
                return node1;

            return null;
        }

        public string GetAdjacentNode(string current)
        {
            if (current == node1)
                return node2;
            if (current == node2)
                return node1;

            return null;
        }

        /// <summary>
        /// Splits given edge into two edges at given point
        /// If a node is given, it will be used to split the edges
        /// Otherwise make a new node
        /// </summary>
        public static Node SplitEdge(Edge edge, Vector3 point, List<Node> nodes, List<Edge> edges, Node node = null)
        {
            bool isNewNode = false;
            // Create new node
            if (node == null)
            {
                node = new Node(point);
                isNewNode = true;
            }

            // Get start and end nodes of the edge
            Node start = Node.GetNode(nodes, edge.Node1);
            Node end = Node.GetNode(nodes, edge.Node2);

            // Add new node between start and end
            start.adjacents.Remove(end.ID);
            end.adjacents.Remove(start.ID);

            start.adjacents.Add(node.ID);
            end.adjacents.Add(node.ID);

            int startIndex = nodes.IndexOf(start);

            if (startIndex == -1)
            {
                Debug.LogError("Edge::SplitEdge() - StartIndex not found. Are you sure you gave the right node list?");
                return null;
            }

            if (isNewNode)
            {
                if (startIndex == nodes.Count - 1)
                    nodes.Add(node);
                else
                    nodes.Insert(startIndex + 1, node);
            }

            // Remove original edge from edges
            edges.Remove(edge);

            // Create new edges
            Edge e1 = new Edge(start.ID, node.ID, edge.width);
            Edge e2 = new Edge(node.ID, end.ID, edge.width);

            edges.Add(e1);
            edges.Add(e2);

            return node;
        }

        public static Vector3 GetLeftPerpendicular(Vector3 v1, Vector3 v2)
        {
            float dx = v2.x - v1.x;
            float dz = v2.z - v1.z;
            return new Vector3(-dz, v1.y, dx).normalized;
        }

        public static Vector3 GetRightPerpendicular(Vector3 v1, Vector3 v2)
        {
            float dx = v2.x - v1.x;
            float dz = v2.z - v1.z;
            return new Vector3(dz, v1.y, -dx).normalized;
        }

        /// <summary>
        /// Return left or right normal for edge, depending on which normal points towards the given point.
        /// </summary>
        public static Vector3 GetPerpendicularTowardsPoint(Vector3 v1, Vector3 v2, Vector3 point)
        {
            //Perpendicular vectors
            Vector3 left = GetLeftPerpendicular(v1, v2);
            Vector3 right = GetRightPerpendicular(v1, v2);

            //Center point of the line
            Vector3 center = (v1 + v2) / 2f;

            //Direction from center to point
            Vector3 toPoint = (point - center).normalized;

            //Dot products
            float dotLeft = Vector3.Dot(left, toPoint);
            float dotRight = Vector3.Dot(right, toPoint);

            //Return perpendicular that is towards the point
            return dotLeft > dotRight ? left : right;
        }

        public static Vector3 GetPerpendicularComparedToDirection(Vector3 v1, Vector3 v2, Vector3 dir)
        {
            //Perpendicular vectors
            Vector3 left = GetLeftPerpendicular(v1, v2);
            Vector3 right = GetRightPerpendicular(v1, v2);

            //Dot products
            float dotLeft = Vector3.Dot(left, dir);
            float dotRight = Vector3.Dot(right, dir);

            //Return perpendicular that is towards the point
            return dotLeft > dotRight ? left : right;
        }

        /// <summary>
        /// Returns shortest distance between point and edge (v1,v2) in X-Z space.
        /// </summary>
        /// Source: http://www.randygaul.net/2014/07/23/distance-point-to-line-segment/
        //public static float GetPointDistanceToEdge(Vector3 point, Vector3 _v1, Vector3 _v2)
        //{
        //    Vector2 p = new Vector2(point.x, point.z);
        //    Vector2 a = new Vector2(_v1.x, _v1.z);
        //    Vector2 b = new Vector2(_v2.x, _v2.z);

        //    Vector2 n = b - a;
        //    Vector2 pa = a - p;
        //    Vector2 c = n * (Vector2.Dot(pa, n) / Vector2.Dot(n, n));
        //    Vector2 d = pa - c;

        //    return Mathf.Sqrt(Vector2.Dot(d, d));
        //}

        /// <summary>
        /// Returns closest point to an edge (v1,v2) in X-Z space.
        /// </summary>
        /// Source: http://www.gamedev.net/topic/444154-closest-point-on-a-line/#entry3941160
        public static Vector2 GetClosestPointOnEdge(Vector3 point, Vector3 _v1, Vector3 _v2)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 a = new Vector2(_v1.x, _v1.z);
            Vector2 b = new Vector2(_v2.x, _v2.z);

            Vector2 ap = p - a;
            Vector2 ab = b - a;

            float ab2 = ab.x * ab.x + ab.y * ab.y;
            float ap_ab = ap.x * ab.x + ap.y * ab.y;
            float t = ap_ab / ab2;

            if (t < 0.0f) t = 0.0f;
            else if (t > 1.0f) t = 1.0f;

            return (a + ab * t);
        }
        #endregion
    }
}