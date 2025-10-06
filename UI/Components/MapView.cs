// ... ����using���ֱ��ֲ��� ...
using EFT;
using EFT.UI;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI; // ȷ�������ҪImage���ʱ�д�����

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        // ... �����ֶκ����� ...

        /// <summary>
        /// ���Խ�MapsPeekComponent��ӵ�BattleUIScreen
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

                // ����Ƿ��Ѵ������
                var existingComponent = battleUI.GetComponent<MapsPeekComponent>();
                if (existingComponent != null)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent already attached to BattleUIScreen");
                    return;
                }

                // ������
                var component = battleUI.gameObject.AddComponent<MapsPeekComponent>();
                if (component != null)
                {
                    Plugin.LogSource.LogInfo("MapsPeekComponent successfully added to BattleUIScreen");
                    // �����ʼ�������������Start��Awake���������Ժ󴥷�
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

        // ... �������� ...
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
            // ������Awake�н��и��ӵĲ��ң�UI����δ׼������
        }

        private void Start()
        {
            Plugin.LogSource.LogInfo("MapsPeekComponent Start called.");
            // �ӳٳ�ʼ�����ȴ�UI�������:cite[1]
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            // �ȴ���֡ȷ��UI����ȫ���غͲ���:cite[1]
            yield return new WaitForSeconds(0.5f); // �ɸ���ʵ����������ȴ�ʱ��

            Plugin.LogSource.LogInfo("MapsPeekComponent: Starting delayed initialization.");

            try
            {
                // ����1: ���ȳ��Դӵ�ǰ����BattleUIScreen����ʼ����
                _mapPanel = FindMapPanelRecursive(transform);
                if (_mapPanel != null)
                {
                    Plugin.LogSource.LogInfo($"MapsPeekComponent: Found MapPanel via recursive search: {_mapPanel.name}");
                }

                // ����2: ����ݹ����û�ҵ�������ʹ����֪�Ŀ���·��
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
                        _mapPanel = GameObject.Find(path); // ����ʹ�ã������ҵ���Ԥ�ڵĶ���
                        if (_mapPanel != null)
                        {
                            Plugin.LogSource.LogInfo($"MapsPeekComponent: Found MapPanel via GameObject.Find: {path}");
                            break;
                        }
                    }
                }

                // ����3: ͨ������BattleUIScreen�������Ӷ���
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

                // ����ȡ��Ҫ�����
                var image = _mapPanel.GetComponent<Image>();
                if (image == null)
                {
                    Plugin.LogSource.LogWarning("MapsPeekComponent: MapPanel does not have an Image component.");
                }

                // ���ó�ʼ״̬
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
        /// �ݹ������Ϊ"MapPanel"��GameObject:cite[1]
        /// </summary>
        private GameObject FindMapPanelRecursive(Transform parent)
        {
            if (parent == null) return null;

            // ��鵱ǰTransform
            if (parent.name.Contains("MapPanel")) // ʹ��Contains���ܱȾ�ȷƥ�������
            {
                return parent.gameObject;
            }

            // �ݹ��������Ӷ���
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
                // ��ȡ��Ϸ��������ʵ��
                if (_gameWorld == null)
                {
                    _gameWorld = Singleton<GameWorld>.Instance;
                    // ��Ҫ�ڴ˴�return��������һ֡���ܻ�ȡ��
                }

                if (_player == null && _gameWorld != null)
                {
                    _player = _gameWorld.MainPlayer;
                    // ��Ҫ�ڴ˴�return��������һ֡���ܻ�ȡ��
                }

                // �����δ��ȡ����ң���������Update
                if (_player == null)
                    return;

                // ���Tab��״̬
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