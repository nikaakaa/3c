from pathlib import Path
import json
import math
import hashlib
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'docs/diagnostics/foot-placement/ik-tests'
TEMP = ROOT/'3cDemo/Client/3C_Client/Temp/FootReleasingNative/height-blend'


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def magnitude(v): return math.sqrt(sum(x*x for x in v))


for kind in ['native','component-poses','goal','ik']:
    shutil.copyfile(TEMP/('releasing-business-'+kind+'.json'), OUT/('releasing-height-'+kind+'.json'))
baseline_native = read(OUT/'releasing-business-native.json')
native = read(OUT/'releasing-height-native.json')
baseline_goal = read(OUT/'releasing-business-goal.json')
goal = read(OUT/'releasing-height-goal.json')
ik = read(OUT/'releasing-height-ik.json')
assert native['status'] == goal['status'] == 'passed'
for old, current in zip(baseline_native['rows'], native['rows']):
    assert old['frame'] == current['frame']
    for name in ['currentLeftSample','currentRightSample']:
        assert {k:v for k,v in old[name].items() if k != 'footHeight'} == {k:v for k,v in current[name].items() if k != 'footHeight'}
assert baseline_goal['historical']['rows'] == goal['historical']['rows']
assert read(OUT/'releasing-business-component-poses.json')['rows'] == read(OUT/'releasing-height-component-poses.json')['rows']
summary = {'status': 'rejected-height-blend', 'firstFrame': 2024, 'lastFrame': 2056, 'seedFrame': 2023,
    'scope': '同一33帧第三版；实际Native Job与正式BlendFootHeights，Foot和FBBIK仍冻结69a36d339；整体历史校准失败',
    'nativeNonHeightFieldsIdenticalToBaseline': True, 'fullNativeComponentPosesIdenticalToBaseline': True,
    'historicalFootRowsIdenticalToBaseline': True, 'footPassed': True, 'ikPassed': False,
    'nativeAllocatedBytes': native['allocatedBytesInWarmedSequence'], 'footAllocatedBytes': goal['current']['allocatedBytes'],
    'ikAllocatedBytes': ik['current']['allocatedBytes'], 'sides': {}}
summary['rejection'] = '2031至2036早期Landing超伸不变，2037至2044右脚Goal伸展比增大，完整超伸仍8帧；最大额外修正步长0.301888增至0.302999m。业务劣化由已校准Goal几何直接证明，不依赖未校准膝角。'
summary['reachComparison'] = [{'frame': a['frame'], 'baseline':a['fixedHipReachRatio'], 'candidate':b['fixedHipReachRatio']} for a,b in zip(baseline_goal['current']['rows'],goal['current']['rows'])]
for side in ['left','right']:
    rows = goal['current']['rows']
    solved = ik['current']['rows'][1:]
    prefix = '' if side == 'right' else 'paired'
    reach = 'fixedHipReachRatio' if side == 'right' else 'pairedReachRatio'
    penetration = 'finalPenetration' if side == 'right' else 'pairedFinalPenetration'
    deltas = [magnitude(r[side+'CorrectionDelta']) for r in rows[1:]]
    summary['sides'][side] = {'maximumCorrectionDelta': max(deltas), 'maximumCorrectionDeltaFrame': rows[1+deltas.index(max(deltas))]['frame'],
        'totalCorrectionVariation': sum(deltas), 'maximumCorrectionSpeed': max(r[side+'CorrectionSpeed'] for r in rows[1:]),
        'targetOverreachFrames': sum(r[reach]>1 for r in rows), 'maximumTargetReach': max(r[reach] for r in rows),
        'maximumPositivePenetration': max(max(0,r[penetration]) for r in rows if r[penetration] is not None),
        'nearStraightSolvedFrames': sum(r[side+'BendDegrees']<1 for r in solved), 'kneeComparisonCalibrated': False}

manifest = read(TEMP/'source-manifest.json')
summary['candidateSourceSha256'] = manifest['candidateSourceSha256']
archive = OUT/'releasing-height-source-snapshot.zip'
with zipfile.ZipFile(archive) as zipped:
    for relative, expected in manifest['candidateSourceSha256'].items():
        assert hashlib.sha256(zipped.read('production/'+relative)).hexdigest() == expected
    assert hashlib.sha256(zipped.read('executed-assemblies/ThirdPersonClient.Editor.dll')).hexdigest() == sha(TEMP/'ThirdPersonClient.Editor.dll')
summary['archiveRecovery'] = '二次生成与生产撤回并行，误截断原ZIP；从保留编译输入和冻结Git内容恢复三个生产成员，逐项匹配原manifest SHA后完整生成并原子替换。ZIP新SHA不冒充原53c43e874295fa4092939f1069781ea3ea3647aa81167443fc0b234d24bae1bd。'
summary['sourceSnapshotSha256'] = sha(archive)
summary['resultSha256'] = {kind: sha(OUT/('releasing-height-'+kind+'.json')) for kind in ['native','component-poses','goal','ik']}
(OUT/'releasing-height-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:summary[k] for k in ['status','nativeAllocatedBytes','footAllocatedBytes','ikAllocatedBytes','sides']},ensure_ascii=False))
