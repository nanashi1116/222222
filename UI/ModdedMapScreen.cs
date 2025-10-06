using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private static ModdedMapScreen _instance;
        private Button _toggleButton;
        private TextMeshProUGUI _toggleButtonText;
        private bool _isMapEnabled = true;

        public static ModdedMapScreen Instance
        {
            get
            {
                return _instance;
            }
        }

        private void Awake()
        {
            try
            {
                Plugin.LogSource.LogInfo("Initializing ModdedMapScreen");

                _instance = this;

                // 延迟创建UI元素
                Invoke("CreateUIElements", 0.1f);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to initialize ModdedMapScreen: " + ex.ToString());
            }
        }

        private void CreateUIElements()
        {
            try
            {
                // 创建切换按钮
                CreateToggleButton();

                Plugin.LogSource.LogInfo("ModdedMapScreen UI elements created successfully");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to create UI elements: " + ex.ToString());
            }
        }

        private void CreateToggleButton()
        {
            try
            {
                // 查找现有的UI元素作为父级
                Transform parent = GameObject.Find("BattleUIScreen(Clone)").transform;
                if (parent == null)
                {
                    Plugin.LogSource.LogError("Could not find parent for toggle button");
                    return;
                }

                // 创建按钮GameObject
                GameObject buttonGO = new GameObject("MapToggleButton");
                buttonGO.transform.SetParent(parent, false);

                // 添加RectTransform
                RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                rectTransform.anchoredPosition = new Vector2(-20f, -20f);
                rectTransform.sizeDelta = new Vector2(120f, 30f);

                // 添加按钮组件
                _toggleButton = buttonGO.AddComponent<Button>();

                // 添加背景图像
                Image image = buttonGO.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                // 创建文本子对象
                GameObject textGO = new GameObject("ButtonText");
                textGO.transform.SetParent(buttonGO.transform, false);

                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                _toggleButtonText = textGO.AddComponent<TextMeshProUGUI>();
                _toggleButtonText.text = "Hide Map";
                _toggleButtonText.color = Color.white;
                _toggleButtonText.alignment = TMPro.TextAlignmentOptions.Center;
                _toggleButtonText.fontSize = 12f;

                // 添加按钮点击事件
                _toggleButton.onClick.AddListener(new Action(ToggleMapVisibility));

                Plugin.LogSource.LogInfo("Toggle button created successfully");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to create toggle button: " + ex.ToString());
            }
        }

        private void ToggleMapVisibility()
        {
            try
            {
                _isMapEnabled = !_isMapEnabled;

                // 更新地图显示状态
                Components.MapsPeekComponent mapsPeekComponent = Components.MapsPeekComponent.Instance;
                if (mapsPeekComponent != null)
                {
                    mapsPeekComponent.SetPeekEnabled(_isMapEnabled);
                }

                // 更新按钮文本
                if (_toggleButtonText != null)
                {
                    _toggleButtonText.text = (_isMapEnabled ? "Hide Map" : "Show Map");
                }

                Plugin.LogSource.LogInfo("Map visibility toggled: " + _isMapEnabled);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Error toggling map visibility: " + ex.ToString());
            }
        }

        private void OnDestroy()
        {
            _instance = null;
            Plugin.LogSource.LogInfo("ModdedMapScreen destroyed");
        }
    }
}