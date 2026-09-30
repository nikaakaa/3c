from pathlib import Path
import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / '3cDemo/Client/3C_Client'
OUT = CLIENT / 'Temp/FootStoredPoseFunctions'
OUT.mkdir(exist_ok=True)
RELATIVE = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseWorldContextAdapter.cs'
historical = subprocess.check_output(['git', '-C', str(ROOT), 'show', '2c757a422:' + RELATIVE]).decode('utf-8')
begin = historical.index('        AnimationPoseSourceContribution RequireFootMotionContribution(')
opening = historical.index('{', begin)
depth = 1
end = opening + 1
while depth:
    depth += (historical[end] == '{') - (historical[end] == '}')
    end += 1
method = historical[opening:end]
source = 'using System;\nusing ThirdPersonCharacter.Pipeline.Animation;\nstatic class HistoricalFootMotionSelector\n{\n public static AnimationPoseSourceContribution Select(AnimationPoseSourceContribution[] contributions, int contributionCount)\n' + method.replace('m_Contributions[i]', 'contributions[i]') + '\n}\n'
OUT.joinpath('HistoricalFootMotionSelector.cs').write_text(source, encoding='utf-8')
references = list(CLIENT.joinpath('Temp/bin/Debug').glob('*.dll'))
references += list(CLIENT.joinpath('Library/ScriptAssemblies').glob('*.dll'))
references += [Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll')]
project = ET.parse(CLIENT / 'ThirdPersonCharacter.Animation.csproj')
for reference in project.iter():
    if reference.tag.split('}')[-1] != 'Reference': continue
    hint = next((child for child in reference if child.tag.split('}')[-1] == 'HintPath'), None)
    if hint is not None:
        path = Path(hint.text)
        if not path.is_absolute(): path = CLIENT / path
        if path.is_file() and path.name not in ['mscorlib.dll','netstandard.dll'] and not path.name.startswith('System.'): references.append(path)
unique = {}
for path in references: unique.setdefault(path.name, path)
root = ET.Element('Project', {'Sdk': 'Microsoft.NET.Sdk'})
group = ET.SubElement(root, 'PropertyGroup')
for key, value in {'OutputType':'Exe','TargetFramework':'net8.0','AssemblyName':'ThirdPersonClient.Editor','EnableDefaultCompileItems':'false','Nullable':'disable','NoWarn':'0436','GenerateAssemblyInfo':'false'}.items(): ET.SubElement(group,key).text=value
items = ET.SubElement(root, 'ItemGroup')
for path in [Path(__file__).with_name('Program.cs'), OUT/'HistoricalFootMotionSelector.cs']:
    ET.SubElement(items,'Compile',{'Include':str(path)})
for name,path in unique.items():
    ref = ET.SubElement(items, 'Reference', {'Include': name.removesuffix('.dll')})
    ET.SubElement(ref, 'HintPath').text=str(path)
ET.indent(root)
OUT.joinpath('StoredPoseVerify.csproj').write_bytes(ET.tostring(root,encoding='utf-8',xml_declaration=True))
manifest = {'baselineCommit': subprocess.check_output(['git','-C',str(ROOT),'rev-parse','2c757a422']).decode().strip(), 'historicalSourcePath':RELATIVE, 'historicalSourceSha256':hashlib.sha256(historical.encode()).hexdigest(), 'historicalMethodSha256':hashlib.sha256(method.encode()).hexdigest(), 'historicalAdaptation':'Only instance array field is changed to the explicit array parameter; the branch body comes from git show', 'candidateAnimationAssembly': str(unique['ThirdPersonCharacter.Animation.dll']), 'candidateAnimationAssemblySha256':hashlib.sha256(unique['ThirdPersonCharacter.Animation.dll'].read_bytes()).hexdigest()}
OUT.joinpath('source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(manifest,ensure_ascii=True))
