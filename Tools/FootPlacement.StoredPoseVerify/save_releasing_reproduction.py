from pathlib import Path
import json,hashlib,zipfile,shutil

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/diagnostics/foot-placement/ik-tests'
BASE=ROOT/'3cDemo/Client/3C_Client/Temp/FootReleasingNative'
REPRO=BASE/'height-snapshot-reproduction'
FOOT=ROOT/'3cDemo/Client/3C_Client/Temp/FootRotationComparison/historical-69a36d339'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

shutil.copyfile(BASE/'releasing-business-ik.json',OUT/'releasing-business-ik-attempt-preserved-quaternion.json')
shutil.copyfile(REPRO/'releasing-business-ik.json',OUT/'releasing-height-ik-attempt-preserved-quaternion.json')
shutil.copyfile(OUT/'releasing-height-ik-attempt-renormalized.json',OUT/'releasing-height-ik.json')
manifest=read(FOOT/'native-goal-source-manifest.json')
archive=OUT/'releasing-preserved-quaternion-source-snapshot.zip'
replacement=BASE/'releasing-preserved-quaternion-pending.zip'
with zipfile.ZipFile(replacement,'w',compression=zipfile.ZIP_DEFLATED) as zipped:
    zipped.write(FOOT/'native-goal-source-manifest.json','source-manifest.json')
    zipped.write(FOOT/'ThirdPersonClient.Editor.dll','executed-assemblies/ThirdPersonClient.Editor.dll')
    for relative,expected in manifest['sources'].items():
        path=ROOT/relative
        assert sha(path)==expected,relative
        zipped.write(path,relative)
archive_path=ROOT/'docs/diagnostics/foot-placement/ik-tests/releasing-height-source-snapshot.zip'
reproduction={'status':'reproduced-rejection-and-ik-calibration-failure','candidateProductionRestored':False,
    'scope':'从归档源码在Temp编译执行33帧Native、预测/Goal和实际FBBIK；生产已撤回，未恢复候选',
    'sourceSnapshotSha256':sha(archive_path),'preservedQuaternionSourceSnapshotSha256':sha(replacement),
    'componentQuaternionRoundTrip':'使用正式已归一化构造并逐项确认位值；历史2033仍0.240472364mm，重复归一化未解释偏差',
    'stages':{},'resultSha256':{},'compilerSha256':{}}
for kind in ['native','poses','goal','ik']:
    reproduction['stages'][kind]=read(REPRO/(kind+'-command.json'))['result']['data']['result']
for kind in ['native','component-poses','goal','ik']:
    reproduction['resultSha256'][kind]=sha(REPRO/('releasing-business-'+kind+'.json'))
for name in ['compile.rsp.log','native-goal-compile.rsp.log']:
    reproduction['compilerSha256'][name]=sha(REPRO/name)
    shutil.copyfile(REPRO/name,OUT/('releasing-height-reproduction-'+name+'.txt'))
reproduction['solverAllocatedBytes']=read(REPRO/'releasing-business-ik.json')['current']['allocatedBytes']
replacement.replace(archive)
(OUT/'releasing-height-reproduction.json').write_text(json.dumps(reproduction,ensure_ascii=False,indent=2),encoding='utf-8')
print('Saved archived-source reproduction and unchanged preserved-quaternion calibration failure with exact new source/DLL identity')
