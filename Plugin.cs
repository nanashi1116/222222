using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DynamicMaps
{
    [BepInPlugin("com.mpstark.DynamicMaps", "Dynamic Maps", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource LogSource;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private void Awake()
        {
            Instance = this;
            LogSource = Logger;

            try
            {
                // 创建全局管理器
                var managerObject = new GameObject("DynamicMaps_Manager");
                DontDestroyOnLoad(managerObject);
                managerObject.AddComponent<DynamicMapsManager>();

                // 应用 Harmony 补丁
                var harmony = new Harmony("com.mpstark.DynamicMaps");
                harmony.PatchAll();

                Logger.LogInfo("DynamicMaps plugin loaded successfully!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load DynamicMaps: {ex}");
            }
        }
    }
}