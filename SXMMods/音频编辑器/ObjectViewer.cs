using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using BepInEx.Configuration;
using System.Reflection;
using UnityEngine.Rendering;
using Newtonsoft.Json;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;
[BepInPlugin("com.zptdf.objectviewer", "Object Viewer", "0.0.3")]
public class ObjectViewer : BaseUnityPlugin
{
    private bool _showWindow = true;
    private Vector2 _componentScrollPos;
    private Vector2 _scrollPosition;
    private GameObject _selectedObject;
    private readonly Dictionary<GameObject, bool> _expandedObjects = new Dictionary<GameObject, bool>();
    private Rect _windowRect = new Rect(20, 20, 800, 600);
    private bool _showContextMenu;         // 是否显示右键菜单
    private Vector2 _contextMenuPosition;  // 右键菜单位置
    private GameObject _contextMenuTarget; // 右键菜单目标对象
    static ConfigEntry<bool> isViewConfig;
    private readonly Dictionary<Component, bool> _expandedComponents = new Dictionary<Component, bool>();
    private PropertyInfo _contextProperty; // 右键菜单操作的属性
    private bool _showComponentMenu;
    private Vector2 _componentMenuPos;

    void Start()
    {
        isViewConfig = Config.Bind("设置", "是否开启对象查看器", true, "是否开启是否开启对象查看器,立即生效");
        PreApply();
    }

    void PreApply()
    {

    }
    private void OnGUI()
    {
        Rect windowRect = new Rect(0, UnityEngine.Screen.height - 40, 300, 50);
        windowRect = GUI.Window(20210218, windowRect, TipWindow, "注意事项[此窗口不可关闭]");
        if (!_showWindow || !isViewConfig.Value) return;

        _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "场景对象编辑器[测试版Ver0.0.2]");

        Rect menuRect = new Rect(_componentMenuPos.x, _componentMenuPos.y, 150, 80);
        //GUI.Window(2, menuRect, DrawComponentMenu, "");
        DrawContextMenu(); // 绘制右键菜单


    }

    private void DrawMainWindow(int windowId)
    {
        // 左侧树状结构面板
        GUILayout.BeginHorizontal();
        DrawObjectTree();

        // 右侧属性面板
        DrawSelectedObjectDetails();
        GUILayout.EndHorizontal();

        GUI.DragWindow(); // 允许拖动窗口
    }
    private void DrawObjectTree()
    {
        GUILayout.BeginVertical(GUILayout.Width(300));
        if (GUILayout.Button("关闭窗口"))
        {
            isViewConfig.Value = false;
        }
        if (GUILayout.Button("刷新"))
        {
            ReRoots();
        }
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        // 初始调用时缩进层级为 0
        foreach (var obj in GetRootGameObjects())
        {
            DrawTreeNode(obj.transform, 0);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }


    private void DrawTreeNode(Transform node, int indentLevel = 0)
    {
        // 添加高亮效果（选中时显示黄色）
        var originalColor = GUI.color;
        if (_selectedObject == node.gameObject)
        {
            GUI.color = Color.yellow;
        }
        // 节点折叠/展开逻辑
        bool isExpanded = _expandedObjects.ContainsKey(node.gameObject) && _expandedObjects[node.gameObject];
        string foldoutLabel = isExpanded ? "▼ " : "▶ ";

        // 使用水平布局和 Space 实现缩进
        GUILayout.BeginHorizontal();
        GUILayout.Space(indentLevel * 20); // 每层缩进 20 像素
        if (GUILayout.Button(foldoutLabel + node.name, GUILayout.ExpandWidth(false)))
        {
            _expandedObjects[node.gameObject] = !isExpanded;
            _selectedObject = node.gameObject;
        }
        GUILayout.EndHorizontal();

        // 递归绘制子节点时缩进层级+1
        if (isExpanded)
        {
            foreach (Transform child in node)
            {
                DrawTreeNode(child, indentLevel + 1);
            }
        }
        GUI.color = originalColor;
        if (Event.current.type == EventType.MouseDown &&
    Event.current.button == 1 &&
    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        {
            _contextMenuTarget = node.gameObject;
            _contextMenuPosition = Event.current.mousePosition;
            _showContextMenu = true;
            Event.current.Use(); // 标记事件已处理
        }
        Rect nodeRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 1 &&
            nodeRect.Contains(Event.current.mousePosition))
        {
            _contextMenuTarget = node.gameObject;
            _contextMenuPosition = Event.current.mousePosition;
            _showContextMenu = true;
            Event.current.Use();
        }
    }

    // 获取所有根对象（无父节点的对象）
    private List<GameObject> GetRootGameObjects()
    {
        List<GameObject> roots = new List<GameObject>();
        foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
        {
            if (obj.transform.parent == null) roots.Add(obj);
        }
        return roots;
    }
    private double _lastClickTime;
    private int _lastClickControlID = -1;
    private string _lastCopyContent;
    IEnumerator ShowCopyFeedback(Rect rect)
    {
        float duration = 0.3f;
        float startTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startTime < duration)
        {
            // 通过强制重绘实现动画效果
            GUI.changed = true;
            yield return null;
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    void DrawCopyableLabel(string displayText, string copyContent)
    {
        // 获取标签布局区域
        Rect rect = GUILayoutUtility.GetRect(new GUIContent(displayText), GUI.skin.label);

        // 创建透明点击区域
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            double currentTime = Time.realtimeSinceStartup;
            bool isDoubleClick = (currentTime - _lastClickTime < 0.3) &&
                               (_lastClickControlID == GUIUtility.GetControlID(FocusType.Passive));

            _lastClickTime = currentTime;
            _lastClickControlID = GUIUtility.GetControlID(FocusType.Passive);

            if (isDoubleClick)
            {
                _lastCopyContent = copyContent;
                GUIUtility.systemCopyBuffer = copyContent;
                StartCoroutine(ShowCopyFeedback(rect));
            }
        }

        // 绘制原始标签
        GUI.Label(rect, displayText);

        // 显示复制反馈
        if (Time.realtimeSinceStartup - _lastClickTime < 0.3 && copyContent == _lastCopyContent)
        {
            GUI.DrawTexture(rect, MakeTex(1, 1, new Color(0, 1, 0, 0.2f)));
        }
    }
    private void DrawSelectedObjectDetails()
    {
        GUILayout.BeginVertical(GUILayout.Width(500));

        if (_selectedObject != null)
        {

            DrawCopyableLabel($"名称: {_selectedObject.name}", _selectedObject.name);

            // 标签
            DrawCopyableLabel($"标签: {_selectedObject.tag}", _selectedObject.tag);

            // 层级
            string layerName = LayerMask.LayerToName(_selectedObject.layer);
            DrawCopyableLabel($"层级: {layerName}", layerName);

            // Transform 详细信息
            GUILayout.Space(10);
            GUILayout.Label("Transform");
            DrawVector3Field("Position", _selectedObject.transform.localPosition);
            DrawVector3Field("Rotation", _selectedObject.transform.localEulerAngles);
            DrawVector3Field("Scale", _selectedObject.transform.localScale);

            // 组件列表
            //_componentScrollPos = GUILayout.BeginScrollView(_componentScrollPos, GUILayout.Height(200));
            //foreach (Component component in _selectedObject.GetComponents<Component>())
            //{
            //    GUILayout.Label($"- {component.GetType().Name}");
            //}
            //GUILayout.EndScrollView();
            _componentScrollPos = GUILayout.BeginScrollView(_componentScrollPos, GUILayout.Height(400));
            foreach (Component component in _selectedObject.GetComponents<Component>())
            {
                DrawComponentTree(component);
            }
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("未选中任何对象");
        }

        GUILayout.EndVertical();
    }
    //void DrawComponentMenu(int id)
    //{
    //    GUILayout.BeginVertical();

    //    if (GUILayout.Button("复制组件信息"))
    //    {
    //        GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(_selectedComponent);
    //        _showComponentMenu = false;
    //    }

    //    if (GUILayout.Button("移除组件"))
    //    {
    //        Destroy(_selectedComponent);
    //        _showComponentMenu = false;
    //    }

    //    GUILayout.EndVertical();

    //    // 点击外部区域关闭菜单
    //    if (Event.current.type == EventType.MouseDown &&
    //        !menuRect.Contains(Event.current.mousePosition))
    //    {
    //        _showComponentMenu = false;
    //    }
    //}
    private void DrawComponentTree(Component component)
    {
        bool isExpanded = _expandedComponents.ContainsKey(component) && _expandedComponents[component];
        string foldoutLabel = isExpanded ? "▼ " : "▶ ";

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(foldoutLabel + component.GetType().Name, GUILayout.ExpandWidth(false)))
        {
            _expandedComponents[component] = !isExpanded;
        }

        // 组件右键菜单
        Rect componentRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 1 &&
            componentRect.Contains(Event.current.mousePosition))
        {
            ShowComponentContextMenu(component);
            Event.current.Use();
        }
        GUILayout.EndHorizontal();

        if (isExpanded)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            DrawComponentProperties(component);
            GUILayout.EndVertical();
        }
    }
    private void DrawComponentProperties(Component component)
    {
        // 使用反射获取所有公共属性
        PropertyInfo[] properties = component.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        switch (component)
        {
            case TMP_Text tmpText:
                DrawTMPProperties(tmpText);
                break;
            case Image uiImage:
                DrawImageProperties(uiImage);
                break;
        }
        foreach (PropertyInfo prop in properties)
        {
            // 过滤掉不支持的属性类型
            if (!IsSupportedType(prop.PropertyType)) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(prop.Name, GUILayout.Width(150));

            try
            {
                object value = prop.GetValue(component, null);
                DrawPropertyField(prop, component, value);
            }
            catch { /* 处理不可读属性 */ }

            GUILayout.EndHorizontal();
        }

        // 特殊处理材质属性
        if (component is Renderer renderer)
        {
            DrawMaterialProperties(renderer.materials);
        }
    }
    #region 属性编辑器控件
    private float EditorFloatField(float value)
    {
        GUILayout.BeginVertical();
        float newValue = value;
        try
        {
            string input = GUILayout.TextField(value.ToString("F2"));
            newValue = float.Parse(input);
        }
        catch
        {
            // 保持原值
        }
        GUILayout.EndVertical();
        return newValue;
    }

    private int EditorIntField(int value)
    {
        int newValue = value;
        try
        {
            string input = GUILayout.TextField(value.ToString());
            newValue = int.Parse(input);
        }
        catch
        {
            // 保持原值
        }
        return newValue;
    }

    private bool EditorBoolField(bool value)
    {
        return GUILayout.Toggle(value, "");
    }

    private string EditorTextField(string value)
    {
        return GUILayout.TextField(value);
    }

    private Color EditorColorField(Color color)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label("R:");
        color.r = ParseColorComponent(GUILayout.TextField(color.r.ToString("F2")));

        GUILayout.Label("G:");
        color.g = ParseColorComponent(GUILayout.TextField(color.g.ToString("F2")));

        GUILayout.Label("B:");
        color.b = ParseColorComponent(GUILayout.TextField(color.b.ToString("F2")));

        GUILayout.Label("A:");
        color.a = ParseColorComponent(GUILayout.TextField(color.a.ToString("F2")));

        GUILayout.EndHorizontal();
        return color;
    }

    private Vector3 EditorVector3Field(Vector3 vector)
    {
        GUILayout.BeginVertical();

        vector.x = EditorFloatField(vector.x);
        vector.y = EditorFloatField(vector.y);
        vector.z = EditorFloatField(vector.z);

        GUILayout.EndVertical();
        return vector;
    }

    #endregion
    private void DrawMaterialProperties(Material[] materials)
    {
        foreach (Material mat in materials)
        {
            GUILayout.Label($"Material: {mat.name}", GUI.skin.label);

            for (int i = 0; i < mat.shader.GetPropertyCount(); i++)
            {
                string propName = mat.shader.GetPropertyName(i);
                ShaderPropertyType propType = mat.shader.GetPropertyType(i);

                GUILayout.BeginHorizontal();
                GUILayout.Label(propName, GUILayout.Width(150));

                try
                {
                    switch (propType)
                    {
                        case ShaderPropertyType.Color:
                            Color colorValue = mat.GetColor(propName);
                            Color newColor = EditorColorField(colorValue);
                            mat.SetColor(propName, newColor);
                            break;

                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            float floatValue = mat.GetFloat(propName);
                            float newFloat = EditorFloatField(floatValue);
                            mat.SetFloat(propName, newFloat);
                            break;

                        case ShaderPropertyType.Vector:
                            Vector4 vecValue = mat.GetVector(propName);
                            Vector4 newVec = EditorVector4Field(vecValue);
                            mat.SetVector(propName, newVec);
                            break;
                    }
                }
                catch (Exception e)
                {
                    GUILayout.Label($"Error: {e.Message}");
                }

                GUILayout.EndHorizontal();
            }
        }
    }
    private Vector4 EditorVector4Field(Vector4 vector)
    {
        GUILayout.BeginVertical();

        vector.x = EditorFloatField(vector.x);
        vector.y = EditorFloatField(vector.y);
        vector.z = EditorFloatField(vector.z);
        vector.w = EditorFloatField(vector.w);

        GUILayout.EndVertical();
        return vector;
    }
    private Color DrawColorField(Color color)
    {
        // 简化版颜色选择
        GUILayout.Label("R:");
        color.r = ParseColorComponent(GUILayout.TextField(color.r.ToString("F2")));
        GUILayout.Label("G:");
        color.g = ParseColorComponent(GUILayout.TextField(color.g.ToString("F2")));
        GUILayout.Label("B:");
        color.b = ParseColorComponent(GUILayout.TextField(color.b.ToString("F2")));
        GUILayout.Label("A:");
        color.a = ParseColorComponent(GUILayout.TextField(color.a.ToString("F2")));
        return color;
    }
    private float ParseColorComponent(string input)
    {
        if (float.TryParse(input, out float value))
        {
            return Mathf.Clamp01(value);
        }
        return 0f;
    }

    private void DrawPropertyField(PropertyInfo prop, Component target, object value)
    {
        if (prop.PropertyType == typeof(float))
        {
            string floatInput = GUILayout.TextField(((float)value).ToString("F2"));
            if (float.TryParse(floatInput, out float newValue))
            {
                prop.SetValue(target, newValue, null);
            }
        }
        else if (prop.PropertyType == typeof(Color))
        {
            Color colorValue = (Color)value;
            colorValue = DrawColorField(colorValue);
            prop.SetValue(target, colorValue, null);
        }
        // ... 其他类型处理 ...
    }
    private void ShowComponentContextMenu(Component component)
    {
        // 使用简易版右键菜单
        if (GUILayout.Button("复制组件信息"))
        {
            GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(component);
        }
        if (GUILayout.Button("移除组件"))
        {
            Destroy(component);
        }
    }
    private bool IsSupportedType(System.Type type)
    {
        return type == typeof(float) ||
               type == typeof(int) ||
               type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(Color) ||
               type == typeof(Vector3);
    }

    private object GetDefaultValue(System.Type type)
    {
        if (type == typeof(float)) return 0f;
        if (type == typeof(int)) return 0;
        if (type == typeof(bool)) return false;
        if (type == typeof(Vector3)) return Vector3.zero;
        return null;
    }

    //private void ShowPropertyContextMenu(Component target)
    //{
    //    GenericMenu menu = new GenericMenu();
    //    menu.AddItem(new GUIContent("重置默认值"), false, () =>
    //    {
    //        _contextProperty.SetValue(target, GetDefaultValue(_contextProperty.PropertyType), null);
    //    });
    //    menu.ShowAsContext();
    //}
    void ReRoots()
    {
        _expandedObjects.Clear();

    }
    #region TMP_Text支持
    private void DrawTMPProperties(TMP_Text tmp)
    {
        // 文本内容
        GUILayout.BeginHorizontal();
        GUILayout.Label("Text", GUILayout.Width(150));
        string newText = GUILayout.TextArea(tmp.text, GUILayout.Height(60));
        if (newText != tmp.text)
        {
            tmp.text = newText;
        }
        GUILayout.EndHorizontal();

        // 字体大小
        DrawFloatProperty("Font Size", tmp.fontSize, value => tmp.fontSize = value);

        // 颜色
        DrawColorProperty("Color", tmp.color, value => tmp.color = value);

        // 对齐方式
        DrawAlignmentProperty(tmp);

        // 其他常用属性
        DrawBoolProperty("Enable Auto Sizing", tmp.enableAutoSizing,
            value => tmp.enableAutoSizing = value);

        if (tmp.enableAutoSizing)
        {
            DrawFloatRange("Font Size Min", tmp.fontSizeMin, 0, 100,
                value => tmp.fontSizeMin = value);
            DrawFloatRange("Font Size Max", tmp.fontSizeMax, 0, 100,
                value => tmp.fontSizeMax = value);
        }

        // 字间距
        DrawFloatProperty("Character Spacing", tmp.characterSpacing,
            value => tmp.characterSpacing = value);
    }

    private void DrawAlignmentProperty(TMP_Text tmp)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Alignment", GUILayout.Width(150));

        TextAlignmentOptions current = tmp.alignment;
        TextAlignmentOptions newAlignment = current;

        // 水平对齐
        GUILayout.BeginVertical();
        if (GUILayout.Toggle(current.HasFlag(TextAlignmentOptions.Left), "Left"))
            newAlignment = TextAlignmentOptions.Left;
        if (GUILayout.Toggle(current.HasFlag(TextAlignmentOptions.Center), "Center"))
            newAlignment = TextAlignmentOptions.Center;
        if (GUILayout.Toggle(current.HasFlag(TextAlignmentOptions.Right), "Right"))
            newAlignment = TextAlignmentOptions.Right;
        GUILayout.EndVertical();

        // 垂直对齐
        GUILayout.BeginVertical();
        if (GUILayout.Toggle(current.HasFlag(TextAlignmentOptions.Top), "Top"))
            newAlignment |= TextAlignmentOptions.Top;
        if (GUILayout.Toggle(current.HasFlag(TextAlignmentOptions.Center), "Middle"))
            newAlignment |= TextAlignmentOptions.Center;
        if (GUILayout.Toggle(current.HasFlag(TextAlignmentOptions.Bottom), "Bottom"))
            newAlignment |= TextAlignmentOptions.Bottom;
        GUILayout.EndVertical();

        if (newAlignment != current)
        {
            tmp.alignment = newAlignment;
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region Image组件支持
    private void DrawImageProperties(Image image)
    {
        // 颜色
        DrawColorProperty("Color", image.color, value => image.color = value);

        // 图片填充类型
        DrawFillMethod(image);

        // 填充量
        if (image.type == Image.Type.Filled)
        {
            DrawFloatRange("Fill Amount", image.fillAmount, 0, 1,
                value => image.fillAmount = value);
        }

        // Sprite显示
        GUILayout.BeginHorizontal();
        GUILayout.Label("Sprite", GUILayout.Width(150));
        GUILayout.Label(image.sprite ? image.sprite.name : "None");
        GUILayout.EndHorizontal();

        // 材质
        if (image.material)
        {
            GUILayout.Label($"Material: {image.material.name}");
        }
    }

    private void DrawFillMethod(Image image)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Fill Method", GUILayout.Width(150));

        Image.Type newType = image.type;
        newType = (Image.Type)GUILayout.Toolbar((int)newType, new[]
        {
        "Simple", "Sliced", "Tiled", "Filled"
    });

        if (newType != image.type)
        {
            image.type = newType;
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region 通用绘制方法
    private void DrawColorProperty(string label, Color current, Action<Color> setter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        Color newColor = EditorColorField(current);
        if (newColor != current)
        {
            setter(newColor);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawFloatProperty(string label, float current, Action<float> setter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        float newValue = EditorFloatField(current);
        if (Math.Abs(newValue - current) > 0.001f)
        {
            setter(newValue);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawFloatRange(string label, float current, float min, float max, Action<float> setter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        float newValue = GUILayout.HorizontalSlider(current, min, max);
        newValue = (float)Math.Round(newValue, 2);
        GUILayout.Label(newValue.ToString("0.00"));
        if (Math.Abs(newValue - current) > 0.001f)
        {
            setter(newValue);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawBoolProperty(string label, bool current, Action<bool> setter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        bool newValue = GUILayout.Toggle(current, "");
        if (newValue != current)
        {
            setter(newValue);
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    // 模仿 Unity 的 Vector3 字段显示
    private void DrawVector3Field(string label, Vector3 value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(60));
        GUILayout.Label("X", GUILayout.Width(10));
        GUILayout.Label(value.x.ToString("F2"), GUILayout.Width(60));
        GUILayout.Label("Y", GUILayout.Width(10));
        GUILayout.Label(value.y.ToString("F2"), GUILayout.Width(60));
        GUILayout.Label("Z", GUILayout.Width(10));
        GUILayout.Label(value.z.ToString("F2"), GUILayout.Width(60));
        GUILayout.EndHorizontal();
    }
    private float _lastRefreshTime;
    private const float RefreshInterval = 2.0f;

    private void Update()
    {

    }
    private void DrawContextMenu()
    {
        if (!_showContextMenu) return;

        // 创建菜单窗口
        Rect menuRect = new Rect(_contextMenuPosition.x, _contextMenuPosition.y, 150, 80);
        GUI.Window(1, menuRect, id => {
            GUILayout.BeginVertical();

            if (GUILayout.Button("删除对象"))
            {
                GameObject.Destroy(_contextMenuTarget);
                _showContextMenu = false;
            }

            if (GUILayout.Button("复制路径"))
            {
                GUIUtility.systemCopyBuffer = GetObjectPath(_contextMenuTarget.transform);
                _showContextMenu = false;
            }

            GUILayout.EndVertical();
        }, "");

        // 点击菜单外区域关闭菜单
        if (Event.current.type == EventType.MouseDown && !menuRect.Contains(Event.current.mousePosition))
        {
            _showContextMenu = false;
        }
        GUI.backgroundColor = Color.gray;
        GUI.Window(1, menuRect, id => { /* ... */ }, "");
        GUI.backgroundColor = Color.white;
    }
    public void TipWindow(int winId)
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

    private string GetObjectPath(Transform tr)
    {
        if (tr.parent == null) return tr.name;
        return GetObjectPath(tr.parent) + "/" + tr.name;
    }
}

