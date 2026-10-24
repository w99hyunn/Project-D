using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName = "DoHwa/World/Dokkaebi Pact")]
    public sealed class DokkaebiPactDefinition : ScriptableObject
    {
        public string id;
        public string title;
        public string offeringHint;
        public ItemAmount[] offering;
    }
}
