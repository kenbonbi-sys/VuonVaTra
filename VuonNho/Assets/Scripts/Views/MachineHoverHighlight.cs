using System.Collections.Generic;
using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>Temporary per-material tint; preserves both renderer-wide and indexed overrides.</summary>
    public sealed class MachineHoverHighlight
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        sealed class SavedSlot
        {
            public Renderer Renderer;
            public int Index;
            public MaterialPropertyBlock Original;
        }

        readonly List<SavedSlot> _slots = new List<SavedSlot>();

        public void Apply(Transform visualRoot, Renderer excludedRenderer)
        {
            Restore();
            if (visualRoot == null) return;
            var renderers = visualRoot.GetComponentsInChildren<Renderer>(false);
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (renderer == excludedRenderer || !renderer.enabled) continue;
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null) continue;
                    bool hasBase = material.HasProperty(BaseColorId);
                    bool hasColor = material.HasProperty(ColorId);
                    if (!hasBase && !hasColor) continue;

                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, index);
                    var highlighted = new MaterialPropertyBlock();
                    if (original.isEmpty)
                        renderer.GetPropertyBlock(highlighted);
                    else
                        renderer.GetPropertyBlock(highlighted, index);

                    if (hasBase) Brighten(highlighted, material, BaseColorId);
                    if (hasColor) Brighten(highlighted, material, ColorId);
                    _slots.Add(new SavedSlot
                    {
                        Renderer = renderer,
                        Index = index,
                        Original = original.isEmpty ? null : original
                    });
                    renderer.SetPropertyBlock(highlighted, index);
                }
            }
        }

        static void Brighten(MaterialPropertyBlock block, Material material, int property)
        {
            Color current = block.HasColor(property) ? block.GetColor(property) : material.GetColor(property);
            Color bright = Color.Lerp(current, new Color(1f, 1f, 0.88f, current.a), 0.24f);
            bright.a = current.a;
            block.SetColor(property, bright);
        }

        public void Restore()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Renderer != null) slot.Renderer.SetPropertyBlock(slot.Original, slot.Index);
            }
            _slots.Clear();
        }
    }
}
