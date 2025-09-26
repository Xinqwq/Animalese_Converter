# -*- coding: utf-8 -*-
from pydub import AudioSegment  # 音频处理模块pydub
from pydub.silence import detect_silence, detect_nonsilent
import time

# 文件名
wav = AudioSegment.from_file("声母韵母22.wav")

# silence_thresh是判定静音的阈值
segment_times = detect_nonsilent(wav, min_silence_len=1, silence_thresh=-55, seek_step=1)
segments = []
for beg,end in segment_times:
    if end - beg > 100:
        seg = wav[beg : end]
        segments.append(seg)

file_names = ["b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h", "j", "q", "x", "zh", "ch", "sh", "r", "z", "c", "s", "y", "w", "a", "o", "e", "i", "u", "v", "ai", "ei", "ui", "ao", "ou", "iu", "ie", "ve", "er", "an", "en", "in", "un", "vn", "ang", "eng", "ing", "ong"]

print(len(segments))

# 输出到当前路径的output文件夹下
for i,seg in enumerate(segments):
    seg.export("output1/"+file_names[i]+".wav", format="wav")
    time.sleep(1)       # 等1秒是为了让文件的修改时间不同，方便后期排序


