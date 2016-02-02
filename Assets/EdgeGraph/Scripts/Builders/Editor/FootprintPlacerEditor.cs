using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using EdgeGraph;

[CustomEditor(typeof(FootprintPlacer))]
public class FootprintPlacerEditor : Editor
{
    FootprintPlacer placer;
    UtilityTools.EditorControls controls;

    int handPlacedIndex = 0;

    void OnEnable()
    {
        placer = (FootprintPlacer)target;

        placer.graph = placer.GetComponent<Graph>();
        if (placer.graph == null)
            placer.graph = placer.GetComponentInParent<Graph>();

        controls = new UtilityTools.EditorControls(placer.gameObject);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Clear edge", GUILayout.Width(100f)))
        {
            placer.ClearUnmodifiedFootprints(true);
        }

        if (GUILayout.Button("Clear inside", GUILayout.Width(100f)))
        {
            placer.ClearUnmodifiedFootprints(false);
        }

        if (GUILayout.Button("Fill Edges", GUILayout.Width(100f)))
        {
            placer.UpdateData();
            placer.FillWithEdgesWithFootprints();
        }

        if (GUILayout.Button("Call Fill Edges On Children", GUILayout.Width(200f)))
        {
            UtilityTools.EditorCoroutine.start(FillChildrenEdgesWithFootprints());
        }

        if (GUILayout.Button("Fill Inside", GUILayout.Width(100f)))
        {
            placer.UpdateData();
            placer.FillInsideWithFootprints();

            EditorUtility.ClearProgressBar();
        }

        if (GUILayout.Button("Call Fill Inside On Children", GUILayout.Width(200f)))
        {
            UtilityTools.EditorCoroutine.start(FillChildrenInsidesWithFootprints());
        }
    }

    IEnumerator FillChildrenEdgesWithFootprints()
    {
        long startTime = DateTime.Now.Ticks;
        EditorUtility.DisplayProgressBar("FillChildrenEdgesWithFootprints", "Filling children graphs with footprints", 0f);
        yield return null;

        for (int i = 0; i < placer.transform.childCount; i++)
        {
            FootprintPlacer childPlacer = placer.transform.GetChild(i).GetComponent<FootprintPlacer>();
            if (!placer) continue;

            childPlacer.footprintPrefabsOnEdge = placer.footprintPrefabsOnEdge;

            try
            {
                childPlacer.UpdateData();
                childPlacer.FillWithEdgesWithFootprints();
                Debug.Log("FillChildrenEdgesWithFootprints... " + (i + 1) + "/" + placer.transform.childCount + " (" + (DateTime.Now.Ticks - startTime) / 10000000 + "s)");
                EditorUtility.DisplayProgressBar("FillChildrenEdgesWithFootprints", "Filling children graphs' edges with footprints " + (i + 1) + "/" + placer.transform.childCount, (float)i / placer.transform.childCount);
            }
            catch (Exception e)
            {
                Debug.Log("FootprintPlacerEditor::FillChildrenEdgesWithFootprints() - Error: " + e.Message);
                EditorUtility.ClearProgressBar();
            }
            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    IEnumerator FillChildrenInsidesWithFootprints()
    {
        long startTime = DateTime.Now.Ticks;
        EditorUtility.DisplayProgressBar("FillChildrenInsidesWithFootprints", "Filling children graphs with footprints", 0f);
        yield return null;

        for (int i = 0; i < placer.transform.childCount; i++)
        {
            FootprintPlacer childPlacer = placer.transform.GetChild(i).GetComponent<FootprintPlacer>();
            if (!placer) continue;

            childPlacer.footprintPrefabsInside = placer.footprintPrefabsInside;

            try
            {
                childPlacer.UpdateData();
                childPlacer.FillInsideWithFootprints();
                Debug.Log("FillChildrenInsidesWithFootprints... " + (i + 1) + "/" + placer.transform.childCount + " (" + (DateTime.Now.Ticks - startTime) / 10000000 + "s)");
                EditorUtility.DisplayProgressBar("FillChildrenInsidesWithFootprints", "Filling children graphs' insides footprints " + (i + 1) + "/" + placer.transform.childCount, (float)i / placer.transform.childCount);
            }
            catch (Exception e)
            {
                Debug.Log("FootprintPlacerEditor::FillChildrenInsidesWithFootprints() - Error: " + e.Message);
                EditorUtility.ClearProgressBar();
            }
            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    void OnSceneGUI()
    {
        if (placer.handPlacementEnabled && placer.footprintPrefabsOnEdge.Count > 0)
        {
            controls.Update();

            Vector3 newFootprintPos = Vector3.zero;
            Quaternion newFootprintRot = Quaternion.identity;

            bool placeInside = EdgeGraphUtility.PointIsInside(controls.cursorLocalPosition, placer.graph.mainPrimitives[0].nodes, placer.graph.mainPrimitives[0].edges);

            if (controls.controlIsPressed)
            {
                if (placer.handPlacementOnEdge)
                {
                    if (handPlacedIndex >= placer.footprintPrefabsOnEdge.Count)
                        handPlacedIndex = placer.footprintPrefabsOnEdge.Count - 1;
                }
                else
                {
                    if (handPlacedIndex >= placer.footprintPrefabsInside.Count)
                        handPlacedIndex = placer.footprintPrefabsInside.Count - 1;
                }

                if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W)
                {
                    if (placer.handPlacementOnEdge)
                        handPlacedIndex = UtilityTools.Helper.GetNextIndex<GameObject>(placer.footprintPrefabsOnEdge, handPlacedIndex);
                    else
                        handPlacedIndex = UtilityTools.Helper.GetNextIndex<GameObject>(placer.footprintPrefabsInside, handPlacedIndex);
                }
                else if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Q)
                {
                    if (placer.handPlacementOnEdge)
                        handPlacedIndex = UtilityTools.Helper.GetPrevIndex<GameObject>(placer.footprintPrefabsOnEdge, handPlacedIndex);
                    else
                        handPlacedIndex = UtilityTools.Helper.GetPrevIndex<GameObject>(placer.footprintPrefabsInside, handPlacedIndex);
                }
                else if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.E)
                {
                    placer.handPlacementOnEdge = !placer.handPlacementOnEdge;
                }

                Quaternion rot;
                Vector3 closestPoint;
                placer.GetFootprintPosAndRotAtTarget(controls.cursorWorldPosition, out newFootprintPos, out rot, out closestPoint, handPlacedIndex, placer.handPlacementOnEdge, placeInside);

                if (placer.handPlacementOnEdge)
                    newFootprintRot = rot;

                Footprint fp;
                if (placer.handPlacementOnEdge)
                    fp = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(placer.footprintPrefabsOnEdge[handPlacedIndex]);
                else
                    fp = UtilityTools.Helper.GetComponentInPrefabChildren<Footprint>(placer.footprintPrefabsInside[handPlacedIndex]);

                if (fp != null)
                {
                    Handles.color = Color.yellow;
                    Matrix4x4 newPosMatrix = Matrix4x4.TRS(newFootprintPos, newFootprintRot, Vector3.one);
                    for (int i = 0; i < fp.nodes.Count; i++)
                    {
                        Node cur = fp.nodes[i];
                        Node next;
                        if (i == fp.nodes.Count - 1)
                            next = fp.nodes[0];
                        else
                            next = fp.nodes[i + 1];

                        Handles.DrawLine(newPosMatrix.MultiplyPoint3x4(cur.Position), newPosMatrix.MultiplyPoint3x4(next.Position));
                        Handles.CubeCap(0, newPosMatrix.MultiplyPoint3x4(cur.Position), Quaternion.LookRotation(newPosMatrix.GetColumn(2), newPosMatrix.GetColumn(1)), .2f);
                        if (placer.handPlacementOnEdge)
                            Handles.Label(controls.cursorWorldPosition, placer.footprintPrefabsOnEdge[handPlacedIndex].name, "box");
                        else
                            Handles.Label(controls.cursorWorldPosition, placer.footprintPrefabsInside[handPlacedIndex].name, "box");

                        Handles.SphereCap(0, newFootprintPos, Quaternion.identity, .2f);
                        Handles.SphereCap(0, closestPoint, Quaternion.identity, .2f);
                    }
                }
            }

            if (controls.MouseClickedDown())
            {
                if (controls.controlIsPressed)
                {
                    placer.PlaceFootprint(newFootprintPos, newFootprintRot, handPlacedIndex, placer.handPlacementOnEdge, true);
                    if (!placer.handPlacementOnEdge)
                        newFootprintRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up);
                }
            }
        }

        Handles.BeginGUI();
        GUILayout.Window(1, new Rect(16f, 232f, 150f, 50f), DrawSceneWindow, "Placement by hand");
        Handles.EndGUI();
    }

    void DrawSceneWindow(int id)
    {
        placer.handPlacementEnabled = GUILayout.Toggle(placer.handPlacementEnabled, "Enable");
        placer.handPlacementOnEdge = GUILayout.Toggle(placer.handPlacementOnEdge, "On Edge");
    }
}
