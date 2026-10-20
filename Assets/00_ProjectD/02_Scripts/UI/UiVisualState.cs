using UnityEngine;
namespace DoHwa.UI
{
    [RequireComponent(typeof(Animator))]
    public sealed class UiVisualState : MonoBehaviour
    {
        private Animator animator;
        private bool activeState;
        private void Awake() => TryGetComponent<Animator>(out animator);
        private void OnEnable() { animator.Update(0); animator.SetBool("StyleActive", activeState); }
        public void SetActiveState(bool value)
        {
            activeState = value;
            if (isActiveAndEnabled) animator.SetBool("StyleActive", value);
        }
    }
}
