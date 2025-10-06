// ... 其他using部分保持不变 ...
using EFT;
using EFT.UI;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 确保如果需要Image组件时有此引用

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        // ... 其他字段和属性 ...

        /// <summary>
        /// 尝试将MapsPeekComponent添加到BattleUIScreen
        /// </summary>
        internal void TryAddPeekComponent(EftBattleUIScreen battleUI)
        {
            if (battleUI == null)
            {
                Plugin.LogSource.LogError("TryAddPeekComponent: battleUI is null");
                return;
            }

            try
            {
                Plugin.LogSource.LogInfo("TryAddPeekComponent: Attempting to add MapsPeekComponent");

                // 检查是否已存在组件
                var existingComponent = battleUI.GetComponent<MapsPeekComponent>();
                if (existingComponent != null)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent already attached to BattleUIScreen");
                    return;
                }

                // 添加组件
                var component = battleUI.gameObject.AddComponent<MapsPeekComponent>();
                if (component != null)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent successfully added to BattleUIScreen");
                    // 组件初始化交给其自身的Start或Awake方法，或稍后触发
                }
                else
                {
                    Plugin.LogSource.LogError("Failed to add MapsPeekComponent to BattleUIScreen");
                }
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError($"Exception in TryAddPeekComponent: {e}");
            }
        }

        // ... 其他方法 ...
    }

    public class MapsPeekComponent : MonoBehaviour
    {
        private GameWorld _gameWorld;
        private Player _player;
        private bool _isPeeking = false;
        private GameObject _mapPanel;
        private bool _isInitialized = false;
        private static MapsPeekComponent _instance;

        public static MapsPeekComponent Instance => _instance;
        public bool EnablePeek { get; set; } = true;

        private void Awake()
        {
            _instance = this;
            Plugin.LogSource.LogInfo("MapsPeekComponent Awake started.");
            // 避免在Awake中进行复杂的查找，UI可能未准备就绪
        }

        private void Start()
        {
            Plugin.LogSource.LogInfo("MapsPeekComponent Start called.");
            // 延迟初始化，等待UI布局完成:cite[1]
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            // 等待几帧确保UI已完全加载和布局:cite[1]
            yield return new WaitForSeconds(0.5f); // 可根据实际情况调整等待时间

            Plugin.LogSource.LogInfo("MapsPeekComponent: Starting delayed initialization.");

            try
            {
                // 方法1: 首先尝试从当前对象（BattleUIScreen）开始查找
                _mapPanel = FindMapPanelRecursive(transform);
                if (_mapPanel != null)
                {
                    Plugin.LogSource.LogInfo($"MapsPeekComponent: Found MapPanel via recursive search: {_mapPanel.name}");
                }

                // 方法2: 如果递归查找没找到，尝试使用已知的可能路径
                if (_mapPanel == null)
                {
                    string[] possiblePaths = {
                        "MapPanel",
                        "HUD/MapPanel",
                        "MainPanel/MapPanel",
                        "BottomLeftLayer/MapPanel",
                        "UI Layer/MapPanel"
                    };
                    foreach (var path in possiblePaths)
                    {
                        _mapPanel = GameObject.Find(path); // 谨慎使用，可能找到非预期的对象
                        if (_mapPanel != null)
                        {
                            Plugin.LogSource.LogInfo($"MapsPeekComponent: Found MapPanel via GameObject.Find: {path}");
                            break;
                        }
                    }
                }

                // 方法3: 通过查找BattleUIScreen再找其子对象
                if (_mapPanel == null)
                {
                    var battleUI = GameObject.Find("BattleUIScreen(Clone)");
                    if (battleUI != null)
                    {
                        _mapPanel = FindMapPanelRecursive(battleUI.transform);
                        if (_mapPanel != null)
                        {
                            Plugin.LogSource.LogInfo($"MapsPeekComponent: Found MapPanel via BattleUIScreen search: {_mapPanel.name}");
                        }
                    }
                }

                if (_mapPanel == null)
                {
                    Plugin.LogSource.LogError("MapsPeekComponent: Could not find MapPanel after all attempts.");
                    yield break;
                }

                // 检查获取必要的组件
                var image = _mapPanel.GetComponent<Image>();
                if (image == null)
                {
                    Plugin.LogSource.LogWarning("MapsPeekComponent: MapPanel does not have an Image component.");
                }

                // 设置初始状态
                _mapPanel.SetActive(false);
                _isInitialized = true;

                Plugin.LogSource.LogInfo("MapsPeekComponent initialized successfully.");

            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"MapsPeekComponent DelayedInitialization Error: {ex}");
            }
        }

        /// <summary>
        /// 递归查找名为"MapPanel"的GameObject:cite[1]
        /// </summary>
        private GameObject FindMapPanelRecursive(Transform parent)
        {
            if (parent == null) return null;

            // 检查当前Transform
            if (parent.name.Contains("MapPanel")) // 使用Contains可能比精确匹配更宽松
            {
                return parent.gameObject;
            }

            // 递归检查所有子对象
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var result = FindMapPanelRecursive(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void Update()
        {
            if (!_isInitialized || !EnablePeek)
                return;

            try
            {
                // 获取游戏世界和玩家实例
                if (_gameWorld == null)
                {
                    _gameWorld = Singleton<GameWorld>.Instance;
                    // 不要在此处return，可能下一帧才能获取到
                }

                if (_player == null && _gameWorld != null)
                {
                    _player = _gameWorld.MainPlayer;
                    // 不要在此处return，可能下一帧才能获取到
                }

                // 如果仍未获取到玩家，跳过本次Update
                if (_player == null)
                    return;

                // 检查Tab键状态
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
                Plugin.LogSource.LogError($"MapsPeekComponent Update Error: {ex}");
            }
        }

        private void ShowMap()
        {
            if (_mapPanel != null && EnablePeek)
            {
                _mapPanel.SetActive(true);
            }
        }

        private void HideMap()
        {
            if (_mapPanel != null)
            {
                _mapPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            Plugin.LogSource.LogInfo("MapsPeekComponent destroyed.");
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