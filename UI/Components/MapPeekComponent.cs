using Comfort.Common;
using EFT;
using System;
using System.Collections;
using UnityEngine;

namespace DynamicMaps.UI
{
    public class MapsPeekComponent : MonoBehaviour
    {
        private GameWorld _gameWorld;
        private Player _player;
        private bool _isPeeking = false;
        private GameObject _mapPanel;
        private bool _isInitialized = false;
        private static MapsPeekComponent _instance;
        private Coroutine _initCoroutine;

        public static MapsPeekComponent Instance => _instance;
        public bool EnablePeek { get; set; } = true;

        private void Awake()
        {
            _instance = this;
            Plugin.LogSource.LogInfo("MapsPeekComponent Awake started");

            // 在 Awake 中开始初始化协程
            _initCoroutine = StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            Plugin.LogSource.LogInfo("Starting delayed initialization...");

            // 等待几帧确保 BattleUIScreen 完全初始化
            yield return new WaitForSeconds(1f);

            try
            {
                // 首先尝试查找 BattleUIScreen
                var battleUI = GameObject.Find("BattleUIScreen(Clone)");
                if (battleUI == null)
                {
                    Plugin.LogSource.LogError("BattleUIScreen(Clone) not found!");
                    yield break;
                }

                Plugin.LogSource.LogInfo($"Found BattleUIScreen: {battleUI.name}");

                // 打印 BattleUIScreen 的子对象结构用于调试
                LogChildrenRecursive(battleUI.transform, 0);

                // 尝试多种可能的 MapPanel 路径
                _mapPanel = FindMapPanel(battleUI.transform);

                if (_mapPanel == null)
                {
                    Plugin.LogSource.LogError("MapPanel not found after all search attempts!");
                    yield break;
                }

                Plugin.LogSource.LogInfo($"Successfully found MapPanel: {_mapPanel.name}");

                // 初始状态 - 隐藏地图
                _mapPanel.SetActive(false);
                _isInitialized = true;

                Plugin.LogSource.LogInfo("MapsPeekComponent initialized successfully");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"MapsPeekComponent initialization error: {ex}");
            }
        }

        private GameObject FindMapPanel(Transform parent)
        {
            // 尝试多种可能的路径
            string[] possiblePaths = {
                "MapPanel",
                "HUD/MapPanel",
                "MainPanel/MapPanel",
                "LeftTop/MapPanel",
                "BottomLeft/MapPanel",
                "TopLeft/MapPanel"
            };

            foreach (var path in possiblePaths)
            {
                var panel = parent.Find(path);
                if (panel != null)
                {
                    Plugin.LogSource.LogInfo($"Found MapPanel at path: {path}");
                    return panel.gameObject;
                }
            }

            // 如果路径查找失败，尝试递归查找
            return FindMapPanelRecursive(parent);
        }

        private GameObject FindMapPanelRecursive(Transform parent)
        {
            if (parent == null) return null;

            // 检查当前对象
            if (parent.name.Contains("Map") || parent.name.Contains("MAP") || parent.name.Contains("map"))
            {
                Plugin.LogSource.LogInfo($"Found potential map object: {parent.name}");
                return parent.gameObject;
            }

            // 递归检查子对象
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var result = FindMapPanelRecursive(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void LogChildrenRecursive(Transform parent, int depth)
        {
            if (depth > 3) return; // 限制递归深度

            string indent = new string(' ', depth * 2);
            Plugin.LogSource.LogInfo($"{indent}{parent.name} ({parent.childCount} children)");

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                LogChildrenRecursive(child, depth + 1);
            }
        }

        private void Update()
        {
            if (!_isInitialized || !EnablePeek)
                return;

            try
            {
                // 安全地获取游戏世界和玩家
                if (_gameWorld == null)
                {
                    _gameWorld = Singleton<GameWorld>.Instance;
                    if (_gameWorld == null) return;
                }

                if (_player == null && _gameWorld != null)
                {
                    _player = _gameWorld.MainPlayer;
                    if (_player == null) return;
                }

                // 检查Tab键
                bool tabPressed = Input.GetKey(KeyCode.Tab);

                if (tabPressed && !_isPeeking)
                {
                    ShowMap();
                    _isPeeking = true;
                }
                else if (!tabPressed && _isPeeking)
                {
                    HideMap();
                    _isPeeking = false;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"MapsPeekComponent Update error: {ex}");
            }
        }

        private void ShowMap()
        {
            if (_mapPanel != null && EnablePeek)
            {
                _mapPanel.SetActive(true);
                Plugin.LogSource.LogInfo("Map shown");
            }
        }

        private void HideMap()
        {
            if (_mapPanel != null)
            {
                _mapPanel.SetActive(false);
                Plugin.LogSource.LogInfo("Map hidden");
            }
        }

        private void OnDestroy()
        {
            if (_initCoroutine != null)
            {
                StopCoroutine(_initCoroutine);
            }

            if (_instance == this)
            {
                _instance = null;
            }

            Plugin.LogSource.LogInfo("MapsPeekComponent destroyed");
        }

        public void SetPeekEnabled(bool enabled)
        {
            EnablePeek = enabled;
            if (!enabled && _mapPanel != null)
            {
                _mapPanel.SetActive(false);
                _isPeeking = false;
            }
        }
    }
}