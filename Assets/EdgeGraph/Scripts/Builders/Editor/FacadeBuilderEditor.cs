using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(FacadeBuilder))]
public class FacadeBuilderEditor : Editor
{
    FacadeBuilder builder;

    void OnEnable()
    {
        builder = (FacadeBuilder)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Build", GUILayout.Width(120f)))
        {
            builder.BuildFacades();
        }

        //if (GUILayout.Button("Group buildings", GUILayout.Width(120f)))
        //{
        //    builder.GroupFacadesToBuildings();
        //}
    }
}
