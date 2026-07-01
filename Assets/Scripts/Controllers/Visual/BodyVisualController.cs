using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BodyVisualController : MonoBehaviour
{
    [System.Serializable]
    public class VisualSlot
    {
        public string partId;
        public SpriteRenderer baseRenderer;
        public Transform overlayRoot;
    }

    [SerializeField] private PlayerBody body;
    [SerializeField] private List<VisualSlot> slots = new();

    private readonly List<SpriteRenderer> transactionOverlays = new();

    private void OnEnable()
    {
        if (body != null)
            body.OnBodyChanged += RebuildFromBody;

        RebuildFromBody();
    }

    private void OnDisable()
    {
        if (body != null)
            body.OnBodyChanged -= RebuildFromBody;
    }

    public void SetBody(PlayerBody targetBody)
    {
        if (body != null)
            body.OnBodyChanged -= RebuildFromBody;

        body = targetBody;

        if (body != null)
            body.OnBodyChanged += RebuildFromBody;

        RebuildFromBody();
    }

    public void RebuildFromBody()
    {
        if (body == null)
            return;

        body.EnsureStates();

        foreach (var slot in slots)
        {
            if (slot == null || slot.baseRenderer == null || slot.overlayRoot == null)
                continue;

            var state = body.GetState(slot.partId);
            if (state == null || state.definition == null)
                continue;

            slot.baseRenderer.sprite = state.definition.baseSprite;
            slot.baseRenderer.enabled = slot.baseRenderer.sprite != null;

            ClearChildren(slot.overlayRoot);

            int orderOffset = 1;
            foreach (var applied in state.appliedVariants.OrderBy(v => v.milestoneLevel))
            {
                if (applied.variant == null || applied.variant.overlaySprite == null)
                    continue;

                CreateOverlay(slot, applied.variant.overlaySprite, 1f, orderOffset++);
            }
        }
    }

    public void BeginTransactionPreview(List<BodyVariantSelection> selections)
    {
        ClearTransactionPreview();

        if (selections == null)
            return;

        foreach (var selection in selections)
        {
            if (selection == null || selection.variant == null || selection.variant.overlaySprite == null)
                continue;

            var slot = slots.FirstOrDefault(s => s.partId == selection.partId);
            if (slot == null || slot.overlayRoot == null || slot.baseRenderer == null)
                continue;

            var overlay = CreateOverlay(slot, selection.variant.overlaySprite, 0f, transactionOverlays.Count + 1);
            transactionOverlays.Add(overlay);
        }
    }

    public void SetTransactionProgress(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        foreach (var overlay in transactionOverlays)
        {
            if (overlay == null)
                continue;

            var c = overlay.color;
            c.a = progress01;
            overlay.color = c;
        }
    }

    public void ClearTransactionPreview()
    {
        for (int i = transactionOverlays.Count - 1; i >= 0; i--)
        {
            if (transactionOverlays[i] != null)
                Destroy(transactionOverlays[i].gameObject);
        }

        transactionOverlays.Clear();
    }

    private void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }

    private SpriteRenderer CreateOverlay(VisualSlot slot, Sprite sprite, float alpha, int sortingOffset)
    {
        var go = new GameObject($"{slot.partId}_overlay_{sortingOffset}");
        go.transform.SetParent(slot.overlayRoot, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        sr.sortingLayerID = slot.baseRenderer.sortingLayerID;
        sr.sortingOrder = slot.baseRenderer.sortingOrder + sortingOffset;

        var c = sr.color;
        c.a = alpha;
        sr.color = c;

        return sr;
    }
}