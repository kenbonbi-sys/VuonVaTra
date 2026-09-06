"""Original, deterministic Vườn Nhỏ sound design; no external samples.

Run `python SourceArt/Audio/generate_sfx.py --write` from the project to regenerate.
Run with --verify to compare the five source and Unity WAVs without writing.
Python standard library only. All files are PCM16, mono, 48 kHz.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import random
import struct
import wave

RATE = 48_000
SEED = 9062026
TAU = 2 * math.pi
SOURCE = Path(__file__).resolve().parent
PROJECT = SOURCE.parents[1]
EXPORT = PROJECT / "Assets" / "Audio"


def pluck(t: float, hz: float, decay: float, brightness: float = 0.18) -> float:
    if t < 0:
        return 0.0
    attack = min(1.0, t / 0.004)
    fundamental = math.sin(TAU * hz * t)
    partial = brightness * math.sin(TAU * hz * 2.003 * t) * math.exp(-t * 24)
    return attack * math.exp(-t * decay) * (fundamental + partial)


def design(name: str, duration: float, seed: int) -> list[float]:
    rng = random.Random(seed)
    samples = []
    filtered = 0.0
    for i in range(round(duration * RATE)):
        t = i / RATE
        filtered += 0.12 * (rng.uniform(-1, 1) - filtered)
        if name == "Click":
            # Gentle wooden tap with a small ceramic top note.
            value = 0.9 * pluck(t, 620, 58) + 0.22 * pluck(t, 1390, 90)
            value += filtered * math.exp(-t * 110) * 0.17
        elif name == "Plant":
            # Low soil pat followed by a rounded rising sprout tone.
            chirp_phase = TAU * (260 * t + 0.5 * 1150 * t * t)
            value = 0.62 * pluck(t, 165, 24, 0.08)
            value += 0.30 * math.sin(chirp_phase) * min(1, t / 0.008) * math.exp(-t * 19)
            value += filtered * math.exp(-t * 32) * 0.38
        elif name == "Harvest":
            # A short leaf rustle and two warm, ascending plucks.
            value = 0.75 * pluck(t, 523.25, 17) + 0.48 * pluck(t - 0.045, 783.99, 20)
            value += filtered * min(1, t / 0.004) * math.exp(-t * 23) * 0.30
        elif name == "Brew":
            # Tiny pour/bubble gesture resolving into a cup-like bell.
            value = filtered * min(1, t / 0.006) * math.exp(-t * 11) * 0.50
            value += 0.32 * pluck(t, 310, 36, 0.05)
            value += 0.25 * pluck(t - 0.040, 440, 32, 0.05)
            value += 0.70 * pluck(t - 0.090, 1046.50, 16, 0.12)
        else:
            # C-major-sixth arpeggio: optimistic but brief enough for repeat use.
            value = (0.70 * pluck(t, 523.25, 13)
                     + 0.60 * pluck(t - 0.065, 659.25, 13)
                     + 0.52 * pluck(t - 0.130, 783.99, 13)
                     + 0.44 * pluck(t - 0.195, 1046.50, 14))
        samples.append(value)

    # Remove DC before smooth global edge fades; force both boundary samples to zero.
    dc = sum(samples) / len(samples)
    for i in range(len(samples)):
        attack = min(1.0, i / (RATE * 0.003))
        release = min(1.0, (len(samples) - 1 - i) / (RATE * 0.025))
        samples[i] = (samples[i] - dc) * math.sin(attack * math.pi / 2) ** 2 * math.sin(release * math.pi / 2) ** 2
    peak = max(abs(x) for x in samples)
    target_peak = {"Click": 0.34, "Plant": 0.38, "Harvest": 0.40, "Brew": 0.37, "Upgrade": 0.40}[name]
    return [x * target_peak / peak for x in samples]


def encode(samples: list[float]) -> bytes:
    pcm = [round(max(-1, min(1, value)) * 32767) for value in samples]
    stream = io.BytesIO()
    with wave.open(stream, "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(struct.pack("<" + "h" * len(pcm), *pcm))
    return stream.getvalue()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--write", action="store_true", help="Regenerate only the five named WAVs and their QA manifest.")
    mode.add_argument("--verify", action="store_true", help="Verify source/export bytes against this generator, no writes.")
    args = parser.parse_args()
    records = []
    for index, (name, duration) in enumerate((("Click", .09), ("Plant", .19), ("Harvest", .24), ("Brew", .34), ("Upgrade", .48))):
        samples = design(name, duration, SEED + index)
        payload = encode(samples)
        filename = "SFX_" + name + ".wav"
        for folder in (SOURCE, EXPORT):
            destination = folder / filename
            if args.write:
                folder.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(payload)
            elif not destination.is_file() or destination.read_bytes() != payload:
                raise SystemExit("Verification failed: " + str(destination))
        record = {
            "cue": name, "file": filename, "source": "Original deterministic synthesis; no third-party samples",
            "sample_rate": RATE, "channels": 1, "bits": 16, "duration_seconds": duration,
            "peak_dbfs": round(20 * math.log10(max(abs(x) for x in samples)), 2),
            "rms_dbfs": round(20 * math.log10(math.sqrt(sum(x * x for x in samples) / len(samples))), 2),
            "first_last_sample_zero": samples[0] == 0 and samples[-1] == 0,
            "sha256": hashlib.sha256(payload).hexdigest(),
        }
        records.append(record)
        print(f"{filename}: {duration:.2f}s, peak {record['peak_dbfs']:.2f} dBFS, verified PCM16 mono 48kHz")
    if args.write:
        (SOURCE / "audio-register.json").write_text(json.dumps({"generator": "generate_sfx.py", "seed": SEED, "assets": records}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
