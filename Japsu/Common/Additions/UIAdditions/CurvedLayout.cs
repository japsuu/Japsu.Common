using UnityEngine;
using UnityEngine.UI;

namespace Japsu.Common.UIAdditions
{
    [AddComponentMenu("Layout/Extensions/Curved Layout")]
    public class CurvedLayout : LayoutGroup
    {
        public Vector3 CurveOffset;

        [Tooltip("axis along which to place the items, Normalized before use")]
        public Vector3 itemAxis;

        [Tooltip("size of each item along the Normalized axis")]
        public float itemSize;

        // the slope can be moved by altering this setting, it could be constrained to the 0-1 range, but other values are usefull for animations
        public float centerPoint = 0.5f;

        protected override void OnEnable()
        {
            base.OnEnable();
            CalculateRadial();
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }

        public override void CalculateLayoutInputVertical()
        {
            CalculateRadial();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            CalculateRadial();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            CalculateRadial();
        }
#endif

        private void CalculateRadial()
        {
            m_Tracker.Clear();
            if (transform.childCount == 0)
                return;

            //one liner for figuring out the pivot (why not a utility function switch statement?)
            Vector2 pivot = new((int)childAlignment % 3 * 0.5f, (int)childAlignment / 3f * 0.5f);

            Vector3 lastPos = new(
                GetStartOffset(0, GetTotalPreferredSize(0)),
                GetStartOffset(1, GetTotalPreferredSize(1)),
                0f
            );

            float lerp = 0;
            float step = 1f / transform.childCount;

            Vector3 dist = itemAxis.normalized * itemSize;

            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = (RectTransform)transform.GetChild(i);

                if (child == null) continue;

                //stop the user from altering certain values in the editor
                m_Tracker.Add(this, child,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.Pivot);
                Vector3 vPos = lastPos + dist;

                child.localPosition = lastPos = vPos + (lerp - centerPoint) * CurveOffset;

                child.pivot = pivot;
                //child anchors are not yet calculated, each child should set it's own size for now
                child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
                lerp += step;
            }
        }
    }
}