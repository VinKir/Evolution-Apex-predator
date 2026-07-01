using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Body Part Definition", fileName = "BodyPartDefinition")]
public class BodyPartDefinitionSO : ScriptableObject
{
    public string partId = "chitin"; // заменять partId не нужно, так как в будущем могут появиться одинаковые части тела, типа Железа 1, Железа 2
    public BodyPartType partType = BodyPartType.Chitin;
    public string displayName = "Хитин";
    public Sprite baseSprite;

    [Tooltip("Список вариантов для каждого 5-го уровня.")]
    public List<BodyPartVariantSO> milestoneVariants = new();

    public List<BodyPartVariantSO> GetVariantsForLevel(int level)
    {
        return milestoneVariants
            .Where(v => v != null && v.unlockLevel <= level)
            .ToList();
    }
}