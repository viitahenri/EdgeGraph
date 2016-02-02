using UnityEngine;
using System;
using System.Collections.Generic;

namespace EdgeGraph
{
    [Serializable]
    public class Node
    {
        #region Fields and properties
        [SerializeField]
        private string _id;
        public string ID
        {
            get { return _id; }
        }

        [SerializeField]
        private Vector3 position;
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        [HideInInspector]
        [SerializeField]
        private float angle;
        public float Angle
        {
            get { return angle; }
            set { angle = value; }
        }

        [HideInInspector]
        [SerializeField]
        public Vector3 dirToInside;

        [HideInInspector]
        [SerializeField]
        public List<string> adjacents;
        #endregion

        #region Constructors
        public Node()
        {
            _id = Guid.NewGuid().ToString();
            position = Vector3.zero;
            adjacents = new List<string>();
            angle = 0f;
        }

        public Node(Node node)
        {
            _id = node.ID;
            position = node.Position;
            adjacents = node.adjacents;
            angle = node.angle;
        }

        public Node(Vector3 pos)
        {
            _id = Guid.NewGuid().ToString();
            position = pos;
            adjacents = new List<string>();
            angle = 0f;
        }

        public Node(Vector3 pos, float _angle)
        {
            _id = Guid.NewGuid().ToString();
            position = pos;
            adjacents = new List<string>();
            angle = _angle;
        }

        #endregion

        #region Minimal cycle

        public void CheckAdjacents(List<Edge> _edges)
        {
            //Init adjacents
            adjacents = new List<string>();

            //Assign adjacents
            for (int j = 0; j < _edges.Count; j++)
            {
                string adjacent = _edges[j].GetAdjacentNode(this);
                if (!string.IsNullOrEmpty(adjacent) && !adjacents.Contains(adjacent))
                    adjacents.Add(adjacent);
            }
        }

        public string GetAdjacent(string ignored)
        {
            for (int i = 0; i < adjacents.Count; i++)
            {
                if (adjacents[i] != ignored)
                    return adjacents[i];
            }

            return null;
        }
        #endregion

        #region Static methods

        public static Node GetNode(List<Node> nodes, string id)
        {
            if (nodes == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].ID.Equals(id)) return nodes[i];
            }
            return null;
        }

        /// <summary>
        /// Combines two nodes into one
        /// </summary>
        /// <param name="node1">First node, this node is transformed into the combined node</param>
        /// <param name="node2">Second node</param>
        /// <param name="edges">List of edges in which the IDs are fixed</param>
        /// <returns>Combined node</returns>
        public static Node CombineNodes(Node node1, Node node2, List<Edge> edges, List<Node> nodes)
        {
            //Combine node positions
            node1.position = (node1.position + node2.position) / 2f;
            //Fix edges
            for (int i = 0; i < edges.Count; i++)
            {
                //Remove edges that end in the node2, which will be removed
                if (node2.adjacents.Count == 1 && (edges[i].Node1 == node2.ID || edges[i].Node2 == node2.ID))
                {
                    edges.RemoveAt(i);
                    i--;
                    continue;
                }

                if (edges[i].Node1 == node2.ID)
                    edges[i].Node1 = node1.ID;

                if (edges[i].Node2 == node2.ID)
                    edges[i].Node2 = node1.ID;
            }

            EdgeGraphUtility.CleanUpEdges(ref nodes, ref edges);

            //Fix node adjacents
            EdgeGraphUtility.CheckAdjacentNodes(ref nodes, ref edges);

            return node1;
        }

        /// <summary>
        /// Gets closest node to point
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Node GetClosestNode(List<Node> nodes, Node other)
        {
            Node closest = null;
            float toClosest = Mathf.Infinity;
            float toCurrent = Mathf.Infinity;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == other) continue;

                toCurrent = Vector3.Distance(nodes[i].Position, other.Position);
                if (toCurrent < toClosest)
                {
                    closest = nodes[i];
                    toClosest = toCurrent;
                }
            }

            return closest;
        }

        #endregion
    }
}