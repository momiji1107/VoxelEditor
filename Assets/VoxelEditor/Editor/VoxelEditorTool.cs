using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Voxel Editor")]
public class VoxelEditorTool : EditorTool
{
    public override GUIContent toolbarIcon
    {
        get
        {
            return new GUIContent("V");
        }
    }

    public override void OnToolGUI(EditorWindow window)
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(new Rect(10, 10, 180, 50), "Voxel Editor", GUI.skin.window);

        GUILayout.Label("Voxel Editor Tool");

        GUILayout.EndArea();

        Handles.EndGUI();
    }
}