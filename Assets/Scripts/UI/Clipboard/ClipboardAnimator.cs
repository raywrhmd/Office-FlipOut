using System;
using System.Collections;
using UnityEngine;

namespace OfficeFlipOut.UI
{
    public class ClipboardAnimator : MonoBehaviour
    {
        private RectTransform boardRect;
        private CanvasGroup boardGroup;
        private CanvasGroup dimmerGroup;
        private Coroutine activeRoutine;

        private const float OpenDuration = 0.34f;
        private const float CloseDuration = 0.22f;
        private const float ClosedScale = 0.9f;

        private Vector2 onScreenPos;
        private Vector2 offScreenBelow;
        private Vector3 onScreenScale = Vector3.one;

        public bool IsAnimating { get; private set; }

        public void Configure(RectTransform board, CanvasGroup boardCanvasGroup, CanvasGroup dimmer)
        {
            boardRect = board;
            boardGroup = boardCanvasGroup;
            dimmerGroup = dimmer;

            RefreshAnchors();
            SnapClosed();
        }

        public void RefreshAnchors()
        {
            if (boardRect == null) return;

            onScreenPos = boardRect.anchoredPosition;
            float travelDistance = Mathf.Max(760f, boardRect.rect.height * 1.18f);
            offScreenBelow = new Vector2(onScreenPos.x, onScreenPos.y - travelDistance);

            if (IsAnimating) return;
            if (boardGroup != null && boardGroup.alpha > 0.99f)
            {
                boardRect.anchoredPosition = onScreenPos;
                boardRect.localScale = onScreenScale;
            }
            else
            {
                boardRect.anchoredPosition = offScreenBelow;
                boardRect.localScale = new Vector3(ClosedScale, ClosedScale, 1f);
            }
        }

        public void PlayOpen(Action onComplete = null)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateOpen(onComplete));
        }

        public void PlayClose(Action onComplete = null)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateClose(onComplete));
        }

        public void SnapOpen()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            boardRect.anchoredPosition = onScreenPos;
            boardRect.localScale = onScreenScale;
            boardRect.localRotation = Quaternion.identity;
            boardGroup.alpha = 1f;
            if (dimmerGroup != null) dimmerGroup.alpha = 1f;
            IsAnimating = false;
        }

        public void SnapClosed()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            boardRect.anchoredPosition = offScreenBelow;
            boardRect.localScale = new Vector3(ClosedScale, ClosedScale, 1f);
            boardRect.localRotation = Quaternion.identity;
            boardGroup.alpha = 0f;
            if (dimmerGroup != null) dimmerGroup.alpha = 0f;
            IsAnimating = false;
        }

        private IEnumerator AnimateOpen(Action onComplete)
        {
            IsAnimating = true;
            float elapsed = 0f;

            while (elapsed < OpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / OpenDuration);
                float eased = EaseOutCubic(t);
                float easedScale = EaseOutBack(Mathf.Clamp01(t * 0.95f));

                boardRect.anchoredPosition = Vector2.Lerp(offScreenBelow, onScreenPos, eased);
                float s = Mathf.Lerp(ClosedScale, 1f, easedScale);
                boardRect.localScale = new Vector3(s, s, 1f);
                boardGroup.alpha = Mathf.Clamp01(t * 3f);
                if (dimmerGroup != null) dimmerGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.85f));

                yield return null;
            }

            boardRect.anchoredPosition = onScreenPos;
            boardRect.localScale = onScreenScale;
            boardGroup.alpha = 1f;
            if (dimmerGroup != null) dimmerGroup.alpha = 1f;
            IsAnimating = false;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateClose(Action onComplete)
        {
            IsAnimating = true;
            float elapsed = 0f;

            while (elapsed < CloseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / CloseDuration);
                float eased = EaseInCubic(t);

                boardRect.anchoredPosition = Vector2.Lerp(onScreenPos, offScreenBelow, eased);
                float s = Mathf.Lerp(1f, ClosedScale, eased);
                boardRect.localScale = new Vector3(s, s, 1f);
                boardGroup.alpha = 1f - Mathf.Clamp01(t * 2f);
                if (dimmerGroup != null) dimmerGroup.alpha = 1f - Mathf.Clamp01(t * 2.4f);

                yield return null;
            }

            boardRect.anchoredPosition = offScreenBelow;
            boardRect.localScale = new Vector3(ClosedScale, ClosedScale, 1f);
            boardGroup.alpha = 0f;
            if (dimmerGroup != null) dimmerGroup.alpha = 0f;
            IsAnimating = false;
            onComplete?.Invoke();
        }

        private static float EaseOutCubic(float t)
        {
            float f = 1f - t;
            return 1f - f * f * f;
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }
    }
}
