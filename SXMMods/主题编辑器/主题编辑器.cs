using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Sika.Logic;
using Sika.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace 主题编辑器
{
    internal class EnvPlanetColor
    {
        public Color _envColor;
        public Color _lightColor;

        public EnvPlanetColor(Color lightColor, Color envColor)
        {
            _lightColor = lightColor;
            _envColor = envColor;
        }
    }

    [BepInDependency("com.bepis.bepinex.configurationmanager")]
    [BepInPlugin("nova.zptdf.plugin.themeeditor", "主题编辑器", "0.0.7")]
    [BepInProcess("ShooperNova.exe")]
    internal class 主题编辑器 : BaseUnityPlugin, IModConfig
    {
        //Env
        //背景颜色
        private static ConfigEntry<Color> Environment_BG_ColorConfig;

        //UI
        //游戏左侧信息
        //第一个(每秒伤害)
        private static ConfigEntry<Color> UI_GameInfoLeft_1;

        //第二个(子弹数量)
        private static ConfigEntry<Color> UI_GameInfoLeft_2;

        //第三个(敌人数量)
        private static ConfigEntry<Color> UI_GameInfoLeft_3;

        //第四个(累计击杀)
        private static ConfigEntry<Color> UI_GameInfoLeft_4;

        //光标颜色
        private static ConfigEntry<Color> UI_CursorColorConfig;

        //游戏右侧信息
        //血量
        //背景颜色
        private static ConfigEntry<Color> UI_Health_BG_ColorConfig;

        //左侧线条颜色
        private static ConfigEntry<Color> UI_Health_LeftLine_ColorConfig;

        //血条边框颜色
        private static ConfigEntry<Color> UI_Health_Border_ColorConfig;

        //血条填充颜色
        private static ConfigEntry<Color> UI_Health_Fill_ColorConfig;

        //血条掉血填充颜色
        private static ConfigEntry<Color> UI_Health_DeFill_ColorConfig;

        //血条文本颜色
        private static ConfigEntry<Color> UI_Health_Text_ColorConfig;

        //分割线颜色
        private static ConfigEntry<Color> UI_Health_SplitLine_ColorConfig;

        //提示文本颜色
        private static ConfigEntry<Color> UI_Health_TipText_ColorConfig;

        //金矿
        //背景颜色
        private static ConfigEntry<Color> UI_Gold_BG_ColorConfig;

        //左侧线条颜色
        private static ConfigEntry<Color> UI_Gold_LeftLine_ColorConfig;

        //金矿文本颜色
        private static ConfigEntry<Color> UI_Gold_Text_ColorConfig;

        //分割线颜色
        private static ConfigEntry<Color> UI_Gold_SplitLine_ColorConfig;

        //提示文本颜色
        private static ConfigEntry<Color> UI_Gold_TipText_ColorConfig;

        //Map
        //游戏边沿颜色
        private static ConfigEntry<Color> Map_EdgeColorConfig;


        //GmaeWorld
        //弹匣子弹颜色
        private static ConfigEntry<Color> GameWorld_BulletColorConfig;

        //主角底光颜色
        private static ConfigEntry<Color> GameWorld_PlayerBaseLightColorConfig;

        //主角背景颜色
        private static ConfigEntry<Color> GameWorld_PlayerBGColorConfig;

        //主角边框颜色
        private static ConfigEntry<Color> GameWorld_PlayerBorderColorConfig;

        //主角表情颜色
        private static ConfigEntry<Color> GameWorld_PlayerEmojiColorConfig;

        private static GameObject EnvironmentObj;
        private static GameObject UIObj;
        private static GameObject GameWorldObj;

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(主题编辑器));
            Environment_BG_ColorConfig = Config.Bind("环境(Environment)", "环境背景颜色", new Color(1f, 1f, 1f, 1f));
            Environment_BG_ColorConfig.SettingChanged += ChangeSetting;
            //UI
            //游戏左侧信息
            {
                UI_GameInfoLeft_1 = Config.Bind("界面(UI)", "游戏左侧信息1", new Color(1f, 1f, 1f, .15f));
                UI_GameInfoLeft_1.SettingChanged += ChangeSetting;
                UI_GameInfoLeft_2 = Config.Bind("界面(UI)", "游戏左侧信息2", new Color(1f, 1f, 1f, .15f));
                UI_GameInfoLeft_2.SettingChanged += ChangeSetting;
                UI_GameInfoLeft_3 = Config.Bind("界面(UI)", "游戏左侧信息3", new Color(1f, 1f, 1f, .15f));
                UI_GameInfoLeft_3.SettingChanged += ChangeSetting;
                UI_GameInfoLeft_4 = Config.Bind("界面(UI)", "游戏左侧信息4", new Color(1f, 1f, 1f, .15f));
                UI_GameInfoLeft_4.SettingChanged += ChangeSetting;

                UI_CursorColorConfig = Config.Bind("界面(UI)", "光标颜色", new Color(1f, 1f, 1f, 1f));
                UI_CursorColorConfig.SettingChanged += ChangeSetting;
            }
            //游戏右侧信息
            //血条
            {
                UI_Health_BG_ColorConfig = Config.Bind("界面(UI)", "血量背景颜色", new Color(.69f, .53f, .93f, .25f));
                UI_Health_BG_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_LeftLine_ColorConfig = Config.Bind("界面(UI)", "血量左侧线条颜色", new Color(.86f, .80f, 1f, .75f));
                UI_Health_LeftLine_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_Border_ColorConfig = Config.Bind("界面(UI)", "血量边框颜色", new Color(.79f, .74f, 1f, .5f));
                UI_Health_Border_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_Fill_ColorConfig = Config.Bind("界面(UI)", "血量填充颜色", new Color(.86f, .44f, .34f, 1f));
                UI_Health_Fill_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_DeFill_ColorConfig = Config.Bind("界面(UI)", "血量掉血填充颜色", new Color(.28f, .16f, .84f, .75f));
                UI_Health_DeFill_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_Text_ColorConfig = Config.Bind("界面(UI)", "血量文本颜色", new Color(1f, .95f, .98f, 1f));
                UI_Health_Text_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_SplitLine_ColorConfig = Config.Bind("界面(UI)", "血量分割线颜色", new Color(.91f, .86f, 1f, .5f));
                UI_Health_SplitLine_ColorConfig.SettingChanged += ChangeSetting;
                UI_Health_TipText_ColorConfig = Config.Bind("界面(UI)", "血量提示文本颜色", new Color(1f, 1f, 1f, .75f));
                UI_Health_TipText_ColorConfig.SettingChanged += ChangeSetting;
            }
            //金矿
            {
                UI_Gold_BG_ColorConfig = Config.Bind("界面(UI)", "金矿背景颜色", new Color(.69f, .53f, .93f, .25f));
                UI_Gold_BG_ColorConfig.SettingChanged += ChangeSetting;
                UI_Gold_LeftLine_ColorConfig = Config.Bind("界面(UI)", "金矿左侧线条颜色", new Color(.86f, .80f, 1f, .75f));
                UI_Gold_LeftLine_ColorConfig.SettingChanged += ChangeSetting;
                UI_Gold_Text_ColorConfig = Config.Bind("界面(UI)", "金矿文本颜色", new Color(1f, .95f, .98f, 1f));
                UI_Gold_Text_ColorConfig.SettingChanged += ChangeSetting;
                UI_Gold_SplitLine_ColorConfig = Config.Bind("界面(UI)", "金矿分割线颜色", new Color(.91f, .86f, 1f, .5f));
                UI_Gold_SplitLine_ColorConfig.SettingChanged += ChangeSetting;
                UI_Gold_TipText_ColorConfig = Config.Bind("界面(UI)", "金矿提示文本颜色", new Color(1f, 1f, 1f, .75f));
                UI_Gold_TipText_ColorConfig.SettingChanged += ChangeSetting;
            }

            //游戏地图(Map)
            {
                Map_EdgeColorConfig = Config.Bind("地图(Map)", "游戏地图边沿颜色", new Color(.52f, .90f, 1f, 1f));
                Map_EdgeColorConfig.SettingChanged += ChangeSetting;
            }

            //GameWorld
            {
                GameWorld_BulletColorConfig = Config.Bind("游戏世界(GameWorld)", "弹匣子弹颜色", new Color(.86f, 1f, 1f, 1f));
                GameWorld_BulletColorConfig.SettingChanged += ChangeSetting;

                GameWorld_PlayerBaseLightColorConfig =
                    Config.Bind("游戏世界(GameWorld)", "玩家底光颜色", new Color(1f, 1f, 1f, 1f));
                GameWorld_PlayerBaseLightColorConfig.SettingChanged += ChangeSetting;
                GameWorld_PlayerBGColorConfig = Config.Bind("游戏世界(GameWorld)", "玩家背景颜色", new Color(1f, 1f, 1f, 1f));
                GameWorld_PlayerBGColorConfig.SettingChanged += ChangeSetting;
                GameWorld_PlayerBorderColorConfig =
                    Config.Bind("游戏世界(GameWorld)", "玩家边框颜色", new Color(.77f, .94f, 1f, 1f));
                GameWorld_PlayerBorderColorConfig.SettingChanged += ChangeSetting;
                GameWorld_PlayerEmojiColorConfig =
                    Config.Bind("游戏世界(GameWorld)", "玩家表情颜色", new Color(.75f, .97f, 1f, 1f));
                GameWorld_PlayerEmojiColorConfig.SettingChanged += ChangeSetting;
            }
        }

        public bool DisableRank => false;


        private void ChangeSetting(object sender, EventArgs e)
        {
            Env_changeBG();

            Map_changeMap();

            UI_changeCursorColor();
            UI_changeGameInfoLeft();

            GameWorld_changerBulletBarColor();
            UI_changeGameInfoRight();

            GameWorld_changerPlayerColor();
        }

        //EnvironmentMgr
        //修改背景
        private static void Env_changeBG()
        {
            EnvironmentObj.transform.Find("BG").transform.Find("BG").gameObject.GetComponent<SpriteRenderer>().color =
                Environment_BG_ColorConfig.Value;
        }

        //UIMgr
        //修改游戏界面左侧信息
        private static void UI_changeGameInfoLeft()
        {
            Transform MiscDescGroup = null;

            MiscDescGroup =
                UIObj.transform.Find("UIRoot/Bottom_Root/#GameView(Clone)/Circle Layout Left/MiscDescGroup");
            if (MiscDescGroup == null) return;

            MiscDescGroup.GetChild(0).Find("Bar").gameObject.GetComponent<UISquareSdfImage>().color =
                UI_GameInfoLeft_1.Value;
            MiscDescGroup.GetChild(1).Find("Bar").gameObject.GetComponent<UISquareSdfImage>().color =
                UI_GameInfoLeft_2.Value;
            MiscDescGroup.GetChild(2).Find("Bar").gameObject.GetComponent<UISquareSdfImage>().color =
                UI_GameInfoLeft_3.Value;
            MiscDescGroup.GetChild(3).Find("Bar").gameObject.GetComponent<UISquareSdfImage>().color =
                UI_GameInfoLeft_4.Value;
        }

        //游戏界面右侧信息
        private static void UI_changeGameInfoRight()
        {
            Transform UI_HPObj = null;
            UI_HPObj = UIObj.transform.Find(
                "UIRoot/Bottom_Root/#GameView(Clone)/Circle Layout Right/GameViewCircleItem_HP");
            Transform UI_GoldObj = null;
            UI_GoldObj =
                UIObj.transform.Find("UIRoot/Bottom_Root/#GameView(Clone)/Circle Layout Right/GameViewCircleItem_Gold");
            if (UI_HPObj == null || UI_GoldObj == null) return;
            //血条
            {
                UI_GoldObj =
                    UIObj.transform.Find(
                        "UIRoot/Bottom_Root/#GameView(Clone)/Circle Layout Right/GameViewCircleItem_Gold");
                UI_HPObj.Find("BG").gameObject.GetComponent<Image>().color = UI_Health_BG_ColorConfig.Value;
                ;
                UI_HPObj.Find("EdgeLine").gameObject.GetComponent<UISquareSdfImage>().color =
                    UI_Health_LeftLine_ColorConfig.Value;
                UI_HPObj.Find("Content/TopContent/PlayerHPWidget/Edge").gameObject.GetComponent<UISquareSdfImage>()
                    .color = UI_Health_Border_ColorConfig.Value;
                UI_HPObj.Find("Content/TopContent/PlayerHPWidget/FillRoot/Fill").gameObject
                    .GetComponent<UISquareSdfImage>().color = UI_Health_Fill_ColorConfig.Value;
                UI_HPObj.Find("Content/TopContent/PlayerHPWidget/FillRoot/FillDelay").gameObject
                    .GetComponent<UISquareSdfImage>().color = UI_Health_DeFill_ColorConfig.Value;
                UI_HPObj.Find("Content/TopContent/PlayerHPWidget/HPText (TMP)").gameObject
                    .GetComponent<TextMeshProUGUI>().color = UI_Health_Text_ColorConfig.Value;
                UI_HPObj.Find("Content/Line").gameObject.GetComponent<UISquareSdfImage>().color =
                    UI_Health_SplitLine_ColorConfig.Value;
                UI_HPObj.Find("Content/DescText").gameObject.GetComponent<TextMeshProUGUI>().color =
                    UI_Health_TipText_ColorConfig.Value;
            }
            //金矿
            {
                UI_GoldObj.Find("BG").gameObject.GetComponent<Image>().color = UI_Gold_BG_ColorConfig.Value;
                UI_GoldObj.Find("EdgeLine").gameObject.GetComponent<UISquareSdfImage>().color =
                    UI_Gold_LeftLine_ColorConfig.Value;
                UI_GoldObj.Find("Content/TopContent/ValueText").gameObject.GetComponent<TextMeshProUGUI>().color =
                    UI_Gold_Text_ColorConfig.Value;
                UI_GoldObj.Find("Content/Line").gameObject.GetComponent<UISquareSdfImage>().color =
                    UI_Gold_SplitLine_ColorConfig.Value;
                UI_GoldObj.Find("Content/DescText").gameObject.GetComponent<TextMeshProUGUI>().color =
                    UI_Gold_TipText_ColorConfig.Value;
            }
        }

        //修改光标颜色
        private static void UI_changeCursorColor()
        {
            UIObj.transform.Find("UIRoot/Bottom_Root/#GameView(Clone)/GameCursorWidget/Pivot/Cursor").gameObject
                .GetComponent<Image>().color = UI_CursorColorConfig.Value;
        }

        //Map
        private static void Map_changeMap()
        {
            GameObject.Find("#Map(Clone)").transform.Find("Renderer").gameObject.GetComponent<MeshRenderer>().material
                .SetColor("_TintColor", Map_EdgeColorConfig.Value);
        }

        //GameWorld
        private static void GameWorld_changerBulletBarColor()
        {
            if (GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody/BulletCountRoot/BulletCount Circle") ==
                null) return;

            GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody/BulletCountRoot/BulletCount Circle")
                .gameObject.GetComponent<SpriteRenderer>().color = GameWorld_BulletColorConfig.Value;
            ;
        }

        //修改玩家颜色
        private static void GameWorld_changerPlayerColor()
        {
            if (GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody") == null) return;
            GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody/ScaleRoot/BGLight/CircleLight").gameObject
                .GetComponent<SpriteRenderer>().color = GameWorld_PlayerBaseLightColorConfig.Value;
            GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody/FaceRoot/FaceCyberBG").gameObject
                .GetComponent<SpriteRenderer>().color = GameWorld_PlayerBGColorConfig.Value;
            GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody/FaceRoot/FaceEdge").gameObject
                .GetComponent<SpriteRenderer>().color = GameWorld_PlayerBorderColorConfig.Value;
            GameWorldObj.transform.Find("DefaultPlayerActor(Clone)/ArtBody/FaceRoot/FaceEmoji/EmojiRenderer").gameObject
                .GetComponent<SpriteRenderer>().color = GameWorld_PlayerEmojiColorConfig.Value;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnvironmentMgr), "InitMgr")]
        public static void EnviromentMgrInitPostFix(EnvironmentMgr __instance)
        {
            //修改主题
            //修改背景
            EnvironmentObj = GameObject.Find("[15] EnvironmentMgr");
            UIObj = GameObject.Find("[12] UIMgr");
            Env_changeBG();

            var assembly = Assembly.GetExecutingAssembly();
            var steam = assembly.GetManifestResourceStream("主题编辑器.Assets.moder.icon");
            //水印测试
            // var ab = AssetBundle.LoadFromStream(steam);
            //
            // if (ab != null)
            // {
            //
            //     var go = ab.LoadAsset<GameObject>("MODERICON");
            //     if (go != null) { 
            //         var n = GameObject.Instantiate(go);
            //         n.transform.SetParent(UIObj.transform);
            //     }
            // }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProgressSystem), "BeginWave")]
        public static void UIMgrInitPostFix(ProgressSystem __instance)
        {
            //修改UI
            //修改游戏界面左侧信息

            GameWorldObj = GameObject.Find("GameWorld");
            UI_changeGameInfoLeft();
            UI_changeCursorColor();
            Map_changeMap();
            GameWorld_changerBulletBarColor();
            GameWorld_changerPlayerColor();
            UI_changeGameInfoRight();
        }
    }
}