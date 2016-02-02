using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using EdgeGraph;

[CustomEditor(typeof(BuildingFacade))]
public class BuildingFacadeEditor : Editor
{
    BuildingFacade facade;

    void OnEnable()
    {
        facade = (BuildingFacade)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (facade.nodes == null)
            facade.nodes = new List<Node>();

        if (GUILayout.Button("Add Node", GUILayout.Width(120f)))
        {
            facade.nodes.Add(new Node());
        }

        if (GUILayout.Button("Create edges", GUILayout.Width(120f)))
        {
            if (facade.nodes.Count < 4) return;

            facade.edges = new List<Edge>();

            facade.edges.Add(new Edge(facade.nodes[0].ID, facade.nodes[1].ID));
            facade.edges.Add(new Edge(facade.nodes[2].ID, facade.nodes[3].ID));
        }

        if (GUI.changed)
            EditorUtility.SetDirty(facade);
    }

    void OnSceneGUI()
    {
        //Draw Node handles
        if (facade.nodes == null || facade.nodes.Count <= 0) return;

        for(int i = 0; i < facade.nodes.Count; i++)
        {
            Vector3 worldPos = facade.transform.TransformPoint(facade.nodes[i].Position);
            worldPos = Handles.FreeMoveHandle(worldPos, Quaternion.identity, HandleUtility.GetHandleSize(worldPos) * .3f, Vector3.zero, Handles.SphereCap);
            facade.nodes[i].Position = facade.transform.InverseTransformPoint(worldPos);
            Handles.Label(worldPos, i.ToString(), "box");
        }

        //Draw edges
        if (facade.edges == null || facade.edges.Count <= 0) return;

        for (int i = 0; i < facade.edges.Count; i++)
        {
            Handles.color = Color.green;
            Node n1 = Node.GetNode(facade.nodes, facade.edges[i].Node1);
            Node n2 = Node.GetNode(facade.nodes, facade.edges[i].Node2);

            if (n1 == null || n2 == null) break;

            Vector3 n1WorldPos = facade.transform.TransformPoint(n1.Position);
            Vector3 n2WorldPos = facade.transform.TransformPoint(n2.Position);

            Handles.DrawLine(n1WorldPos, n2WorldPos);
        }
    }
}
