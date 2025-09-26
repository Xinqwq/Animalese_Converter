using System;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Registers code page encodings (e.g., GB2312 codepage 936) before any encoding usage.
/// Attach to a bootstrap GameObject in the first scene or use RuntimeInitializeOnLoadMethod.
/// </summary>
public static class EncodingBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterCodePages()
    {
        // 主动加载相关程序集，避免未被自动加载导致的类型解析失败
        try { System.Reflection.Assembly.Load("System.Text.Encoding.CodePages"); } catch {}
        try { System.Reflection.Assembly.Load("I18N"); } catch {}
        try { System.Reflection.Assembly.Load("I18N.CJK"); } catch {}

        // 通过反射注册 CodePagesEncodingProvider，避免编译期硬依赖导致找不到命名空间
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
                // 尝试强制加载 I18N.CJK（旧版 Mono 的代码页实现）
                var cp936Type = Type.GetType("I18N.CJK.CP936, I18N.CJK");
                if (cp936Type != null)
                {
                    // 通过构造触发模块加载
                    Activator.CreateInstance(cp936Type);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to register CodePagesEncodingProvider via reflection: " + e.Message);
        }

        // 预热 GB2312/936，若失败记录日志
        try { _ = Encoding.GetEncoding("GB2312"); }
        catch (Exception)
        {
            try { _ = Encoding.GetEncoding(936); }
            catch (Exception)
            {
                // 再次尝试：若 I18N.CJK 存在，此时再触发一次构造
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
}


