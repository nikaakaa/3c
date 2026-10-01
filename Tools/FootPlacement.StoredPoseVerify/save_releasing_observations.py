from pathlib import Path
import json,hashlib,shutil,zipfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/diagnostics/foot-placement/ik-tests'
TEMP=ROOT/'3cDemo/Client/3C_Client/Temp/FootReleasingNative'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

for binding in ['cold','loaded']:
    shutil.copyfile(TEMP/('ik-'+binding)/'result.json',OUT/('releasing-business-ik-diagnostic-'+binding+'.json'))
geometry=read(TEMP/'source-geometry/result.json')
shutil.copyfile(TEMP/'source-geometry/result.json',OUT/'releasing-source-geometry-raw.json')
observations=[]
for row in geometry['rows']:
    facts={'frame':row['frame'],'preRoll':row['preRoll'],'rootWorldPosition':row['rootWorldPosition'],'sides':{}}
    for side in ['left','right']:
        terms=[]
        for source in row['sources']:
            y=source[side+'Geometry']['soleMidpoint'][1]
            height=source[side+'FootHeight']
            terms.append({'sourceName':source['sourceName'],'clipIdentity':source['clipIdentity'],'groupClipIndex':source['groupClipIndex'],
                'weight':source[side+'Weight'],'sourceSoleY':y,'footHeight':height,'baselineY':y-height})
        weight=sum(x['weight'] for x in terms)
        baseline=sum(x['weight']*x['baselineY'] for x in terms)/weight
        final=row['final'+side.title()+'Geometry']['soleMidpoint'][1]
        facts['sides'][side]={'sources':terms,'totalWeight':weight,'weightedBaselineY':baseline,'finalSoleY':final,
            'finalSoleMinusWeightedBaseline':final-baseline,'weightedFootHeight':sum(x['weight']*x['footHeight'] for x in terms)/weight,
            'soleBlendDifference':final-sum(x['weight']*x['sourceSoleY'] for x in terms)/weight}
    observations.append(facts)
summary={'status':'observed-not-a-candidate','scope':'同一2023种子与33帧；组件脚底中点、实际每脚权重、每源作者基准；只记录事实，未判新方案通过',
    'rootBonePolicy':geometry['rootBonePolicy'],'scalePolicy':geometry['scalePolicy'],'calibrationRevision':geometry['calibrationRevision'],
    'formula':'B_i=sourceSoleY-FootHeight_i；H=finalSoleY-Σ(w_i B_i)/Σw_i；组件Y不是已知地面',
    'rows':observations,'ikDiagnostics':{}}
for binding in ['cold','loaded']:
    result=read(OUT/('releasing-business-ik-diagnostic-'+binding+'.json'))
    summary['ikDiagnostics'][binding]={'status':result['status'],'maximumHistoricalJointError':max(x['maxRecordedJointError'] for x in result['historical']['rows']),
        'allocatedBytes':result['historical']['allocatedBytes'],'solverModuleMvid':result['solverModuleMvid'],'rootMotionModuleMvid':result['rootMotionModuleMvid']}
summary['exclusions']='逐帧独立求解器及实际Editor加载求解器均仍0.240472364mm；不支持跨帧状态或冻结实现绑定是本偏差原因。现有原采样未提供完整组件页，尚未定位剩余输入差异。'
archive=OUT/'releasing-observation-source-snapshot.zip'
pending=TEMP/'releasing-observation-pending.zip'
with zipfile.ZipFile(pending,'w',compression=zipfile.ZIP_DEFLATED) as zipped:
    for directory,manifest_name in [('source-geometry','source-manifest.json'),('ik-cold','native-goal-source-manifest.json'),('ik-loaded','native-goal-source-manifest.json')]:
        folder=TEMP/directory
        manifest=read(folder/manifest_name)
        actual={relative:sha(ROOT/relative) for relative in manifest['sources']}
        corrections={relative:{'prepared':expected,'compiled':actual[relative]} for relative,expected in manifest['sources'].items() if expected!=actual[relative]}
        manifest['sources']=actual
        manifest['snapshotManifestCorrections']=corrections
        if corrections:manifest['correctionReason']='几何装配首次编译暴露using NativeArray写入限制；使用可写别名后成功编译，封存实际参与最后编译的源文件。'
        zipped.writestr(directory+'/'+manifest_name,json.dumps(manifest,ensure_ascii=False,indent=2))
        zipped.write(folder/'ThirdPersonClient.Editor.dll',directory+'/executed-assemblies/ThirdPersonClient.Editor.dll')
        for relative,expected in actual.items():
            assert sha(ROOT/relative)==expected
            zipped.write(ROOT/relative,directory+'/'+Path(relative).as_posix())
with zipfile.ZipFile(pending) as zipped:
    for directory,manifest_name in [('source-geometry','source-manifest.json'),('ik-cold','native-goal-source-manifest.json'),('ik-loaded','native-goal-source-manifest.json')]:
        manifest=json.loads(zipped.read(directory+'/'+manifest_name))
        for relative,expected in manifest['sources'].items():assert hashlib.sha256(zipped.read(directory+'/'+Path(relative).as_posix())).hexdigest()==expected
pending.replace(archive)
summary['sourceSnapshotSha256']=sha(archive)
summary['resultSha256']={name:sha(OUT/name) for name in ['releasing-source-geometry-raw.json','releasing-business-ik-diagnostic-cold.json','releasing-business-ik-diagnostic-loaded.json']}
summary['handoffStatus']='所有Unity作业已结束；测试窗口未持有DisallowAutoRefresh；独立编译后build-server已关闭；按用户实测准备要求停止后续调用。'
(OUT/'releasing-source-geometry.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print('Saved completed IK exclusions and 34-frame actual source geometry observations; no Unity or tests started')
print([(r['frame'],r['sides']['right']['finalSoleMinusWeightedBaseline']) for r in observations if 2037<=r['frame']<=2043])
