from pathlib import Path
import argparse
import hashlib
import json
import math

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--results', type=Path, default=ROOT / 'docs/diagnostics/foot-placement/ik-tests')
args = parser.parse_args()
OUT = args.results
TEMP = ROOT / '3cDemo/Client/3C_Client/Temp/FootRotationComparison'

def distance(a, b):
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))

def bend(row):
    hip, knee, ankle = row['fixedHip'], row['originalKnee'], row['originalAnkle']
    upper, lower = distance(hip, knee), distance(knee, ankle)
    def angle(target):
        length = distance(hip, target)
        if length > upper + lower or length < abs(upper - lower):
            return None
        cosine = (upper * upper + lower * lower - length * length) / (2 * upper * lower)
        return 180 - math.degrees(math.acos(max(-1, min(1, cosine))))
    return {'originalAnimationBendDegrees': angle(ankle), 'fixedHipRequiredBendDegrees': angle(row['ankle'])}

report = {'status': 'running', 'scope': '完整连续单脚窗口的跨版本比较；固定录制骨盆后的Hip，尚未接真实双脚骨盆反馈，不代表完整FBBIK', 'historicalCommit': '552f13083', 'candidateCommit': '15894ee7b', 'cases': []}
for name in ['live-switch-right', 'stored-no-contact-left', 'stored-contact-rotation-right']:
    pair = {variant: json.loads(OUT.joinpath(name + '-rotation-' + variant + '-goal.json').read_text(encoding='utf-8')) for variant in ['historical', 'current']}
    checks = []
    def check(label, passed, actual, required):
        checks.append({'label': label, 'passed': passed, 'actual': actual, 'required': required})
    old, now = pair['historical']['historical'], pair['current']['current']
    check('两版实际函数实验完成', all(r['status'] == 'passed' for r in pair.values()), [r['status'] for r in pair.values()], 'passed / passed')
    check('同一完整窗口', [r['frame'] for r in old['rows']] == [r['frame'] for r in now['rows']], [old['frames'], now['frames']], '逐帧身份一致')
    check('暖机0GC', old['allocatedBytes'] == now['allocatedBytes'] == 0, [old['allocatedBytes'], now['allocatedBytes']], '0 / 0 B')
    check('全窗有效旋转步长不增加', now['maximumRotationStepDegrees'] <= old['maximumRotationStepDegrees'] + .001, now['maximumRotationStepDegrees'], old['maximumRotationStepDegrees'] + .001)
    check('全窗动画相对修正步长不增加', now['maximumAnimationRelativeCorrectionStepDegrees'] <= old['maximumAnimationRelativeCorrectionStepDegrees'] + .001, now['maximumAnimationRelativeCorrectionStepDegrees'], old['maximumAnimationRelativeCorrectionStepDegrees'] + .001)
    check('全窗脚位步长不增加', now['maximumSoleStepMeters'] <= old['maximumSoleStepMeters'] + .0002, now['maximumSoleStepMeters'], old['maximumSoleStepMeters'] + .0002)
    check('全窗水平脚位步长不增加', now['maximumHorizontalSoleStepMeters'] <= old['maximumHorizontalSoleStepMeters'] + .0002, now['maximumHorizontalSoleStepMeters'], old['maximumHorizontalSoleStepMeters'] + .0002)
    check('保持实际查询覆盖', now['observedFinalFrames'] == old['observedFinalFrames'], now['observedFinalFrames'], old['observedFinalFrames'])
    check('实际查询未新增正穿透', now['maximumFinalPenetration'] <= .0002, now['maximumFinalPenetration'], '<=0.2mm')
    weight_changes = [b['frame'] for a, b in zip(old['rows'], now['rows']) if a['authorWeight'] != b['authorWeight'] or a['positionWeight'] != b['positionWeight']]
    check('保持位置作者预算', not weight_changes, weight_changes, '逐帧原输入及正式权重一致')
    reach_regressions = [{'frame': b['frame'], 'historical': a['fixedHipReachRatio'], 'current': b['fixedHipReachRatio'], 'increase': b['fixedHipReachRatio'] - a['fixedHipReachRatio']} for a, b in zip(old['rows'], now['rows']) if b['fixedHipReachRatio'] > a['fixedHipReachRatio'] + .0002]
    check('保留逐帧固定髋弯曲余量', not reach_regressions, reach_regressions, '同帧伸展比增量<=0.0002；不是只检查是否超1')
    check('不增加固定髋超伸时间', now['fixedHipOverreachDurationSeconds'] <= old['fixedHipOverreachDurationSeconds'] + .000001, now['fixedHipOverreachDurationSeconds'], old['fixedHipOverreachDurationSeconds'] + .000001)
    state_changes = [b['frame'] for a, b in zip(old['rows'], now['rows']) if a['state'] != b['state'] or a['hasContactAnchor'] != b['hasContactAnchor']]
    check('保持生命周期状态与锚点退出', not state_changes, state_changes, '逐帧状态及实际退出一致')
    geometry = [{**{'frame': a['frame']}, 'historical': bend(a), 'current': bend(b)} for a, b in zip(old['rows'], now['rows'])]
    sources = {}
    for variant, commit in [('historical', '552f13083'), ('current', '15894ee7b')]:
        folder = TEMP / (variant + '-' + commit)
        manifest = json.loads(folder.joinpath('native-goal-source-manifest.json').read_text(encoding='utf-8'))
        manifest['compiledEntrySha256'] = hashlib.sha256(folder.joinpath('ThirdPersonClient.Editor.dll').read_bytes()).hexdigest()
        sources[variant] = manifest
    report['cases'].append({'name': name, 'status': 'passed' if all(c['passed'] for c in checks) else 'failed', 'checks': checks, 'historical': pair['historical'], 'current': pair['current'], 'geometry': geometry, 'sources': sources})
report['status'] = 'passed' if all(c['status'] == 'passed' for c in report['cases']) else 'failed'
report['failedChecks'] = [{'case': c['name'], **test} for c in report['cases'] for test in c['checks'] if not test['passed']]
OUT.joinpath('rotation-business-comparison.json').write_text(json.dumps(report, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
print('Cross-version business result:', report['status'])
for c in report['cases']:
    print(c['name'], c['status'], [x['label'] for x in c['checks'] if not x['passed']])
raise SystemExit(0 if report['status'] == 'passed' else 1)
