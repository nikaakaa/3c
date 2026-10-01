from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[2]
TEMP = ROOT / '3cDemo/Client/3C_Client/Temp/FootReleasingP1'
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
state = json.loads((TEMP / 'editor-state-once.json').read_text(encoding='utf-8-sig'))
initial = state['result']['data']['result']
shutil.copyfile(TEMP / 'editor-state-once.json', OUT / 'releasing-p1-editor-state-once.json')
result = {
    'status': 'not-executed', 'utc': datetime.now(timezone.utc).isoformat(),
    'reason': '初次Editor核对空闲；原执行入口在加载测试程序集前被项目/播放/编译/导入状态联合检查拦截。现有错误未单列具体状态，不能推断是哪项变化。停止Unity路径，不恢复服务或重试。',
    'baselineCommit': 'fbed4b1923b90d7126a50bab512b3a5e9b2a824b',
    'seedFrame': 2023, 'firstFrame': 2024, 'lastFrame': 2056, 'expectedFrames': 33,
    'scope': '同一既有完整双脚释放业务；只替换封存的Lifecycle/Module，Native与其它正式源码固定基线。未改生产Assets，未新增场景，未执行Replay或IK校准。',
    'initialEditorState': {key: initial[key] for key in ['project_path', 'play', 'compiling', 'updating']},
    'unityInstance': 'e852139597e42532', 'availabilityChecks': 1,
    'commands': {}, 'compilation': {},
    'completedNativeFrames': 0, 'completedFootFrames': {'baseline': 0, 'candidate': 0},
    'metrics': None, 'contactClearanceComparison': None, 'firstNonFiniteFrame': None,
    'qualityDecision': '未取得业务结果，不能判断P1有效、无回归、0GC或无接触悬脚。',
    'pendingAcceptance': [
        '相同2023种子和2024～2056完整33帧，左右Complete使用当帧ResolvePelvis的PelvisDelta×PositionWeight',
        '双脚逐帧目标伸展比、超长帧和持续时间、额外修正向量步长、作者与Goal权重',
        '最终加权Goal实际地形查询；Landing/Locked单列heel/toe和整脚净空，保留相对基线新增>1cm和>5cm帧',
        '无实根或非有限结果保存首个失败帧，不补默认成功；无命中保存未观测',
        '实际Native与Foot暖机分配；原FBBIK历史0.240472mm>0.2mm校准失败保留'
    ],
    'jobs': '两次动态入口均在加载测试程序集前返回失败，调用已经结束；未启动Native/Foot业务作业，未持有DisallowAutoRefresh，各次编译后build-server已关闭。CLI退出码0的错误判断已修正，后续入口必须同时检查result.success及业务状态。'
}
for variant in ['baseline', 'candidate']:
    command_path = TEMP / variant / 'command.json'
    result['commands'][variant] = json.loads(command_path.read_text(encoding='utf-8-sig'))
    assert result['commands'][variant]['result']['success'] is False
for variant in ['native', 'baseline', 'candidate']:
    compile_path = TEMP / variant / 'compile.txt'
    content = compile_path.read_text(encoding='utf-8-sig')
    assert 'error CS' not in content
    (OUT / ('releasing-p1-' + variant + '-compile.txt')).write_text(content.rstrip() + '\n', encoding='utf-8')
    result['compilation'][variant] = {'status': 'passed', 'compiler': 'Unity 2022.3.62f2c1 Roslyn 4.3.1',
        'assemblySha256': hashlib.sha256((TEMP / variant / 'ThirdPersonClient.Editor.dll').read_bytes()).hexdigest()}
result['sealedZipSha256'] = {name: hashlib.sha256((OUT / name).read_bytes()).hexdigest() for name in [
    'releasing-p1-production-snapshot.zip', 'releasing-p1-build-inputs.zip', 'releasing-p1-compiled-assemblies.zip']}
with zipfile.ZipFile(OUT / 'releasing-p1-production-snapshot.zip') as archive:
    result['candidateManifest'] = json.loads(archive.read('manifest.json'))
(OUT / 'releasing-p1-result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({key: result[key] for key in ['status', 'compilation', 'completedFootFrames', 'sealedZipSha256']}, ensure_ascii=False, indent=2))
