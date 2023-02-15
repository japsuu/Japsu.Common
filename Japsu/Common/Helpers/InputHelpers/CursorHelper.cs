using UnityEngine;

namespace Japsu.Common.InputAdditions
{
    public static class CursorHelper
    {
        public static void LockCursor()
        {
            if (!Cursor.visible) return;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public static void ReleaseCursor()
        {
            if (Cursor.visible) return;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}