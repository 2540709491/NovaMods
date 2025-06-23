using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;
using Sika.Tools;
using Sika.Logic;
using System.Xml;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NovaMEMOD
{
    [BepInPlugin("nova.zptdf.plugin.saveeditor", "存档编辑器", "1.1.1")]
    [BepInProcess("ShooperNova.exe")]
    public class SaveEditorMod : BaseUnityPlugin
    {
        // 配置项
        private static ConfigEntry<bool> isSaveDEDConfig;
        private static ConfigEntry<KeyCode> toggleKeyConfig;

        // 存档系统引用

        private string _originalJson;
        private string _editedJson;
        private string _savePath;

        // 窗口状态
        private bool _isEditorOpen;
        private Vector2 _scrollPos;

        void Start()
        {
            toggleKeyConfig = Config.Bind("Hotkeys",
                "Toggle Editor",
                KeyCode.F10,
                "开关存档编辑器");

            isSaveDEDConfig = Config.Bind("Security",
                "Disable Save Encryption",
                true,
                "禁用存档加密");

            Harmony.CreateAndPatchAll(typeof(SaveEditorMod));

        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKeyConfig.Value))
            {
                _isEditorOpen = !_isEditorOpen;
                if (_isEditorOpen) ReloadSaveData();
            }
        }

        private void ReloadSaveData()
        {
            if (GlobalSaveMgr.Saver == null) return;

            try
            {
                // 强制重新加载存档
                GlobalSaveMgr.Saver.LoadFromDisk();


                _originalJson = GlobalSaveMgr.Saver.SaveToJson();
                _editedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(_originalJson), Newtonsoft.Json.Formatting.Indented);
                Logger.LogInfo("成功加载内存存档数据");
            }
            catch (Exception e)
            {
                Logger.LogError($"载入失败: {e.Message}");
            }
        }

        // GUI绘制
        private void OnGUI()
        {
            if (!_isEditorOpen) return;

            Rect mainRect = new Rect(50, 50, Screen.width - 100, Screen.height - 100);
            mainRect = GUI.Window(87361254, mainRect, DrawMainWindow, "内存存档编辑器");
        }

        private void DrawMainWindow(int id)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // 操作按钮区
            DrawActionButtons();

            // 内容编辑区
            _scrollPos = GUILayout.BeginScrollView(_scrollPos,
                GUILayout.Height(Screen.height * 0.8f));

            _editedJson = GUILayout.TextArea(_editedJson,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("重新加载", GUILayout.Width(80)))
            {
                ReloadSaveData();
            }

            if (GUILayout.Button("应用修改", GUILayout.Width(80)))
            {
                ApplyChanges();
            }

            if (GUILayout.Button("保存到磁盘", GUILayout.Width(100)))
            {
                SaveToDisk();
            }
            _savePath = GUILayout.TextField(_savePath, GUILayout.Width(800));
            if (GUILayout.Button("加载本地存档", GUILayout.Width(100)))
            {
                if (GlobalSaveMgr.Saver == null) return;

                try
                {
                    // 强制重新加载存档
                    _originalJson=Traverse.Create(GameObject.Find("GameWorld").GetComponent<World>().Saver).Field<GameSaver>("_gameSaver").Value.SaveToJson();
                    _editedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(_originalJson), Newtonsoft.Json.Formatting.Indented);
                    Logger.LogInfo("成功加载内存存档数据");
                }
                catch (Exception e)
                {
                    Logger.LogError($"载入失败: {e.Message}");
                }
            }
            if (GUILayout.Button("获取所有ID", GUILayout.Width(100)))
            {

                string tempW="";
                foreach (ShopItemConfig item1 in ShopItemConfigTable.GetAllConfigs()){
                    try
                    {
                        
                        tempW+= "Name:"+Traverse.Create(item1).Property<string>("Name").Value+" ID:"+ item1.ID+"\r\n";
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }

                _editedJson = tempW;
            }
               
            GUILayout.EndHorizontal();
        }

        private void ApplyChanges()
        {
            try
            {
                if (GlobalSaveMgr.Saver == null) return;

                // 反序列化修改后的JSON
                GlobalSaveMgr.Saver.ClearSaveData();
                _editedJson=JsonConvert.SerializeObject(JsonConvert.DeserializeObject(_editedJson), Newtonsoft.Json.Formatting.None);
                GlobalSaveMgr.Saver.LoadFromJson(_editedJson);
                _originalJson = _editedJson;
                Logger.LogInfo("内存数据修改已应用");
            }
            catch (Exception e)
            {
                Logger.LogError($"应用失败: {e.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                if (GlobalSaveMgr.Saver == null) return;

                // 触发游戏原生保存流程
                GlobalSaveMgr.Saver.SaveToDisk();
                Logger.LogInfo("存档已持久化到磁盘");
            }
            catch (Exception e)
            {
                Logger.LogError($"保存失败: {e.Message}");
            }
        }
        public class Wrapper
        {
            public List<string> data;
        }

    }
}