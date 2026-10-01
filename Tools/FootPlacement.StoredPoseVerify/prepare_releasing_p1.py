from pathlib import Path
import hashlib
import json
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[2]
TEMP = ROOT / '3cDemo/Client/3C_Client/Temp/FootReleasingP1'
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
PYTHON = Path('C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
BASELINE = 'fbed4b1923b90d7126a50bab512b3a5e9b2a824b'
SNAPSHOT = OUT / 'releasing-p1-production-snapshot.zip'
assert hashlib.sha256(SNAPSHOT.read_bytes()).hexdigest() == '3060209b316a61f5656bb7e0053bdcb4dba5d5de3d58492a5f5acd8b596729c8'
state = json.loads((TEMP / 'editor-state-once.json').read_text(encoding='utf-8-sig'))['result']['data']['result']
assert state['project_path'] == 'D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets'
assert not (state['play'] or state['compiling'] or state['updating'])
assemblies = TEMP / 'editor-assemblies.json'
assemblies.write_text(json.dumps({'result': {'data': {'result': state['assemblies']}}}), encoding='utf-8')
assembly_args = ['--editor-assemblies', str(assemblies)]
subprocess.run([str(PYTHON), str(Path(__file__).with_name('prepare_native_releasing.py')), '--commit', BASELINE,
                '--sampling-snapshot', str(OUT / 'releasing-boundary-production-snapshot.zip'), '--sampling-variant', 'baseline',
                '--out', str(TEMP / 'native')] + assembly_args, check=True)
for variant in ['baseline', 'candidate']:
    subprocess.run([str(PYTHON), str(Path(__file__).with_name('prepare_native_goal.py')), '--mode', 'releasing', '--commit', BASELINE,
                    '--p1-snapshot', str(SNAPSHOT), '--p1-variant', variant, '--out', str(TEMP / variant)] + assembly_args, check=True)
members = {'editor-state-once.json': (TEMP / 'editor-state-once.json').read_bytes()}
for variant in ['native', 'baseline', 'candidate']:
    manifest_name = 'source-manifest.json' if variant == 'native' else 'native-goal-source-manifest.json'
    manifest_path = TEMP / variant / manifest_name
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    members[variant + '/' + manifest_name] = manifest_path.read_bytes()
    for relative, expected in manifest['sources'].items():
        content = ROOT.joinpath(relative).read_bytes()
        assert hashlib.sha256(content).hexdigest() == expected, relative
        members[variant + '/sources/' + Path(relative).as_posix()] = content
    response = 'compile.rsp' if variant == 'native' else 'native-goal-compile.rsp'
    members[variant + '/' + response] = (TEMP / variant / response).read_bytes()
pending = TEMP / 'build-inputs-pending.zip'
with zipfile.ZipFile(pending, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
    for name, content in members.items():
        archive.writestr(name, content)
with zipfile.ZipFile(pending) as archive:
    for name, content in members.items():
        assert archive.read(name) == content
pending.replace(OUT / 'releasing-p1-build-inputs.zip')
pending = TEMP / 'production-snapshot-pending.zip'
pending.write_bytes(SNAPSHOT.read_bytes())
assert hashlib.sha256(pending.read_bytes()).hexdigest() == '3060209b316a61f5656bb7e0053bdcb4dba5d5de3d58492a5f5acd8b596729c8'
pending.replace(OUT / 'releasing-p1-production-snapshot.zip')
print('P1现有业务输入已封存；只有Lifecycle/Module使用候选，Native与其它正式源码固定fbed4b192')
