from pathlib import Path
import json,hashlib,zipfile
ROOT=Path(__file__).resolve().parents[2]
TEMP=ROOT/'3cDemo/Client/3C_Client/Temp/FootReleasingBoundary'
OUT=ROOT/'docs/diagnostics/foot-placement/ik-tests'
members={};manifest={}
for variant in ['baseline','candidate','foot']:
    dll=TEMP/variant/'ThirdPersonClient.Editor.dll'
    members[variant+'/ThirdPersonClient.Editor.dll']=dll.read_bytes()
    manifest[variant]=hashlib.sha256(members[variant+'/ThirdPersonClient.Editor.dll']).hexdigest()
members['manifest.json']=json.dumps(manifest,indent=2).encode('utf-8')
target=OUT/'releasing-boundary-executed-assemblies.zip';pending=TEMP/'assemblies-pending.zip'
with zipfile.ZipFile(pending,'w',compression=zipfile.ZIP_DEFLATED) as archive:
    for name,value in members.items():archive.writestr(name,value)
with zipfile.ZipFile(pending) as archive:
    for name,value in members.items():assert archive.read(name)==value
pending.replace(target)
print('Sealed executed assemblies',manifest)
