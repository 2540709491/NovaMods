using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using BepInEx;
using BepInEx;
using Sika.Logic;
using HarmonyLib;
using System.Reflection.Emit;
using BepInEx.Configuration;
using Sika;
using Sika.UI;
using UnityEngine;
using UnityEngine.Windows;
using Input = UnityEngine.Input;

[BepInPlugin("nova.sxm.plugin.maxtc", "超级倍速", "0.0.1")]
[BepInProcess("ShooperNova.exe")]
internal class 超级倍速 : BaseUnityPlugin,IModConfig
{
    public bool DisableRank => false;

    private static readonly Harmony _harmony = new Harmony("nova.sxm.plugin.maxtc");
    public static ConfigEntry<float> fixSpeed;

    private void Start()
    {
        Harmony.CreateAndPatchAll(typeof(超级倍速));
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ModMgr), "TryNotifyModsWhenStartup")]
    public static bool TryNotifyModsWhenStartup(ModMgr __instance)
    {
        MessageBoxView.ShowInfo("超级倍速提示", $"当前加速倍速为:{fixSpeed.Value},\r\n如需更改,按F1配置加速倍速后重启游戏生效!");
        return true;
    }
    void Awake()
    {
        fixSpeed = Config.Bind("倍速设置(重启游戏生效)", "加速倍速", 5f, "当游戏触发加速时,由原来的1.5倍速改为当前设置的倍速,推荐使用半血以上加速选项,因为全程加速容易死");
        // 验证目标方法是否存在
        var targetMethod = GameWaveRunningStatePatch.GetMoveNextMethod();
        if (targetMethod == null)
        {
            Logger.LogError("未找到 <Tick>d__7.MoveNext 方法!");
            return;
        }

        // 应用补丁
        _harmony.Patch(
            original: targetMethod,
            transpiler: new HarmonyMethod(typeof(TickMoveNextTranspiler), "Transpiler")
        );
        
        Logger.LogInfo("协程补丁安装成功!");
    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Harmony.CreateAndPatchAll(typeof(超级倍速));
        }
        
    }
    //
    // [HarmonyTranspiler]
    // [HarmonyPatch(typeof(GameWaveRunningState), "Tick")]
    // static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instructions)
    // {
    //     var codes = instructions.ToList();
    //     Debug.Log("11111111111111111");
    //     Debug.Log(String.Join("\r\n", codes));
    //     // codes[66].opcode = OpCodes.Ldc_R4;
    //     // codes[66].operand = 10f;
    //     
    //     return instructions;
    // }
    public class GameWaveRunningStatePatch
    {
        // 获取目标嵌套类型 <Tick>d__7
        public static Type GetTickIteratorType()
        {
            // 获取外层类 GameWaveRunningState
            Type gameWaveStateType = Type.GetType("Sika.Logic.GameWaveRunningState, Assembly-CSharp");
        
            // 获取私有嵌套类 <Tick>d__7
            return gameWaveStateType?.GetNestedType("<Tick>d__7", BindingFlags.NonPublic);
        }

        // 获取要 Hook 的 MoveNext 方法
        public static MethodBase GetMoveNextMethod()
        {
            Type tickClass = GetTickIteratorType();
            return tickClass?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }

    [HarmonyPatch]
    public class TickMoveNextTranspiler
    {
        static MethodBase TargetMethod() => 
            GameWaveRunningStatePatch.GetMoveNextMethod();

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var codes = new List<CodeInstruction>(instructions);
        
            // 查找将 1.5f 加载到栈上的指令
            for (int i = 0; i < codes.Count; i++)
            {
                // 查找 ldc.r4 1.5 指令
                if (codes[i].opcode == OpCodes.Ldc_R4 && 
                    codes[i].operand is float value && 
                    Mathf.Approximately(value,1.5f))
                {
                    // 修改为加载 10.0f
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, fixSpeed.Value);
                    found = true;
                    Debug.Log("成功修改 1.5f 为 10.0f");
                    break; // 找到并修改后退出循环
                }
            }

            if (!found)
            {
                Debug.LogError("未找到 1.5f 赋值指令!");
            
                // 调试输出所有指令
                for (int i = 0; i < codes.Count; i++)
                {
                    Debug.Log($"{i}: {codes[i].opcode} {codes[i].operand}");
                }
            }

            return codes;
        }
    }
}