from pathlib import Path
import hashlib
import json
import math
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
TEMP = ROOT / '3cDemo/Client/3C_Client/Temp/FootReleasingNative'
GOAL = ROOT / '3cDemo/Client/3C_Client/Temp/FootRotationComparison/historical-69a36d339'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


names = ['releasing-business-native.json', 'releasing-business-component-poses.json', 'releasing-business-goal.json',
         'releasing-business-ik.json', 'releasing-business-ik-attempt-native-pose-calibration.json',
         'releasing-business-ik-diagnostic-recorded-leg.json', 'releasing-business-ik-diagnostic-recorded-goals.json']
for name in names:
    shutil.copyfile(TEMP / name, OUT / name)

goal = read(OUT / 'releasing-business-goal.json')
ik = read(OUT / 'releasing-business-ik.json')
summary = {'status': 'failed-ik-calibration', 'firstFrame': 2024, 'lastFrame': 2056, 'seedFrame': 2023,
           'executionLayer': 'Unity内真实函数实验；未执行正式Runner、Replay或自动输入',
           'footCommit': '69a36d339', 'nativeCommit': 'd6d6ab9f9', 'variants': {},
           'reportCorrection': '旧报告序列结束后读取复用路径池页，使全33帧Accepted误写false。执行当帧路径正确；结果已改为当帧复制标量，重跑Goal不变。Prediction.State/Event与Observation是值类型标量，GroundPath引用未再延后消费。',
           'limitation': '完整Native/Foot计算通过；FBBIK实际两版求解完成但2033历史六关节点误差0.240472mm超过0.2mm，不能判业务通过；KCC未来端口为录制边界；未复现历史previous-dot/Revision等元数据'}
for variant in ['historical', 'current']:
    rows = goal[variant]['rows']
    solved = ik[variant]['rows'][1:]
    values = {}
    for side, prefix in [('right', ''), ('left', 'paired')]:
        delta = [math.sqrt(sum(v*v for v in row[side+'CorrectionDelta'])) for row in rows[1:]]
        weight = 'positionWeight' if side == 'right' else 'pairedPositionWeight'
        contact = 'finalSupportAvailable' if side == 'right' else 'pairedFinalSupportAvailable'
        penetration = 'finalPenetration' if side == 'right' else 'pairedFinalPenetration'
        reach = 'fixedHipReachRatio' if side == 'right' else 'pairedReachRatio'
        state = 'state' if side == 'right' else 'pairedState'
        values[side] = {'maximumCorrectionDelta': max(delta), 'maximumCorrectionDeltaFrame': rows[1+delta.index(max(delta))]['frame'],
                        'totalCorrectionVariation': sum(delta), 'maximumCorrectionSpeed': max(row[side+'CorrectionSpeed'] for row in rows[1:]),
                        'activeGoalFrames': sum(row[weight]>0 for row in rows), 'observedFrames': sum(row[contact] for row in rows),
                        'maximumPositivePenetration': max(max(0, row[penetration]) for row in rows if row[penetration] is not None),
                        'targetOverreachFrames': sum(row[reach]>1 for row in rows), 'maximumTargetReach': max(row[reach] for row in rows),
                        'states': [{'frame': row['frame'], 'state': row[state]} for row in rows],
                        'nearStraightSolvedFrames': sum(row[side+'BendDegrees']<1 for row in solved),
                        'nearStraightSolvedDuration': sum(a['dt'] for a,b in zip(rows,solved) if b[side+'BendDegrees']<1),
                        'kneeComparisonCalibrated': False}
    summary['variants'][variant] = {'nativeAllocatedBytes': read(OUT/'releasing-business-native.json')['allocatedBytesInWarmedSequence'],
        'footAllocatedBytes': goal[variant]['allocatedBytes'], 'ikAllocatedBytes': ik[variant]['allocatedBytes'],
        'historicalJointMaximumError': max(row['maxRecordedJointError'] for row in ik['historical']['rows']), **values}

archive = OUT/'releasing-business-source-snapshot.zip'
manifests = [TEMP/'source-manifest.json', GOAL/'native-goal-source-manifest.json']
replacement = TEMP/'releasing-business-source-snapshot-pending.zip'
with zipfile.ZipFile(replacement, 'w', compression=zipfile.ZIP_DEFLATED) as zipped:
    added = set()
    for manifest in manifests:
        zipped.write(manifest, 'manifests/'+manifest.parent.name+'-'+manifest.name)
        for relative, expected in read(manifest)['sources'].items():
            path = ROOT/relative
            assert sha(path) == expected, relative
            if relative not in added:
                zipped.write(path, relative)
                added.add(relative)
    for path in [TEMP/'ThirdPersonClient.Editor.dll', GOAL/'ThirdPersonClient.Editor.dll']:
        zipped.write(path, 'executed-assemblies/'+path.parent.name+'/'+path.name)
replacement.replace(archive)
summary['evidenceSha256'] = {name: sha(OUT/name) for name in names + ['releasing-business-right-input.json']}
summary['sourceSnapshotSha256'] = sha(archive)
(OUT/'releasing-business-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print('Saved full 33-frame Native/Foot and failed IK calibration evidence, per-frame path snapshots, vector correction and immutable sources')
