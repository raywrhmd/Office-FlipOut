using UnityEngine;

namespace OfficeFlipOut.Systems
{
    public static class Easing
    {
        public static float EaseOutCubic(float t)
        {
            float f = 1f - t;
            return 1f - f * f * f;
        }

        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }

        public static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}