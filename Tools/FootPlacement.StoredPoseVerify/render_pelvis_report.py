from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
data = json.loads(OUT.joinpath('pelvis-rotation-business-comparison.json').read_text(encoding='utf-8'))
template = Path(__file__).with_name('pelvis-report-template.html').read_text(encoding='utf-8')
payload = json.dumps(data, ensure_ascii=False, separators=(',', ':')).replace('</', '<\\/')
OUT.joinpath('live-rotation-response.html').write_text(template.replace('@@DATA@@', payload), encoding='utf-8')
print('Updated same Live report with both feet, actual pelvis and frozen failed candidates')
