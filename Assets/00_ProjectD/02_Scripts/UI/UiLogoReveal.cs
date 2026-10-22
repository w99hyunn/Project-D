using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    // Animator owns timing; this component only supplies the shader's reveal value.
    public sealed class UiLogoReveal : BaseMeshEffect
    {
        [SerializeField, Range(0, 1)] private float progress = 1;

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.uv1.x = progress;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
