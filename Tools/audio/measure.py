"""Measures the loudness of the soundtrack files: duration, integrated LUFS (BS.1770), RMS and peak. Usage: python measure.py <files...>"""
import sys
import numpy as np
import soundfile as sf
import pyloudnorm as pyln

for path in sys.argv[1:]:
    data, rate = sf.read(path, always_2d=True)
    meter = pyln.Meter(rate)
    lufs = meter.integrated_loudness(data)
    rms = 20 * np.log10(np.sqrt(np.mean(data ** 2)) + 1e-12)
    peak = 20 * np.log10(np.max(np.abs(data)) + 1e-12)
    print(f"{path.split('/')[-1]:28s} {len(data)/rate:7.2f}s {rate}Hz ch={data.shape[1]} LUFS={lufs:6.1f} RMS={rms:6.1f}dBFS peak={peak:6.1f}dBFS")
