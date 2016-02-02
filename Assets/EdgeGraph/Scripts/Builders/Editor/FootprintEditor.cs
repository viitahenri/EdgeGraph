using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using EdgeGraph;

[CustomEditor(typeof(Footprint))]
public class FootprintEditor : Editor
{
    Footprint footprint;

    void OnEnable()
    {
        footprint = (Footprint)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (footprint.nodes == null)
            footprint.nodes = new List<Node>();

        if (GUILayout.Button("Add Node", GUILayout.Width(120f)))
        {
            footprint.nodes.Add(new Node());
        }

        if (GUILayout.Button("Create edges", GUILayout.Width(120f)))
        {
            footprint.CreateEdges();
        }

        if (GUI.changed)
            EditorUtility.SetDirty(footprint);
    }

    void OnSceneGUI()
    {
        //Draw Node handles
        if (footprint.nodes == null || footprint.nodes.Count <= 0) return;

        for (int i = 0; i < footprint.nodes.Count; i++)
        {
            Vector3 worldPos = footprint.transform.TransformPoint(footprint.nodes[i].Position);
            worldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            footprint.nodes[i].Position = footprint.transform.InverseTransformPoint(worldPos);
            Handles.Label(worldPos, i.ToString(), "box");
        }

        //Draw edges
        if (footprint.edges == null || footprint.edges.Count <= 0) return;

        for (int i = 0; i < footprint.edges.Count; i++)
        {
            Handles.color = Color.green;
            Node n1 = Node.GetNode(footprint.nodes, footprint.edges[i].Node1);
            Node n2 = Node.GetNode(footprint.nodes, footprint.edges[i].Node2);

            if (n1 == null || n2 == null) break;

            Vector3 n1WorldPos = footprint.transform.TransformPoint(n1.Position);
            Vector3 n2WorldPos = footprint.transform.TransformPoint(n2.Position);

            Handles.DrawLine(n1WorldPos, n2WorldPos);
        }
    }
}
