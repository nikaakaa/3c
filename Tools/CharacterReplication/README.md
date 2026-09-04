# 角色复刻资料生成器

Corin、Unagi、Anbi 共用同一个生成器。`sources/` 明确指定每个角色的控制器、动画、角色配置、事件、区域和镜头来源；原数据保留在 `D:/ZZZ_Dump/output/`。

```powershell
python -X utf8 Tools/CharacterReplication/build_guide.py --sources Tools/CharacterReplication/sources/unagi.json
python -X utf8 Tools/CharacterReplication/summarize_character.py --sources Tools/CharacterReplication/sources/unagi.json
python -X utf8 Tools/CharacterReplication/build_guide.py --sources Tools/CharacterReplication/sources/unagi.json
```

将 `unagi.json` 换成 `anbi.json` 即可重建安比；Corin 的既有包使用 `corin.json`。

输出包括按动作查看的动画身份、转场混合、时间区域、事件、攻击配置、镜头参数、技能和点击/长按映射，另外保存机器可读 JSON、原始来源哈希及同身份变体。没有绘图，不启动 Unity，不改作者资产。

运行 `python -X utf8 Tools/CharacterReplication/build_index.py` 重建总入口：`D:/ZZZ_Dump/output/character_replication/README.md`。

## 来源和绑定

- 二进制 Odin 解释继续复用 `D:/ZZZ_Dump/kern_tools/OdinBinaryDecoder.py`，按原节点展开内部引用。
- 配置清单和动画导出清单以显式格式读取，不用相似文件名推断类型。
- Unagi、Anbi 的事件绑定读取 `AnimatorStateEventPatternsDict.pairList`，保留不同状态共用同一事件组的关系。
- 动画按 `SerializedFile + PathID` 关联；未绑定和缺失文件会写入对账结果。
- 同名角色模型、角色配置与情绪配置用原资源身份区分；不按名称合并不同对象。
- 输入缓冲、未知枚举、负时间和标准配置键不补默认值。
- 公共曲线只复用原始公共资源证据，不借用另一个角色的镜头参数。

## 既有公共证据重建

共享资源的原始导出位于 `D:/ZZZ_Dump/output/corin_replication/replication-guide/analysis/camera-resource-search/`。保留这个已经建立的精确来源路径，角色包只引用它，不复制成多份公共真相。

```powershell
python -X utf8 Tools/CharacterReplication/decode_native_curves.py
python -X utf8 Tools/CharacterReplication/decode_native_camera.py
python -X utf8 Tools/CharacterReplication/complete_camera_data.py
python -X utf8 Tools/CharacterReplication/analyze_corin_evidence.py
python -X utf8 Tools/CharacterReplication/build_guide.py --sources Tools/CharacterReplication/sources/corin.json
```

`decode_native_curves.py` 读取已确认的 Unity 原生 AnimationCurveLibrary 格式，校验完整文件消费。`analyze_corin_evidence.py` 保存此前 Corin 的变体和同版本有限函数证据；它不伪装成已分析其它角色的执行实例。

对外部定位器与 Odin 简化输出的修改证据仍位于共享分析目录中的 `locator-terms.diff`、`odin-positional-fields.diff`。原始资源不改写。

## 动画同步证据重建

`animation_sync.py` 是同一资料生成器的分析模块。每个角色的正式输出增加 `动画同步.md` 和 `data/animation-sync.json`，包括全部状态的时间参数、层关系、多子节点、自动偏移和移动转场；动作页同步展示时间与循环参数绑定。

```powershell
python -X utf8 Tools/CharacterReplication/scan_animation_calls.py
python -X utf8 Tools/CharacterReplication/analyze_animation_sync.py
python -X utf8 Tools/CharacterReplication/build_index.py
```

扫描器查找已确认 Animator 方法入口和原生跳转槽，核对调用点所在指令边界。分析器复用既有 PE/快照读取器，校验同版本哈希，保存完整函数、字段偏移、静态参数 ID 和文件常量，并比较重新导出的控制器是否改动既有字段。手工核实的传值链路与仍缺的证据维护在 `evidence/animation-sync.md`，随分析器发布到资料总目录。

新增的 `m_TimeParamID` 来自对原 AnimeStudio 类和结构化导出器的正式修正，来源配置统一指向同资源身份的完整导出。原导出仅保留作差异证据；没有新增 Unity 运行时导入链路。

## 镜头原生正文

`decode_native_camera.py` 的输入是既有 raw-camera 清单和同版本元数据，输出是原始字段值、每字段字节范围、来源哈希和格式清单。它复用已有 Odin 头部、Unity 曲线读取器及元数据导出器，读取 Shot、锁定 JSON 列表与基础镜头尾部。

Shot 两种布局按已核对的源文件 SHA-256 显式登记，不通过解析失败尝试另一种布局。三份紧凑布局中每条记录的 5 个字保留为未命名 uint；不猜字段名、不借新格式填值。所有对象要求消费到文件末尾。

`complete_camera_data.py` 将基础镜头原生字段并入原 camera-data 文件，同时生成共享 Shot/锁定索引及中文参数页。角色资料生成器按事件精确键引用共享 Shot，`resolved` 仅表示定义正文已定位；prefab 动画和运行时资源选择另行对账。

镜头资源扫描继续使用原 `CorinParryAssetLocator`，搜索词由 `complete_camera_data.py` 写入 `camera-resource-search/native-camera/prefab-terms.txt`。扫描结果存入 `camera-resource-search/shot-resource-search`，运行 `python -X utf8 Tools/CharacterReplication/index_shot_resources.py` 生成对账页。对象名匹配只登记候选，不伪装成 prefab、Timeline 与动画的引用闭环；扫描错误与对象解析错误分别保留。
