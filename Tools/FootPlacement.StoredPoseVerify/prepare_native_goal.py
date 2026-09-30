from pathlib import Path
import hashlib
import json
import subprocess
import argparse

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / '3cDemo/Client/3C_Client'
parser = argparse.ArgumentParser()
parser.add_argument('--mode', choices=['sources', 'rotation'], default='sources')
parser.add_argument('--variant', choices=['historical', 'current'], default='historical')
args = parser.parse_args()
HELPER_COMMIT = '409b40b8a'
RUNTIME_COMMIT = '18e1f2a4f' if args.mode == 'sources' else {'historical': '552f13083', 'current': '15894ee7b'}[args.variant]
TEMP = CLIENT / 'Temp/FootStoredPoseFunctions' if args.mode == 'sources' else CLIENT / 'Temp/FootRotationComparison'
OUT = TEMP if args.mode == 'sources' else TEMP / (args.variant + '-' + RUNTIME_COMMIT)
OUT.mkdir(parents=True, exist_ok=True)
files = [Path(__file__).with_name('CapturedStoredFootGoalTests.cs')]
sources = {}
for name in ['CharacterFootCapturedContactTests.cs', 'CharacterFootCapturedReleaseTests.cs']:
    relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Analysis/FootPlacement/' + name
    path = OUT / 'native-helpers' / name
    path.parent.mkdir(exist_ok=True)
    path.write_bytes(subprocess.check_output(['git', '-C', str(ROOT), 'show', HELPER_COMMIT + ':' + relative]))
    files.append(path)
runtime_names = ['CharacterFootLifecycle.cs', 'CharacterFootInterpolationRuntime.cs', 'CharacterFootHardConstraintResolver.cs', 'CharacterFootLandingRuntime.cs']
if args.mode == 'rotation':
    runtime_names += ['CharacterFootLifecycleContracts.cs', 'CharacterFootTransitionRuntime.cs', 'CharacterFootTransitionResolver.cs', 'CharacterFootStateTargetResolver.cs', 'CharacterFootSwingMotionContracts.cs', 'CharacterFootSwingMotionBuilder.cs', 'CharacterFootConstraintMath.cs']
for name in runtime_names:
    relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/' + name
    path = OUT / ('goal-runtime-' + RUNTIME_COMMIT) / name
    path.parent.mkdir(exist_ok=True)
    path.write_bytes(subprocess.check_output(['git', '-C', str(ROOT), 'show', RUNTIME_COMMIT + ':' + relative]))
    files.append(path)
response = ['-nostdlib+', '-langversion:latest', '-target:library', '-utf8output', '-nowarn:0436', '-out:"' + str(OUT / 'ThirdPersonClient.Editor.dll') + '"']
if args.mode == 'rotation' and args.variant == 'current': response.append('-define:ROTATION_CANDIDATE')
response.extend('"' + str(path) + '"' for path in files)
editor = json.loads(CLIENT.joinpath('Temp/FootStoredPoseFunctions/editor-assemblies.json').read_text(encoding='utf-8-sig'))
references = editor['result']['data']['result']
needed = ['mscorlib.dll', 'System.Core.dll', 'System.dll', 'netstandard.dll', 'UnityEngine.CoreModule.dll', 'UnityEngine.PhysicsModule.dll', 'UnityEngine.AnimationModule.dll', 'UnityEditor.CoreModule.dll', 'ThirdPersonClient.Runtime.dll', 'ThirdPersonCharacter.Animation.dll', 'ThirdPersonSimulation.Core.dll', 'Unity.Collections.dll', 'Newtonsoft.Json.dll', 'nunit.framework.dll']
if args.mode == 'rotation':
    needed += ['KK.GeneratedDiagnosticSampling.Host.dll', 'ThirdPersonCharacter.PresentationReplicationDiagnosticSampling.dll', 'KK.GeneratedDiagnosticSampling.Annotations.dll', 'KK.GeneratedDiagnosticSampling.dll', 'ThirdPersonCharacter.FootIkDiagnosticSampling.dll', 'BTSMTL.Diagnostics.dll']
by_name = {Path(path).name: path for path in references}
assert all(name in by_name for name in needed), [name for name in needed if name not in by_name]
response.extend('-r:"' + by_name[name] + '"' for name in needed)
OUT.joinpath('native-goal-compile.rsp').write_text('\n'.join(response), encoding='utf-8')
manifest = {'runtimeSourceCommit': RUNTIME_COMMIT, 'helperCommit': HELPER_COMMIT, 'mode': args.mode, 'variant': args.variant, 'sources': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in files}, 'bindings': {name: hashlib.sha256(Path(by_name[name]).read_bytes()).hexdigest() for name in needed}}
OUT.joinpath('native-goal-source-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print('Prepared frozen native Goal sources:', len(files), 'and real Editor bindings:', len(needed))
