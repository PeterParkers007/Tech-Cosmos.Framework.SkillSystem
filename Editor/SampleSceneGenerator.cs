#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 自动生成技能系统 Demo 场景，包含玩家、敌人与场景控制器。
    /// </summary>
    public static class SampleSceneGenerator
    {
        private const string SceneFolder = "Assets/Samples/TechCosmos SkillSystem/2.1.0/Demo/Scenes";
        private const string ScenePath = SceneFolder + "/SkillSystemDemo.unity";

        /// <summary>创建并保存 SkillSystemDemo 演示场景。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Create Demo Scene", priority = 100)]
        public static void CreateDemoScene()
        {
            var demoCharacterType = FindType("TechCosmos.SkillSystem.Samples.DemoCharacter");
            var controllerType = FindType("TechCosmos.SkillSystem.Samples.DemoSceneController");

            if (demoCharacterType == null || controllerType == null)
            {
                EditorUtility.DisplayDialog("缺少 Demo 程序集",
                    "未找到 Samples Demo 类型。\n\n请确认 Samples~/Demo 已导入/编译，然后重新打开 Unity。",
                    "确定");
                return;
            }

            Directory.CreateDirectory(SceneFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(-2f, 0f, 0f);
            var player = playerGo.AddComponent(demoCharacterType);
            SetField(player, "Attack", 25f);
            SetField(player, "Health", 100f);

            var enemyGo = new GameObject("Enemy");
            enemyGo.transform.position = new Vector3(2f, 0f, 0f);
            var enemy = enemyGo.AddComponent(demoCharacterType);
            SetField(enemy, "Attack", 12f);
            SetField(enemy, "Health", 80f);

            var controllerGo = new GameObject("DemoSceneController");
            var controller = controllerGo.AddComponent(controllerType);
            SetField(controller, "player", player);
            SetField(controller, "enemy", enemy);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 5f, -8f);
                camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Demo 场景已创建",
                $"场景已保存到:\n{ScenePath}\n\nSpace: 攻击  |  H: 治疗",
                "确定");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .FirstOrDefault(t => t.FullName == fullName);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName);
            field?.SetValue(target, value);
        }
    }
}
#endif
