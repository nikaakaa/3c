from pathlib import Path
import json
import math

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
fixture = json.loads((OUT / 'releasing-right-input.json').read_text(encoding='utf-8'))
report = json.loads((OUT / 'releasing-action-native.json').read_text(encoding='utf-8-sig'))
columns = fixture['columns']['main']
frames = []
for frame in fixture['frames']:
    row = dict(zip(columns, frame['main']))
    def number(key): return float(row[key])
    def vector(key): return [number(key + suffix) for suffix in ['.x', '.y', '.z']]
    hip, knee, ankle = [vector('leg/leg-pose/original-' + name) for name in ['hip', 'knee', 'ankle']]
    a = [x-y for x, y in zip(hip, knee)]
    b = [x-y for x, y in zip(ankle, knee)]
    original_bend = 180 - math.degrees(math.acos(max(-1, min(1, sum(x*y for x,y in zip(a,b)) / math.sqrt(sum(x*x for x in a)*sum(x*x for x in b))))))
    frames.append({'frame': frame['frame'], 'dt': number('input/presentation-delta-seconds'),
        'root': vector('physical-body/pose-root-world-position'), 'ankle': vector('foot/source-ankle-position'),
        'sole': vector('foot/resolved/core/effective-sole'), 'sourceWeight': number('input/foot-step-observation/source-weight'),
        'state': row['foot/foot-motion/core/state'], 'originalBend': original_bend,
        'solvedBend': number('leg/leg-pose/solved-bend-degrees'), 'targetReach': number('leg/leg-pose/target-extension-ratio')})
data = {'recorded': frames, 'report': report}
payload = json.dumps(data, ensure_ascii=False, separators=(',', ':')).replace('</', '<\\/')
template = Path(__file__).with_name('releasing-native-template.html').read_text(encoding='utf-8')
(OUT / 'releasing-action-native.html').write_text(template.replace('@@DATA@@', payload), encoding='utf-8')
print('Rendered releasing scene with actual execution state and original captured knee/foot data')
