using UnityEngine;

namespace DoHwa
{
    [CreateAssetMenu(menuName = "DoHwa/완결형 캐릭터 몸체")]
    public sealed class DohwaAvatarBodySet : ScriptableObject
    {
        public string profile;
        // Complete anatomy in every pose. Clothes never define the shape of the base body.
        public Sprite[] frames;
    }
}
