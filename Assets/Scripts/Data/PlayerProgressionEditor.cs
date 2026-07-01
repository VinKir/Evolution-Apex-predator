#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerProgression))]
public class PlayerProgressionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var p = (PlayerProgression)target;

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Debug Buttons", EditorStyles.boldLabel);

        if (GUILayout.Button("Add 1 XP"))
        {
            Undo.RecordObject(p, "Add XP");
            p.AddExperience(1);
            EditorUtility.SetDirty(p);
        }

        if (GUILayout.Button("Add 10 XP"))
        {
            Undo.RecordObject(p, "Add XP");
            p.AddExperience(10);
            EditorUtility.SetDirty(p);
        }

        if (GUILayout.Button("Add 1 Biomass"))
        {
            Undo.RecordObject(p, "Add Biomass");
            p.AddBiomass(1f);
            EditorUtility.SetDirty(p);
        }

        if (GUILayout.Button("Add 10 Biomass"))
        {
            Undo.RecordObject(p, "Add Biomass");
            p.AddBiomass(10f);
            EditorUtility.SetDirty(p);
        }

        if (GUILayout.Button("Force Evolve"))
        {
            Undo.RecordObject(p, "Evolve");
            p.Evolve();
            EditorUtility.SetDirty(p);
        }
    }
}
#endif