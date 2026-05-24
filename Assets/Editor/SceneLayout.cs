using UnityEngine;

namespace SlotDemo.EditorTools
{
    // Snapshot of a manually-tweaked scene layout. Build All reads this (when present)
    // and applies the captured anchoredPosition / sizeDelta to each widget after creating it,
    // so user-nudged positions survive future rebuilds.
    //
    // Asset lives in Assets/Data/SceneLayout.asset. Delete it to fall back to code defaults.
    public class SceneLayout : ScriptableObject
    {
        [System.Serializable]
        public struct WidgetLayout
        {
            public string name;                  // GameObject name to find under Canvas
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
        }

        public WidgetLayout[] widgets;
    }
}
