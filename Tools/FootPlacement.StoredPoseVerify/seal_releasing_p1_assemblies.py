from pathlib import Path
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[2]
TEMP = ROOT / '3cDemo/Client/3C_Client/Temp/FootReleasingP1'
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
members = {variant + '/ThirdPersonClient.Editor.dll': (TEMP / variant / 'ThirdPersonClient.Editor.dll').read_bytes()
           for variant in ['native', 'baseline', 'candidate']}
manifest = {'executionStatus': 'compiled-not-executed', 'assemblies': {name: hashlib.sha256(content).hexdigest() for name, content in members.items()}}
members['manifest.json'] = json.dumps(manifest, indent=2).encode('utf-8')
pending = TEMP / 'executed-assemblies-pending.zip'
with zipfile.ZipFile(pending, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
    for name, content in members.items():
        archive.writestr(name, content)
with zipfile.ZipFile(pending) as archive:
    for name, content in members.items():
        assert archive.read(name) == content
pending.replace(OUT / 'releasing-p1-compiled-assemblies.zip')
print('本轮三份已编译程序集已封存；此封存不证明实际执行', manifest)
