using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Japsu.Common.UIAdditions
{
    /// <summary>
    /// https://gist.github.com/baba-s/100b68db60da4e564356d836be44876e
    /// </summary>
    public class RadialSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        private bool isPointerDown;

        // Called when the pointer enters our GUI component.
        // Start tracking the mouse
        public void OnPointerEnter(PointerEventData eventData)
        {
            StartCoroutine(nameof(TrackPointer));
        }

        // Called when the pointer exits our GUI component.
        // Stop tracking the mouse
        public void OnPointerExit(PointerEventData eventData)
        {
            StopCoroutine(nameof(TrackPointer));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
        }

        private IEnumerator TrackPointer()
        {
            GraphicRaycaster ray = GetComponentInParent<GraphicRaycaster>();
            StandaloneInputModule input = FindObjectOfType<StandaloneInputModule>();

            Text text = GetComponentInChildren<Text>();

            if (ray != null && input != null)
                while (Application.isPlaying)
                {
                    if (isPointerDown)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform,
                            Input.mousePosition, ray.eventCamera, out Vector2 localPos);

                        // local pos is the mouse position.
                        float angle = (Mathf.Atan2(-localPos.y, localPos.x) * 180f / Mathf.PI + 180f) / 360f;

                        GetComponent<Image>().fillAmount = angle;

                        GetComponent<Image>().color = Color.Lerp(Color.green, Color.red, angle);

                        text.text = ((int)(angle * 360f)).ToString();
                    }

                    yield return 0;
                }
            else
                UnityEngine.Debug.LogWarning("Could not find GraphicRaycaster and/or StandaloneInputModule");
        }
    }
}