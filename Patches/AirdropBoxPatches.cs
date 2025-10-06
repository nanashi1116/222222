using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.SynchronizableObjects;  // 修正命名空间
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DynamicMaps.Patches
{
    internal class AirdropBoxOnBoxLandPatch : ModulePatch
    {
        internal static event Action<AirdropSynchronizableObject> OnAirdropLanded;
        internal static List<AirdropSynchronizableObject> Airdrops = new List<AirdropSynchronizableObject>();

        private bool _hasRegisteredEvents = false;

        protected override MethodBase GetTargetMethod()
        {
            if (!_hasRegisteredEvents)
            {
                GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
                _hasRegisteredEvents = true;
            }

            // 补丁 AirdropLogicClass 的 method_3 方法（空投落地方法）
            return AccessTools.Method(typeof(AirdropLogicClass), "method_3");
        }

        [PatchPostfix]
        public static void PatchPostfix(AirdropLogicClass __instance)
        {
            // 确保实例不为空，且我们还没有跟踪这个空投
            if (__instance != null && !Airdrops.Contains(__instance.AirdropSynchronizableObject_0))
            {
                Airdrops.Add(__instance.AirdropSynchronizableObject_0);
                OnAirdropLanded?.Invoke(__instance.AirdropSynchronizableObject_0);
            }
        }

        internal static void OnRaidEnd()
        {
            Airdrops.Clear();
        }
    }
}