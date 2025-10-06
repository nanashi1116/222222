using HarmonyLib;
using System;
using UnityEngine;
using EFT.UI;

namespace DynamicMaps.Patches
{
    internal class BattleUIScreenPatches
    {
        private static bool _isComponentAdded = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EftBattleUIScreen), "Awake")]
        private static void Postfix(EftBattleUIScreen __instance)
        {
            try
            {
                if (_isComponentAdded)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent already added, skipping...");
                    return;
                }

                if (__instance == null)
                {
                    Plugin.LogSource.LogError("EftBattleUIScreen instance is null in patch");
                    return;
                }

                Plugin.LogSource.LogInfo("Adding MapsPeekComponent to EftBattleUIScreen");

                var gameObject = __instance.gameObject;
                if (gameObject == null)
                {
                    Plugin.LogSource.LogError("EftBattleUIScreen gameObject is null");
                    return;
                }

                // ����Ƿ��Ѵ������
                var existingComponent = gameObject.GetComponent<UI.MapsPeekComponent>();
                if (existingComponent != null)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent already exists on EftBattleUIScreen");
                    _isComponentAdded = true;
                    return;
                }

                // ������
                var component = gameObject.AddComponent<UI.MapsPeekComponent>();
                if (component != null)
                {
                    _isComponentAdded = true;
                    Plugin.LogSource.LogInfo("MapsPeekComponent added successfully to EftBattleUIScreen");
                }
                else
                {
                    Plugin.LogSource.LogError("Failed to add MapsPeekComponent to EftBattleUIScreen");
                }
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError($"Failed to create MapsPeekComponent: {e}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EftBattleUIScreen), "Show")]
        private static void ShowPostfix(EftBattleUIScreen __instance)
        {
            try
            {
                Plugin.LogSource.LogInfo("EftBattleUIScreen Show called");

                // ȷ���������Ļ��ʾʱ��������
                var component = __instance.GetComponent<UI.MapsPeekComponent>();
                if (component != null)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent found and active on Show");
                }
                else
                {
                    Plugin.LogSource.LogWarning("MapsPeekComponent not found on Show, attempting to add...");
                    _isComponentAdded = false; // ���ñ�־�������������
                    Postfix(__instance); // ���³���������
                }
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError($"Error in EftBattleUIScreen Show patch: {e}");
            }
        }
    }
}