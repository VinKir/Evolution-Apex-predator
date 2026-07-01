using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Evolution/Body Part Variant", fileName = "BodyPartVariant")]
public class BodyPartVariantSO : ScriptableObject
{
    public string variantId;
    public string displayName;
    public int unlockLevel = 5;

    [TextArea(minLines: 5, maxLines: 50)]
    public string description;

    public Sprite overlaySprite;
    public List<BodyStatModifier> modifiers = new List<BodyStatModifier>();
}