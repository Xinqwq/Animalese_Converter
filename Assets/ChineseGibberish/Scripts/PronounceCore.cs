﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PinYinSpell;
using System.Collections;
using System.Text;
using System.Linq;

[System.Serializable]
public class AllSoundClips
{
    public AudioClip _b;
    public AudioClip _p;
    public AudioClip _m;
    public AudioClip _f;
    public AudioClip _d;
    public AudioClip _t;
    public AudioClip _n;
    public AudioClip _l;
    public AudioClip _g;
    public AudioClip _k;
    public AudioClip _h;
    public AudioClip _j;
    public AudioClip _q;
    public AudioClip _x;
    public AudioClip _zh;
    public AudioClip _ch;
    public AudioClip _sh;
    public AudioClip _r;
    public AudioClip _z;
    public AudioClip _c;
    public AudioClip _s;
    public AudioClip _y;
    public AudioClip _w;
    public AudioClip _a;
    public AudioClip _o;
    public AudioClip _e;
    public AudioClip _i;
    public AudioClip _u;
    public AudioClip _v;
    public AudioClip _ai;
    public AudioClip _ei;
    public AudioClip _ui;
    public AudioClip _ao;
    public AudioClip _ou;
    public AudioClip _iu;
    public AudioClip _ie;
    public AudioClip _ve;
    public AudioClip _er;
    public AudioClip _an;
    public AudioClip _en;
    public AudioClip _in;
    public AudioClip _un;
    public AudioClip _vn;
    public AudioClip _ang;
    public AudioClip _eng;
    public AudioClip _ing;
    public AudioClip _ong;
    public AudioClip __;

    public Dictionary<string, AudioClip> ClipsDict()
    {
        return new Dictionary<string, AudioClip> {
            { "b",_b }, { "p",_p }, { "m",_m }, { "f",_f }, { "d",_d }, { "t",_t },
            { "n",_n }, { "l",_l }, { "g",_g }, { "k",_k }, { "h",_h }, { "j",_j },
            { "q",_q }, { "x",_x }, { "zh",_zh }, { "ch",_ch }, { "sh",_sh }, { "r",_r },
            { "z",_z }, { "c",_c }, { "s",_s }, { "y",_y }, { "w",_w }, { "a",_a },
            { "o",_o }, { "e",_e }, { "i",_i }, { "u",_u }, { "v",_v }, { "ai",_ai },
            { "ei",_ei }, { "ui",_ui }, { "ao",_ao }, { "ou",_ou }, { "iu",_iu }, { "ie",_ie },
            { "ve",_ve }, { "er",_er }, { "an",_an }, { "en",_en }, { "in",_in }, { "un",_un },
            { "vn",_vn }, { "ang",_ang }, { "eng",_eng }, { "ing",_ing }, { "ong",_ong }, {"_", __}
        };
    }
}

public class PronounceCore : MonoBehaviour
{
    AudioSource audioSource;

    string[] syllables = { "a", "ai", "an", "ang", "ao", "b", "c", "ch", "d", "e"
            , "ei", "en", "eng", "er", "f", "g", "h", "i", "ie", "in", "ing", "iu", "j"
            , "k", "l", "m", "n", "o", "ong", "ou", "p", "q", "r", "s", "sh", "t"
            , "u", "ui", "un", "v", "ve", "vn", "w", "x", "y", "z", "zh", "_", };

    Dictionary<string, AudioClip> clips;
    Coroutine lastCo;

    public string audioPath = "PinYinAudio";
    [Tooltip("每个音节开头去掉一部分")]
    [Range(0, 0.5f)]
    public float trimBegin = 0;
    [Tooltip("每个音节结尾去掉一部分")]
    [Range(0.5f, 1f)]
    public float trimEnd = 1;

    public AllSoundClips allClips;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        clips = allClips.ClipsDict();
    }

    public string ConvertPinYin(string chinese)
    {
        string s = Spell.PreProcess(chinese);
        string ret = Spell.MakePinYin(s, SpellOptions.AddSpace | SpellOptions.EnableUnicodeLetter);
        return ret;
    }

    // 汉字输入过滤方法
    private string FilterChineseInput(string chinese)
    {
        // 过滤掉常见的汉字符号，这些符号通常不用于断句且会导致拼音识别错误
        string filteredText = chinese;
        
        // 常见的汉字符号字典
        Dictionary<string, string> symbolReplacements = new Dictionary<string, string>
        {
            // 引号类
            { "【", "" }, { "】", "" }, // 方括号
            { "《", "" }, { "》", "" }, // 书名号
            { "「", "" }, { "」", "" }, // 直角引号
            { "“", "" }, { "”", "" }, // 中文引号
            { "’", "" }, { "‘", "" }, // 中文单引号
            { "\"", "" }, // 英文引号
            
            // 其他符号
            { "（", " " }, { "）", " " }, // 中文括号 -> 空格
            { "(", " " }, { ")", " " }, // 英文括号 -> 空格
            { "[", " " }, { "]", " " }, // 方括号 -> 空格
            
            // 连接符
            { "——", " " }, // 长破折号 -> 空格
            { "—", " " }, // 破折号 -> 空格
            { "－", " " }, // 全角连字符 -> 空格
            { "-", " " }, // 半角连字符 -> 空格
            
            // 省略号
            { "……", " " }, // 中文省略号 -> 空格
            { "⋯⋯", " " }, // 中文省略号变体 -> 空格
            
            // 特殊符号
            { "※", "" }, { "★", "" }, { "☆", "" }, { "●", "" }, { "○", "" },{ "*", "" },{ "#", "" },
            { "◆", "" }, { "◇", "" }, { "■", "" }, { "□", "" },
            { "▲", "" }, { "△", "" }, { "▼", "" }, { "▽", "" },
            
            // 数学符号
            { "＋", "" }, { "×", "" }, { "÷", "" },
            { "≈", "" }, { "≠", "" }, { "≤", "" }, { "≥", "" },
            
            // 货币和其他符号
            { "￥", "元" }, { "$", "" }, { "€", "" }, { "£", "" },
            { "％", "" }, { "%", "" }, { "‰", "" },
            
            // 单位符号
            { "℃", "度" }, { "℉", "度" }, { "°", "度" },
            
            // 特殊标记
            { "§", "" }, { "№", "" },
            { "〒", "" }, { "◯", "" }, { "◎", "" },
            
            // 全角数字和字母（可选）
            { "０", "0" }, { "１", "1" }, { "２", "2" }, { "３", "3" }, { "４", "4" },
            { "５", "5" }, { "６", "6" }, { "７", "7" }, { "８", "8" }, { "９", "9" }
        };
        
        // 执行替换
        foreach (var replacement in symbolReplacements)
        {
            filteredText = filteredText.Replace(replacement.Key, replacement.Value);
        }
        
        // 清理多余的空格
        filteredText = System.Text.RegularExpressions.Regex.Replace(filteredText, @"\s+", " ");
        filteredText = filteredText.Trim();
        
        Debug.Log($"原始输入: '{chinese}' -> 过滤后: '{filteredText}'");
        return filteredText;
    }

    // 新的转换方法，支持压缩策略
    public string ConvertPinYinWithCompression(string chinese)
    {
        // 先过滤输入文本
        string filteredChinese = FilterChineseInput(chinese);
        List<string> sentences = SplitIntoSentences(filteredChinese);
        List<string> compressedSentences = new List<string>();
        Debug.Log("句子数量: " + sentences.Count);
        foreach (string sentence in sentences)
        {
            if (string.IsNullOrEmpty(sentence.Trim()))
                continue;
                
            // 统计汉字数量（排除标点符号和空格）
            int charCount = CountChineseCharacters(sentence);
            Debug.Log("汉字数量: " + charCount);
            // 转换为拼音
            string pinyin = ConvertPinYin(sentence);
            Debug.Log("拼音: " + pinyin);
            // 应用压缩策略
            string compressedPinyin = CompressPronunciation(pinyin, charCount);
            Debug.Log("压缩拼音: " + compressedPinyin);
            compressedSentences.Add(compressedPinyin);
        }
        // 在句子间添加特殊的停顿标记
        string result = string.Join(" ^_^ ", compressedSentences);
        Debug.Log("读出的结果: " + result);
        return result;
    }

    // 统计汉字数量的方法
    private int CountChineseCharacters(string text)
    {
        int count = 0;
        foreach (char c in text)
        {
            // 判断是否为汉字（CJK统一汉字）
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                count++;
            }
        }
        return count;
    }

    // 拼音分析：分离声母和韵母
    private (string consonant, string vowel) AnalyzePinyin(string pinyin)
    {
        if (string.IsNullOrEmpty(pinyin))
            return ("", "");

        // 常见的声母（辅音）
        string[] consonants = {
            "b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h",
            "j", "q", "x", "zh", "ch", "sh", "r", "z", "c", "s", "y", "w"
        };

        // 按照长度排序，先匹配长的声母
        consonants = consonants.OrderByDescending(c => c.Length).ToArray();

        foreach (string consonant in consonants)
        {
            if (pinyin.StartsWith(consonant))
            {
                string vowel = pinyin.Substring(consonant.Length);
                Debug.Log("分离结果：" + consonant + " " + vowel);
                return (consonant, vowel);
            }
        }
        Debug.Log("分离结果：" + pinyin + " 没有声母");
        // 如果没有声母，整个都是韵母
        return ("", pinyin);
    }

    // 创建交替发音：处理已经分隔好的声母-韵母交替格式
    private string CreateAlternatingPronunciation(List<string> pinyinList)
    {
        if (pinyinList.Count <= 1)
            return string.Join(" ", pinyinList);

        List<string> result = new List<string>();
        bool expectConsonant = true; // 期望下一个是声母（开始交替）
        
        List<string> charPairs = new List<string>();
        
        // 配对处理：每两个音节组成一个汉字
        for (int i = 0; i < pinyinList.Count; i += 2)
        {
            if (i + 1 < pinyinList.Count)
            {
                // 一对：声母 + 韵母
                string pair = pinyinList[i] + " " + pinyinList[i + 1];
                charPairs.Add(pair);
            }
            else
            {
                // 最后一个单个拼音，是完整的汉字拼音
                charPairs.Add(pinyinList[i]);
            }
        }
        
        Debug.Log("配对结果: " + string.Join(" | ", charPairs));
        
        // 对配对后的结果应用交替规律
        for (int i = 0; i < charPairs.Count; i++)
        {
            if (i == charPairs.Count - 1)
            {
                // 最后一个汉字：使用完整发音
                result.Add(charPairs[i]);
            }
            else
            {
                string[] pair = charPairs[i].Split(' ');
                if (pair.Length == 2 && expectConsonant)
                {
                    // 期望声母，取第一个（声母）
                    result.Add(pair[0]);
                }
                else if (pair.Length == 2 && !expectConsonant)
                {
                    // 期望韵母，取第二个（韵母）
                    result.Add(pair[1]);
                }
                else
                {
                    // 配对不完整，跳过这个汉字
                    Debug.Log("跳过不完整的汉字配对: " + charPairs[i]);
                }
                
                // 切换期望状态
                expectConsonant = !expectConsonant;
            }
        }
        
        return string.Join(" ", result);
    }

    // 从汉字配对创建交替发音
    private string CreateAlternatingPronunciationFromCharPairs(List<string> charPairs)
    {
        List<string> result = new List<string>();
        bool expectConsonant = true; // 期望下一个是声母（开始交替）
        
        Debug.Log("汉字配对输入: " + string.Join(" | ", charPairs));
        
        // 对配对后的结果应用交替规律
        for (int i = 0; i < charPairs.Count; i++)
        {
            if (i == charPairs.Count - 1)
            {
                // 最后一个汉字：使用完整发音
                result.Add(charPairs[i]);
            }
            else
            {
                string[] pair = charPairs[i].Split(' ');
                if (pair.Length == 2 && expectConsonant)
                {
                    // 期望声母，取第一个（声母）
                    result.Add(pair[0]);
                }
                else if (pair.Length == 2 && !expectConsonant)
                {
                    // 期望韵母，取第二个（韵母）
                    result.Add(pair[1]);
                }
                else
                {
                    // 配对不完整，跳过这个汉字
                    Debug.Log("跳过不完整的汉字配对: " + charPairs[i]);
                }
                
                // 切换期望状态
                expectConsonant = !expectConsonant;
            }
        }
        
        string finalResult = string.Join(" ", result);
        Debug.Log("交替发音结果: " + finalResult);
        return finalResult;
    }

    // 分割句子的方法
    private List<string> SplitIntoSentences(string chinese)
    {
        List<string> sentences = new List<string>();
        // 中文标点符号
        char[] punctuationMarks = { '。', '！', '？', '；', '，', '、', '.', '!', '?', ';', ',' };
        
        StringBuilder currentSentence = new StringBuilder();
        
        foreach (char c in chinese)
        {
            if (punctuationMarks.Contains(c))
            {
                if (currentSentence.Length > 0)
                {
                    sentences.Add(currentSentence.ToString().Trim());
                    currentSentence.Clear();
                }
            }
            else
            {
                currentSentence.Append(c);
            }
        }
        
        // 添加最后一个句子（如果有的话）
        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString().Trim());
        }
        
        return sentences;
    }

    // 压缩发音的方法：按汉字单元压缩
    private string CompressPronunciation(string pinyin, int originalCharCount)
    {
        string[] syllables = pinyin.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        // 先配对成汉字单元
        List<string> charPairs = new List<string>();
        for (int i = 0; i < syllables.Length; i += 2)
        {
            if (i + 1 < syllables.Length)
            {
                // 一对：声母 + 韵母
                string pair = syllables[i] + " " + syllables[i + 1];
                charPairs.Add(pair);
            }
            else
            {
                // 最后一个单个音节，是完整的汉字拼音
                charPairs.Add(syllables[i]);
            }
        }
        
        Debug.Log($"配对后汉字数量: {charPairs.Count}, 原汉字数量: {originalCharCount}");
        
        if (originalCharCount <= 6)
        {
            // 不大于6个字，应用交替发音策略
            return CreateAlternatingPronunciationFromCharPairs(charPairs);
        }
        else
        {
            // 大于6个字，先压缩至7个汉字，再应用交替发音策略
            if (charPairs.Count <= 7)
            {
                return CreateAlternatingPronunciationFromCharPairs(charPairs); // 如果本身就不超过7个汉字，直接应用交替发音
            }
            
            // 保留前6个汉字和最后1个汉字的拼音
            List<string> compressedChars = new List<string>();
            
            // 添加前6个汉字的拼音
            for (int i = 0; i < 6 && i < charPairs.Count; i++)
            {
                compressedChars.Add(charPairs[i]);
            }
            
            // 添加最后一个汉字的拼音
            if (charPairs.Count > 6)
            {
                compressedChars.Add(charPairs[charPairs.Count - 1]);
            }
            
            Debug.Log($"压缩后汉字数量: {compressedChars.Count}");
            return CreateAlternatingPronunciationFromCharPairs(compressedChars);
        }
    }

    public void Speak(string pinyin)
    {
        pinyin = pinyin.Trim();
        string[] ss = pinyin.Split();
        if (lastCo != null)
        {
            StopCoroutine(lastCo);
        }
        lastCo = StartCoroutine(CoSpeak(ss));
    }

    IEnumerator CoSpeak(string[] source)
    {
        foreach (string p in source)
        {
            // 检查是否是停顿标记
            if (p == "^_^")
            {
                Debug.Log("句子间停顿：0.3秒");
                yield return new WaitForSeconds(0.3f);
                continue;
            }
            
            if (!clips.ContainsKey(p) || clips[p] == null)
            {
                Debug.Log("跳过未知音节：" +  p + ";");
                continue;
            }
            AudioClip clip = clips[p];
            audioSource.clip = clip;
            audioSource.time = clip.length * trimBegin;
            audioSource.Play();
			float speedFactor = 1.3f; // >1 = faster，<1 = slower
			yield return new WaitForSeconds((clip.length / audioSource.pitch * trimEnd - audioSource.time) / speedFactor);

            //yield return new WaitForSeconds(clip.length / audioSource.pitch * trimEnd - audioSource.time);
        }
    }
}