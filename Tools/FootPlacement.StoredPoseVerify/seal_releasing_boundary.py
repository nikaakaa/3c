from pathlib import Path
import hashlib,json,subprocess,zipfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/diagnostics/foot-placement/ik-tests'
PREFIX='3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/Rig/'
expected={'AnimationFootStepObservationCurves.cs':'84eff81224ed1c859a9bc76fa30bdfda928ea4d0ce9232a7f2ba36db611719d3',
          'AnimationFootStepLandingEvents.cs':'a88c5ec123e16ae1143494d44d498a6f83b809e0ce63ab901f235b83fe3c8a0e'}
baseline=subprocess.check_output(['git','-C',str(ROOT),'rev-parse','fbed4b192']).decode('utf-8').strip()
members={}; manifest={'baselineCommit':baseline,'candidateSourceSha256':{},'scope':'同一2023种子与2024至2056释放业务；只叠加两采样文件，所有其它正式源冻结基线'}
for name,sha in expected.items():
    relative=PREFIX+name
    value=ROOT.joinpath(relative).read_bytes()
    assert hashlib.sha256(value).hexdigest()==sha,relative
    members['candidate/'+relative]=value
    members['baseline/'+relative]=subprocess.check_output(['git','-C',str(ROOT),'show',baseline+':'+relative])
    manifest['candidateSourceSha256'][relative]=sha
members['manifest.json']=json.dumps(manifest,ensure_ascii=False,indent=2).encode('utf-8')
pending=OUT/'releasing-boundary-production-pending.zip'
target=OUT/'releasing-boundary-production-snapshot.zip'
with zipfile.ZipFile(pending,'w',compression=zipfile.ZIP_DEFLATED) as archive:
    for name,value in members.items():archive.writestr(name,value)
with zipfile.ZipFile(pending) as archive:
    for name,value in members.items():assert archive.read(name)==value
pending.replace(target)
print(json.dumps({'snapshot':str(target),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'baselineCommit':baseline},ensure_ascii=False))
