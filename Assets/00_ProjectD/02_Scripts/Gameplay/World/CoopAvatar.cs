using UnityEngine;

namespace DoHwa.World
{
    public sealed class CoopAvatar : MonoBehaviour
    {
        [SerializeField] private DohwaLayeredCharacter character;
        private Vector3 destination;
        private Vector3 previous;
        private AvatarFacing facing;
        public void SetTarget(Vector3 position) { destination = position; }
        private void OnEnable() { destination = previous = transform.position; }
        private void LateUpdate()
        {
            var camera = Camera.main.transform;
            transform.position = Vector3.Lerp(transform.position, destination, 1-Mathf.Exp(-18*Time.deltaTime));
            transform.rotation = camera.rotation;
            var delta = Vector3.ProjectOnPlane(transform.position-previous,Vector3.up);
            bool moving = delta.sqrMagnitude > .000001f;
            if (moving)
            {
                var right = Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized;
                var forward = Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
                facing = (AvatarFacing)(Mathf.RoundToInt(Mathf.Atan2(Vector3.Dot(delta,right),-Vector3.Dot(delta,forward))*Mathf.Rad2Deg/45+8)%8);
            }
            character.Animate(facing, moving ? AvatarMotion.Walk : AvatarMotion.Idle, Time.deltaTime, 1);
            previous = transform.position;
        }
    }
}
