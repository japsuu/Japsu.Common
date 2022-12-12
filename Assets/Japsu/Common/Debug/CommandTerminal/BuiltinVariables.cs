using UnityEngine;

namespace Japsu.Common.CommandTerminal
{
    public static class BuiltinVariables
    {
        [RegisterVariable]
        public static bool HandleUnityLog
        {
            get => Terminal.LogUnityMessages;
            set => Terminal.LogUnityMessages = value;
        }

        [RegisterVariable]
        public static float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
    }
}