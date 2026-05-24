using UnityEngine;

namespace SlotDemo
{
    [CreateAssetMenu(fileName = "SymbolTable", menuName = "SlotDemo/Symbol Table")]
    public class SymbolTable : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public SlotSymbol symbol;
            public Sprite sprite;
            public int weight = 1;
            public int multiplier = 1;
        }

        public Entry[] entries;

        // Caches built in OnEnable / OnValidate so hot paths don't recompute every call.
        int totalWeight;
        SlotSymbol fallbackSymbol;
        Sprite[] spriteByEnum;
        int[] multiplierByEnum;

        void OnEnable()
        {
            RebuildCache();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Inspector edits should reflect immediately in Play Mode without re-entering.
            RebuildCache();
        }
#endif

        public void RebuildCache()
        {
            totalWeight = 0;
            spriteByEnum = null;
            multiplierByEnum = null;

            if (entries == null || entries.Length == 0) return;

            int maxIdx = -1;
            for (int i = 0; i < entries.Length; i++)
            {
                totalWeight += Mathf.Max(0, entries[i].weight);
                int idx = (int)entries[i].symbol;
                if (idx > maxIdx) maxIdx = idx;
            }
            fallbackSymbol = entries[entries.Length - 1].symbol;

            int size = maxIdx + 1;
            if (size <= 0) return;

            spriteByEnum = new Sprite[size];
            multiplierByEnum = new int[size];
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                int idx = (int)e.symbol;
                if (idx < 0) continue;
                spriteByEnum[idx] = e.sprite;
                multiplierByEnum[idx] = e.multiplier;
            }
        }

        public Sprite GetSprite(SlotSymbol s)
        {
            int i = (int)s;
            if (spriteByEnum == null || (uint)i >= (uint)spriteByEnum.Length) return null;
            return spriteByEnum[i];
        }

        public int GetMultiplier(SlotSymbol s)
        {
            int i = (int)s;
            if (multiplierByEnum == null || (uint)i >= (uint)multiplierByEnum.Length) return 0;
            return multiplierByEnum[i];
        }

        public SlotSymbol WeightedRandom()
        {
            if (totalWeight <= 0 || entries == null || entries.Length == 0)
                return entries != null && entries.Length > 0 ? entries[0].symbol : default;

            int r = Random.Range(0, totalWeight);
            int acc = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                acc += Mathf.Max(0, entries[i].weight);
                if (r < acc) return entries[i].symbol;
            }
            return fallbackSymbol;
        }
    }
}
