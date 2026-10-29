#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(EnemySpawnZone))]
public class EnemySpawnZoneEditor : Editor
{
    private static readonly string[] KillCounterFields = { "killCountMode", "requiredKills", "killRewardFlag" };
    private static readonly string[] CircleOnlyFields = { "radius" };
    private static readonly string[] BoxOnlyFields = { "boxSize" };
    private static readonly string[] GuardHomeFields = { "homeRadius" };

    private readonly SphereBoundsHandle circleHandle = new() { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y };
    private readonly BoxBoundsHandle boxHandle = new() { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y };

    public override void OnInspectorGUI()
    {
        var zone = (EnemySpawnZone)target;

        serializedObject.Update();

        var excluded = new System.Collections.Generic.List<string>();
        if (!zone.countKills)
            excluded.AddRange(KillCounterFields);
        excluded.AddRange(zone.shape == EnemySpawnZone.ZoneShape.Circle ? BoxOnlyFields : CircleOnlyFields);
        if (!zone.guardHome)
            excluded.AddRange(GuardHomeFields);

        DrawPropertiesExcluding(serializedObject, excluded.ToArray());
        serializedObject.ApplyModifiedProperties();

        DrawWarnings(zone);

        if (Application.isPlaying)
            DrawRuntimeInfo(zone);
    }

    private void DrawWarnings(EnemySpawnZone zone)
    {
        if (zone.entries == null || zone.entries.Count == 0)
            EditorGUILayout.HelpBox("Нет ни одной записи врагов: зона ничего не спавнит.", MessageType.Warning);

        if (zone.entries != null)
        {
            for (int index = 0; index < zone.entries.Count; index++)
            {
                var entry = zone.entries[index];
                if (entry == null)
                    continue;

                if (entry.prefab == null)
                {
                    EditorGUILayout.HelpBox($"Запись {index}: не задан префаб.", MessageType.Warning);
                    continue;
                }

                if (entry.prefab.GetComponent<OrganismBody>() == null)
                    EditorGUILayout.HelpBox($"Запись {index} ({entry.prefab.name}): на префабе нет OrganismBody, у врага не будет органов.", MessageType.Warning);

                bool hasEvolution = false;
                if (entry.evolutions != null)
                {
                    foreach (var option in entry.evolutions)
                    {
                        if (option == null || option.evolution == null)
                            continue;

                        hasEvolution = true;

                        int levelCap = option.evolution.LevelCap;
                        if (zone.maxLevel > levelCap)
                            EditorGUILayout.HelpBox(
                                $"Запись {index}: максимум уровня эволюции {option.evolution.name} — {levelCap}, maxLevel {zone.maxLevel} будет обрезан.",
                                MessageType.Info);
                    }
                }

                if (!hasEvolution)
                    EditorGUILayout.HelpBox($"Запись {index} ({entry.prefab.name}): нет эволюций, запись пропускается.", MessageType.Warning);
            }
        }

        if (zone.countKills && string.IsNullOrEmpty(zone.killRewardFlag))
            EditorGUILayout.HelpBox("Счётчик убийств включён, но флаг награды не задан.", MessageType.Warning);
    }

    private static void DrawRuntimeInfo(EnemySpawnZone zone)
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Alive", $"{zone.AliveCount} / {zone.maxAlive}");

        if (zone.countKills)
            EditorGUILayout.LabelField("Kills", $"{zone.KillCount} / {zone.requiredKills}");

        EditorGUILayout.LabelField("Disabled by flags", zone.IsDisabled ? "Yes" : "No");
    }

    private void OnSceneGUI()
    {
        var zone = (EnemySpawnZone)target;
        Vector3 center = zone.transform.position;

        // Handles draw in the zone's own space so the shape ignores object rotation and scale, like the zone itself
        using (new Handles.DrawingScope(Matrix4x4.Translate(center)))
        {
            if (zone.shape == EnemySpawnZone.ZoneShape.Circle)
            {
                Handles.color = new Color(1f, 0.35f, 0.2f, 0.08f);
                Handles.DrawSolidDisc(Vector3.zero, Vector3.forward, zone.radius);

                circleHandle.center = Vector3.zero;
                circleHandle.radius = zone.radius;
                circleHandle.SetColor(new Color(1f, 0.35f, 0.2f, 1f));

                EditorGUI.BeginChangeCheck();
                circleHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Resize Spawn Zone");
                    zone.radius = Mathf.Max(0.1f, circleHandle.radius);
                }
            }
            else
            {
                boxHandle.center = Vector3.zero;
                boxHandle.size = new Vector3(zone.boxSize.x, zone.boxSize.y, 0f);
                boxHandle.SetColor(new Color(1f, 0.35f, 0.2f, 1f));

                EditorGUI.BeginChangeCheck();
                boxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { zone, zone.transform }, "Resize Spawn Zone");
                    zone.boxSize = new Vector2(Mathf.Max(0.1f, boxHandle.size.x), Mathf.Max(0.1f, boxHandle.size.y));
                    // Dragging one side moves the box center
                    zone.transform.position = center + new Vector3(boxHandle.center.x, boxHandle.center.y, 0f);
                }
            }
        }

        Handles.Label(center, zone.name, EditorStyles.boldLabel);
    }
}
#endif
