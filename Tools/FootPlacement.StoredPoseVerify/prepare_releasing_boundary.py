from pathlib import Path
import json,hashlib,subprocess,zipfile
import argparse

ROOT=Path(__file__).resolve().parents[2]
TEMP=ROOT/'3cDemo/Client/3C_Client/Temp/FootReleasingBoundary'
SNAPSHOT=ROOT/'docs/diagnostics/foot-placement/ik-tests/releasing-boundary-production-snapshot.zip'
PYTHON=Path('C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
parser=argparse.ArgumentParser();parser.add_argument('--editor-assemblies',type=Path,required=True);args=parser.parse_args()
assembly_args=['--editor-assemblies',str(args.editor_assemblies)]
with zipfile.ZipFile(SNAPSHOT) as archive: baseline=json.loads(archive.read('manifest.json'))['baselineCommit']
for variant in ['baseline','candidate']:
    subprocess.run([str(PYTHON),str(Path(__file__).with_name('prepare_native_releasing.py')),'--commit',baseline,'--sampling-snapshot',str(SNAPSHOT),'--sampling-variant',variant,'--out',str(TEMP/variant)]+assembly_args,check=True)
subprocess.run([str(PYTHON),str(Path(__file__).with_name('prepare_native_goal.py')),'--mode','releasing','--commit',baseline,'--out',str(TEMP/'foot')]+assembly_args,check=True)
source_members={}
for variant,filename in [('baseline','source-manifest.json'),('candidate','source-manifest.json'),('foot','native-goal-source-manifest.json')]:
    manifest=json.loads((TEMP/variant/filename).read_text(encoding='utf-8'))
    source_members[variant+'/'+filename]=(TEMP/variant/filename).read_bytes()
    for relative,expected in manifest['sources'].items():
        value=ROOT.joinpath(relative).read_bytes()
        assert hashlib.sha256(value).hexdigest()==expected,relative
        source_members[variant+'/sources/'+Path(relative).as_posix()]=value
    response='compile.rsp' if variant!='foot' else 'native-goal-compile.rsp'
    source_members[variant+'/'+response]=(TEMP/variant/response).read_bytes()
target=ROOT/'docs/diagnostics/foot-placement/ik-tests/releasing-boundary-build-inputs.zip'
pending=TEMP/'releasing-boundary-build-inputs-pending.zip'
with zipfile.ZipFile(pending,'w',compression=zipfile.ZIP_DEFLATED) as archive:
    for name,value in source_members.items():archive.writestr(name,value)
with zipfile.ZipFile(pending) as archive:
    for name,value in source_members.items():assert archive.read(name)==value
pending.replace(target)
print('Sealed actual frozen build inputs',hashlib.sha256(target.read_bytes()).hexdigest())
