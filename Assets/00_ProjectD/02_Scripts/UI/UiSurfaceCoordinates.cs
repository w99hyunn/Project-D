using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    // Store element-local coordinates before Canvas batching transforms the vertices.
    public sealed class UiSurfaceCoordinates : BaseMeshEffect
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var rect = graphic.rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0) return;
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.uv1 = new Vector2((vertex.position.x - rect.xMin) / rect.width,
                    (vertex.position.y - rect.yMin) / rect.height);
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
