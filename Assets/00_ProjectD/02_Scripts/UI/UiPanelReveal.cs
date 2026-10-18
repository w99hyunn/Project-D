using System;
using UnityEngine;

namespace DoHwa.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiPanelReveal : MonoBehaviour
    {
        [SerializeField] private RectTransform[] sections = Array.Empty<RectTransform>();
        private CanvasGroup group;
        private CanvasGroup[] sectionGroups;
        private Vector2[] rest;
        private float[] started;
        private bool[] moving;
        private int revision;
        private float began, initialAlpha;
        private bool entering, leaving;
        private Layer[][] layers;
        private Vector2[] directions;
        private sealed class Layer
        {
            public RectTransform rect;
            public CanvasGroup opacity;
            public Vector2 position;
            public Vector3 scale;
            public float delay;
            public bool layoutDriven, artwork;
        }

        public void Configure(RectTransform[] targets) => sections = targets;
        private void Awake()
        {
            TryGetComponent<CanvasGroup>(out group);
            rest = new Vector2[sections.Length];
            started = new float[sections.Length];
            moving = new bool[sections.Length];
            sectionGroups = new CanvasGroup[sections.Length];
            layers = new Layer[sections.Length][];
            directions = new Vector2[sections.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                rest[i] = sections[i].anchoredPosition;
                if (!sections[i].TryGetComponent<CanvasGroup>(out sectionGroups[i]))
                    sectionGroups[i] = sections[i].gameObject.AddComponent<CanvasGroup>();
                directions[i] = sections[i].name == "Detail" ? Vector2.right * 38 : i == 0 ? Vector2.left * 30 : Vector2.down * 24;
                bool layout = sections[i].TryGetComponent<UnityEngine.UI.LayoutGroup>(out var layoutGroup);
                var children = new System.Collections.Generic.List<Layer>();
                int order = 0;
                foreach (Transform child in sections[i])
                {
                    if (child is not RectTransform rect) continue;
                    if (!child.TryGetComponent<CanvasGroup>(out var opacity)) opacity=child.gameObject.AddComponent<CanvasGroup>();
                    bool artwork = child.name == "Art" || child.name.Contains("Illustration") || child.name.StartsWith("Part");
                    children.Add(new Layer {rect=rect,opacity=opacity,position=rect.anchoredPosition,scale=rect.localScale,layoutDriven=layout,artwork=artwork,delay=Mathf.Min(order++,8)*.014f});
                }
                layers[i]=children.ToArray();
            }
        }
        private void OnEnable() => Replay();
        public void Replay()
        {
            revision++;
            leaving = false;
            entering = true;
            began = Time.unscaledTime;
            initialAlpha = group.alpha;
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = true;
            for (int i = 0; i < sections.Length; i++) StartSection(i, Mathf.Min(i, 4) * .045f);
        }
        private void StartSection(int index, float delay)
        {
            started[index] = Time.unscaledTime + delay;
            moving[index] = true;
            sectionGroups[index].alpha = 0;
            sections[index].anchoredPosition = rest[index] + directions[index];
            ApplyLayers(index,0);
        }
        private void ApplyLayers(int index,float elapsed)
        {
            foreach(var layer in layers[index])
            {
                float p=Mathf.Clamp01((elapsed-layer.delay)/.30f);
                float ease=1-Mathf.Pow(1-p,4);
                layer.opacity.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(p*1.6f));
                if(!layer.layoutDriven)layer.rect.anchoredPosition=layer.position+Vector2.down*((layer.artwork?24:10)*(1-ease));
                if(layer.layoutDriven||layer.artwork)layer.rect.localScale=layer.scale*Mathf.Lerp(.94f,1,ease);
            }
        }
        public void RefreshContent(int index)
        {
            if (!isActiveAndEnabled || entering || leaving) return;
            StartSection(index, 0);
        }
        private void Update()
        {
            float elapsed = Time.unscaledTime - began;
            if (leaving)
            {
                float p = Mathf.Clamp01(elapsed / .12f);
                group.alpha = initialAlpha * (1 - p * p);
                for (int i = 0; i < sections.Length; i++)
                    sections[i].anchoredPosition = Vector2.Lerp(sections[i].anchoredPosition, rest[i] - directions[i]*.35f, p);
                return;
            }
            if (entering)
            {
                group.alpha = 1 - Mathf.Pow(1 - Mathf.Clamp01(elapsed / .16f), 3);
                group.interactable = elapsed >= .09f;
                if (elapsed >= .55f) entering = false;
            }
            for (int i = 0; i < sections.Length; i++)
            {
                if (!moving[i]) continue;
                float sectionTime = Time.unscaledTime - started[i];
                float p = Mathf.Clamp01(sectionTime / .32f);
                float eased = 1 - Mathf.Pow(1 - p, 4);
                sectionGroups[i].alpha = 1 - Mathf.Pow(1 - Mathf.Clamp01(p * 1.5f), 3);
                sections[i].anchoredPosition = rest[i] + directions[i] * (1 - eased);
                ApplyLayers(i,sectionTime);
                if (sectionTime >= .43f) moving[i] = false;
            }
        }
        public async Awaitable HideAsync()
        {
            int current = ++revision;
            entering = false;
            leaving = true;
            began = Time.unscaledTime;
            initialAlpha = group.alpha;
            group.interactable = false;
            try
            {
                while (Time.unscaledTime - began < .12f)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                    if (current != revision) return;
                }
                group.alpha = 0;
            }
            catch (OperationCanceledException) { }
        }
        private void OnDisable()
        {
            revision++;
            entering = leaving = false;
            for (int i = 0; i < sections.Length; i++)
            {
                moving[i] = false;
                sections[i].anchoredPosition = rest[i];
                sectionGroups[i].alpha = 1;
                foreach(var layer in layers[i])
                {
                    if(!layer.layoutDriven)layer.rect.anchoredPosition=layer.position;
                    layer.rect.localScale=layer.scale;
                    layer.opacity.alpha=1;
                }
            }
        }
    }
}
