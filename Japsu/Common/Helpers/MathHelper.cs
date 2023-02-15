using UnityEngine;

namespace Japsu.Common.MathAdditions
{
    public static class MathHelper
    {
        /// <summary>
        /// Selects a random value inside [-range,range], while preferring higher values in the curve.
        /// </summary>
        /// <returns>A random value inside [-range,range]</returns>
        public static float WeightedRandomRange(AnimationCurve weightCurve, float range)
        {
            float v = Random.Range(0f, 1f);
            return RescaleCurveValue(weightCurve, v) * range;
        }

        /// <returns>curve.Evaluate() result in [-1,1] -range.</returns>
        public static float RescaleCurveValue(AnimationCurve curve, float t)
        {
            return (curve.Evaluate(t) - 0.5f) * 2f;
        }
    }
}