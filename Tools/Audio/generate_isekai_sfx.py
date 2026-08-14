import csv
import html
import math
import random
import struct
import sys
import wave
from pathlib import Path


SR = 44100
TAU = math.tau


def clamp(v):
    return max(-1.0, min(1.0, v))


def env(t, duration, attack=0.01, release=0.12):
    a = min(1.0, t / max(attack, 1e-6))
    r = min(1.0, (duration - t) / max(release, 1e-6))
    return max(0.0, min(a, r))


def osc(phase, shape):
    if shape == "square":
        return 1.0 if math.sin(phase) >= 0 else -1.0
    if shape == "triangle":
        return 2.0 / math.pi * math.asin(math.sin(phase))
    if shape == "saw":
        return 2.0 * ((phase / TAU) % 1.0) - 1.0
    return math.sin(phase)


def tone(duration, f0, f1=None, shape="sine", volume=0.55, attack=0.005,
         release=0.12, vibrato=0.0, seed=0):
    f1 = f0 if f1 is None else f1
    rng = random.Random(seed)
    phase = rng.random() * TAU
    out = []
    n = int(duration * SR)
    for i in range(n):
        t = i / SR
        p = t / duration
        freq = f0 * ((f1 / f0) ** p) if f0 > 0 and f1 > 0 else f0 + (f1 - f0) * p
        freq *= 1.0 + vibrato * math.sin(TAU * 6.2 * t)
        phase += TAU * freq / SR
        out.append(osc(phase, shape) * volume * env(t, duration, attack, release))
    return out


def noise(duration, volume=0.35, attack=0.002, release=0.12, seed=0,
          smooth=0.0):
    rng = random.Random(seed)
    out = []
    state = 0.0
    for i in range(int(duration * SR)):
        t = i / SR
        raw = rng.uniform(-1.0, 1.0)
        state = state * smooth + raw * (1.0 - smooth)
        out.append(state * volume * env(t, duration, attack, release))
    return out


def mix(*tracks):
    size = max(len(x) for x in tracks)
    out = [0.0] * size
    for track in tracks:
        for i, value in enumerate(track):
            out[i] += value
    peak = max(max(abs(v) for v in out), 0.01)
    gain = min(0.94 / peak, 1.0)
    return [v * gain for v in out]


def sequence(parts, gap=0.0):
    out = []
    silence = [0.0] * int(gap * SR)
    for part in parts:
        out.extend(part)
        out.extend(silence)
    return out


def echo(track, delay=0.09, decay=0.35, repeats=2):
    out = track[:] + [0.0] * int(delay * SR * repeats)
    step = int(delay * SR)
    for rep in range(1, repeats + 1):
        for i, value in enumerate(track):
            out[i + step * rep] += value * (decay ** rep)
    return [clamp(v) for v in out]


def pad(track, before=0.0, after=0.0):
    return [0.0] * int(before * SR) + track + [0.0] * int(after * SR)


def chord(duration, notes, shape="sine", volume=0.2, seed=0):
    return mix(*(tone(duration, n, shape=shape, volume=volume,
                       attack=0.02, release=0.2, seed=seed + i)
                 for i, n in enumerate(notes)))


def write_wav(path, mono):
    path.parent.mkdir(parents=True, exist_ok=True)
    # Mild stereo widening without external samples.
    delay = 19
    frames = bytearray()
    for i, left in enumerate(mono):
        right = mono[i - delay] * 0.92 if i >= delay else left * 0.92
        frames.extend(struct.pack("<hh", int(clamp(left) * 32767), int(clamp(right) * 32767)))
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(frames)


def hit(seed, heavy=False):
    base = 62 if heavy else 104
    return mix(tone(0.34 if heavy else 0.22, base, 34 if heavy else 62,
                    "sine", 0.75, release=0.18, seed=seed),
               noise(0.16, 0.36 if heavy else 0.25, release=0.12,
                     smooth=0.72, seed=seed))


def chime(notes, seed, shape="sine", gap=0.035):
    parts = [tone(0.13, n, n * 1.01, shape, 0.42, release=0.09, seed=seed + i)
             for i, n in enumerate(notes)]
    return echo(sequence(parts, gap), 0.075, 0.28, 2)


def siren(seed, high=False):
    duration = 2.4
    out = []
    phase = 0.0
    rng = random.Random(seed)
    base = 610 if high else 430
    for i in range(int(duration * SR)):
        t = i / SR
        freq = base + (180 if high else 130) * math.sin(TAU * (0.78 + seed * .02) * t)
        phase += TAU * freq / SR
        out.append((math.sin(phase) + 0.25 * math.sin(phase * 2.01)) * 0.34 * env(t, duration, .04, .1))
    return out


def engine(seed, driving=False):
    duration = 2.0
    base = 46 + seed * 3 + (24 if driving else 0)
    out = []
    p1 = p2 = p3 = 0.0
    for i in range(int(duration * SR)):
        t = i / SR
        wobble = 1 + 0.018 * math.sin(TAU * (3.0 + seed * .2) * t)
        p1 += TAU * base * wobble / SR
        p2 += TAU * base * 2 * wobble / SR
        p3 += TAU * base * 3 * wobble / SR
        pulse = 0.48 * math.sin(p1) + 0.25 * math.sin(p2) + 0.12 * math.sin(p3)
        out.append(pulse * 0.68)
    # Tiny crossfade makes preview and looping cleaner.
    fade = int(.05 * SR)
    for i in range(fade):
        a = i / fade
        out[i] *= a
        out[-1-i] *= a
    return out


def make(cue, v):
    seed = 1000 + v * 97 + sum(map(ord, cue))
    shift = [0.92, 1.0, 1.10][v - 1]

    if cue == "engine_start":
        return mix(tone(1.1, 35 * shift, 92 * shift, "saw", .42, .03, .18, seed=seed),
                   noise(.35, .2, .01, .2, seed, .83))
    if cue == "engine_idle_loop": return engine(v, False)
    if cue == "engine_drive_loop": return engine(v, True)
    if cue == "engine_accelerate": return tone(1.15, 62 * shift, 190 * shift, "saw", .42, .02, .14, .01, seed)
    if cue == "brake_skid": return mix(tone(.72, 1900 * shift, 620 * shift, "sine", .18, .01, .14, seed=seed), noise(.72, .45, .01, .16, seed, .42))
    if cue == "truck_horn": return chord(.62, [185 * shift, 246 * shift], "square", .25, seed)

    if cue == "resident_hit_light": return hit(seed, False)
    if cue == "resident_hit_heavy": return hit(seed, True)
    if cue == "resident_launch_whoosh": return mix(tone(.42, 210 * shift, 760 * shift, "sine", .24, release=.18, seed=seed), noise(.36, .22, release=.15, seed=seed, smooth=.55))
    if cue == "resident_reaction_cartoon": return tone(.38, 360 * shift, 820 * shift, "triangle", .38, .01, .12, .035, seed)
    if cue == "soul_release": return echo(mix(tone(.62, 260 * shift, 930 * shift, "sine", .36, release=.22, seed=seed), tone(.62, 390 * shift, 1395 * shift, "sine", .18, release=.25, seed=seed+1)), .1, .32, 3)
    if cue == "soul_collect": return chime([523*shift, 784*shift, 1047*shift], seed)
    if cue == "xp_collect": return chime([660*shift, 880*shift], seed, "triangle", .018)
    if cue == "combo_step": return chime([440*shift, 554*shift, 659*shift, 880*shift], seed, "square", .012)

    if cue == "level_up": return chime([392*shift, 523*shift, 659*shift, 784*shift, 1047*shift], seed, "triangle", .045)
    if cue == "upgrade_point": return chime([494*shift, 659*shift, 988*shift], seed)
    if cue == "upgrade_health": return mix(hit(seed, True), pad(chime([262*shift, 392*shift, 523*shift], seed), .08))
    if cue == "upgrade_speed": return echo(tone(.48, 260*shift, 1450*shift, "sine", .4, release=.14, seed=seed), .065, .28, 2)
    if cue == "upgrade_size": return mix(tone(.65, 95*shift, 48*shift, "sine", .65, release=.22, seed=seed), pad(chime([196*shift, 262*shift], seed), .2))
    if cue == "custom_unlock": return chime([523*shift, 659*shift, 784*shift, 1047*shift], seed)
    if cue == "insufficient_resource": return sequence([tone(.14, 180*shift, 145*shift, "square", .3, release=.06, seed=seed), tone(.18, 145*shift, 112*shift, "square", .3, release=.08, seed=seed+1)], .025)
    if cue == "permanent_point": return echo(chord(.75, [220*shift, 330*shift, 440*shift, 660*shift], "sine", .2, seed), .13, .38, 3)

    if cue == "wanted_level_up": return sequence([tone(.16, 310*shift, 370*shift, "square", .28, release=.06, seed=seed), tone(.28, 430*shift, 620*shift, "square", .3, release=.1, seed=seed+1)], .03)
    if cue == "enemy_spawn": return mix(tone(.58, 95*shift, 42*shift, "saw", .4, release=.22, seed=seed), noise(.28, .28, release=.18, seed=seed, smooth=.7))
    if cue == "police_siren_loop": return siren(v, v == 3)
    if cue == "truck_damage": return mix(hit(seed, v == 3), tone(.23, 760*shift, 210*shift, "triangle", .24, release=.1, seed=seed))
    if cue == "low_health_warning": return sequence([tone(.18, 770*shift, 650*shift, "square", .28, release=.05, seed=seed), tone(.18, 770*shift, 650*shift, "square", .28, release=.05, seed=seed+1)], .12)
    if cue == "truck_destroy": return echo(mix(hit(seed, True), noise(.9, .48, .005, .35, seed, .78), tone(.9, 78*shift, 28*shift, "sine", .58, release=.4, seed=seed)), .14, .28, 3)
    if cue == "respawn": return chime([196*shift, 262*shift, 392*shift, 523*shift], seed, "triangle", .055)

    if cue == "portal_available": return echo(chime([440*shift, 660*shift, 880*shift], seed), .12, .35, 3)
    if cue == "portal_summon": return echo(mix(tone(1.25, 74*shift, 520*shift, "saw", .26, .02, .25, .01, seed), tone(1.25, 148*shift, 1040*shift, "sine", .28, .02, .3, seed=seed+1)), .12, .35, 3)
    if cue == "portal_loop": return mix(tone(2.0, 110*shift, 110*shift, "sine", .28, .04, .04, .018, seed), tone(2.0, 330*shift, 330*shift, "sine", .14, .04, .04, .025, seed=seed+1))
    if cue == "portal_enter": return echo(mix(tone(.75, 140*shift, 1700*shift, "sine", .4, release=.22, seed=seed), noise(.55, .24, release=.22, seed=seed, smooth=.58)), .08, .32, 3)
    if cue == "world_arrival": return echo(mix(hit(seed, True), chime([262*shift, 392*shift, 659*shift], seed)), .11, .32, 2)
    if cue == "rebirth_available": return echo(chord(.85, [262*shift, 392*shift, 523*shift, 784*shift], "sine", .18, seed), .14, .4, 3)
    if cue == "rebirth_execute": return echo(mix(tone(1.25, 85*shift, 1250*shift, "sine", .42, .02, .32, seed=seed), chord(1.1, [262*shift, 392*shift, 659*shift], "triangle", .16, seed)), .13, .36, 4)
    if cue == "blessing_reveal": return echo(chime([330*shift, 494*shift, 659*shift, 988*shift], seed), .1, .4, 4)
    if cue == "blessing_select": return mix(hit(seed, False), pad(chime([523*shift, 784*shift, 1047*shift], seed), .05))
    if cue == "rare_blessing": return echo(mix(chord(1.25, [196*shift, 294*shift, 392*shift, 588*shift], "sine", .2, seed), pad(chime([784*shift, 988*shift, 1319*shift], seed), .25)), .16, .42, 4)
    if cue == "skill_ready": return chime([660*shift, 990*shift], seed, "triangle", .025)
    if cue == "skill_activate": return echo(mix(hit(seed, v == 3), tone(.52, 180*shift, 1350*shift, "saw", .3, release=.18, seed=seed)), .07, .27, 2)

    if cue == "ui_select": return tone(.075, 640*shift, 720*shift, "triangle", .25, release=.025, seed=seed)
    if cue == "ui_confirm": return chime([520*shift, 780*shift], seed, "triangle", .012)
    if cue == "ui_cancel": return tone(.16, 520*shift, 310*shift, "triangle", .3, release=.06, seed=seed)
    if cue == "ui_menu_open": return echo(tone(.24, 260*shift, 720*shift, "sine", .28, release=.08, seed=seed), .055, .22, 1)
    if cue == "ui_error": return sequence([tone(.11, 170*shift, 155*shift, "square", .28, release=.04, seed=seed)] * 2, .035)
    if cue == "game_start": return chime([262*shift, 330*shift, 392*shift, 523*shift, 784*shift], seed, "triangle", .035)
    raise KeyError(cue)


GROUPS = {
    "01_Vehicle": [
        ("engine_start", "엔진 시동"), ("engine_idle_loop", "엔진 공회전 루프"),
        ("engine_drive_loop", "주행 엔진 루프"), ("engine_accelerate", "가속"),
        ("brake_skid", "브레이크/타이어 스키드"), ("truck_horn", "트럭 경적")],
    "02_Resident_Impact": [
        ("resident_hit_light", "가벼운 주민 충돌"), ("resident_hit_heavy", "강한 주민 충돌"),
        ("resident_launch_whoosh", "주민이 날아가는 효과"), ("resident_reaction_cartoon", "비언어적 코믹 반응"),
        ("soul_release", "영혼 방출"), ("soul_collect", "영혼 획득"),
        ("xp_collect", "경험치 획득"), ("combo_step", "연속 충돌 콤보")],
    "03_Progression": [
        ("level_up", "레벨업"), ("upgrade_point", "업그레이드 포인트 획득"),
        ("upgrade_health", "체력 업그레이드"), ("upgrade_speed", "속도 업그레이드"),
        ("upgrade_size", "크기 업그레이드"), ("custom_unlock", "커스터마이징 해금"),
        ("insufficient_resource", "재화 부족"), ("permanent_point", "영구 포인트 획득")],
    "04_Wanted_Combat": [
        ("wanted_level_up", "지명수배 등급 상승"), ("enemy_spawn", "적 출현"),
        ("police_siren_loop", "경찰 사이렌 루프"), ("truck_damage", "트럭 피격"),
        ("low_health_warning", "체력 부족 경고"), ("truck_destroy", "트럭 파괴"),
        ("respawn", "리스폰")],
    "05_Portal_Rebirth": [
        ("portal_available", "포탈 사용 가능"), ("portal_summon", "포탈 소환"),
        ("portal_loop", "포탈 유지 루프"), ("portal_enter", "포탈 진입"),
        ("world_arrival", "새 세계 도착"), ("rebirth_available", "환생 가능"),
        ("rebirth_execute", "환생 실행"), ("blessing_reveal", "축복 공개"),
        ("blessing_select", "축복 선택"), ("rare_blessing", "고등급 축복"),
        ("skill_ready", "액티브 스킬 준비"), ("skill_activate", "축복 스킬 발동")],
    "06_UI": [
        ("ui_select", "UI 선택"), ("ui_confirm", "UI 확인"),
        ("ui_cancel", "UI 취소"), ("ui_menu_open", "메뉴 열기"),
        ("ui_error", "UI 오류"), ("game_start", "게임 시작")],
}


def main():
    if len(sys.argv) != 2:
        raise SystemExit("Usage: generate_isekai_sfx.py OUTPUT_DIR")
    root = Path(sys.argv[1])
    rows = []
    for group, cues in GROUPS.items():
        for cue, korean in cues:
            for variant in range(1, 4):
                filename = f"{cue}_v{variant:02d}.wav"
                rel = Path(group) / filename
                write_wav(root / rel, make(cue, variant))
                rows.append({
                    "category": group,
                    "event_id": cue,
                    "korean_name": korean,
                    "variant": variant,
                    "file": rel.as_posix(),
                    "source": "Original procedural synthesis for Isekai_truck",
                    "license": "Project-owned original / no external samples",
                })
    with (root / "SFX_CATALOG.csv").open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)
    readme = """# Isekai Truck SFX Candidate Library

이 폴더에는 게임 핵심 이벤트 47종에 대한 후보 3개씩, 총 141개의 WAV 파일이 있습니다.

## 후보 선택 기준

- `v01`: 대체로 낮고 무거운 음색
- `v02`: 중간 음역의 균형 잡힌 기본안
- `v03`: 대체로 높고 밝거나 더 강한 음색
- `*_loop_*`: 반복 재생을 전제로 만든 루프 후보

`SFX_REVIEW.html`을 브라우저로 열면 이벤트별 후보를 나란히 재생할 수 있습니다.
선택한 파일은 원본 파일명을 유지하고, 게임 코드에서는 `event_id`를 기준으로 연결하는 방식을 권장합니다.

## 형식

- WAV PCM
- Stereo
- 44.1 kHz
- 16-bit

## 출처

외부 음원이나 샘플을 포함하지 않고 이 프로젝트를 위해 절차적으로 합성했습니다.
세부 목록은 `SFX_CATALOG.csv`에 기록되어 있습니다.

## 한계와 다음 단계

UI, 성장, 포탈, 환생, 마법 계열은 실제 게임용 후보로 사용할 수 있습니다.
엔진, 타이어, 충돌, 사이렌 계열은 합성된 프로토타입이므로 최종 출시 전 실제 녹음 기반 음원과 비교하는 것을 권장합니다.
주민 음성은 특정 인물의 목소리가 아닌 비언어적 합성 반응음만 포함합니다.
"""
    (root / "README.md").write_text(readme, encoding="utf-8")

    cards = []
    labels = {1: "v01 · 낮고 무거움", 2: "v02 · 균형형", 3: "v03 · 높고 밝음/강함"}
    for group, cues in GROUPS.items():
        cards.append(f'<section><h2>{html.escape(group)}</h2>')
        for cue, korean in cues:
            players = []
            for variant in range(1, 4):
                filename = f"{cue}_v{variant:02d}.wav"
                rel = f"{group}/{filename}"
                players.append(
                    '<div class="variant">'
                    f'<b>{html.escape(labels[variant])}</b>'
                    f'<audio controls preload="none" src="{html.escape(rel)}"></audio>'
                    f'<code>{html.escape(filename)}</code>'
                    '</div>'
                )
            cards.append(
                '<article>'
                f'<h3>{html.escape(korean)} <small>{html.escape(cue)}</small></h3>'
                f'<div class="variants">{"".join(players)}</div>'
                '</article>'
            )
        cards.append('</section>')
    review = f"""<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Isekai Truck SFX Review</title>
<style>
:root{{--bg:#10131a;--panel:#191e29;--line:#303849;--text:#f2f5fa;--muted:#9ba7ba;--accent:#76d7c4}}
*{{box-sizing:border-box}} body{{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,sans-serif}}
header{{position:sticky;top:0;z-index:2;background:#10131aee;border-bottom:1px solid var(--line);padding:18px 4vw;backdrop-filter:blur(10px)}}
main{{width:min(1200px,92vw);margin:28px auto 80px}} h1{{margin:0;font-size:24px}} header p,small{{color:var(--muted)}}
section{{margin:34px 0}} article{{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px;margin:10px 0}}
h2{{color:var(--accent)}} h3{{margin:0 0 12px}} h3 small{{font-weight:400;margin-left:8px}}
.variants{{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px}} .variant{{display:grid;gap:7px;background:#11151d;padding:12px;border-radius:8px}}
audio{{width:100%;height:34px}} code{{font-size:12px;color:#bec8d8;overflow-wrap:anywhere}}
@media(max-width:760px){{.variants{{grid-template-columns:1fr}}}}
</style></head><body>
<header><h1>이세계 트럭 키우기 · SFX 후보 리뷰</h1><p>47개 이벤트 × 후보 3개 = 141 WAV · 마음에 드는 파일명을 기록하세요.</p></header>
<main>{''.join(cards)}</main></body></html>"""
    (root / "SFX_REVIEW.html").write_text(review, encoding="utf-8")
    # Validate the deliverables so broken/truncated WAVs never enter the project.
    failures = []
    durations = []
    for path in root.rglob("*.wav"):
        try:
            with wave.open(str(path), "rb") as wf:
                duration = wf.getnframes() / wf.getframerate()
                durations.append(duration)
                if (wf.getnchannels(), wf.getsampwidth(), wf.getframerate()) != (2, 2, SR):
                    failures.append(f"Unexpected format: {path}")
                if duration <= 0:
                    failures.append(f"Empty audio: {path}")
        except Exception as exc:
            failures.append(f"Unreadable WAV {path}: {exc}")
    if len(durations) != len(rows):
        failures.append(f"Expected {len(rows)} WAV files, found {len(durations)}")
    if failures:
        raise RuntimeError("\n".join(failures))
    print(
        f"Generated and verified {len(rows)} WAV files in {root} "
        f"({min(durations):.3f}s to {max(durations):.3f}s, stereo 44.1kHz/16-bit)"
    )


if __name__ == "__main__":
    main()
