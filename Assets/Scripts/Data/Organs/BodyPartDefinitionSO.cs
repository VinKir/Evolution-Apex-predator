using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Body Part Definition", fileName = "BodyPartDefinition")]
public class BodyPartDefinitionSO : ScriptableObject
{
    public string partId = "chitin"; // Заменять partId не нужно, так как в будущем могут появиться одинаковые части тела, типа Железа 1, Железа 2
    public BodyPartType partType = BodyPartType.Chitin;
    public string displayName = "Хитин";
    
    [TextArea(minLines: 5, maxLines: 50)]
    public string description;
    public Sprite baseSprite;

    [Header("Evolution purchase")]
    [Min(0)]
    public int priceInEvolutionPoints = 1;

    [Header("Base modifiers")]
    public List<BodyStatModifier> modifiers = new();

    [Header("Mutations")]
    public List<BodyPartVariantSO> bodyPartMutations = new();

    public List<BodyPartVariantSO> GetVariantsForLevel(int level)
    {
        return bodyPartMutations
            .Where(v => v != null && v.unlockLevel <= level)
            .ToList();
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var variant in bodyPartMutations)
        {
            if (variant == null)
                continue;

            if (variant.bodyPart != this)
            {
                variant.bodyPart = this;
                EditorUtility.SetDirty(variant);
            }
        }
    }
    #endif
}