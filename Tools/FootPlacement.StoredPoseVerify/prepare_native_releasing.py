from pathlib import Path
import hashlib
import json
import re
import subprocess
import argparse
import zipfile

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / '3cDemo/Client/3C_Client'
parser = argparse.ArgumentParser()
parser.add_argument('--commit', default='d6d6ab9f9')
parser.add_argument('--working-native', action='store_true')
parser.add_argument('--foot-height-candidate', action='store_true')
parser.add_argument('--out', type=Path)
parser.add_argument('--candidate-snapshot', type=Path)
ARGS = parser.parse_args()
OUT = ARGS.out.resolve() if ARGS.out else CLIENT / 'Temp/FootReleasingNative'
OUT.mkdir(parents=True, exist_ok=True)
COMMIT = ARGS.commit
BASELINE = '2c757a422'
PREFIX = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/'


def source(commit, relative):
    if ARGS.foot_height_candidate and relative in [PREFIX+'BlendStack/AnimationSlotBlendJob.cs', PREFIX+'Contracts/Rig/AnimationFootStepObservationCurves.cs', PREFIX+'PoseGraph/CharacterPoseWorldContextAdapter.cs']:
        if ARGS.candidate_snapshot:
            with zipfile.ZipFile(ARGS.candidate_snapshot) as archive:
                captured = archive.read('production/'+relative)
                expected = json.loads(archive.read('source-manifest.json'))['candidateSourceSha256'][relative]
                assert hashlib.sha256(captured).hexdigest() == expected
                return captured.decode('utf-8')
        return ROOT.joinpath(relative).read_text(encoding='utf-8')
    if ARGS.working_native and relative in [PREFIX + 'BlendStack/' + name for name in ['AnimationSlotBlendJob.cs', 'AnimationSlotBlendPoseWorkspace.cs', 'AnimationBlendStackRuntime.cs']]:
        return ROOT.joinpath(relative).read_text(encoding='utf-8')
    return subprocess.check_output(['git', '-C', str(ROOT), 'show', commit + ':' + relative]).decode('utf-8')


def method(text, name):
    match = re.search(r'^        (?:internal |public |static )*(?:void|float) ' + name + r'\(', text, re.M)
    begin = match.start()
    opening = text.index('{', begin)
    end, depth = opening + 1, 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[begin:end]


def declaration(text, name):
    match = re.search(r'^    (?:internal |public |readonly |static |sealed |abstract )*(?:class|struct) ' + name + r'\b', text, re.M)
    opening = text.index('{', match.start())
    end, depth = opening + 1, 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[match.start():end]


files = [Path(__file__).with_name('CapturedReleasingNativeTests.cs')]
provenance = {}
for commit, label in [(BASELINE, 'Historical'), (COMMIT, 'Current')]:
    methods = ['BlendPoses', 'CopyPose', 'BlendParameters', 'AppendContributions', 'BlendFeet', 'BoneOutputWeight']
    if label == 'Current': methods.append('BlendContributions')
    relative = PREFIX + 'PoseGraph/CharacterPoseNativeAnimationSlotHandler.cs'
    text = source(commit, relative)
    bodies = [method(text, name) for name in methods]
    compiled_bodies = bodies
    if label == 'Historical':
        compiled_bodies = [body.replace('value.RightFootWeight * factor);', 'value.RightFootWeight * factor, in value.FootMotion);') for body in bodies]
    fields = '''
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        ulong m_LastSourceContinuity, m_LastActionContinuity, m_ContinuityIdentity;
        ulong m_NextContinuityIdentity = 1;
        float m_LastActionWeight;
        CharacterPoseNativeLocalPoseValue m_Output;
        PoseNodeId NodeId => new PoseNodeId("corin.full-body-action.slot");
        internal void Evaluate(in CharacterPoseNativePoseReadBinding source, in CharacterPoseNativePoseReadBinding action,
            in AnimationPlayerPoseNativeWriteBinding output, bool hasAction) { m_WriteBinding = output; if (hasAction) BlendPoses(in source, in action); else CopyPose(in source, CharacterPoseSpace.Local); }
'''
    path = OUT / (label + 'ActionSlot.cs')
    path.write_text('using System;\nusing Unity.Collections;\nusing UnityEngine;\nnamespace ThirdPersonCharacter.Pipeline.Animation { internal sealed class ' + label + 'ActionSlot {\n' + fields + '\n'.join(compiled_bodies) + '\n}}', encoding='utf-8')
    files.append(path)
    provenance[label] = {'commit': commit, 'path': relative, 'sourceSha256': hashlib.sha256(text.encode()).hexdigest(),
        'adaptation': 'Production method bodies with fixture-owned instance fields and explicit native bindings. Historical primitive constructor additionally carries unchanged FootMotion because the modern primitive contract added that field; no historical weight/pose formula is changed.',
        'methodSha256': {name: hashlib.sha256(body.encode()).hexdigest() for name, body in zip(methods, bodies)}}
relative = PREFIX + 'BlendStack/AnimationSlotBlendJob.cs'
path = OUT / 'AnimationSlotBlendJob.cs'
path.write_text(source(COMMIT, relative), encoding='utf-8')
files.append(path)
declarations = {
    'Contracts/Rig/AnimationFootStepObservationCurves.cs': ['AnimationFootMotionRuntimeSample', 'AnimationFootMotionSourceSample'],
    'Contracts/Workspace/AnimationPoseNativeWorkspaceContracts.cs': ['AnimationPrimitivePoseContribution', 'AnimationPlayerPoseNativeWriteBinding', 'AnimationPoseValueNativeReadBinding'],
    'Contracts/Pose/CharacterPoseNativePortValues.cs': ['CharacterPoseNativePoseReadBinding', 'CharacterPoseNativePortValue', 'CharacterPoseNativeLocalPoseValue'],
    'PoseGraph/CharacterPoseNativeSpaceConversionHandler.cs': ['CharacterPoseNativePoseBufferCopy']
}
for relative, names in declarations.items():
    text = source(COMMIT, PREFIX + relative)
    imports = text[:text.index('namespace ')]
    path = OUT / Path(relative).name
    path.write_text(imports + 'namespace ThirdPersonCharacter.Pipeline.Animation {\n' + '\n'.join(declaration(text, name) for name in names) + '\n}', encoding='utf-8')
    files.append(path)
for relative in ['BlendStack/AnimationSlotBlendPoseWorkspace.cs', 'BlendStack/AnimationBlendSourcePoseNativeReadBinding.cs', 'PoseGraph/CharacterPoseNativeNodePoseBuffer.cs']:
    path = OUT / Path(relative).name
    path.write_text(source(COMMIT, PREFIX + relative), encoding='utf-8')
    files.append(path)
relative = PREFIX + 'PoseGraph/CharacterPoseWorldContextAdapter.cs'
text = source(COMMIT, relative)
begin = text.index('        internal static int RequireFootMotionContribution(')
opening = text.index('{', begin)
end, depth = opening + 1, 1
while depth:
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
path = OUT / 'CurrentFootSelector.cs'
path.write_text('using System;\nnamespace ThirdPersonCharacter.Pipeline.Animation { internal static class CurrentFootSelector {\n' + text[begin:end] + '\n}}', encoding='utf-8')
files.append(path)
references = json.loads(CLIENT.joinpath('Temp/FootStoredPoseFunctions/editor-assemblies.json').read_text(encoding='utf-8-sig'))['result']['data']['result']
by_name = {Path(path).name: path for path in references}
needed = ['mscorlib.dll', 'System.Core.dll', 'System.dll', 'netstandard.dll', 'UnityEngine.CoreModule.dll',
    'UnityEngine.AnimationModule.dll', 'UnityEditor.CoreModule.dll', 'ThirdPersonClient.Runtime.dll',
    'ThirdPersonCharacter.Animation.dll', 'ThirdPersonSimulation.Core.dll', 'Unity.Collections.dll',
    'Unity.Burst.dll', 'Unity.Mathematics.dll', 'Newtonsoft.Json.dll', 'nunit.framework.dll']
needed += ['KK.GeneratedDiagnosticSampling.Annotations.dll', 'KK.GeneratedDiagnosticSampling.dll', 'BTSMTL.EventGraphs.dll']
assert all(name in by_name for name in needed), [name for name in needed if name not in by_name]
response = ['-nostdlib+', '-langversion:latest', '-target:library', '-unsafe+', '-utf8output', '-nowarn:0436',
    '-out:"' + str(OUT / 'ThirdPersonClient.Editor.dll') + '"']
if ARGS.foot_height_candidate: response.append('-define:FOOT_HEIGHT_CANDIDATE')
response += ['"' + str(path) + '"' for path in files]
response += ['-r:"' + by_name[name] + '"' for name in needed]
response += ['-r:EditorAnimation="' + by_name['ThirdPersonCharacter.Animation.dll'] + '"']
OUT.joinpath('compile.rsp').write_text('\n'.join(response), encoding='utf-8')
manifest = {'nativeJobCommit': COMMIT, 'baselineActionSlotCommit': BASELINE, 'actionSlot': provenance,
    'sources': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in files},
    'bindings': {name: hashlib.sha256(Path(by_name[name]).read_bytes()).hexdigest() for name in needed}}
manifest['workingNative'] = ARGS.working_native
manifest['footHeightCandidate'] = ARGS.foot_height_candidate
if ARGS.foot_height_candidate:
    manifest['candidateSourceSha256'] = {relative: hashlib.sha256(source(COMMIT, relative).encode('utf-8')).hexdigest() for relative in
        [PREFIX+'BlendStack/AnimationSlotBlendJob.cs', PREFIX+'Contracts/Rig/AnimationFootStepObservationCurves.cs', PREFIX+'PoseGraph/CharacterPoseWorldContextAdapter.cs']}
    manifest['candidateScope'] = 'Native Job和实际BlendFootHeights参与计算；adapter选择器及同序正式BlendFootHeights调用由fixture装配，未运行adapter其它host装配。'
if ARGS.working_native:
    manifest['workingNativeSources'] = {PREFIX + 'BlendStack/' + name: hashlib.sha256(ROOT.joinpath(PREFIX + 'BlendStack/' + name).read_bytes()).hexdigest()
        for name in ['AnimationSlotBlendJob.cs', 'AnimationSlotBlendPoseWorkspace.cs', 'AnimationBlendStackRuntime.cs']}
    manifest['workingNativeScope'] = 'Job and Workspace are executed here; Runtime unique Stored continuity reader belongs to the compiled production caller and is not called by this fixture.'
OUT.joinpath('source-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print('Prepared actual Native Slot job and both complete Action Slot method sets')
