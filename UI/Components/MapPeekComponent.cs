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

            // �� Awake �п�ʼ��ʼ��Э��
            _initCoroutine = StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            Plugin.LogSource.LogInfo("Starting delayed initialization...");

            // �ȴ���֡ȷ�� BattleUIScreen ��ȫ��ʼ��
            yield return new WaitForSeconds(1f);

            try
            {
                // ���ȳ��Բ��� BattleUIScreen
                var battleUI = GameObject.Find("BattleUIScreen(Clone)");
                if (battleUI == null)
                {
                    Plugin.LogSource.LogError("BattleUIScreen(Clone) not found!");
                    yield break;
                }

                Plugin.LogSource.LogInfo($"Found BattleUIScreen: {battleUI.name}");

                // ��ӡ BattleUIScreen ���Ӷ���ṹ���ڵ���
                LogChildrenRecursive(battleUI.transform, 0);

                // ���Զ��ֿ��ܵ� MapPanel ·��
                _mapPanel = FindMapPanel(battleUI.transform);

                if (_mapPanel == null)
                {
                    Plugin.LogSource.LogError("MapPanel not found after all search attempts!");
                    yield break;
                }

                Plugin.LogSource.LogInfo($"Successfully found MapPanel: {_mapPanel.name}");

                // ��ʼ״̬ - ���ص�ͼ
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
            // ���Զ��ֿ��ܵ�·��
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

            // ���·������ʧ�ܣ����Եݹ����
            return FindMapPanelRecursive(parent);
        }

        private GameObject FindMapPanelRecursive(Transform parent)
        {
            if (parent == null) return null;

            // ��鵱ǰ����
            if (parent.name.Contains("Map") || parent.name.Contains("MAP") || parent.name.Contains("map"))
            {
                Plugin.LogSource.LogInfo($"Found potential map object: {parent.name}");
                return parent.gameObject;
            }

            // �ݹ����Ӷ���
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
            if (depth > 3) return; // ���Ƶݹ����

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
                // ��ȫ�ػ�ȡ��Ϸ��������
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

                // ���Tab��
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