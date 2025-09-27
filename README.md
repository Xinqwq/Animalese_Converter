# Animalese_Converter Desktop

一个 **中文汉字 → 拼音 + 按音节顺序播放音频** 的 Unity 项目。  
灵感来自「动森」风格的动物语音（Animalese）。
本项目提供了一个 Desktop 版本，支持在 Windows 上输入中文并播放对应的拼音音节音频。

---

## ✨ 功能 Features
- ⌨️ 输入你想要的中文汉字
- 🔊 按音节顺序播放音频（类似动森的 Animalese 语音）
- 🖥️ 支持 Windows 桌面运行
- 🛠️ Unity 开发，可自行扩展

---

## 📥 下载 Download
- Windows Desktop installer: [AnimaleseConverterSetup.exe](https://github.com/Xinqwq/Animalese_Converter/releases/download/v0.1/AnimaleseConverterSetup.exe)
- Windows Desktop zip: [Animalese.Converter.zip](https://github.com/Xinqwq/Animalese_Converter/releases/download/v0.1/Animalese.Converter.zip)

---

## 🖼️ 截图 Screenshots
![输入中文界面](Assets/Screenshots/Example_Interface.png)

---


## 📚 开发笔记 / Troubleshooting Notes

本节详细记录了从 **编辑器内运行正常** 到 **Build 后报错** 的问题分析、日志证据、代码改动、依赖 DLL 配置、Player Settings、构建步骤与排错清单，确保可复现并成功打包发布。

---

### 一、运行环境
- Unity 2018.4.28f1（Windows）
- 目标平台：PC, Mac & Linux Standalone（Windows x86_64）

### 二、关键目录
- `Assets/ChineseGibberish/Scripts/`
  - `PinYinSpell.cs`：拼音转换核心
  - `PronounceCore.cs`、`SpeakPronounce.cs`：UI 与播放逻辑
  - `EncodingBootstrap.cs`：启动时注册/加载编码提供器
- `Assets/Plugins/`
  - `I18N.dll`
  - `I18N.CJK.dll`
- `Assets/link.xml`：保留 I18N 程序集，避免裁剪

---

### 三、问题现象与日志证据

#### 1) Player 运行时报错：Encoding 936 不可用
- 典型日志（多次重复）：
```18:23:Logs/output_log5.txt
GB2312/936 encoding not available: Encoding 936 data could not be found. Make sure you have correct international codeset assembly installed and enabled.
```
- 表现：点击按钮时回调中断或容错，UI 看起来“无响应”。

#### 2) Player 运行时报“跳过未知音节”
- 典型日志（输出节选）：
```30:38:Logs/output_log4.txt
跳过未知音节：大家好_;
跳过未知音节：我是十六_;
跳过未知音节：希望可以和你做好朋友;
```
- 含义：拼音转换失败（编码不可用时触发了容错返回原文），导致后续找不到对应的 `AudioClip`，于是打印“跳过未知音节”。

#### 3) 构建失败（没有输出）：Editor.log 指向 CodePages 依赖 Unsafe
- 关键日志：
```19152:19155:c:/Users/XIN/AppData/Local/Unity/Editor/Editor.log
ArgumentException: The Assembly System.Runtime.CompilerServices.Unsafe is referenced by System.Text.Encoding.CodePages ('Assets/Plugins/System.Text.Encoding.CodePages.dll'). But the dll is not allowed to be included or could not be found.
  at UnityEditor.AssemblyHelper.AddReferencedAssembliesRecurse [...] AssemblyHelper.cs:152
```
- 含义：`System.Text.Encoding.CodePages.dll` 在 Unity 2018 下引入 `System.Runtime.CompilerServices.Unsafe`，未能满足依赖，构建被终止。

---

### 四、原因分析（编辑器 OK、Player 不 OK）
- 编辑器运行时（MonoBleedingEdge）自带完整 I18N/936 支持，不裁剪，故正常。
- Player 构建（Mono/IL2CPP）启用裁剪且不保证加载相关程序集；
- 将 `System.Text.Encoding.CodePages.dll` 直接放入 Plugins 在 2018 版本下会引入 `Unsafe` 依赖，导致构建失败；
- 因 936 不可用，`MakePinYin` 抛错或走容错导致“跳过未知音节”。

---

### 五、最终解决方案

#### 方案要点
- 放弃 `System.Text.Encoding.CodePages.dll` 路线（Unity 2018 下坑多）。
- 使用 Unity 自带 I18N 路线：仅引入 `I18N.dll` 与 `I18N.CJK.dll`。
- 启动时主动加载/预热，确保 936 可用。

#### 步骤 1：放置 DLL 到 Plugins
- 从 Unity 安装目录复制：
  - `I18N.dll`、`I18N.CJK.dll`
  - 推荐来源：`<Unity安装目录>/Editor/Data/MonoBleedingEdge/lib/mono/4.5/`
- 放入：`Assets/Plugins/`

#### 步骤 2：设置插件导入（非常关键）
- 在 Unity 中逐个选中 DLL → Inspector → Plugin Import Settings：
  - 取消勾选 `Any Platform`
  - 勾选 `Standalone`（`Editor` 可选）
  - 点击 `Apply`
  - 右键 DLL 执行 `Reimport`

#### 步骤 3：保留规则（避免裁剪）
- `Assets/link.xml` 内容：
```1:7:Assets/link.xml
<linker>
  <assembly fullname="System.Text.Encoding.CodePages">
    <type fullname="System.Text.CodePagesEncodingProvider" preserve="all" />
  </assembly>
  <assembly fullname="I18N" preserve="all" />
  <assembly fullname="I18N.CJK" preserve="all" />
</linker>
```
- 说明：虽然最终没有使用 CodePages.dll，但保留该条目无影响。

#### 步骤 4：启动时注册/加载编码（代码引用）
- 文件：`Assets/ChineseGibberish/Scripts/EncodingBootstrap.cs`
```10:70:Assets/ChineseGibberish/Scripts/EncodingBootstrap.cs
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void RegisterCodePages()
{
    // 主动加载相关程序集
    try { System.Reflection.Assembly.Load("System.Text.Encoding.CodePages"); } catch {}
    try { System.Reflection.Assembly.Load("I18N"); } catch {}
    try { System.Reflection.Assembly.Load("I18N.CJK"); } catch {}

    // 反射注册 CodePagesEncodingProvider（存在则注册，不存在忽略）
    try
    {
        var providerType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
        if (providerType != null)
        {
            var instanceProp = providerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceProp != null ? instanceProp.GetValue(null, null) : null;
            var register = typeof(Encoding).GetMethod("RegisterProvider", BindingFlags.Public | BindingFlags.Static);
            if (instance != null && register != null)
            {
                register.Invoke(null, new object[] { instance });
            }
        }
        else
        {
            // 强制加载 I18N.CJK 的 CP936（Mono 路线）
            var cp936Type = Type.GetType("I18N.CJK.CP936, I18N.CJK");
            if (cp936Type != null) Activator.CreateInstance(cp936Type);
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning("Failed to register CodePagesEncodingProvider via reflection: " + e.Message);
    }

    // 预热 GB2312/936
    try { _ = Encoding.GetEncoding("GB2312"); }
    catch (Exception)
    {
        try { _ = Encoding.GetEncoding(936); }
        catch (Exception)
        {
            try
            {
                var t = Type.GetType("I18N.CJK.CP936, I18N.CJK");
                if (t != null) Activator.CreateInstance(t);
                _ = Encoding.GetEncoding(936);
            }
            catch (Exception warmEx)
            {
                Debug.LogWarning("GB2312/936 encoding not available: " + warmEx.Message);
            }
        }
    }
}
```

#### 步骤 5：拼音转换容错（代码引用）
- 文件：`Assets/ChineseGibberish/Scripts/PinYinSpell.cs`
```218:241:Assets/ChineseGibberish/Scripts/PinYinSpell.cs
public static string MakePinYin(string strChinese, SpellOptions options)
{
    string[] pinyin = _PinYinSplit;
    // Encoding for GBK mapping needs GB2312/936
    Encoding encoding;
    try
    {
        encoding = Encoding.GetEncoding("GB2312");
    }
    catch (System.Exception ex)
    {
        Debug.LogWarning("GB2312 encoding not available, skip pinyin conversion: " + ex.Message);
        return strChinese; // graceful fallback to avoid breaking UI callbacks
    }

    byte[] local = encoding.GetBytes(strChinese);
    ...
}
```

#### 步骤 6：Player Settings（2018.4）
- File → Build Settings → Player Settings… → Other Settings：
  - Scripting Backend：建议先用 Mono 验证，再尝试 IL2CPP；
  - Api Compatibility Level：`.NET 4.x`；
  - Managed Stripping Level（仅 IL2CPP 可见）：`Low` 或 `Medium`；
  - 某些版本/平台无 `Strip Engine Code`，可忽略；
- Build 前确保 “Scenes In Build” 至少有一个场景勾选。

---

### 六、构建与自检
1) 构建完成后，检查输出目录 `<YourBuild>_Data/Managed/` 是否包含：
   - `I18N.dll`
   - `I18N.CJK.dll`
2) 首次运行时日志不应出现：`Encoding 936 data could not be found`。
3) 点击“转换/播放”按钮应正常：若日志出现大量“跳过未知音节”，说明编码仍未生效，回到上文逐步排查。

---

### 七、常见报错与处理
- 构建失败（Editor.log 报 Unsafe 依赖）：
  - 移除 `System.Text.Encoding.CodePages.dll`（或换用与 Unity 2018 兼容的 net46/net461 版本并补齐 Unsafe 依赖，不推荐）。
- Player 运行仍报 936 不可用：
  - 检查 `I18N.dll`、`I18N.CJK.dll` 是否真实进入 `Managed/`；
  - 检查 Plugins 导入配置是否勾选 Standalone；
  - 确保 Api Compatibility Level 为 .NET 4.x；
  - 查看启动日志是否有“已尝试注册/加载/预热”记录。

---

### 八、项目使用
- 打开 `Assets/ChineseGibberish/TestPronounce.unity`；
- 输入中文 → 点击“点我”可查看拼音，或“Play”听音节；
- 音频源文件在 `Assets/ChineseGibberish/PinYinAudio/`，可替换。

---

### 九、变更摘要
- `PinYinSpell.cs`：改为 GB2312；加入容错，编码不可用时返回原文并告警。
- `EncodingBootstrap.cs`：添加启动初始化，主动加载程序集，反射注册 CodePages，强制加载 I18N.CJK.CP936，并预热 936。
- `link.xml`：保留 I18N 与 I18N.CJK。
- `Assets/Plugins/`：引入 `I18N.dll`、`I18N.CJK.dll`；移除 `System.Text.Encoding.CodePages.dll`。

---

### 十、许可说明
- `I18N.dll`、`I18N.CJK.dll` 为 Unity/Mono 组件的一部分，请遵循对应许可。

若在新环境/新机器上构建仍遇到问题，请联系并附上：
- Player 运行日志（`*_Data/output_log.txt` 或 `Player.log`）；
- 构建产物 `Managed/` 目录文件清单；
- Editor.log 末尾错误段落。

我将根据日志提供下一步建议。


## 📌 致谢 Acknowledgement
本项目最初基于 [知乎专栏文章](https://zhuanlan.zhihu.com/p/341407630) 分享的工程文件进行改动与扩展。  
非常感谢原作者提供的思路与代码参考。

---

## 👩‍💻 贡献 Credits
- 开发与维护: [@Xinqwq](https://github.com/Xinqwq)
[@buptcuican](https://github.com/buptcuican)

---

