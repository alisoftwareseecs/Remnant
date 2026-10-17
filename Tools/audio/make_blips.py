"""Cuts ONE clean dialogue blip out of SFX/sfx_dialogue_source.mp3 and makes the Shadow's deeper, wronger version.

The source is a run of fast blips with no silence between them (the amplitude only dips a few percent), so the blips
are found as the valley-to-valley stretches of a 3 ms RMS envelope that hold exactly one hump. The stretch of 45-65 ms
whose two ends are quietest (relative to its peak) is cut, snapped to zero crossings, and given 2 ms fades so it cannot
click. Output: sfx_blip.wav (mono, 44.1 kHz, 16 bit).

The Shadow blip is baked from that blip: resampled to 0.75x speed (about 5 semitones lower and 33 % slower), low-passed
at 2.6 kHz, driven through a soft clipper and given a little bit-crushed grit. sfx_blip_shadow.wav.
Run:  python make_blips.py
"""
from pathlib import Path
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfilt, find_peaks, resample_poly

SFX = Path(r"D:/Unity/Remnant/Assets/_Remnant/Audio/SFX")
data, sr = sf.read(SFX / "sfx_dialogue_source.mp3", always_2d=True)
x = data[:, 0].astype(np.float64)

w = int(sr * 0.003)
env = np.sqrt(np.convolve(x ** 2, np.ones(w) / w, mode="same"))
peaks, _ = find_peaks(env, height=env.max() * 0.25, distance=int(sr * 0.025))

# valleys between neighbouring peaks
valleys = [int(a + env[a:b].argmin()) for a, b in zip(peaks[:-1], peaks[1:])]
best = None
for i in range(len(valleys) - 1):
    a, b = valleys[i], valleys[i + 1]
    length = (b - a) / sr
    if not 0.045 <= length <= 0.065:
        continue
    peak = env[a:b].max()
    score = (env[a] + env[b]) / (2 * peak)
    if best is None or score < best[0]:
        best = (score, a, b)

_, a, b = best


def snap(i, direction):
    # nearest zero crossing (rising for the start, falling for the end) within 2 ms
    for k in range(int(sr * 0.002)):
        for j in (i + direction * k, i - direction * k):
            if 0 < j < len(x) - 1 and x[j - 1] <= 0 < x[j]:
                return j
    return i


a, b = snap(a, 1), snap(b, 1)
blip = x[a:b].copy()
fade = int(sr * 0.002)
ramp = np.linspace(0, 1, fade)
blip[:fade] *= ramp
blip[-fade:] *= ramp[::-1]
blip /= np.abs(blip).max()
blip *= 0.7   # about -3 dBFS peak; the cue gain sets the loudness
sf.write(SFX / "sfx_blip.wav", blip.astype(np.float32), sr, subtype="PCM_16")
print("blip: %.1f ms, from %.3f s to %.3f s of the source (score %.2f)" % (len(blip) / sr * 1000, a / sr, b / sr, best[0]))

# ---- Shadow blip
slow = resample_poly(blip, 4, 3)                  # 0.75x speed = -4.98 semitones, 33 % slower
sos = butter(4, 2600, btype="low", fs=sr, output="sos")
dark = sosfilt(sos, slow)
dark /= np.abs(dark).max()
driven = np.tanh(dark * 3.2) / np.tanh(3.2)       # soft clip: grit
crushed = np.repeat(driven[::4], 4)[:len(driven)]  # ~11 kHz sample-and-hold, mixed in lightly
shadow = 0.8 * driven + 0.2 * crushed
shadow = sosfilt(butter(2, 3200, btype="low", fs=sr, output="sos"), shadow)
fade = int(sr * 0.003)
ramp = np.linspace(0, 1, fade)
shadow[:fade] *= ramp
shadow[-fade:] *= ramp[::-1]
shadow /= np.abs(shadow).max()
shadow *= 0.7
sf.write(SFX / "sfx_blip_shadow.wav", shadow.astype(np.float32), sr, subtype="PCM_16")
print("shadow blip: %.1f ms" % (len(shadow) / sr * 1000))
