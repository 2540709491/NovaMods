using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Logic.UI;
using Sika.Logic;
using UnityEngine;

namespace 挂机助手;

[BepInPlugin("nova.sxm.plugin.autorun", "挂机助手", "0.0.2")]
[BepInProcess("ShooperNova.exe")]
internal class 挂机助手 : BaseUnityPlugin, IModConfig
{
    /// <summary>
    ///     注入鼠标位置
    /// </summary>
    private static Vector2 nowMousePos;

    private ConfigEntry<float> minHP;
    private ConfigEntry<bool> _isShow;

    private GameObject Playerobj;
    //GUI
    private string _runRadius;
    private string _runSpeed;
    private string _minHP;

    /// <summary>
    ///     旋转角度
    /// </summary>
    private float angle;

    private readonly KeyCode ControlKey = KeyCode.LeftControl;


    //核心参数
    private static bool isRun;
    private ConfigEntry<KeyCode> runKey;

    /// <summary>
    ///     旋转半径
    /// </summary>
    private ConfigEntry<float> runRadius;

    /// <summary>
    ///     旋转速度
    /// </summary>
    /// <returns></returns>
    private ConfigEntry<float> runSpeed;

    private ConfigEntry<KeyCode> showKey;
    private Rect windowsRect = new(Screen.width * 0.2f, Screen.height * 0.4f, 320, 260);

    private void Start()
    {

        runSpeed = Config.Bind("旋转速度", "旋转速度", 0.1f);
        runRadius = Config.Bind("旋转半径", "旋转半径", 5f);
        minHP = Config.Bind("最小警报生命值(0则不警报)在0~1.0范围内,警报自动暂停游戏.", "最小警报生命值(百分比)", 0.1f);
        //热键默认加上ControlKey
        runKey = Config.Bind("运行热键", "运行热键(Ctrl+)", KeyCode.B);
        showKey = Config.Bind("界面开关热键", "界面开关热键(Ctrl+)", KeyCode.A);

        _isShow = Config.Bind("可视状态", "可视状态", true);
        Harmony.CreateAndPatchAll(typeof(挂机助手));
    }

    private void Update()
    {

        if (Input.GetKey(ControlKey) && Input.GetKeyDown(showKey.Value)) _isShow.Value = !_isShow.Value;
        if (Input.GetKey(ControlKey) && Input.GetKeyDown(runKey.Value)) isRun = !isRun;
        // 根据角度和半径计算新的鼠标位置
        if (isRun)
        {
            angle += runSpeed.Value * Time.deltaTime;
            nowMousePos = new Vector2(Mathf.Cos(angle) * runRadius.Value, Mathf.Sin(angle) * runRadius.Value);
        }

        if (initPalyer())
        {
            var HP =Playerobj.GetComponent<Actor>().AttrList.GetAttr(ActorAttr.HP);
            var MaxHP=Playerobj.GetComponent<Actor>().AttrList.GetAttr(ActorAttr.MaxHP);

            if (HP.BaseValue<=MaxHP.BaseValue*minHP.Value)
            {
                UIMgr.OpenUI<PauseView>(false, null);
            }
        }

    }

    private void OnGUI()
    {
        if (!_isShow.Value) return;
        windowsRect = GUI.Window(25111226, windowsRect, DrawWindow, $"挂机助手({ControlKey} + {showKey.Value} 开关此界面)");
    }

    public bool DisableRank => false;

    private void DrawWindow(int id)
    {
        GUILayout.BeginArea(new Rect(5, 20, 310, 260));
        GUILayout.Label($"当前运行状态:{(isRun ? "正在运行" : "未运行")}");
        GUILayout.Label($"当前运行热键(请到Mod配置界面修改):\n{ControlKey} + {runKey.Value}");
        GUILayout.Label($"当前界面开关热键(请到Mod配置界面修改):\n{ControlKey} + {showKey.Value}");
        try
        {
            var mousePos = CameraMgr.GetWorldPositionOfMouse();

            GUILayout.Label($"当前鼠标位置:x-{mousePos.x}  y-{mousePos.y}");
        }
        catch (Exception e)
        {
            GUILayout.Label("当前鼠标位置:CameraMgr未加载");
        }


        GUILayout.BeginHorizontal();
        GUILayout.Label("运行速度");
        _runSpeed = GUILayout.TextField(_runSpeed);
        if (_runSpeed == null) _runSpeed = runSpeed.Value.ToString();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("运行半径");
        _runRadius = GUILayout.TextField(_runRadius);
        if (_runRadius == null) _runRadius = runRadius.Value.ToString();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("最小警报生命值(百分比)");
        _minHP = GUILayout.TextField(_minHP);
        if (_minHP != null)
        {

            if (_minHP!="")
            {
                float NHP=0f;
                float.TryParse(_minHP, out NHP);
                if (NHP <= 100 && NHP >= 0)
                {
                    minHP.Value = NHP * 0.01f;
                }
            }

        }


        GUILayout.Label("%");
        GUILayout.EndHorizontal();
        string startbutton;

        if (GUILayout.Button("开始/暂停 自动挂机")) isRun = !isRun;
        float tempRS;
        float tempRR;
        float.TryParse(_runSpeed,out tempRS);
        float.TryParse(_runRadius,out tempRS);
        runSpeed.Value = tempRS;
        runRadius.Value = tempRS;


        GUILayout.EndArea();

        GUI.DragWindow();
    }
    bool initPalyer()
    {
        if (Playerobj == null)
        {
            if (UnityEngine.Object.FindObjectsByType<PlayerControlModule>(FindObjectsSortMode.None).Length != 0)
            {
                Logger.LogInfo($"找到了{UnityEngine.Object.FindObjectsByType<PlayerControlModule>(FindObjectsSortMode.None).Length}个PlayerControlModule");

                Playerobj = UnityEngine.Object.FindObjectsByType<PlayerControlModule>(FindObjectsSortMode.None)[0].gameObject;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }

    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CameraMgr), "GetWorldPositionOfMouse")]
    public static bool GetWorldPositionOfMouse(CameraMgr __instance,ref Vector2 __result)
    {
        if (isRun)
        {
            __result = nowMousePos;
        }
        else
        {
            __result=CameraMgr.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        return false;
    }
}