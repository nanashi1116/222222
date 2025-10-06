using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DynamicMaps.Data;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using UnityEngine;

namespace DynamicMaps.Utils
{
    // NOTE: Most of this is adapted from work done for Prop's GTFO mod (https://github.com/dvize/GTFO)
    // this likely does not count as a "substantial portion" of the software
    // under MIT license (https://github.com/dvize/GTFO/blob/master/LICENSE.txt)
    public static class QuestUtils
    {
        // 修复的反射字段 - 适配 SPT 4.0
        private static FieldInfo _playerQuestControllerField = AccessTools.Field(typeof(Player), "_questController");
        private static PropertyInfo _questControllerQuestsProperty = AccessTools.Property(typeof(AbstractQuestControllerClass), "Quests");

        // 修复：使用更安全的方式获取字段
        private static FieldInfo _questsListField = null;
        private static MethodInfo _questsGetConditionalMethod = null;
        private static Type _questType = null;
        private static MethodInfo _questIsConditionDone = null;

        // 静态构造函数，安全初始化反射字段
        static QuestUtils()
        {
            try
            {
                // 初始化基础反射字段
                if (_questControllerQuestsProperty != null)
                {
                    var questsType = _questControllerQuestsProperty.PropertyType;

                    // 安全获取 List_1 字段
                    _questsListField = AccessTools.Field(questsType, "List_1") ??
                                      AccessTools.Field(questsType, "list_0") ??
                                      AccessTools.Field(questsType, "QuestsList");

                    // 安全获取 GetConditional 方法
                    _questsGetConditionalMethod = AccessTools.Method(questsType, "GetConditional", new Type[] { typeof(string) }) ??
                                                 AccessTools.Method(questsType, "GetQuest", new Type[] { typeof(string) });

                    // 安全获取任务类型
                    _questType = questsType.BaseType?.GetGenericArguments().FirstOrDefault() ??
                                typeof(QuestDataClass); // 回退到已知类型

                    if (_questType != null)
                    {
                        _questIsConditionDone = AccessTools.Method(_questType, "IsConditionDone") ??
                                               AccessTools.Method(_questType, "get_IsConditionDone");
                    }
                }

                Plugin.Log.LogInfo("QuestUtils reflection initialized successfully for SPT 4.0");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"QuestUtils static constructor failed: {e}");
                // 即使反射失败，也要让类继续工作，只是任务标记功能会受限
            }
        }

        // TODO: move to config
        private const string _questCategory = "Quest";
        private const string _questImagePath = "Markers/quest.png";
        private static Vector2 _questPivot = new Vector2(0.5f, 0f);
        private static Color _questColor = Color.green;

        public static List<TriggerWithId> TriggersWithIds;
        public static List<LootItem> QuestItems;

        internal static void TryCaptureQuestData()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            try
            {
                if (TriggersWithIds == null)
                {
                    TriggersWithIds = GameObject.FindObjectsOfType<TriggerWithId>().ToList();
                }

                if (QuestItems == null)
                {
                    // 修复：使用更安全的方式获取战利品物品
                    var lootItems = GameObject.FindObjectsOfType<LootItem>();
                    QuestItems = lootItems.Where(i => i.Item?.QuestItem == true).ToList();

                    // 备用方法：如果上面失败，尝试使用反射
                    if (QuestItems == null || QuestItems.Count == 0)
                    {
                        var gameWorldTraverse = Traverse.Create(gameWorld);
                        var lootItemsField = gameWorldTraverse.Field("LootItems");
                        if (lootItemsField.FieldExists())
                        {
                            var lootContainer = lootItemsField.GetValue();
                            if (lootContainer != null)
                            {
                                var listField = Traverse.Create(lootContainer).Field("list_0");
                                if (listField.FieldExists())
                                {
                                    var allItems = listField.GetValue<List<LootItem>>();
                                    QuestItems = allItems?.Where(i => i.Item?.QuestItem == true).ToList();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error capturing quest data: {e}");
            }
        }

        internal static void DiscardQuestData()
        {
            if (TriggersWithIds != null)
            {
                TriggersWithIds.Clear();
                TriggersWithIds = null;
            }

            if (QuestItems != null)
            {
                QuestItems.Clear();
                QuestItems = null;
            }
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForPlayer(Player player)
        {
            if (TriggersWithIds == null || QuestItems == null)
            {
                Plugin.Log.LogWarning($"TriggersWithIds null: {TriggersWithIds == null} or QuestItems null: {QuestItems == null}");
                return Enumerable.Empty<MapMarkerDef>(); // 返回空集合而不是null
            }

            var markers = new List<MapMarkerDef>();

            try
            {
                var quests = GetIncompleteQuests(player);
                foreach (var quest in quests)
                {
                    markers.AddRange(GetMarkerDefsForQuest(player, quest));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error getting marker defs for player: {e}");
            }

            return markers;
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForQuest(Player player, QuestDataClass quest)
        {
            var markers = new List<MapMarkerDef>();

            try
            {
                var conditions = GetIncompleteQuestConditions(player, quest);
                foreach (var condition in conditions)
                {
                    var questName = GameUtils.BSGLocalized(quest.Template.NameLocaleKey);
                    var conditionDescription = GameUtils.BSGLocalized(condition.id);

                    var positions = GetPositionsForCondition(condition, questName, conditionDescription);
                    foreach (var position in positions)
                    {
                        var isDuplicate = false;

                        // check against previously created markers for duplicate position
                        foreach (var marker in markers)
                        {
                            if (MathUtils.ApproxEquals(marker.Position.x, position.x)
                             && MathUtils.ApproxEquals(marker.Position.y, position.y)
                             && MathUtils.ApproxEquals(marker.Position.z, position.z))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (isDuplicate)
                        {
                            continue;
                        }

                        markers.Add(CreateQuestMapMarkerDef(position, questName, conditionDescription));
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error getting marker defs for quest: {e}");
            }

            return markers;
        }

        private static IEnumerable<Vector3> GetPositionsForCondition(Condition condition, string questName,
                                                                    string conditionDescription)
        {
            switch (condition)
            {
                case ConditionZone zoneCondition:
                    {
                        foreach (var position in GetPositionsForZoneId(zoneCondition.zoneId, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionLaunchFlare flareCondition:
                    {
                        foreach (var position in GetPositionsForZoneId(flareCondition.zoneID, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionVisitPlace place:
                    {
                        foreach (var position in GetPositionsForZoneId(place.target, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionInZone zone:
                    {
                        foreach (var zoneId in zone.zoneIds)
                        {
                            foreach (var position in GetPositionsForZoneId(zoneId, questName, conditionDescription))
                            {
                                yield return position;
                            }
                        }
                        break;
                    }
                case ConditionFindItem findItemCondition:
                    {
                        foreach (var position in GetPositionsForQuestItems(findItemCondition.target, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                case ConditionExitName exitCondition:
                    {
                        var exfils = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints;
                        var specifiedExit = exfils.FirstOrDefault(e => e.Settings.Name == exitCondition.exitName);

                        if (specifiedExit != null)
                        {
                            yield return MathUtils.ConvertToMapPosition(specifiedExit.transform);
                        }

                        break;
                    }
                case ConditionCounterCreator conditionCreator:
                    {
                        // 安全地处理条件创建器
                        foreach (var position in GetPositionsForConditionCreator(conditionCreator, questName, conditionDescription))
                        {
                            yield return position;
                        }
                        break;
                    }
                default:
                    {
                        Plugin.Log.LogDebug($"Unhandled condition type: {condition.GetType().Name}");
                        break;
                    }
            }
        }

        private static IEnumerable<Vector3> GetPositionsForConditionCreator(ConditionCounterCreator conditionCreator,
                                                                            string questName, string conditionDescription)
        {
            // 方法1: 直接使用 Conditions 属性（推荐）
            if (conditionCreator.Conditions != null && conditionCreator.Conditions.List_0 != null)
            {
                foreach (Condition condition in conditionCreator.Conditions.List_0)
                {
                    foreach (var position in GetPositionsForCondition(condition, questName, conditionDescription))
                    {
                        yield return position;
                    }
                }
                yield break;
            }

            // 方法2: 备用方法 - 通过 TemplateConditions 字段
            if (conditionCreator.TemplateConditions?.Conditions?.List_0 != null)
            {
                foreach (Condition condition in conditionCreator.TemplateConditions.Conditions.List_0)
                {
                    foreach (var position in GetPositionsForCondition(condition, questName, conditionDescription))
                    {
                        yield return position;
                    }
                }
                yield break;
            }

            Plugin.Log.LogDebug($"ConditionCounterCreator has no conditions to process for quest: {questName}");
        }

        private static IEnumerable<Vector3> GetPositionsForZoneId(string zoneId, string questName,
                                                                  string conditionDescription)
        {
            if (TriggersWithIds == null)
            {
                yield break;
            }

            var zones = TriggersWithIds.GetZoneTriggers(zoneId);
            foreach (var zone in zones)
            {
                yield return MathUtils.ConvertToMapPosition(zone.transform.position);
            }
        }

        private static IEnumerable<Vector3> GetPositionsForQuestItems(IEnumerable<string> questItemIds, string questName,
                                                                      string conditionDescription)
        {
            if (QuestItems == null)
            {
                yield break;
            }

            foreach (var questItemId in questItemIds)
            {
                var questItems = QuestItems.Where(i => i.TemplateId == questItemId);
                foreach (var item in questItems)
                {
                    yield return MathUtils.ConvertToMapPosition(item.transform.position);
                }
            }
        }

        private static IEnumerable<Condition> GetIncompleteQuestConditions(Player player, QuestDataClass quest)
        {
            if (quest?.Template?.Conditions == null)
            {
                Plugin.Log.LogError($"GetIncompleteQuestConditions: quest.Template.Conditions is null, skipping quest");
                yield break;
            }

            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                Plugin.Log.LogError($"Quest {GameUtils.BSGLocalized(quest.Template.NameLocaleKey)} doesn't have conditions marked AvailableForFinish, skipping it");
                yield break;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    Plugin.Log.LogWarning($"Quest {GameUtils.BSGLocalized(quest.Template.NameLocaleKey)} has null condition, skipping it");
                    continue;
                }

                // filter out completed conditions
                if (IsConditionCompleted(player, quest, condition))
                {
                    continue;
                }

                yield return condition;
            }
        }

        private static IEnumerable<QuestDataClass> GetIncompleteQuests(Player player)
        {
            if (_playerQuestControllerField == null || _questControllerQuestsProperty == null || _questsListField == null)
            {
                Plugin.Log.LogError("Quest reflection fields not initialized");
                yield break;
            }

            var questController = _playerQuestControllerField.GetValue(player);
            if (questController == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player: {player.Id}, questController is null");
                yield break;
            }

            var quests = _questControllerQuestsProperty.GetValue(questController);
            if (quests == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player: {player.Id}, quests is null");
                yield break;
            }

            var questsList = _questsListField.GetValue(quests) as List<QuestDataClass>;
            if (questsList == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player: {player.Id}, questsList is null");
                yield break;
            }

            foreach (var quest in questsList)
            {
                if (quest?.Template?.Conditions == null)
                {
                    continue;
                }

                if (quest.Status != EQuestStatus.Started)
                {
                    continue;
                }

                yield return quest;
            }
        }

        private static bool IsConditionCompleted(Player player, QuestDataClass questData, Condition condition)
        {
            if (_playerQuestControllerField == null || _questControllerQuestsProperty == null ||
                _questsGetConditionalMethod == null || _questIsConditionDone == null)
            {
                Plugin.Log.LogError("Quest reflection methods not initialized");
                return false;
            }

            // CompletedConditions is inaccurate (it doesn't reset when some quests do on death)
            // and also does not contain optional objectives, need to recheck if something is in there
            if (condition.IsNecessary && !questData.CompletedConditions.Contains(condition.id))
            {
                return false;
            }

            var questController = _playerQuestControllerField.GetValue(player);
            if (questController == null)
            {
                return false;
            }

            var quests = _questControllerQuestsProperty.GetValue(questController);
            if (quests == null)
            {
                return false;
            }

            var quest = _questsGetConditionalMethod.Invoke(quests, new object[] { questData.Id });
            if (quest == null)
            {
                return false;
            }

            return (bool)_questIsConditionDone.Invoke(quest, new object[] { condition });
        }

        private static IEnumerable<TriggerWithId> GetZoneTriggers(this IEnumerable<TriggerWithId> triggerWithIds, string zoneId)
        {
            return triggerWithIds.Where(t => t.Id == zoneId);
        }

        private static MapMarkerDef CreateQuestMapMarkerDef(Vector3 position, string questName, string conditionDescription)
        {
            return new MapMarkerDef
            {
                Category = _questCategory,
                Color = _questColor,
                ImagePath = _questImagePath,
                Position = position,
                Pivot = _questPivot,
                Text = questName
            };
        }
    }
}