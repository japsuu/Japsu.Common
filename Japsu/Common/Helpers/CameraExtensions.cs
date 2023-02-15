
using UnityEngine;

namespace Japsu.Common.CameraAdditions
{
    public static class CameraExtensions
    {
        public static bool IsObjectVisible(this UnityEngine.Camera @this, Renderer renderer)
        {
            return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(@this), renderer.bounds);
        }
    }
}
