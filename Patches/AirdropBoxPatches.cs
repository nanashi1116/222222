using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.SynchronizableObjects;  // ���������ռ�
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

            // ���� AirdropLogicClass �� method_3 ��������Ͷ��ط�����
            return AccessTools.Method(typeof(AirdropLogicClass), "method_3");
        }

        [PatchPostfix]
        public static void PatchPostfix(AirdropLogicClass __instance)
        {
            // ȷ��ʵ����Ϊ�գ������ǻ�û�и��������Ͷ
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