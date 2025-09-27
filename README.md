# Animalese_Converter Desktop
<details open> 
<summary>🇺🇸 English</summary>
  
**A desktop version Animalese Converter**: Convert text and play syllable sounds like Animal Crossing.
Type any Chinese characters and hear them spoken back as syllable-based audio!

---

## ✨ Features

- ⌨️ Chinese Input → Pinyin Audio
Type or paste Chinese text, and it gets converted to pinyin.

- 🔊 Animalese-Style Voice Playback
Each pinyin syllable is played in sequence, producing a playful Animalese-like effect.

- 🖥️ Windows Desktop Support
Runs as a standalone desktop application (tested on Windows).

- 🛠️ Unity-Based & Extensible
Built in Unity — you can easily expand with your own voice banks, UI, or mods.

---

## 📥 Download
- `Windows Desktop installer`: [AnimaleseConverterSetup-v0.2.exe](https://github.com/Xinqwq/Animalese_Converter/releases/download/v0.2/AnimaleseConverterSetup_v0.2.exe)    18.3 MB
- `Windows Desktop zip`: [Animalese Converter v0.2.zip](https://github.com/Xinqwq/Animalese_Converter/releases/download/v0.2/Animalese.Converter.v0.2.zip)    22.6 MB

---

## 🖼️ Screenshots
![Program Interface](Assets/Screenshots/Example_Interface_v0.2.png)

---

## 📚 Developer Notes / Troubleshooting

This section documents the process from **working in the Unity Editor** to **errors after Player build**, including logs, code changes, DLL configuration, Player Settings, build steps, and troubleshooting checklist to ensure reproducible and successful builds.

---

### 1. Environment
- `Unity 2018.4.28f1` (`Windows`)
- `Target Platform: PC, Mac & Linux Standalone (Windows x86_64)`

### 2. Key Directories
- `Assets/ChineseGibberish/Scripts/`
  - `PinYinSpell.cs`: Core pinyin conversion
  - `PronounceCore.cs`, `SpeakPronounce.cs`: UI and playback logic
  - `EncodingBootstrap.cs`: Register/load encoding provider at startup
- `Assets/Plugins/`
  - `I18N.dll`
  - `I18N.CJK.dll`
- `Assets/link.xml`: Preserve I18N assemblies to avoid stripping

---

### 3. Observed Issues & Log Evidence

#### 3.1) Player Error: Encoding 936 not available
- Example log:
```18:23:Logs/output_log5.txt
GB2312/936 encoding not available: Encoding 936 data could not be found. Make sure you have correct international codeset assembly installed and enabled.
```
- Symptoms: Button callbacks are interrupted or fail silently, UI appears unresponsive.

#### 3.2) Player logs "Skipping unknown syllables"

- Example log excerpt:
```text
30:38:Logs/output_log4.txt
Skipping unknown syllable: 大家好_;
Skipping unknown syllable: 我是十六_;
Skipping unknown syllable: 希望可以和你做好朋友;
```

- Meaning: Pinyin conversion failed (due to unavailable encoding fallback), so the corresponding AudioClip cannot be found, triggering "skip unknown syllable" messages.

#### 3.3) Build failure: Editor.log shows CodePages depends on Unsafe

- Key log:
```text
19152:19155:c:/Users/XIN/AppData/Local/Unity/Editor/Editor.log
ArgumentException: The Assembly System.Runtime.CompilerServices.Unsafe is referenced by System.Text.Encoding.CodePages ('Assets/Plugins/System.Text.Encoding.CodePages.dll'). But the dll is not allowed to be included or could not be found.
  at UnityEditor.AssemblyHelper.AddReferencedAssembliesRecurse [...] AssemblyHelper.cs:152
```

- Meaning: `System.Text.Encoding.CodePages.dll` in `Unity 2018` introduces `System.Runtime.CompilerServices.Unsafe`, which is not satisfied, causing build termination.

### 4. Cause Analysis (Editor OK, Player NOT OK)

- Editor (`MonoBleedingEdge`) has full I18N/936 support without stripping → works fine.
- Player build (`Mono/IL2CPP`) may strip unused assemblies → fails to load encodings.
- `Including System.Text.Encoding.CodePages.dll` directly in Unity 2018 introduces Unsafe dependency → build fails.
- `GB2312/936` unavailable → MakePinYin triggers fallback → "skipping unknown syllable".

### 5. Final Solution
#### Key Points
- Abandon `System.Text.Encoding.CodePages.dll` approach (prone to issues in `Unity 2018`).
- Use `Unity` built-in `I18N`: only include `I18N.dll` and `I18N.CJK.dll`.
- Preload/initialize encodings at startup to ensure `936` is available.
#### Step 1: Place DLLs into Plugins

- Copy from Unity installation:

  - I18N.dll, I18N.CJK.dll

  - Recommended path: <UnityInstallDir>/Editor/Data/MonoBleedingEdge/lib/mono/4.5/

- Put them into `Assets/Plugins/`.

#### Step 2: Set Plugin Import Settings (crucial)

- In Unity, select each DLL → Inspector → Plugin Import Settings:

  - Uncheck `Any Platform`
  - Check `Standalone` (`Editor` optional)
  - Click `Apply`
  - Right-click DLL → `Reimport`

#### Step 3: Preserve Assemblies (avoid stripping)

```xml
1:7:Assets/link.xml
<linker>
  <assembly fullname="System.Text.Encoding.CodePages">
    <type fullname="System.Text.CodePagesEncodingProvider" preserve="all" />
  </assembly>
  <assembly fullname="I18N" preserve="all" />
  <assembly fullname="I18N.CJK" preserve="all" />
</linker>
```

- Note: Keeping CodePages entry has no effect if the DLL is not used.

#### Step 4: Register/Load Encodings at Startup

File: `Assets/ChineseGibberish/Scripts/EncodingBootstrap.cs`
```cs
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void RegisterCodePages()
{
    // Load assemblies
    try { System.Reflection.Assembly.Load("System.Text.Encoding.CodePages"); } catch {}
    try { System.Reflection.Assembly.Load("I18N"); } catch {}
    try { System.Reflection.Assembly.Load("I18N.CJK"); } catch {}

    // Register CodePagesEncodingProvider via reflection (if available)
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
            // Force load I18N.CJK CP936 (Mono route)
            var cp936Type = Type.GetType("I18N.CJK.CP936, I18N.CJK");
            if (cp936Type != null) Activator.CreateInstance(cp936Type);
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning("Failed to register CodePagesEncodingProvider via reflection: " + e.Message);
    }

    // Prewarm GB2312/936
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
#### Step 5: Pinyin Conversion Fallback

File: `Assets/ChineseGibberish/Scripts/PinYinSpell.cs`
```cs
public static string MakePinYin(string strChinese, SpellOptions options)
{
    string[] pinyin = _PinYinSplit;
    // GBK mapping requires GB2312/936
    Encoding encoding;
    try
    {
        encoding = Encoding.GetEncoding("GB2312");
    }
    catch (System.Exception ex)
    {
        Debug.LogWarning("GB2312 encoding not available, skip pinyin conversion: " + ex.Message);
        return strChinese; // graceful fallback to avoid breaking UI
    }

    byte[] local = encoding.GetBytes(strChinese);
    ...
}
```
#### Step 6: Player Settings (2018.4)

- File → Build Settings → Player Settings… → Other Settings:

  - Scripting Backend: test with Mono first, then IL2CPP
  - Api Compatibility Level: `.NET 4.x`
  - Managed Stripping Level (IL2CPP only): `Low` or `Medium`
  - Some versions/platforms may not have `Strip Engine Code` → ignore
- Ensure at least one scene is checked in "Scenes In Build" before building.

---

### 6. Build & Verification

1) Check <YourBuild>_Data/Managed/ contains:
- `I18N.dll`
- `I18N.CJK.dll`

2) First run logs should not show `Encoding 936 data could not be found`.
3) Clicking "Convert/Play" should work. If many "Skipping unknown syllable" messages appear → encoding still not applied → review steps above.

---

### 7. Common Errors & Solutions

- Build fails with Unsafe dependency:
  - Remove `System.Text.Encoding.CodePages.dll` (or use Unity 2018 compatible net46/net461 + Unsafe DLL, not recommended)
- Player still reports `936` unavailable:
  - Check `I18N.dll` / `I18N.CJK.dll` exist in `Managed/`
  - Verify Plugins import settings: Standalone checked
  - Api Compatibility = `.NET 4.x`
  - Check startup log for "register/load/prewarm" messages

---

### 8. Usage

- Open ·Assets/ChineseGibberish/TestPronounce.unity·
- Enter Chinese text → Click "点我" to see pinyin or "Play" to hear syllables
- Audio files are in `Assets/ChineseGibberish/PinYinAudio/` and can be replaced

---

### 9. Change Summary

- `PinYinSpell.cs`: switched to GB2312; added fallback if encoding unavailable
- `EncodingBootstrap.cs`: startup initialization, assembly load, CodePages registration via reflection, force load I18N.CJK.CP936, prewarm 936
- `link.xml`: preserve I18N and I18N.CJK
- `Assets/Plugins/`: add `I18N.dll` and `I18N.CJK.dll`; remove `System.Text.Encoding.CodePages.dll`

---

### 10. License

`I18N.dll` and `I18N.CJK.dll` are Unity/Mono components; follow their respective licenses.

If building on a new environment fails, please provide:
- Player logs (`*_Data/output_log.txt` or `Player.log`)
- `Managed/` directory file list
- `Editor.log` ending error section

We can provide further guidance based on logs.

## 📌 Acknowledgements

This project is adapted and extended from a [Zhihu article](https://zhuanlan.zhihu.com/p/341407630).
Thanks to the original author for sharing ideas and code references.

---

## 👩‍💻 Credits
- Development and Maintenance: [@Xinqwq](https://github.com/Xinqwq)
[@buptcuican](https://github.com/buptcuican)


---


</details>

<details open> 
<summary>🇨🇳 中文</summary>
  
一个**中文汉字 → 拼音 + 按音节顺序播放音频**的 Unity 项目。  
灵感来自「动森」风格的动物语音（Animalese）。
本项目提供了一个 `Desktop` 版本，支持在 `Windows` 上输入中文并播放对应的拼音音节音频。

---

## ✨ 功能 Features
- ⌨️ 输入你想要的中文汉字
- 🔊 按音节顺序播放音频（类似动森的 Animalese 语音）
- 🖥️ 支持 `Windows` 桌面运行
- 🛠️ `Unity` 开发，可自行扩展

---

## 📥 下载 Download
- `Windows Desktop installer`: [AnimaleseConverterSetup-v0.2.exe](https://github.com/Xinqwq/Animalese_Converter/releases/download/v0.2/AnimaleseConverterSetup_v0.2.exe)    18.3 MB
- `Windows Desktop zip`: [Animalese Converter v0.2.zip](https://github.com/Xinqwq/Animalese_Converter/releases/download/v0.2/Animalese.Converter.v0.2.zip)    22.6 MB

---

## 🖼️ 截图 Screenshots
![输入中文界面](Assets/Screenshots/Example_Interface_v0.2.png)

---


## 📚 开发笔记 / Troubleshooting Notes

本节详细记录了从 **编辑器内运行正常** 到 **Build 后报错** 的问题分析、日志证据、代码改动、依赖 DLL 配置、Player Settings、构建步骤与排错清单，确保可复现并成功打包发布。

---

### 一、运行环境
- `Unity 2018.4.28f1`（`Windows`）
- 目标平台：`PC, Mac & Linux Standalone（Windows x86_64）`

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
- 含义：`System.Text.Encoding.CodePages.dll` 在 `Unity 2018` 下引入 `System.Runtime.CompilerServices.Unsafe`，未能满足依赖，构建被终止。

---

### 四、原因分析（编辑器 OK、Player 不 OK）
- 编辑器运行时（MonoBleedingEdge）自带完整 I18N/936 支持，不裁剪，故正常。
- Player 构建（Mono/IL2CPP）启用裁剪且不保证加载相关程序集；
- 将 `System.Text.Encoding.CodePages.dll` 直接放入 `Plugins` 在 2018 版本下会引入 `Unsafe` 依赖，导致构建失败；
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

</details>
