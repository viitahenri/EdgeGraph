using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EdgeGraph
{
    [CustomEditor(typeof(Graph))]
    public class GraphEditor : Editor
    {
        #region Fields
        static readonly float PRIMITIVE_SETTINGS_LABEL_WIDTH = 200f;
        static readonly float PRIMITIVE_SETTINGS_VALUE_WIDTH = 50f;

        static readonly string NODE_TOOLS_HELP = "CTRL: Add new node on cursor.\nSHIFT: Remove nodes.\nSHIFT & CTRL: Add node by splitting an edge.";
        static readonly string EDGE_TOOLS_HELP = "CTRL: Add new edge between nodes.\nSHIFT: Remove edges.";
        static readonly string EDGE_WIDTH_TOOL_HELP = "Set edge width and brush size\nor set all edge widths with the button.";
        static readonly string PRIMITIVE_TOOLS_HELP = "Click to select primitives.\n\nWhile a primitive is selected:\nSHIFT: Set subedge generation root node.\nCTRL: Add new subedges";

        private enum GraphEditorState
        {
            None = 0,
            Nodes,
            Edges,
            Primitives
        }

        private GraphEditorState currentEditorState = GraphEditorState.None;

        private bool SinglePrimitiveIsSelected
        {
            get
            {
                return !(singleSelectedPrimitiveIdx == -1 || singleSelectedPrimitiveIdx > graph.mainPrimitives.Count);
            }
        }

        //The editor target graph
        private Graph graph;
        //Node handle control id array, used in determining the selected handle
        private int[] nodeControlIds;
        //Currently selected node, determined by the handles
        private int activeNode;
        //Closest node to the cursor
        //private Node closestNode;
        //Closest edge to the cursor
        private Edge closestEdge;
        //Edge to be created
        private Edge creatingNewEdge;

        //Editor controls
        private UtilityTools.EditorControls controls;
        private Vector3 cursorWorldPosition { get { return controls.cursorWorldPosition; } }
        private bool shiftIsPressed { get { return controls.shiftIsPressed; } }
        private bool controlIsPressed { get { return controls.controlIsPressed; } }
        private bool mouseIsPressed { get { return controls.mouseIsPressed; } }
        private Vector3 cursorLocalPosition { get { return controls.cursorLocalPosition; } }
        private bool MouseClickedUp { get { return controls.MouseClickedUp(); } }
        private bool MouseClickedDown { get { return controls.MouseClickedDown(); } }
        // True if mouse was released during this frame
        // Is used to handle a situation when editor handles use mouse events
        private bool mouseWasReleased = false;

        //Inspector and scene view variables
        bool showData = false;
        int singleSelectedPrimitiveIdx = -1;
        int selectedPrimitiveMask = 0;
        //Determines if newly added node be connected to the closest node by edge
        bool connectNewNode = false;

        //Subnode modification fields
        int[] subNodeControlIds;
        int activeSubNode;
        private Edge creatingNewSubEdge;

        //Subedge generation root node
        Node subEdgeRootNode = null;

        //Edge width setting fields
        bool setEdgeWidth = false;
        float setEdgeToWidth;
        float setEdgeWidthBrushSize = 2f;
        List<Edge> edgesToSetWidth = new List<Edge>();
        float childEdgeWidth = 0f;

        bool debugMode = false;
        #endregion

        void OnEnable()
        {
            graph = (Graph)target;
            controls = new UtilityTools.EditorControls(graph.gameObject);
        }

        void OnDisable()
        {
            Tools.hidden = false;
        }

        #region Inspector

        void GetNewSeed()
        {
            Random.seed = System.Guid.NewGuid().GetHashCode();
            graph.randomSeed = Random.Range(System.Int32.MinValue, System.Int32.MaxValue);
        }

        public override void OnInspectorGUI()
        {
            showData = EditorGUILayout.Foldout(showData, "Data Modification");
            if (showData)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.indentLevel = 1;

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();
                // Draw nodes list
                SerializedProperty nodes = serializedObject.FindProperty("nodes");
                EditorGUILayout.PropertyField(nodes, true);

                // Draw edges list
                SerializedProperty edges = serializedObject.FindProperty("edges");
                EditorGUILayout.PropertyField(edges, true);

                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                EditorGUI.indentLevel = 0;

                if (graph.nodes == null) graph.nodes = new List<Node>();
                if (graph.edges == null) graph.edges = new List<Edge>();

                if (GUILayout.Button("Clear nodes", GUILayout.Width(100f)))
                {
                    graph.nodes = new List<Node>();
                }

                if (GUILayout.Button("Clear edges", GUILayout.Width(100f)))
                {
                    graph.edges = new List<Edge>();
                }

                if (GUILayout.Button("Clear SubPrimitives", GUILayout.Width(130f)))
                {
                    graph.ClearPrimitiveSubPrimitives();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Process Minimal Cycles", GUILayout.Width(200f)))
            {
                singleSelectedPrimitiveIdx = -1;
                graph.ProcessMinimalCycles();
                graph.ProcessPrimitives();

                currentEditorState = GraphEditorState.Primitives;
            }

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Child Edge Width", GUILayout.Width(100f));
            childEdgeWidth = EditorGUILayout.FloatField(childEdgeWidth, GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Extract Main Primitives to Children", GUILayout.Width(250f)))
            {
                graph.ExtractMainPrimitives(childEdgeWidth);
            }

            EditorGUILayout.Separator();

            if (graph.mainPrimitives != null && graph.mainPrimitives.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    GUILayout.Label("Sub Edge Generation");

                    EditorGUILayout.BeginHorizontal("box");
                    if (GUILayout.Button("Seed", GUILayout.Width(50f)))
                    {
                        GetNewSeed();
                    }
                    graph.randomSeed = EditorGUILayout.IntField(graph.randomSeed, GUILayout.Width(200f));
                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.BeginHorizontal();
                    //if (GUILayout.Button("Generate Sub Edges", GUILayout.Width(200f)))
                    //{
                    //    graph.ProcessPrimitives();
                    //    graph.ClearPrimitiveSubPrimitives();
                    //    graph.GeneratePrimitiveSubEdges(graph.randomSeed);
                    //}

                    //if (GUILayout.Button("With new seed", GUILayout.Width(100f)))
                    //{
                    //    graph.ProcessPrimitives();
                    //    graph.ClearPrimitiveSubPrimitives();

                    //    GetNewSeed();

                    //    graph.GeneratePrimitiveSubEdges(graph.randomSeed);
                    //}
                    //EditorGUILayout.EndHorizontal();

                    //if (GUILayout.Button("Process Sub Primitives", GUILayout.Width(200f)))
                    //{
                    //    graph.ClearPrimitiveSubPrimitives();
                    //    graph.ProcessPrimitiveSubPrimitives();
                    //}

                    EditorGUILayout.LabelField("Primitive count: " + graph.mainPrimitives.Count);
                    DrawPrimitiveInspector();
                }
                EditorGUILayout.EndVertical();
            }

            FacadeBuilder builder = graph.GetComponent<FacadeBuilder>();
            if (builder != null)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    GUILayout.Label("Facades");
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.HelpBox("Calls facade building script on all SubGraphs if they exist.", MessageType.None);
                        if (GUILayout.Button("Build all SubGraph facades", GUILayout.Width(200f)))
                        {
                            for (int i = 0; i < graph.transform.childCount; i++)
                            {
                                FacadeBuilder childBuilder = graph.transform.GetChild(i).GetComponent<FacadeBuilder>();
                                if (childBuilder == null) continue;

                                childBuilder.BuildFacades();
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }

            if (GUI.changed)
                EditorUtility.SetDirty(graph);
        }

        #endregion

        #region Inspector for primitives

        void DrawPrimitiveInspector()
        {
            if (graph.mainPrimitives == null || graph.mainPrimitives.Count <= 0) return;

            if (SinglePrimitiveIsSelected)
            {
                graph._subTargetCount = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeTargetCount;
                graph._subEdgeWidth = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeWidth;
                graph._subEdgeTargetMargin = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeTargetMargin;
                graph._subEdgeAngle = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeMinAngle;
                graph._subEdgeSegmentLength = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeSegmentLength;
                graph._subEdgeMinDistance = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeMinDistance;
                graph._subEdgeMaxDistance = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeMaxDistance;
                graph._subEdgeNodeCombineRange = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeNodeCombineRange;
                graph._subEdgeEndConnectionRange = graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeEndConnectionRange;
            }

            EditorGUILayout.BeginVertical("box");

            GUIContent content = new GUIContent();
            Color guiColor = GUI.color;

            //string text = SinglePrimitiveIsSelected ? ("Selected primitive: " + singleSelectedPrimitiveIdx.ToString()) : "Changing settings for all primitives";
            string text = "Settings for ";
            if (SinglePrimitiveIsSelected)
            {
                GUI.color = Color.green;
                text += "primitive: " + singleSelectedPrimitiveIdx.ToString();
            }
            else if (selectedPrimitiveMask != 0)
            {
                GUI.color = new Color(0f, 1f, .75f, 1f);
                text += "selected primitives";
            }
            else
            {
                GUI.color = Color.cyan;
                text += "all primitives";
            }
            EditorGUILayout.LabelField(text);

            GUI.color = guiColor;

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Subedge Generation");

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge target count", "The amount of points to generate within primitive bounds, takes <margin> into account.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subTargetCount = EditorGUILayout.IntField(graph._subTargetCount, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge margin", "Generated points are not allowed closer than this to the primitive edges.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeTargetMargin = EditorGUILayout.FloatField(graph._subEdgeTargetMargin, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge width", "Edge width given to the generated subedges");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeWidth = EditorGUILayout.FloatField(graph._subEdgeWidth, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge min angle", "If the angle between adjacent generated edges goes below this, make a 90 degree angle.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeAngle = EditorGUILayout.FloatField(graph._subEdgeAngle, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge segment length", "Length of one step the edge generator goes forward.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeSegmentLength = EditorGUILayout.FloatField(graph._subEdgeSegmentLength, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge min distance", "Edge generator considers targets within this distance visited.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeMinDistance = EditorGUILayout.FloatField(graph._subEdgeMinDistance, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subedge max distance", "Edge generator finds closest target with this max distance.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeMaxDistance = EditorGUILayout.FloatField(graph._subEdgeMaxDistance, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Generated node and edge connection");

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subnode combine range", "After the generation, nodes within this range are combined together.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeNodeCombineRange = EditorGUILayout.FloatField(graph._subEdgeNodeCombineRange, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                content = new GUIContent("Subnode end connection range", "Edge ending nodes seek other nodes within this distance to connect to. If none are found, the ending node connects with line cast to the intersecting edge.");
                EditorGUILayout.LabelField(content, GUILayout.Width(PRIMITIVE_SETTINGS_LABEL_WIDTH));
                graph._subEdgeEndConnectionRange = EditorGUILayout.FloatField(graph._subEdgeEndConnectionRange, GUILayout.Width(PRIMITIVE_SETTINGS_VALUE_WIDTH));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (SinglePrimitiveIsSelected)
            {
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeTargetCount = graph._subTargetCount;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeWidth = graph._subEdgeWidth;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeTargetMargin = graph._subEdgeTargetMargin;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeMinAngle = graph._subEdgeAngle;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeSegmentLength = graph._subEdgeSegmentLength;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeMinDistance = graph._subEdgeMinDistance;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeMaxDistance = graph._subEdgeMaxDistance;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeNodeCombineRange = graph._subEdgeNodeCombineRange;
                graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeEndConnectionRange = graph._subEdgeEndConnectionRange;

                if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                {
                    graph.ClearPrimitiveSubPrimitives(singleSelectedPrimitiveIdx);
                    //graph.ProcessPrimitives(selectedPrimitiveIdx);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate", GUILayout.Width(100f)))
                {
                    //graph.ProcessPrimitives(selectedPrimitiveIdx);
                    graph.GeneratePrimitiveSubEdges(graph.randomSeed, singleSelectedPrimitiveIdx);
                }
                if (GUILayout.Button("With new seed", GUILayout.Width(100f)))
                {
                    graph.ClearPrimitiveSubPrimitives(singleSelectedPrimitiveIdx);
                    //graph.ProcessPrimitives(selectedPrimitiveIdx);

                    GetNewSeed();

                    graph.GeneratePrimitiveSubEdges(graph.randomSeed, singleSelectedPrimitiveIdx);
                }
                if (GUILayout.Button("Process", GUILayout.Width(100f)))
                {
                    graph.ClearPrimitiveSubPrimitives(singleSelectedPrimitiveIdx);
                    graph.ProcessPrimitiveSubPrimitives(singleSelectedPrimitiveIdx);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                for (int i = 0; i < graph.mainPrimitives.Count; i++)
                {
                    int iMask = 1 << i;
                    if (selectedPrimitiveMask == 0 || (selectedPrimitiveMask & iMask) == iMask)
                    {
                        graph.mainPrimitives[i].subEdgeTargetCount = graph._subTargetCount;
                        graph.mainPrimitives[i].subEdgeWidth = graph._subEdgeWidth;
                        graph.mainPrimitives[i].subEdgeTargetMargin = graph._subEdgeTargetMargin;
                        graph.mainPrimitives[i].subEdgeMinAngle = graph._subEdgeAngle;
                        graph.mainPrimitives[i].subEdgeSegmentLength = graph._subEdgeSegmentLength;
                        graph.mainPrimitives[i].subEdgeMinDistance = graph._subEdgeMinDistance;
                        graph.mainPrimitives[i].subEdgeMaxDistance = graph._subEdgeMaxDistance;
                        graph.mainPrimitives[i].subEdgeNodeCombineRange = graph._subEdgeNodeCombineRange;
                        graph.mainPrimitives[i].subEdgeEndConnectionRange = graph._subEdgeEndConnectionRange;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate", GUILayout.Width(100f)))
                {
                    if (selectedPrimitiveMask != 0)
                    {
                        for (int i = 0; i < graph.mainPrimitives.Count; i++)
                        {
                            int iMask = 1 << i;
                            if ((selectedPrimitiveMask & iMask) == iMask)
                            {
                                graph.GeneratePrimitiveSubEdges(graph.randomSeed, i);
                            }
                        }
                    }
                    else
                        graph.GeneratePrimitiveSubEdges(graph.randomSeed);
                }
                if (GUILayout.Button("With new seed", GUILayout.Width(100f)))
                {
                    if (selectedPrimitiveMask != 0)
                    {
                        for (int i = 0; i < graph.mainPrimitives.Count; i++)
                        {
                            int iMask = 1 << i;
                            if ((selectedPrimitiveMask & iMask) == iMask)
                            {
                                graph.ClearPrimitiveSubPrimitives(i);

                                GetNewSeed();

                                graph.GeneratePrimitiveSubEdges(graph.randomSeed, i);
                            }
                        }
                    }
                    else
                    {
                        graph.ClearPrimitiveSubPrimitives();

                        GetNewSeed();

                        graph.GeneratePrimitiveSubEdges(graph.randomSeed);
                    }
                }
                if (GUILayout.Button("Process", GUILayout.Width(100f)))
                {
                    if (selectedPrimitiveMask != 0)
                    {
                        for (int i = 0; i < graph.mainPrimitives.Count; i++)
                        {
                            int iMask = 1 << i;
                            if ((selectedPrimitiveMask & iMask) == iMask)
                            {
                                graph.ClearPrimitiveSubPrimitives(i);
                                graph.ProcessPrimitiveSubPrimitives(i);
                            }
                        }
                    }
                    else
                    {
                        graph.ClearPrimitiveSubPrimitives();
                        graph.ProcessPrimitiveSubPrimitives();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Scene View

        void OnSceneGUI()
        {
            controls.Update();
            mouseWasReleased = Event.current.type == EventType.MouseUp;

            DrawCursor();

            Tools.hidden = currentEditorState != GraphEditorState.None;

            DrawDumbEdgeHandles();

            DrawNodeHandles();
            NodeTools();

            DrawEdgeHandles();
            EdgeTools();

            DrawSubNodeHandles();
            DrawPrimitiveHandles();
            PrimitiveTools();

            activeNode = -1;
            activeSubNode = -1;

            Handles.BeginGUI();
            GUILayout.Window(0, new Rect(16f, 32f, 100f, 50f), DrawSceneWindow, "Graph Tools");
            Handles.EndGUI();

            if (graph.nodes == null) graph.nodes = new List<Node>();
            if (graph.edges == null) graph.edges = new List<Edge>();
            EdgeGraphUtility.CleanUpEdges(ref graph.nodes, ref graph.edges);

            // Prevent flushing dirty data while we are dragging stuff around
            if (GUI.changed && !mouseIsPressed)
                EditorUtility.SetDirty(graph);
        }

        void DrawSceneWindow(int id)
        {
            debugMode = GUILayout.Toggle(debugMode, "Debug mode", GUILayout.Width(100f));

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical("box");
                {
                    int selectedState = (int)currentEditorState;
                    selectedState = GUILayout.SelectionGrid(selectedState, System.Enum.GetNames(typeof(GraphEditorState)), 1, GUILayout.Width(100f));
                    currentEditorState = (GraphEditorState)selectedState;
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();

                switch (currentEditorState)
                {
                    case GraphEditorState.None:
                        singleSelectedPrimitiveIdx = -1;
                        break;
                    case GraphEditorState.Nodes:
                        singleSelectedPrimitiveIdx = -1;
                        DrawNodeSceneTools();
                        break;
                    case GraphEditorState.Edges:
                        singleSelectedPrimitiveIdx = -1;
                        DrawEdgeSceneTools();
                        break;
                    case GraphEditorState.Primitives:
                        DrawPrimitiveSceneTools();
                        break;
                    default:
                        singleSelectedPrimitiveIdx = -1;
                        break;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUI.changed = true;
        }

        void DrawNodeSceneTools()
        {
            GUILayout.Label(NODE_TOOLS_HELP);

            connectNewNode = GUILayout.Toggle(connectNewNode, "Connect new node automatically", GUILayout.Width(200f));
            if (connectNewNode)
            {
                var style = new GUIStyle(GUI.skin.box);
                style.normal.textColor = Color.grey * 1.5f;
                GUILayout.Label("New nodes will be connected to nearest node.", style);
            }
        }

        void DrawEdgeSceneTools()
        {
            GUILayout.BeginVertical("box");
            setEdgeWidth = GUILayout.Toggle(setEdgeWidth, "Edge width", GUILayout.Width(100f));
            if (setEdgeWidth)
            {
                GUILayout.Label(EDGE_WIDTH_TOOL_HELP);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Width");
                setEdgeToWidth = GUILayout.HorizontalSlider(setEdgeToWidth, 0f, 20f, GUILayout.Width(120f));
                setEdgeToWidth = EditorGUILayout.FloatField(setEdgeToWidth, GUILayout.Width(50f));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Brush");
                setEdgeWidthBrushSize = GUILayout.HorizontalSlider(setEdgeWidthBrushSize, 0f, 5f, GUILayout.Width(120f));
                setEdgeWidthBrushSize = EditorGUILayout.FloatField(setEdgeWidthBrushSize, GUILayout.Width(50f));
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Set all", GUILayout.Width(70f)))
                {
                    for (int i = 0; i < graph.edges.Count; i++)
                    {
                        graph.edges[i].Width = setEdgeToWidth;
                    }
                }
            }
            else
            {
                GUILayout.Label(EDGE_TOOLS_HELP);
            }
            GUILayout.EndVertical();
        }

        void DrawPrimitiveSceneTools()
        {
            GUILayout.Label(PRIMITIVE_TOOLS_HELP);
        }
        #endregion

        #region Handles

        void DrawCursor()
        {
            float handleSize = HandleUtility.GetHandleSize(cursorWorldPosition) * .2f;

            if (setEdgeWidth && currentEditorState == GraphEditorState.Edges)
            {
                handleSize = setEdgeWidthBrushSize * 2f;
            }

            Handles.color = new Color(0f, 1f, 0f, .2f);
            Handles.SphereCap(0, cursorWorldPosition, Quaternion.identity, handleSize);

            Handles.color = Color.green;
        }

        void DrawNodeHandles()
        {
            if (currentEditorState != GraphEditorState.Nodes) return;

            //Node selection
            DrawNodePositionHandles(true);

            // Deleting nodes
            Handles.color = new Color(1f, 0f, 0f, .5f);
            if (shiftIsPressed && !controlIsPressed)
            {
                float toClosest;
                Node closestNode = CheckClosestNode(graph.nodes, out toClosest);

                if (closestNode != null)
                {
                    Handles.SphereCap(0, graph.transform.TransformPoint(closestNode.Position), Quaternion.identity, HandleUtility.GetHandleSize(graph.transform.TransformPoint(closestNode.Position)) * .4f);
                }
            }

            // Node adding handle
            if (controlIsPressed)
            {
                Handles.color = Color.blue;
                if (shiftIsPressed)
                {
                    //Vector3 posOnEdgeWorld = graph.transform.TransformPoint(GetPointOnClosestEdge());
                    //Vector3 posOnEdgeWorld = EdgeGraphUtility.GetPointOnClosestEdge(cursorWorldPosition, graph.nodes, graph.edges, graph.transform);
                    Vector3 posOnEdgeWorld = EdgeGraphUtility.GetClosestPointOnEdge(cursorWorldPosition, graph.nodes, graph.edges, graph.transform, out closestEdge);
                    Handles.SphereCap(0, posOnEdgeWorld, Quaternion.identity, HandleUtility.GetHandleSize(posOnEdgeWorld) * .1f);
                }
                else
                {
                    Handles.SphereCap(0, cursorWorldPosition, Quaternion.identity, HandleUtility.GetHandleSize(cursorWorldPosition) * .1f);
                    if (connectNewNode)
                    {
                        Handles.color = Color.yellow;
                        float toClosest;
                        Node closestNode = CheckClosestNode(graph.nodes, out toClosest);
                        if (closestEdge != null)
                        {
                            Vector3 nodePos = graph.transform.TransformPoint(closestNode.Position);
                            Handles.DrawLine(cursorWorldPosition, nodePos);
                        }
                    }
                }
            }
        }

        void DrawNodePositionHandles(bool selection = false)
        {
            //Ensure controlID count before drawing the handles
            if (nodeControlIds == null || nodeControlIds.Length != graph.nodes.Count)
                nodeControlIds = new int[graph.nodes.Count];

            // Node handles without moving functionality
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                Vector3 worldPos = graph.transform.TransformPoint(graph.nodes[i].Position);

                if (selection)
                {
                    worldPos = Handles.FreeMoveHandle(worldPos, Quaternion.identity, .5f, Vector3.one, (controlID, position, rotation, size) =>
                    {
                        nodeControlIds[i] = controlID;
                        Handles.CubeCap(i, worldPos, Quaternion.identity, HandleUtility.GetHandleSize(worldPos) * .2f);
                    });

                    if (GUIUtility.keyboardControl == nodeControlIds[i] && GUIUtility.keyboardControl != 0)
                    {
                        activeNode = i;
                    }
                }
                else
                {
                    Handles.CubeCap(0, worldPos, Quaternion.identity, HandleUtility.GetHandleSize(worldPos) * .2f);
                    activeNode = -1;
                }
            }
        }

        void DrawEdgeHandles()
        {
            if (currentEditorState != GraphEditorState.Edges) return;

            Handles.color = Color.green * new Color(1f, 1f, 1f, .5f);

            DrawNodePositionHandles(controlIsPressed);

            // Adding new edges
            if (controlIsPressed && !setEdgeWidth)
            {
                /// -----------------------------------
                /// Connect two closest nodes to the cursor
                /// Wasn't that intuitive
                /// -----------------------------------
                //List<Node> _nodes = new List<Node>();
                //_nodes.AddRange(graph.nodes);

                //// Sort nodes according to the distance from cursor
                //_nodes.Sort((n1, n2) =>
                //{
                //    Vector3 n1Pos = graph.transform.TransformPoint(n1.Position);
                //    Vector3 n2Pos = graph.transform.TransformPoint(n2.Position);
                //    float distN1 = Vector3.Distance(n1Pos, cursorWorldPosition);
                //    float distN2 = Vector3.Distance(n2Pos, cursorWorldPosition);

                //    return distN1.CompareTo(distN2);
                //});

                //// Draw lines to closest nodes from the cursor
                //Handles.color = Color.green * new Color(1f, 1f, 1f, .5f);
                //Handles.DrawLine(cursorWorldPosition, graph.transform.TransformPoint(_nodes[0].Position));
                //Handles.DrawLine(cursorWorldPosition, graph.transform.TransformPoint(_nodes[1].Position));

                //Handles.color = Color.yellow;
                //Handles.DrawLine(graph.transform.TransformPoint(_nodes[0].Position), graph.transform.TransformPoint(_nodes[1].Position));
                /// -----------------------------------

                /// -----------------------------------
                /// Connect selected node and closest other node to cursor
                /// -----------------------------------

                if (activeNode >= 0 && activeNode < graph.nodes.Count)
                {
                    Node _selected = graph.nodes[activeNode];
                    float toClosest;
                    Node closestNode = CheckClosestNode(graph.nodes, out toClosest, _selected);
                    float toSelected = Vector3.Distance(_selected.Position, cursorLocalPosition);

                    if (toClosest < toSelected)
                    {
                        Handles.color = Color.green;
                        Handles.DrawLine(graph.transform.TransformPoint(_selected.Position), graph.transform.TransformPoint(closestNode.Position));

                        // Add the new edge
                        creatingNewEdge = new Edge(_selected.ID, closestNode.ID);
                    }
                    else
                        creatingNewEdge = null;
                }
            }

            // Deleting edges
            Handles.color = new Color(1f, 0f, 0f, .5f);
            if (shiftIsPressed && !controlIsPressed && !setEdgeWidth)
            {
                //float toClosestNode;
                //Node closestNode = CheckClosestNode(graph.nodes, out toClosestNode);
                //float toClosestEdge = CheckClosestEdge();

                Handles.color = new Color(1f, 0f, 0f, .5f);
                Handles.SphereCap(0, graph.GetEdgePosition(closestEdge.ID), Quaternion.identity, HandleUtility.GetHandleSize(graph.GetEdgePosition(closestEdge.ID)) * .4f);

                Handles.color = Color.blue;

                for (int i = 0; i < graph.edges.Count; i++)
                {
                    Handles.CubeCap(i, graph.GetEdgePosition(graph.edges[i].ID), Quaternion.identity, HandleUtility.GetHandleSize(graph.GetEdgePosition(graph.edges[i].ID)) * .2f);
                }
            }

            // Edge width setting and debug information
            if (graph.edges != null && graph.edges.Count > 0)
            {
                edgesToSetWidth = new List<Edge>();

                for (int i = 0; i < graph.edges.Count; i++)
                {
                    Handles.color = Color.yellow;
                    Node n1 = graph[graph.edges[i].Node1];
                    Node n2 = graph[graph.edges[i].Node2];
                    if (n1 != null && n2 != null)
                    {

                        Handles.DrawLine(graph.transform.TransformPoint(n1.Position), graph.transform.TransformPoint(n2.Position));
                        Vector3 edgePos = graph.GetEdgePosition(graph.edges[i].ID);
                        float handleSize = HandleUtility.GetHandleSize(edgePos) * .2f;

                        if (setEdgeWidth)
                        {
                            if (Vector3.Distance(cursorWorldPosition, edgePos) <= setEdgeWidthBrushSize)
                            {
                                Handles.color = Color.green;
                                edgesToSetWidth.Add(graph.edges[i]);
                            }

                            Handles.CubeCap(i, edgePos, Quaternion.identity, handleSize);
                            Handles.DrawLine(edgePos, edgePos + Vector3.right + Vector3.back);
                            Handles.Label(edgePos + Vector3.right + Vector3.back, string.Format("{0:0.0}", graph.edges[i].Width), "box");
                        }

                        if (debugMode)
                        {
                            //Edge normals
                            Handles.color = Color.red;
                            Vector3 edgePerpLeft = Edge.GetLeftPerpendicular(n1.Position, n2.Position);
                            Handles.DrawLine(edgePos, edgePos + edgePerpLeft * .1f);

                            Handles.color = Color.blue;
                            Vector3 edgePerpRight = Edge.GetRightPerpendicular(n1.Position, n2.Position);
                            Handles.DrawLine(edgePos, edgePos + edgePerpRight * .1f);

                            //Edge widths visualized
                            Handles.color = Color.yellow;
                            float edgeWidth = graph.edges[i].Width / 2f;
                            Vector3 left1 = graph.transform.TransformPoint(n1.Position + edgePerpLeft.normalized * edgeWidth);
                            Vector3 left2 = graph.transform.TransformPoint(n2.Position + edgePerpLeft.normalized * edgeWidth);

                            Vector3 right1 = graph.transform.TransformPoint(n1.Position + edgePerpRight.normalized * edgeWidth);
                            Vector3 right2 = graph.transform.TransformPoint(n2.Position + edgePerpRight.normalized * edgeWidth);

                            Handles.DrawDottedLine(left1, left2, HandleUtility.GetHandleSize((left1 + left2) / 2f));
                            Handles.DrawDottedLine(right1, right2, HandleUtility.GetHandleSize((right1 + right2) / 2f));
                        }
                    }
                }
            }
        }

        void DrawDumbEdgeHandles()
        {
            if (graph.edges != null && graph.edges.Count > 0)
            {
                edgesToSetWidth = new List<Edge>();

                for (int i = 0; i < graph.edges.Count; i++)
                {
                    Handles.color = Color.yellow;
                    Node n1 = graph[graph.edges[i].Node1];
                    Node n2 = graph[graph.edges[i].Node2];
                    if (n1 != null && n2 != null)
                    {

                        Handles.DrawLine(graph.transform.TransformPoint(n1.Position), graph.transform.TransformPoint(n2.Position));
                    }
                }
            }
        }

        void DrawSubNodeHandles()
        {
            if (currentEditorState != GraphEditorState.Primitives) return;

            if (SinglePrimitiveIsSelected)
            {
                Handles.color = Color.blue;

                if (subNodeControlIds == null || subNodeControlIds.Length != graph.mainPrimitives[singleSelectedPrimitiveIdx].subNodes.Count)
                    subNodeControlIds = new int[graph.mainPrimitives[singleSelectedPrimitiveIdx].subNodes.Count];

                for (int j = 0; j < graph.mainPrimitives[singleSelectedPrimitiveIdx].subNodes.Count; j++)
                {
                    Node node = graph.mainPrimitives[singleSelectedPrimitiveIdx].subNodes[j];

                    Vector3 worldPos = graph.transform.TransformPoint(node.Position);

                    worldPos = Handles.FreeMoveHandle(worldPos, Quaternion.identity, .5f, Vector3.one, (controlID, position, rotation, size) =>
                    {
                        subNodeControlIds[j] = controlID;
                        Handles.CubeCap(j, worldPos, Quaternion.identity, HandleUtility.GetHandleSize(worldPos) * .2f);
                        //Handles.Label(worldPos + new Vector3(.5f, .5f), graph.nodes[i].Position.ToString(), "box");
                    });

                    if (GUIUtility.keyboardControl == subNodeControlIds[j] && GUIUtility.keyboardControl != 0)
                    {
                        activeSubNode = j;
                    }
                }
            }
        }

        void DrawPrimitiveHandles()
        {
            if (currentEditorState != GraphEditorState.Primitives) return;

            if (graph.mainPrimitives != null && graph.mainPrimitives.Count > 0)
            {
                Primitive p = null;
                Node n1 = null;
                Node n2 = null;
                for (int i = 0; i < graph.mainPrimitives.Count; i++)
                {
                    p = graph.mainPrimitives[i];

                    foreach (var edge in p.edges)
                    {
                        n1 = p.nodes.Find(x => x.ID == edge.Node1);
                        n2 = p.nodes.Find(x => x.ID == edge.Node2);

                        if (n1 != null && n2 != null)
                        {
                            Handles.color = Color.cyan;
                            Color singleSelectColor = new Color(Color.red.r - .3f, Color.red.g - .3f, Color.red.b - .3f);
                            Color multiSelectColor = Color.red;

                            int indexMask = 1 << i;

                            if (singleSelectedPrimitiveIdx == i)
                                Handles.color = singleSelectColor;
                            else if ((selectedPrimitiveMask & indexMask) == indexMask)
                                Handles.color = multiSelectColor;

                            Handles.DrawLine(graph.transform.TransformPoint(n1.Position), graph.transform.TransformPoint(n2.Position));
                        }
                    }

                    if (debugMode)
                    {
                        for (int j = 0; j < p.nodes.Count; j++)
                        {
                            Vector3 center = graph.transform.TransformPoint(p.nodes[j].Position + p.nodes[j].dirToInside);
                            Handles.color = Color.green;
                            Handles.DrawLine(graph.transform.TransformPoint(p.nodes[j].Position), center);

                            Handles.Label(center, j.ToString(), "box");
                        }
                    }
                }

                if (SinglePrimitiveIsSelected)
                {
                    // Root index
                    // ----------------------------------------------------------------------------------------
                    p = graph.mainPrimitives[singleSelectedPrimitiveIdx];

                    int idx = p.subEdgeRootIndex;
                    if (idx >= p.nodes.Count)
                    {
                        idx = p.nodes.Count - 1;
                    }

                    Handles.color = Color.yellow;
                    int nextIdx = UtilityTools.Helper.GetNextIndex<Node>(p.nodes, idx);
                    Vector3 toNext = (p.nodes[nextIdx].Position - p.nodes[idx].Position).normalized;
                    Vector3 rightNormal = -UtilityTools.MathHelper.LeftSideNormal(toNext);
                    Vector3 pos = graph.transform.TransformPoint(p.nodes[idx].Position);
                    float dist = 3f;
                    //Handles.SphereCap(i, pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * .2f);
                    Handles.Label(pos + rightNormal * dist, "Root", "box");
                    Handles.DrawLine(pos, pos + rightNormal * dist);

                    // Root node modification
                    if (shiftIsPressed && !controlIsPressed)
                    {
                        subEdgeRootNode = EdgeGraphUtility.GetClosestNode(cursorWorldPosition, graph.mainPrimitives[singleSelectedPrimitiveIdx].nodes, graph.transform);

                        Vector3 nodePos = graph.transform.TransformPoint(subEdgeRootNode.Position);
                        Vector3 arrowDir = (cursorWorldPosition - nodePos).normalized;
                        Vector3 arrowPos = nodePos + arrowDir;
                        arrowPos += arrowDir * HandleUtility.GetHandleSize(arrowPos);
                        Quaternion arrowRot = Quaternion.LookRotation((nodePos - arrowPos).normalized);
                        Handles.ArrowCap(0, arrowPos, arrowRot, HandleUtility.GetHandleSize(arrowPos));
                        Handles.DrawLine(arrowPos, cursorWorldPosition);
                    }
                    // ----------------------------------------------------------------------------------------
                    // Create new subedges
                    // ----------------------------------------------------------------------------------------
                    if (activeSubNode >= 0 && controlIsPressed && !shiftIsPressed && p.subNodes != null && p.subNodes.Count > 0)
                    {
                        Node _selected = p.subNodes[activeSubNode];
                        float toClosest;
                        Node closestNode = CheckClosestNode(p.subNodes, out toClosest, _selected);
                        float toSelected = Vector3.Distance(_selected.Position, cursorLocalPosition);

                        if (toClosest < toSelected)
                        {
                            Handles.color = Color.green;
                            Handles.DrawLine(graph.transform.TransformPoint(_selected.Position), graph.transform.TransformPoint(closestNode.Position));

                            // Add the new edge
                            creatingNewSubEdge = new Edge(_selected.ID, closestNode.ID, p.subEdgeWidth);
                        }
                        else
                            creatingNewSubEdge = null;
                    }
                    // ----------------------------------------------------------------------------------------
                }
            }

            // Subgraphs

            if (graph.subGraphs != null && graph.subGraphs.Count > 0)
            {
                Node n1 = null;
                Node n2 = null;
                Primitive p = null;
                Edge edge = null;

                for (int i = 0; i < graph.subGraphs.Count; i++)
                {
                    if (graph.subGraphs[i] == null) continue;

                    Handles.color = new Color(0f, 1f, .5f, 1f);
                    for (int j = 0; j < graph.subGraphs[i].mainPrimitives.Count; j++)
                    {
                        p = graph.subGraphs[i].mainPrimitives[j];
                        for (int k = 0; k < p.edges.Count; k++)
                        {
                            edge = p.edges[k];
                            n1 = p.nodes.Find(x => x.ID == edge.Node1);
                            n2 = p.nodes.Find(x => x.ID == edge.Node2);

                            if (n1 != null && n2 != null)
                            {
                                Handles.DrawLine(graph.transform.TransformPoint(n1.Position), graph.transform.TransformPoint(n2.Position));
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Tools

        /// Move selected node while dragging
        void NodeTools()
        {
            if (currentEditorState != GraphEditorState.Nodes) return;

            // Moving nodes
            if (mouseIsPressed && !shiftIsPressed && !controlIsPressed && activeNode >= 0 && activeNode < graph.nodes.Count)
            {
                graph.nodes[activeNode].Position = cursorLocalPosition;
            }

            if (mouseWasReleased)
            {
                // Node deleting
                float toClosest;
                Node closestNode = CheckClosestNode(graph.nodes, out toClosest);
                if (shiftIsPressed && !controlIsPressed && closestNode != null)
                {
                    graph.RemoveNode(closestNode.ID);
                }
            }

            if (MouseClickedDown)
            {
                // Node adding
                if (controlIsPressed)
                {
                    if (shiftIsPressed)
                    {
                        Vector3 splitPointLocal = graph.transform.InverseTransformPoint(EdgeGraphUtility.GetClosestPointOnEdge(cursorWorldPosition, graph.nodes, graph.edges, graph.transform, out closestEdge));

                        Node newNode = new Node(splitPointLocal);

                        graph.nodes.Add(newNode);
                        graph.edges.Add(new Edge(closestEdge.Node1, newNode.ID, closestEdge.Width));
                        graph.edges.Add(new Edge(newNode.ID, closestEdge.Node2, closestEdge.Width));

                        graph.edges.Remove(closestEdge);
                        closestEdge = null;
                    }
                    else
                    {
                        if (connectNewNode)
                        {
                            Node newNode = new Node(cursorLocalPosition);
                            graph.nodes.Add(newNode);
                            float toClosest;
                            Node closestNode = CheckClosestNode(graph.nodes, out toClosest);
                            if (closestNode != null)
                                graph.edges.Add(new Edge(closestNode.ID, newNode.ID));
                        }
                        else
                            graph.nodes.Add(new Node(cursorLocalPosition));
                    }
                }

            }
        }

        void EdgeTools()
        {
            if (graph.nodes == null || graph.nodes.Count <= 0 || currentEditorState != GraphEditorState.Edges) return;

            if (mouseWasReleased)
            {
                // Edge removal
                float toClosest;
                Node closestNode = CheckClosestNode(graph.nodes, out toClosest);
                if (shiftIsPressed && !controlIsPressed && closestNode != null && !setEdgeWidth)
                {
                    graph.edges.Remove(closestEdge);
                }
                // Edge adding
                if (controlIsPressed && creatingNewEdge != null && !setEdgeWidth)
                {
                    graph.edges.Add(creatingNewEdge);
                    creatingNewEdge = null;
                }
            }

            // Edge width setting brush
            if (mouseIsPressed && setEdgeWidth)
            {
                foreach (var edge in edgesToSetWidth)
                    edge.Width = setEdgeToWidth;
            }
        }

        void PrimitiveTools()
        {
            if (currentEditorState != GraphEditorState.Primitives) return;

            if (mouseIsPressed && !shiftIsPressed && !controlIsPressed)
            {
                if (activeSubNode >= 0 && singleSelectedPrimitiveIdx >= 0 && singleSelectedPrimitiveIdx < graph.mainPrimitives.Count &&
                    activeSubNode >= 0 && activeSubNode < graph.mainPrimitives[singleSelectedPrimitiveIdx].subNodes.Count)
                {
                    graph.mainPrimitives[singleSelectedPrimitiveIdx].subNodes[activeSubNode].Position = cursorLocalPosition;
                }
            }

            //if (MouseClickedDown)
            //{

            //}

            if (mouseWasReleased)
            {
                // Adding new subedge
                if (controlIsPressed && !shiftIsPressed && SinglePrimitiveIsSelected && creatingNewSubEdge != null)
                {
                    graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdges.Add(creatingNewSubEdge);
                    creatingNewSubEdge = null;
                }

                //Select primitive
                if (!shiftIsPressed && graph.mainPrimitives != null && graph.mainPrimitives.Count > 0 && activeSubNode == -1)
                {
                    singleSelectedPrimitiveIdx = -1;
                    int primitiveClicked = -1;

                    for (int i = 0; i < graph.mainPrimitives.Count; i++)
                    {
                        if (EdgeGraphUtility.PointIsInside(cursorLocalPosition, graph.mainPrimitives[i].nodes, graph.mainPrimitives[i].edges))
                        {
                            primitiveClicked = i;
                            break;
                        }
                    }

                    if (controlIsPressed && selectedPrimitiveMask != 0 && selectedPrimitiveMask != (1 << primitiveClicked))
                    {
                        selectedPrimitiveMask |= 1 << primitiveClicked;
                    }
                    else if (primitiveClicked != -1)
                    {
                        singleSelectedPrimitiveIdx = primitiveClicked;
                        selectedPrimitiveMask = 1 << primitiveClicked;
                    }
                    else
                    {
                        selectedPrimitiveMask = 0;
                    }

                    GUI.changed = true;
                }

                // Change subedge root index
                if (shiftIsPressed && !controlIsPressed && SinglePrimitiveIsSelected && subEdgeRootNode != null)
                {
                    //subEdgeRootNode = EdgeGraphUtility.GetClosestNode(cursorWorldPosition, graph.mainPrimitives[selectedPrimitiveIdx].nodes, graph.transform);

                    int subEdgeRootIdx = graph.mainPrimitives[singleSelectedPrimitiveIdx].nodes.IndexOf(subEdgeRootNode);

                    graph.mainPrimitives[singleSelectedPrimitiveIdx].subEdgeRootIndex = subEdgeRootIdx;
                }
            }
        }

        #endregion

        #region Utility

        Node CheckClosestNode(List<Node> nodes, out float toClosest, Node ignore = null)
        {
            toClosest = Mathf.Infinity;
            if (nodes == null || nodes.Count <= 0) return null;

            Node closestNode = nodes[0];
            for (int i = 0; i < nodes.Count; i++)
            {
                if (ignore != null && nodes[i] == ignore) continue;

                Vector3 currentPos = graph.transform.TransformPoint(nodes[i].Position);
                float toCurrent = Vector3.Distance(currentPos, cursorWorldPosition);
                Vector3 closestPos = graph.transform.TransformPoint(closestNode.Position);
                toClosest = Vector3.Distance(closestPos, cursorWorldPosition);

                if (toCurrent < toClosest)
                {
                    closestNode = nodes[i];
                    toClosest = toCurrent;
                }
            }

            return closestNode;
        }

        float CheckClosestEdge()
        {
            if (graph.edges == null || graph.edges.Count <= 0) return Mathf.Infinity;

            Vector3 splitPoint = EdgeGraphUtility.GetClosestPointOnEdge(cursorWorldPosition, graph.nodes, graph.edges, graph.transform, out closestEdge);

            return Vector3.Distance(cursorWorldPosition, splitPoint);
        }
        #endregion
    }
}