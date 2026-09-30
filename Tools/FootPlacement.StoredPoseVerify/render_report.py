from pathlib import Path
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
TEMP = ROOT / '3cDemo/Client/3C_Client/Temp/FootStoredPoseFunctions'
manifest = json.loads(TEMP.joinpath('source-manifest.json').read_text(encoding='utf-8'))
assembly = TEMP / 'bin/Debug/net8.0/ThirdPersonCharacter.Animation.dll'
manifest['candidateAnimationAssemblySha256'] = hashlib.sha256(assembly.read_bytes()).hexdigest()
manifest['runtimeCommit'] = subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD']).decode().strip()
manifest['candidateCommit'] = subprocess.check_output(['git','-C',str(ROOT),'rev-parse','18e1f2a4f']).decode().strip()
manifest['candidateSourceSha256'] = {}
for relative in ['Animation/Contracts/Rig/AnimationFootStepObservationCurves.cs','Animation/PoseGraph/CharacterPoseWorldContextAdapter.cs','Animation/Contracts/Rig/AnimationFootStepLandingEvents.cs']:
    path = ROOT / '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline' / relative
    manifest['candidateSourceSha256'][relative] = hashlib.sha256(path.read_bytes()).hexdigest()
manifest['executionBoundary'] = 'Historical method comes from git show; only its array field is passed explicitly. Current selection and Stored methods come from the actual compiled production assembly. No native Slot Job, Goal, Physics or FBBIK is implied by source-functions Passed.'
manifest['zeroWeightMissingIdentity'] = 'Omit only zero-weight Action entries whose SourceId was not recorded; preserve zero-weight Idle and all positive contributors. Test source-owner indices are preparation positions, not a physical source registry reconstruction.'
manifest['sourceHashes'] = {}
for name in ['Program.cs','prepare.py','prepare_inputs.py','run.ps1']:
    path = Path(__file__).with_name(name)
    manifest['sourceHashes'][name] = hashlib.sha256(path.read_bytes()).hexdigest()
data = {'provenance': manifest, 'cases': []}
native_manifest = EVIDENCE / 'stored-native-goal-provenance.json'
data['nativeProvenance'] = json.loads(native_manifest.read_text(encoding='utf-8')) if native_manifest.exists() else None
for name in ['stored-no-contact-left','stored-contact-right']:
    functions = json.loads(EVIDENCE.joinpath(name+'-functions.json').read_text(encoding='utf-8'))
    goal_path = EVIDENCE / (name+'-goal.json')
    data['cases'].append({'name':name, 'functions':functions, 'goal':json.loads(goal_path.read_text(encoding='utf-8')) if goal_path.exists() else None})
EVIDENCE.joinpath('stored-foot-motion-provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
template = Path(__file__).with_name('report-template.html').read_text(encoding='utf-8')
encoded = json.dumps(data,ensure_ascii=False,separators=(',',':')).replace('</','<\\/')
EVIDENCE.joinpath('stored-foot-motion.html').write_text(template.replace('@@DATA@@',encoded),encoding='utf-8')
print('Saved interactive Stored foot-motion report and actual source identities')
