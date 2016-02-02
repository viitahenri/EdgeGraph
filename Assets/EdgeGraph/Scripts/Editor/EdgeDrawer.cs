using UnityEngine;
using UnityEditor;
using EdgeGraph;

[CustomPropertyDrawer(typeof(Edge))]
public class EdgeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw the beginning of the id label
        label.text = label.text.Substring(0, 13);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect node1LabelRect = new Rect(position.x, position.y, 20f, position.height);
        Rect node1Rect = new Rect(position.x + 20f, position.y, 80f, position.height);

        Rect node2LabelRect = new Rect(position.x + 100f, position.y, 20f, position.height);
        Rect node2Rect = new Rect(position.x + 120f, position.y, 80f, position.height);

        Rect widthLabelRect = new Rect(position.x + 200f, position.y, 40f, position.height);
        Rect widthRect = new Rect(position.x + 240f, position.y, 40f, position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels

        EditorGUI.LabelField(node1LabelRect, "N1");
        EditorGUI.PropertyField(node1Rect, property.FindPropertyRelative("node1"), GUIContent.none);

        EditorGUI.LabelField(node2LabelRect, "N2");
        EditorGUI.PropertyField(node2Rect, property.FindPropertyRelative("node2"), GUIContent.none);

        EditorGUI.LabelField(widthLabelRect, "Width");
        EditorGUI.PropertyField(widthRect, property.FindPropertyRelative("width"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
