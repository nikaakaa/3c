from pathlib import Path
import argparse
import hashlib
import json
import math

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--results', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()

def distance(a, b):
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))

def geometry(row, side):
    prefix = 'paired' if side == 'left' else ''
    def key(name):
        return prefix + name[0].upper() + name[1:] if prefix else name
    original_hip = row[key('originalHip')]
    original_knee = row[key('originalKnee')]
    original_ankle = row[key('originalAnkle')]
    hip = row['pairedHip'] if side == 'left' else row['fixedHip']
    ankle = row[key('ankle')]
    upper, lower = distance(original_hip, original_knee), distance(original_knee, original_ankle)
    def bend(length):
        if length > upper + lower or length < abs(upper - lower):
            return None
        cosine = (upper * upper + lower * lower - length * length) / (2 * upper * lower)
        return 180 - math.degrees(math.acos(max(-1, min(1, cosine))))
    return {'frame': row['frame'], 'dt': row['dt'], 'upperBoneLength': upper, 'lowerBoneLength': lower,
            'originalBendDegrees': bend(distance(original_hip, original_ankle)),
            'requiredBendDegrees': bend(distance(hip, ankle)),
            'reachRatio': row['pairedReachRatio'] if side == 'left' else row['fixedHipReachRatio']}

def with_rates(rows):
    previous = None
    for row in rows:
        row['originalBendSpeed'] = None
        row['requiredBendSpeed'] = None
        row['extraBendSpeed'] = None
        row['bendCorrectionSpeed'] = None
        if previous is not None:
            row['originalBendSpeed'] = abs(row['originalBendDegrees'] - previous['originalBendDegrees']) / row['dt']
            if row['requiredBendDegrees'] is not None and previous['requiredBendDegrees'] is not None:
                row['requiredBendSpeed'] = abs(row['requiredBendDegrees'] - previous['requiredBendDegrees']) / row['dt']
                row['extraBendSpeed'] = max(0, row['requiredBendSpeed'] - row['originalBendSpeed'])
                current_correction = row['requiredBendDegrees'] - row['originalBendDegrees']
                old_correction = previous['requiredBendDegrees'] - previous['originalBendDegrees']
                row['bendCorrectionSpeed'] = abs(current_correction - old_correction) / row['dt']
        previous = row
    return rows

commits = ['552f13083', '15894ee7b', '8c0e878f8']
reports = {}
for commit in commits:
    file = args.results / ('live-switch-continued-right-pelvis-' + commit + '-goal.json')
    raw = json.loads(file.read_text(encoding='utf-8'))
    summary = raw['historical' if commit == commits[0] else 'current']
    data = {'commit': commit, 'status': raw['status'], 'scope': raw['scope'], 'hipScope': raw['fixedHipScope'],
            'summary': summary, 'goalResultSha256': hashlib.sha256(file.read_bytes()).hexdigest(),
            'sources': json.loads(args.results.joinpath('pelvis-' + commit + '-source-manifest.json').read_text(encoding='utf-8-sig'))}
    data['geometry'] = {side: with_rates([geometry(row, side) for row in summary['rows']]) for side in ['left', 'right']}
    for side in ['left', 'right']:
        values = data['geometry'][side]
        data[side + 'Metrics'] = {'maximumReachRatio': max(v['reachRatio'] for v in values),
            'overreachFrames': sum(v['reachRatio'] > 1 for v in values),
            'overreachDurationSeconds': sum(v['dt'] for v in values if v['reachRatio'] > 1),
            'maximumExtraBendSpeed': max(v['extraBendSpeed'] or 0 for v in values),
            'maximumBendCorrectionSpeed': max(v['bendCorrectionSpeed'] or 0 for v in values)}
    reports[commit] = data

baseline = reports[commits[0]]
comparisons = []
for commit in commits[1:]:
    current = reports[commit]
    checks = []
    def check(label, passed, actual, required):
        checks.append({'label': label, 'passed': passed, 'actual': actual, 'required': required})
    check('两版真实函数完成', baseline['status'] == current['status'] == 'passed', [baseline['status'], current['status']], 'passed / passed')
    check('暖机0GC', baseline['summary']['allocatedBytes'] == current['summary']['allocatedBytes'] == 0, current['summary']['allocatedBytes'], '0B')
    old_rows, new_rows = baseline['summary']['rows'], current['summary']['rows']
    check('同一完整窗口', [x['frame'] for x in old_rows] == [x['frame'] for x in new_rows], len(new_rows), '2192～2249连续58帧')
    for side in ['right', 'left']:
        old_geometry, new_geometry = baseline['geometry'][side], current['geometry'][side]
        regressions = [{'frame': b['frame'], 'historical': a['reachRatio'], 'current': b['reachRatio'], 'increase': b['reachRatio'] - a['reachRatio']} for a, b in zip(old_geometry, new_geometry) if b['reachRatio'] > a['reachRatio'] + .0002]
        check(side + '：保留同帧弯曲余量', not regressions, regressions, '伸展比增量<=0.0002')
        old_metrics, new_metrics = baseline[side + 'Metrics'], current[side + 'Metrics']
        check(side + '：不增加超伸时间', new_metrics['overreachDurationSeconds'] <= old_metrics['overreachDurationSeconds'] + .000001,
              new_metrics['overreachDurationSeconds'], old_metrics['overreachDurationSeconds'] + .000001)
        check(side + '：不增加额外膝角需求速率', new_metrics['maximumExtraBendSpeed'] <= old_metrics['maximumExtraBendSpeed'] + .001,
              new_metrics['maximumExtraBendSpeed'], old_metrics['maximumExtraBendSpeed'] + .001)
        check(side + '：不增加膝角修正变化率', new_metrics['maximumBendCorrectionSpeed'] <= old_metrics['maximumBendCorrectionSpeed'] + .001,
              new_metrics['maximumBendCorrectionSpeed'], old_metrics['maximumBendCorrectionSpeed'] + .001)
        penetration = 'pairedFinalPenetration' if side == 'left' else 'finalPenetration'
        available = 'pairedFinalSupportAvailable' if side == 'left' else 'finalSupportAvailable'
        check(side + '：保持实际净空覆盖', all(a[available] == b[available] for a, b in zip(old_rows, new_rows)), sum(r[available] for r in new_rows), sum(r[available] for r in old_rows))
        hits = [r[penetration] for r in new_rows if r[available]]
        check(side + '：无新增正穿透', max(hits) <= .0002, max(hits), '<=0.2mm；无命中为null')
        rotation_key = 'pairedRelativeRotationStepDegrees' if side == 'left' else 'animationRelativeCorrectionStepDegrees'
        old_max = max(r[rotation_key] or 0 for r in old_rows)
        new_max = max(r[rotation_key] or 0 for r in new_rows)
        check(side + '：动画相对旋转修正步长不增加', new_max <= old_max + .001, new_max, old_max + .001)
        sole_key = 'pairedSoleStepMeters' if side == 'left' else 'soleStepMeters'
        old_max = max(r[sole_key] or 0 for r in old_rows)
        new_max = max(r[sole_key] or 0 for r in new_rows)
        check(side + '：脚位最大步长不增加', new_max <= old_max + .0002, new_max, old_max + .0002)
        state_key = 'pairedState' if side == 'left' else 'state'
        anchor_key = 'pairedHasContactAnchor' if side == 'left' else 'hasContactAnchor'
        changes = [b['frame'] for a, b in zip(old_rows, new_rows) if a[state_key] != b[state_key] or a[anchor_key] != b[anchor_key]]
        check(side + '：原状态和实际锚点退出一致', not changes, changes, '不移走异常事件或截掉退出阶段')
    comparisons.append({'commit': commit, 'status': 'passed' if all(x['passed'] for x in checks) else 'failed', 'checks': checks})

result = {'status': 'passed' if all(c['status'] == 'passed' for c in comparisons) else 'failed',
    'scope': '真实双脚Goal、重新计算的Pelvis与反馈；原动画骨段全部取同一录制post三点，候选Hip取新骨盆结果；没有完整FBBIK',
    'firstFrame': 2192, 'lastFrame': 2249, 'reports': reports, 'comparisons': comparisons}
args.output.write_text(json.dumps(result, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
print('Bilateral pelvis business result:', result['status'])
for c in comparisons:
    print(c['commit'], c['status'], [x['label'] for x in c['checks'] if not x['passed']])
raise SystemExit(0 if result['status'] == 'passed' else 1)
