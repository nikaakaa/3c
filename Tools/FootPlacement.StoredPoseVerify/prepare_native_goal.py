from pathlib import Path
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '3cDemo/Client/3C_Client/Temp/FootStoredPoseFunctions'
CLIENT = ROOT / '3cDemo/Client/3C_Client'
HELPER_COMMIT = '409b40b8a'
RUNTIME_COMMIT = '18e1f2a4f'
files = [Path(__file__).with_name('CapturedStoredFootGoalTests.cs')]
sources = {}
for name in ['CharacterFootCapturedContactTests.cs', 'CharacterFootCapturedReleaseTests.cs']:
    relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Analysis/FootPlacement/' + name
    path = OUT / 'native-helpers' / name
    path.parent.mkdir(exist_ok=True)
    path.write_bytes(subprocess.check_output(['git', '-C', str(ROOT), 'show', HELPER_COMMIT + ':' + relative]))
    files.append(path)
for name in ['CharacterFootLifecycle.cs', 'CharacterFootInterpolationRuntime.cs', 'CharacterFootHardConstraintResolver.cs', 'CharacterFootLandingRuntime.cs']:
    relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/' + name
    path = OUT / ('goal-runtime-' + RUNTIME_COMMIT) / name
    path.parent.mkdir(exist_ok=True)
    path.write_bytes(subprocess.check_output(['git', '-C', str(ROOT), 'show', RUNTIME_COMMIT + ':' + relative]))
    files.append(path)
response = ['-nostdlib+', '-langversion:latest', '-target:library', '-utf8output', '-nowarn:0436', '-out:"' + str(OUT / 'ThirdPersonClient.Editor.dll') + '"']
response.extend('"' + str(path) + '"' for path in files)
editor = json.loads(OUT.joinpath('editor-assemblies.json').read_text(encoding='utf-8-sig'))
references = editor['result']['data']['result']
needed = ['mscorlib.dll', 'System.Core.dll', 'System.dll', 'netstandard.dll', 'UnityEngine.CoreModule.dll', 'UnityEngine.PhysicsModule.dll', 'UnityEngine.AnimationModule.dll', 'UnityEditor.CoreModule.dll', 'ThirdPersonClient.Runtime.dll', 'ThirdPersonCharacter.Animation.dll', 'ThirdPersonSimulation.Core.dll', 'Unity.Collections.dll', 'Newtonsoft.Json.dll', 'nunit.framework.dll']
by_name = {Path(path).name: path for path in references}
assert all(name in by_name for name in needed), [name for name in needed if name not in by_name]
response.extend('-r:"' + by_name[name] + '"' for name in needed)
OUT.joinpath('native-goal-compile.rsp').write_text('\n'.join(response), encoding='utf-8')
manifest = {'runtimeSourceCommit': RUNTIME_COMMIT, 'helperCommit': HELPER_COMMIT, 'sources': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in files}, 'bindings': {name: hashlib.sha256(Path(by_name[name]).read_bytes()).hexdigest() for name in needed}}
OUT.joinpath('native-goal-source-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print('Prepared frozen native Goal sources:', len(files), 'and real Editor bindings:', len(needed))
