using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Body Part Definition", fileName = "BodyPartDefinition")]
public class BodyPartDefinitionSO : ScriptableObject
{
    public string partId = "chitin"; // Заменять partId не нужно, так как в будущем могут появиться одинаковые части тела, типа Железа 1, Железа 2
    public BodyPartType partType = BodyPartType.Chitin;
    public string displayName = "�����";
    public Sprite baseSprite;

    [Tooltip("������ ��������� ��� ������� 5-�� ������.")]
    public List<BodyPartVariantSO> milestoneVariants = new();

    public List<BodyPartVariantSO> GetVariantsForLevel(int level)
    {
        return milestoneVariants
            .Where(v => v != null && v.unlockLevel <= level)
            .ToList();
    }
}