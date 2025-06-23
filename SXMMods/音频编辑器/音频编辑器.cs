using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Sika.Logic;
using Sika.UI;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace 音频编辑器
{
    [BepInDependency("com.bepis.bepinex.configurationmanager")]
    [BepInPlugin("nova.sxm.plugin.musiceditor", "音频编辑器", "0.0.1")]
    [BepInProcess("ShooperNova.exe")]
    internal class 音频编辑器 : BaseUnityPlugin, IModConfig
    {
        public static string filePath = "D:\\只因你太美.mp3";

        // 音乐配置
        private static ConfigEntry<string> MusicPathsText;
        private static MusicConfig MusicPaths;

        public static List<AudioClip> mainUIMusics;

        public static List<AudioClip> gameMusics;
        // 配置项

        private static ConfigEntry<KeyCode> toggleKeyConfig;
        private readonly string[] musicTabs = { "主界面音乐", "游戏音乐" };
        private bool _isEditorOpen;
        private bool isNOOBGM = true;
        private Vector2 musicScrollPos;
        private string OMusicText = "禁用游戏原始音乐";
        private int selectedMusicTab;

        // 窗口状态
        private Rect windowRect;

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(音频编辑器));

            // 初始化列表
            mainUIMusics = new List<AudioClip>();
            gameMusics = new List<AudioClip>();
            MusicPathsText = Config.Bind("音乐设置", "MusicPathsText", "");
            toggleKeyConfig = Config.Bind("Hotkeys",
                "开关音乐编辑器",
                KeyCode.F9,
                "开关音乐编辑器");


            var base64String = MusicPathsText.Value;
            var data = Convert.FromBase64String(base64String);
            MusicPaths = JsonUtility.FromJson<MusicConfig>(
                Encoding.UTF8.GetString(data)) ?? new MusicConfig();
            // 加载主界面音乐
            LoadMusicList(MusicPaths.MainMenuPaths, mainUIMusics);

            // 加载游戏音乐
            LoadMusicList(MusicPaths.GamePaths, gameMusics);

            // 配置初始化
            windowRect = new Rect(Screen.width * 0.1f, Screen.height - 350, 400, 300);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKeyConfig.Value))
            {
                _isEditorOpen = !_isEditorOpen;
                //setGAudioMgr();
            }

            
        }

        private void OnGUI()
        {
            if (!_isEditorOpen) return;

            windowRect = GUI.Window(2025622, windowRect, DrawMusicInfo, "音频编辑器");
        }

        public bool DisableRank => false;

        private void DrawMusicInfo(int winId)
        {
            GUI.skin.label.fontSize = 12;
            GUI.skin.textArea.fontSize = 12;
            GUI.skin.button.fontSize = 12;


            GUILayout.BeginArea(new Rect(0, 20, 400, 300));
            {
                selectedMusicTab = GUILayout.Toolbar(selectedMusicTab, musicTabs);

                musicScrollPos = GUILayout.BeginScrollView(musicScrollPos, GUILayout.Height(250));
                {
                    // 修复：添加空值保护
                    var mainPaths = MusicPaths.MainMenuPaths;
                    var gamePaths = MusicPaths.GamePaths ?? new List<string>();

                    var currentConfig = selectedMusicTab == 0 ? mainPaths : gamePaths;

                    foreach (var path in currentConfig.ToList()) // 创建副本遍历
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(path, GUILayout.ExpandWidth(true));

                            if (GUILayout.Button("×", GUILayout.Width(20)))
                            {
                                // 修复：直接操作原始列表
                                if (selectedMusicTab == 0)
                                    mainPaths.Remove(path);
                                else
                                    gamePaths.Remove(path);
                                MusicPaths.MainMenuPaths = mainPaths;
                                MusicPaths.GamePaths = gamePaths;
                                ReloadMusicList();
                                break;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }


                    if (GUILayout.Button("添加音乐"))
                        try
                        {
                            var paths = OpenFileDialog.Show(
                                "选择MP3文件",
                                "",
                                "MP3 Files (*.mp3)\0*.mp3", // 简化过滤器字符串
                                OpenFileDialog.Flags.ALLOWMULTISELECT
                            );

                            if (paths != null && paths.Length > 0)
                            {
                                // 根据当前标签页添加路径
                                var targetList = selectedMusicTab == 0
                                    ? MusicPaths.MainMenuPaths
                                    : MusicPaths.GamePaths;

                                targetList.AddRange(paths);
                                ReloadMusicList();
                                SaveConfig();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"文件选择失败: {ex}");
                        }

                    if (GUILayout.Button(OMusicText))
                    {
                        if (OMusicText == "禁用游戏原始音乐")
                        {
                            OMusicText = "启用游戏原始音乐";
                            isNOOBGM = true;
                        }

                        else
                        {
                            OMusicText = "禁用游戏原始音乐";
                            isNOOBGM = false;
                        }
                    }

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("删除最后一个"))
                        {
                            // 修复3：直接操作原始列表
                            var targetList = selectedMusicTab == 0 ? mainPaths : gamePaths;
                            if (targetList.Count > 0)
                            {
                                targetList.RemoveAt(targetList.Count - 1);

                                ReloadMusicList();
                                SaveConfig();
                            }
                        }

                        if (GUILayout.Button("清空"))
                        {
                            var targetList = selectedMusicTab == 0 ? mainPaths : gamePaths;
                            targetList.Clear();
                            ReloadMusicList();
                            SaveConfig();
                        }
                    }

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
            GUI.DragWindow();
        }

        private void setGAudioMgr()
        {
            var _AudioMgr = GameObject.Find("[13] AudioMgr");

            GameObject AGMP=Traverse.Create(_AudioMgr).Field("GlobalMusicPlayer").GetValue<GameObject>();
            List<InGameMusicGroup> AGAL = Traverse.Create(AGMP.GetComponent<InGameMusicAudioPlayer>())
                .GetValue<List<InGameMusicGroup>>("_groups");
            
            Debug.Log(JsonUtility.ToJson(AGAL));
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ModMgr), "TryNotifyModsWhenStartup")]
        public static bool TryNotifyModsWhenStartup(ModMgr __instance)
        {
            MessageBoxView.ShowInfo("音乐编辑器提示", "按F1配置MOD,默认按F9打开音乐编辑器界面");
            return true;
        }

        private void LoadMusicList(List<string> paths, List<AudioClip> targetList)
        {
            foreach (var path in paths)
                using (var www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
                {
                    www.SendWebRequest();

                    // 同步等待加载完成
                    while (!www.isDone)
                    {
                    }

                    if (www.result == UnityWebRequest.Result.Success)
                        targetList.Add(DownloadHandlerAudioClip.GetContent(www));
                    else
                        Debug.LogError($"加载音乐失败: {path}\n错误信息: {www.error}");
                }
        }


        // 修改后的ReloadMusicList方法
        private void ReloadMusicList()
        {
            // 添加空值保护
            if (MusicPaths == null) return;

            if (selectedMusicTab == 0)
            {
                mainUIMusics.Clear();
                LoadMusicList(MusicPaths.MainMenuPaths ?? new List<string>(), mainUIMusics);
            }
            else
            {
                gameMusics.Clear();
                LoadMusicList(MusicPaths.GamePaths ?? new List<string>(), gameMusics);
            }
        }

        private void SaveConfig()
        {
            // 原始JSON
            var json = JsonUtility.ToJson(MusicPaths);
            // Base64编码
            var bytes = Encoding.UTF8.GetBytes(json);
            MusicPathsText.Value = Convert.ToBase64String(bytes);
            Config.Save();
        }


        private string FilterMP3Paths(string input)
        {
            return string.Join("\n", input.Split('\n')
                .Where(path =>
                    path.Trim().EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                .Distinct());
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(AudioMgr), "PlayGlobalMusic")]
        public static void PlayGlobalMusic(AudioMgr __instance, IGlobalMusicPlayer globalMusicPlayer,
            AudioClip musicAudioClip)
        {
            if (musicAudioClip.name == "主界面 星漩Nova Vortex")
            {
                if (mainUIMusics.Count > 0)
                {
                    // 随机选择音乐
                    var randomIndex = Random.Range(0, mainUIMusics.Count);
                    var randomClip = mainUIMusics[randomIndex];

                    // 获取文件名（不带后缀）
                    var fileName = Path.GetFileNameWithoutExtension(MusicPaths.MainMenuPaths[randomIndex]);

                    Debug.Log($"播放随机音乐: {fileName}");
                    AudioMgr.MusicAudioSourceProxy.Stop();
                    randomClip.name = fileName; // 设置名称
                    AudioMgr.MusicAudioSourceProxy.Play(randomClip);
                }
                else
                {
                    Debug.LogWarning("主界面音乐列表为空");
                }
            }
            else
            {
                if (gameMusics.Count > 0)
                {
                    // 随机选择音乐
                    var randomIndex = Random.Range(0, gameMusics.Count);
                    var randomClip = gameMusics[randomIndex];


                    // 获取文件名（不带后缀）
                    var fileName = Path.GetFileNameWithoutExtension(MusicPaths.GamePaths[randomIndex]);

                    Debug.Log($"播放随机音乐: {fileName}");
                    AudioMgr.MusicAudioSourceProxy.Stop();
                    randomClip.name = fileName; // 设置名称
                    AudioMgr.MusicAudioSourceProxy.Play(randomClip);
                }
                else
                {
                    Debug.LogWarning("游戏音乐列表为空");
                }
            }
        }

        [Serializable]
        public class MusicConfig
        {
            public List<string> MainMenuPaths = new List<string>();
            public List<string> GamePaths = new List<string>();
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare([In] [MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray
        {
            [PreserveSig]
            int BindToHandler(IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, out IntPtr ppvOut);

            [PreserveSig]
            int GetPropertyStore(int Flags, [In] ref Guid riid, out IntPtr ppv);

            [PreserveSig]
            int GetPropertyDescriptionList([In] ref Guid keyType, [In] ref Guid riid, out IntPtr ppv);

            [PreserveSig]
            int GetAttributes(uint dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);

            [PreserveSig]
            int GetCount(out uint pdwNumItems);

            [PreserveSig]
            int GetItemAt(uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [PreserveSig]
            int EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
        }

        [assembly: ComVisible(true)]
        private static class OpenFileDialog
        {
            [Flags]
            public enum Flags : uint
            {
                ALLOWMULTISELECT = 0x00000200
            }

            [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int SHCreateItemFromParsingName(
                [MarshalAs(UnmanagedType.LPWStr)] string path,
                IntPtr pbc,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

            public static string[] Show(string title, string initialDir, string filter, Flags flags)
            {
                if (string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir)) initialDir = "C://";

                var dialog = (IFileOpenDialog)new FileOpenDialogRCW();

                try
                {
                    // 设置基本选项
                    dialog.SetOptions((uint)(FileOpenOptions.AllowMultiselect | FileOpenOptions.PathMustExist |
                                             FileOpenOptions.FileMustExist));
                    dialog.SetTitle(title);

                    // 设置初始目录
                    if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
                    {
                        var shellItemGuid = typeof(IShellItem).GUID;
                        if (SHCreateItemFromParsingName(initialDir, IntPtr.Zero, ref shellItemGuid,
                                out var shellItem) == 0)
                            dialog.SetFolder(shellItem);
                    }

                    try
                    {
                        // 重构过滤器解析逻辑
                        var filterSpec = new[]
                        {
                            new COMDLG_FILTERSPEC
                            {
                                pszName = "MP3文件 (*.mp3)",
                                pszSpec = "*.mp3"
                            }
                        };

                        Debug.Log("应用文件过滤器：MP3文件 (*.mp3)");
                        dialog.SetFileTypes(1, ref filterSpec[0]); // 明确设置单个过滤器
                        dialog.SetFileTypeIndex(1); // 设置默认选中第一个过滤器

                        // 强制设置默认扩展名（可选）
                        dialog.SetDefaultExtension(".mp3");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"设置文件过滤器失败: {ex}");
                    }

                    // 显示对话框
                    if (dialog.Show(IntPtr.Zero) == 0)
                    {
                        dialog.GetResults(out var results);
                        results.GetCount(out var count);

                        var paths = new List<string>();
                        for (uint i = 0; i < count; i++)
                        {
                            results.GetItemAt(i, out var item);
                            item.GetDisplayName(0x80028000, out var path);
                            paths.Add(path);
                        }

                        return paths.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"文件对话框异常: {ex}");
                }
                finally
                {
                    Marshal.FinalReleaseComObject(dialog);
                }

                return null;
            }

            private static COMDLG_FILTERSPEC[] ParseFilterSpec(string filter)
            {
                var specs = new List<COMDLG_FILTERSPEC>();
                var parts = filter.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i < parts.Length; i += 2)
                {
                    if (i + 1 >= parts.Length) break;

                    // 简化处理逻辑，保留原始格式
                    var spec = parts[i + 1];
                    specs.Add(new COMDLG_FILTERSPEC
                    {
                        pszName = parts[i],
                        pszSpec = spec.Contains(";") ? string.Join(";", spec.Split(';').Select(s => s.Trim())) : spec
                    });
                }

                return specs.ToArray();
            }

            [ComImport]
            [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
            private class FileOpenDialogRCW
            {
            }

            [ComImport]
            [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IFileOpenDialog
            {
                [PreserveSig]
                uint Show(IntPtr parent);

                void SetFileTypes(uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);
                void SetFileTypeIndex(uint iFileType);
                void GetFileTypeIndex(out uint piFileType);
                void Advise([In] [MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);
                void Unadvise(uint dwCookie);
                void SetOptions(uint fos);
                void GetOptions(out uint pfos);
                void SetDefaultFolder([In] [MarshalAs(UnmanagedType.Interface)] IShellItem psi);
                void SetFolder([In] [MarshalAs(UnmanagedType.Interface)] IShellItem psi);
                void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
                void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
                void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
                void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
                void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
                void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
                void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
                void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
                void AddPlace([In] [MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);
                void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
                void Close([MarshalAs(UnmanagedType.Error)] uint hr);
                void SetClientGuid([In] ref Guid guid);
                void ClearClientData();
                void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
                void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);
                void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
            }


            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            private struct COMDLG_FILTERSPEC
            {
                [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
                [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
            }

            private enum FileOpenOptions : uint
            {
                AllowMultiselect = 0x00000200,
                PathMustExist = 0x00000800,
                FileMustExist = 0x00001000
            }
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(GlobalMusicAudioPlayer), "UpdateActiveMusicPlayer")]
        //public static void UpdateActiveMusicPlayer(GlobalMusicAudioPlayer __instance)
        //{
        //    AudioSourceProxy musicAudioSourceProxy = AudioMgr.MusicAudioSourceProxy;
        //    if (GameMusics.Count > 0 &&( musicAudioSourceProxy == null || musicAudioSourceProxy.LeftDuration <= 1.5f))
        //    {

        //    }
        //}
    }
}