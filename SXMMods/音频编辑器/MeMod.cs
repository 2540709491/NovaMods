using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Sika.Logic;
using HarmonyLib;
using Sika.DebugTool;
using System.Reflection;
using static Sika.DebugTool.VisualDebugMgr;
using UnityEngine.UI;

public static class TMDebug
{
    public static void DebugLog(string msg)
    {
        Debug.Log("[MOD:MeMOD] " + msg);
    }
}
namespace NovaMEMOD
{
    [BepInDependency("com.bepis.bepinex.configurationmanager")]
    [BepInPlugin("nova.zptdf.plugin.testmod1", "MeMod", "1.0.2")]
    [BepInProcess("ShooperNova.exe")]
    //[BepInDependency("com.bepinex.plugin.somedependency", BepInDependency.DependencyFlags.SoftDependency)]
    // 未来可能的依赖(软)
    //[BepInDependency("com.bepinex.plugin.importantdependency", BepInDependency.DependencyFlags.HardDependency)]
    // 未来可能的依赖(硬)

    public class MeMod : BaseUnityPlugin
    {
        static ConfigEntry<KeyCode> AddGoldKeyConfig, SubGoldKeyConfig;
        static ConfigEntry<int> AddMoneyConfig, SubMoneyConfig;
        static ConfigEntry<bool> isDebugConfig;

        static private GameObject Playerobj = null;
        static private GameObject BattleSystem = null;
        private readonly string OutputPath = @"D:\test.txt";


        void Start()
        {
            AddGoldKeyConfig = Config.Bind("config", "加钱热键", KeyCode.UpArrow, "加钱热键");
            SubGoldKeyConfig = Config.Bind("config", "减钱热键", KeyCode.DownArrow, "减钱热键");
            AddMoneyConfig = Config.Bind("config", "加钱数额", 1000, "每次增加的钱");
            SubMoneyConfig = Config.Bind("config", "减钱数额", 1000, "每次减少的钱");
            isDebugConfig = Config.Bind("config", "是否开启调试面板", true, "是否开启调试模式,重启生效");
            Harmony.CreateAndPatchAll(typeof(MeMod));
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                Debug.Log("画质类型有这些：" + QualitySettings.names[i]);
            }
            //QualitySettings.globalTextureMipmapLimit = 2;
            //Application.targetFrameRate = 60;
            //QualitySettings.pixelLightCount = 0;
            //QualitySettings.antiAliasing = 0;
            //QualitySettings.shadows = ShadowQuality.Disable;
            //QualitySettings.vSyncCount = 0;
            //QualitySettings.lodBias = 0.01f;
        }

        void Update()
        {

            if (Input.GetKeyDown(AddGoldKeyConfig.Value))
            {
                addGold();
            }
            if (Input.GetKeyDown(SubGoldKeyConfig.Value))
            {
                subGold();
            }

        }
        static bool initBattleSystem()
        {
            if (BattleSystem == null)
            {
                if (UnityEngine.Object.FindObjectsByType<BattleSystem>(FindObjectsSortMode.None).Length != 0)
                {
                    Debug.Log($"[MOD]找到了{UnityEngine.Object.FindObjectsByType<BattleSystem>(FindObjectsSortMode.None).Length}个BattleSystem");
                    BattleSystem = UnityEngine.Object.FindObjectsByType<BattleSystem>(FindObjectsSortMode.None)[0].gameObject;
                    return true;
                }
                else
                {
                    Debug.Log("[MOD]未找到BattleSystem");
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        bool initPalyer()
        {
            if (Playerobj == null)
            {
                if (UnityEngine.Object.FindObjectsByType<PlayerSystem>(FindObjectsSortMode.None).Length != 0)
                {
                    Logger.LogInfo($"找到了{UnityEngine.Object.FindObjectsByType<PlayerSystem>(FindObjectsSortMode.None).Length}个PlayerSystem");

                    Playerobj = UnityEngine.Object.FindObjectsByType<PlayerSystem>(FindObjectsSortMode.None)[0].gameObject;
                    return true;
                }
                else
                {
                    Logger.LogError("未找到PlayerSystem");
                    return false;
                }
            }
            else
            {
                return true;
            }

        }
        void addGold()
        {


            if (!initPalyer()) { return; }
            Playerobj.GetComponent<PlayerSystem>().Inventory.AddMoney(AddMoneyConfig.Value);


        }
        void subGold()
        {
            if (!initPalyer()) { return; }
            Playerobj.GetComponent<PlayerSystem>().Inventory.CostMoney(SubMoneyConfig.Value);
        }
        private void OnGUI()
        {
            Rect windowRect = new Rect(0, UnityEngine.Screen.height - 40, 300, 50);
            windowRect = GUI.Window(20210218, windowRect, DoMyWindow, "注意事项[此窗口不可关闭]");


        }
        public void DoMyWindow(int winId)
        {
            GUIStyle syl = new GUIStyle();
            syl.normal.textColor = Color.red;
            syl.fontSize = 14;
            GUILayout.BeginArea(new Rect(10, 20, 290, 20));
            {
                GUILayout.Label("注意,目前游戏内容为Mod修改后内容", syl);

            }
            GUILayout.EndArea();
        }

        //Debug功能
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Sika.DebugTool.VisualDebugMgr), "InitMgr")]
        public static bool InitMgrPrefix(VisualDebugMgr __instance)
        {
            TMDebug.DebugLog("[MOD:TEST1] DEBUG已开启");
            var _debugLevel = Traverse.Create(__instance).Field("_debugLevel");
            var _debugActions = Traverse.Create(__instance).Field("_debugActions");
            _debugLevel.SetValue(VisualDebugMgr.VisualDebugLevel.None);
            if (isDebugConfig.Value)
            {
                _debugLevel.SetValue(VisualDebugMgr.VisualDebugLevel.Full);
            }
            else
            {
                _debugLevel.SetValue(VisualDebugMgr.VisualDebugLevel.Restrict);
            }
            MethodInfo[] methods = typeof(VisualDebugMgr).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                DebugAttribute customAttribute = method.GetCustomAttribute<DebugAttribute>();
                if (customAttribute != null && (!customAttribute.IsRestrict || _debugLevel.GetValue<VisualDebugLevel>() != VisualDebugMgr.VisualDebugLevel.Restrict))
                {
                    DebugAction debugAction = new DebugAction(customAttribute.LabelName, delegate
                    {
                        method.Invoke(__instance, null);
                    });
                    _debugActions.GetValue<List<DebugAction>>().Add(debugAction);

                }
            }
            return false;
        }
        
        //反作弊
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Sika.Logic.AntiCheatMgr), "NotifyCheat")]
        public static bool AntiCheatMgr(bool isOpsDetected, bool isGUIDetected)
        {

            return false;
        }
        
        //自定义
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InGameMusicAudioPlayer),"GetRandomMusicClip")]
        


    }
}