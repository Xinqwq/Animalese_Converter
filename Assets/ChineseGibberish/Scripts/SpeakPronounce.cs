using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PinYinSpell;

public class SpeakPronounce : MonoBehaviour
{
    public InputField inputChinese;
    public InputField inputPinYin;

    PronounceCore core;

    void Start()
    {
        core = GetComponent<PronounceCore>();
    }

    // 汉字转拼音
    public void OnConvert()
    {
        string t = core.ConvertPinYin(inputChinese.text);
        inputPinYin.text = t;
    }

    // 播放拼音
    public void OnSpeak()
    {
        core.Speak(inputPinYin.text);
    }

    // 转换并播放
    public void ConvertAndSpeak()
    {
        string py = core.ConvertPinYin(inputChinese.text);
        core.Speak(py);
    }
}
