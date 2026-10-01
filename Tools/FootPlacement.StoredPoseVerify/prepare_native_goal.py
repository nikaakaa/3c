from pathlib import Path
import hashlib
import json
import subprocess
import argparse
import re
import zipfile

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / '3cDemo/Client/3C_Client'
parser = argparse.ArgumentParser()
parser.add_argument('--mode', choices=['sources', 'rotation', 'releasing'], default='sources')
parser.add_argument('--variant', choices=['historical', 'current'], default='historical')
parser.add_argument('--commit')
parser.add_argument('--ik-binding', choices=['frozen','loaded','cold'], default='frozen')
parser.add_argument('--out', type=Path)
parser.add_argument('--editor-assemblies', type=Path)
parser.add_argument('--p1-snapshot', type=Path)
parser.add_argument('--p1-variant', choices=['baseline', 'candidate'])
args = parser.parse_args()
HELPER_COMMIT = '409b40b8a'
RUNTIME_COMMIT = '69a36d339' if args.mode == 'releasing' else '18e1f2a4f' if args.mode == 'sources' else {'historical': '552f13083', 'current': '15894ee7b'}[args.variant]
if args.commit is not None: RUNTIME_COMMIT = args.commit
TEMP = CLIENT / 'Temp/FootStoredPoseFunctions' if args.mode == 'sources' else CLIENT / 'Temp/FootRotationComparison'
OUT = TEMP if args.mode == 'sources' else TEMP / (args.variant + '-' + RUNTIME_COMMIT)
if args.out: OUT = args.out.resolve()
OUT.mkdir(parents=True, exist_ok=True)
files = [Path(__file__).with_name('CapturedStoredFootGoalTests.cs')]
sources = {}
module_relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootPlacementModule.cs'
module_source = subprocess.check_output(['git', '-C', str(ROOT), 'show', RUNTIME_COMMIT + ':' + module_relative]).decode('utf-8')
if args.p1_snapshot:
    with zipfile.ZipFile(args.p1_snapshot) as archive:
        module_source = archive.read(args.p1_variant + '/CharacterFootPlacementModule.cs').decode('utf-8')
methods = []
module_names = ['ResolveFootGoalOwnershipLoss', 'CreatePelvisGoal']
if args.mode == 'releasing': module_names.append('EncodeFootGoal')
for name in module_names:
    match = re.search(r'        static \w+ ' + name + r'\(', module_source)
    begin = match.start()
    opening = module_source.index('{', begin)
    depth = 1
    end = opening + 1
    while depth:
        depth += (module_source[end] == '{') - (module_source[end] == '}')
        end += 1
    methods.append(module_source[begin:end].replace('        static ', '        internal static ', 1))
module_fragment = OUT / 'CapturedFootModuleFunctions.cs'
module_fragment.write_text('using UnityEngine;\nusing ThirdPersonCharacter.Pipeline.Animation;\nusing ThirdPersonCharacter.Pipeline.Presentation;\nnamespace ThirdPersonCharacter.Pipeline.Editor { internal static class CapturedFootModuleFunctions {\n' + '\n'.join(methods) + '\n}}', encoding='utf-8')
files.append(module_fragment)
if args.mode == 'releasing':
    files.append(Path(__file__).with_name('CapturedReleasingFootPrediction.cs'))
    files.append(Path(__file__).with_name('CapturedReleasingFullIkTests.cs'))
    for name in ([] if args.ik_binding == 'loaded' else ['CharacterFinalIkFullBodySolver.cs', 'CharacterFinalIkPoseBufferBackend.cs', 'CharacterFullBodyIkDiagnostics.cs']):
        relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseConstraints/' + name
        path = OUT / name
        path.write_bytes(subprocess.check_output(['git', '-C', str(ROOT), 'show', RUNTIME_COMMIT + ':' + relative]))
        files.append(path)
    begin = module_source.index('        CharacterFootGroundPathResult PrepareGroundPath(')
    end = module_source.index('        static float ResolveCurrentSegmentRemainingSeconds(', begin)
    body = module_source[begin:end].replace('CharacterFootPlacementFrameInput', 'CapturedPredictionFrame').replace('CommittedLocomotionPlanarMotionTimeline', 'CapturedPredictionTimeline').replace('CharacterBodyPresentationFrame', 'CapturedPredictionBody')
    for result, name in [('CharacterFootGroundPathResult','PrepareGroundPath'),('CharacterFootLandingPredictionPair','PredictFootPair'),('CharacterFutureBodyTranslation','ResolveBodyTrajectory')]:
        body = body.replace('        ' + result + ' ' + name + '(', '        internal ' + result + ' ' + name + '(')
    prefix = '''using System;\nusing UnityEngine;\nusing ThirdPersonCharacter.Pipeline.Animation;\nusing ThirdPersonCharacter.Pipeline.Presentation;\nusing ThirdPersonSimulation;\nnamespace ThirdPersonCharacter.Pipeline.Editor { internal sealed class CapturedFootPredictionFunctions {\n
        readonly CharacterFootPlacementModuleSettings m_Settings;
        readonly ICharacterFootPlacementWorldQuery m_WorldQuery;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        readonly ActorId m_ActorId;
        internal CapturedFootPredictionFunctions(CharacterFootPlacementModuleSettings settings, ICharacterFootPlacementWorldQuery world, ICharacterFutureBodyTranslationSource future, ActorId actor) { m_Settings=settings; m_WorldQuery=world; m_FutureBodyTranslationSource=future; m_ActorId=actor; }
'''
    prediction_path = OUT / 'CapturedFootPredictionFunctions.cs'
    prediction_path.write_text(prefix + body + '\n}}', encoding='utf-8')
    files.append(prediction_path)
for name in (['CharacterFootCapturedContactTests.cs'] if args.p1_snapshot else ['CharacterFootCapturedContactTests.cs', 'CharacterFootCapturedReleaseTests.cs']):
    relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Analysis/FootPlacement/' + name
    path = OUT / 'native-helpers' / name
    path.parent.mkdir(exist_ok=True)
    content = subprocess.check_output(['git', '-C', str(ROOT), 'show', HELPER_COMMIT + ':' + relative])
    if args.p1_snapshot:
        text = content.decode('utf-8')
        members = []
        for member in ['AssetRoot', 'Repository', 'Sha256', 'Fact', 'AnimatedPose', 'PathLanding', 'PathInput', 'Git', 'Run', 'Vec', 'Row']:
            match = re.search(r'^        (?:const string |static readonly string |static \w+ |sealed class )' + member + r'\b', text, re.M)
            start = match.start()
            declarations = list(re.finditer(r'^        (?:const |static |sealed class |struct )', text[match.end():], re.M))
            end = match.end() + declarations[0].start() if declarations else text.rfind('    }')
            members.append(text[start:end].rstrip())
        content = (text[:text.index('        const string AssetRoot')] + '\n'.join(members) + '\n    }\n}\n').encode('utf-8')
    path.write_bytes(content)
    files.append(path)
runtime_names = ['CharacterFootLifecycle.cs', 'CharacterFootInterpolationRuntime.cs', 'CharacterFootHardConstraintResolver.cs', 'CharacterFootLandingRuntime.cs']
if args.mode in ['rotation', 'releasing']:
    runtime_names += ['CharacterFootLifecycleContracts.cs', 'CharacterFootTransitionRuntime.cs', 'CharacterFootTransitionResolver.cs', 'CharacterFootStateTargetResolver.cs', 'CharacterFootSwingMotionContracts.cs', 'CharacterFootSwingMotionBuilder.cs', 'CharacterFootConstraintMath.cs', 'CharacterFootStrideHipsBuilder.cs']
for name in runtime_names:
    relative = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/' + name
    path = OUT / ('goal-runtime-' + RUNTIME_COMMIT) / name
    path.parent.mkdir(exist_ok=True)
    if args.p1_snapshot and name == 'CharacterFootLifecycle.cs':
        with zipfile.ZipFile(args.p1_snapshot) as archive:
            path.write_bytes(archive.read(args.p1_variant + '/' + name))
    else:
        path.write_bytes(subprocess.check_output(['git', '-C', str(ROOT), 'show', RUNTIME_COMMIT + ':' + relative]))
    files.append(path)
response = ['-nostdlib+', '-langversion:latest', '-target:library', '-utf8output', '-nowarn:0436', '-out:"' + str(OUT / 'ThirdPersonClient.Editor.dll') + '"']
if args.mode == 'rotation' and args.variant == 'current': response.append('-define:ROTATION_CANDIDATE')
if args.mode == 'releasing': response.append('-define:RELEASING_FIXTURE')
if args.p1_snapshot:
    response.append('-define:P1_FIXTURE' + (',P1_CANDIDATE' if args.p1_variant == 'candidate' else ''))
if args.ik_binding == 'cold': response.append('-define:FBBIK_COLD_SOLVER_DIAGNOSTIC')
if args.ik_binding == 'loaded': response.append('-define:FBBIK_LOADED_SOLVER_DIAGNOSTIC')
response.extend('"' + str(path) + '"' for path in files)
assembly_file=args.editor_assemblies if args.editor_assemblies else CLIENT/'Temp/FootStoredPoseFunctions/editor-assemblies.json'
editor = json.loads(assembly_file.read_text(encoding='utf-8-sig'))
references = editor['result']['data']['result']
needed = ['mscorlib.dll', 'System.Core.dll', 'System.dll', 'netstandard.dll', 'UnityEngine.CoreModule.dll', 'UnityEngine.PhysicsModule.dll', 'UnityEngine.AnimationModule.dll', 'UnityEditor.CoreModule.dll', 'ThirdPersonClient.Runtime.dll', 'ThirdPersonCharacter.Animation.dll', 'ThirdPersonSimulation.Core.dll', 'Unity.Collections.dll', 'Newtonsoft.Json.dll', 'nunit.framework.dll']
if args.mode in ['rotation', 'releasing']:
    needed += ['KK.GeneratedDiagnosticSampling.Host.dll', 'ThirdPersonCharacter.PresentationReplicationDiagnosticSampling.dll', 'KK.GeneratedDiagnosticSampling.Annotations.dll', 'KK.GeneratedDiagnosticSampling.dll', 'ThirdPersonCharacter.FootIkDiagnosticSampling.dll', 'BTSMTL.Diagnostics.dll']
if args.mode == 'releasing': needed += ['RootMotion.dll']
by_name = {Path(path).name: path for path in references}
assert all(name in by_name for name in needed), [name for name in needed if name not in by_name]
response.extend('-r:"' + by_name[name] + '"' for name in needed)
OUT.joinpath('native-goal-compile.rsp').write_text('\n'.join(response), encoding='utf-8')
manifest = {'runtimeSourceCommit': RUNTIME_COMMIT, 'helperCommit': HELPER_COMMIT, 'mode': args.mode, 'variant': args.variant, 'sources': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in files}, 'bindings': {name: hashlib.sha256(Path(by_name[name]).read_bytes()).hexdigest() for name in needed}}
manifest['ikBinding'] = args.ik_binding
if args.p1_snapshot:
    manifest['p1SnapshotSha256'] = hashlib.sha256(args.p1_snapshot.read_bytes()).hexdigest()
    manifest['p1Variant'] = args.p1_variant
    manifest['helperAdaptation'] = '只保留现有双脚业务实际使用的冻结解析和几何成员；不编译其它旧用例入口，不增加Complete兼容重载'
manifest['moduleMethods'] = {'sourcePath': module_relative, 'sourceSha256': hashlib.sha256(module_source.encode('utf-8')).hexdigest(), 'adaptation': '只把private static可见性改为internal static；正式函数体来自固定Git版本或指定封存候选', 'bodySha256': [hashlib.sha256(method.encode('utf-8')).hexdigest() for method in methods]}
OUT.joinpath('native-goal-source-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print('Prepared frozen native Goal sources:', len(files), 'and real Editor bindings:', len(needed))
