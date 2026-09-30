from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
report = json.loads(OUT.joinpath('rotation-business-comparison.json').read_text(encoding='utf-8'))
template = Path(__file__).with_name('rotation-report-template.html').read_text(encoding='utf-8')
payload = json.dumps(report, ensure_ascii=False, separators=(',', ':')).replace('</', '<\\/')
OUT.joinpath('live-rotation-response.html').write_text(template.replace('@@DATA@@', payload), encoding='utf-8')
print('Saved Live rotation response and Stored regression report, with actual failed business checks')
