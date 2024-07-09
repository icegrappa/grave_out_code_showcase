#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Generator), true)] // The 'true' flag ensures the editor works for subclasses of Generator
public class GeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default inspector options

        Generator generator = (Generator)target;

        if (GUILayout.Button("Generate World"))
        {
            generator.GenerateWorld();
        }
    }
}
#endif