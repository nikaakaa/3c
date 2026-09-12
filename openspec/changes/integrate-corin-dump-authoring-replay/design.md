## Context

本设计承接 `proposal.md` 和六组delta spec。当前Corin的上游资料包含外部ZZZ Dump索引（工作区可见 `Tools/Rendering/CorinRenderData/整理数据.json` 及其Dump输出引用）、Unity正式作者资产、BTSMTL v7 Document、Character Program/Projection产物和Fixed Input/Runtime Dump。它们目前可以单独存在，但不能用同一身份证明“这次Replay确实运行了这批配置”。

Corin正式根是 `Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。现有BTSMTL、Character Build和3C Development Center已经分别拥有Document生命周期、精确Build入口、固定输入Trace与Replay Gate；本设计只增加它们之间的身份和串行Gate，不新增编译器、Replay执行器或第二份运行时配置。

## Goals / Non-Goals

2026-09-12 接收边界：除下述来源闭环外，本change第6节接收旧FlowCanvas任务4.5.5的网络Pass/Adapter收尾和8.1的正式Skill运行对账。网络只接现有SessionSource/Pipeline/Adapter，不能新增Skill专用网络语义或第二运行链。FSM节点、Document v8与作者资产迁移消费integrate-native-fsm-skill-authoring成果；通用观察消费finish-skill-runtime-observation成果，不在本change实现。

网络校验输入为精确Composition、Program及同一请求/输入，输出为兼容结果、实际网络载荷、状态/hash/snapshot及运行事件关联。业务取舍：统一Program使本地、Rollback和Authority复用技能语义，代价是必须核对各正式Pass的输入确认与恢复边界；只做静态兼容检查不能替代实际载荷和中断证据。此前v7记录只表示当时协议，后续Run固定采用作者change正式发布的唯一schema，不独立维护兼容版本。

**Goals:**

- 用一个不可变Dump Source Manifest描述外部来源及归一化后的正式Unity资产。
- 从Corin Definition计算完整作者闭包，并为每个Gate生成可追溯的canonical identity/hash。
- 让Document、Build、Session、Runtime Dump和Replay使用同一组Definition/Program/Projection/Session/Trace身份。
- 让失败停在明确Gate，禁止旧产物、其他worktree或目录扫描补齐缺失配置。
- 使用现有3C Development Center的change、RunHost、fixed input trace、replay和compare流程，保存前后证据。

**Non-Goals:**

- 不把ZZZ Dump直接加载进Runtime或Program。
- 不把Runtime Dump转成作者配置，不让生成Program/Projection反向修改Definition。
- 不替代Agent metadata重构；该工作由独立change负责，本change只消费其稳定v7生命周期。
- 不新增临时Replay执行器、不新增测试代码、不启动Player性能采集流程。
- 不在本change里重写Pose、Foot Analysis、IK或网络Pass实现；它们只作为闭包依赖和Gate输入。

## Decisions

### 1. 三类数据严格分层

将数据分成三类：

1. `Source Dump`：外部资源来源与导入证据，只能通过manifest进入作者资产 provenance。
2. `Authoring/Build`：Unity正式作者资产、v7 Document和正式Program/Projection，是可重建的业务配置与产物。
3. `Runtime/Replay Evidence`：Runtime Dump、固定输入Trace、RunId和比较结果，是验证证据，不是作者输入。

选择分层而不是让Dump直接成为Runtime输入，是因为外部Dump路径不可移植，也不应该决定确定性Simulation的运行语义。直接读取Dump虽然短期省掉导入步骤，但会让Replay依赖外部机器目录、无法进行稳定hash和正式资产回滚。

### 2. Source Manifest保存逻辑身份，不保存运行时绝对路径

Source Manifest以Character、SourceSet、ArtifactKind、SourceIdentity、Revision、ContentHash和NormalizedAssetIdentity为核心字段。原始绝对路径只放在Editor侧导入证据或不可发布的诊断中；Document、Program、Projection和Runtime注册只使用稳定来源identity、正式Unity GUID/路径和hash。

Manifest必须能表达：模型/Rig、AnimationClip、BlendSpace、Binding、Foot Analysis、Curve、Pose Source Slot和外部快照之间的依赖。遇到同一SourceIdentity多个内容hash、缺少归一化资产或来源hash变化，Gate直接失败。

### 3. 以Definition闭包作为唯一Corin配置索引

从精确Definition向下解析Control/Input、Skill/Action、GameplayEffect/Tag/Attribute、Motion、Presentation/Pose、Foot/IK/Blend、Timeline、Session Composition、Prefab/Scene和Generated Product引用。所有列表按稳定identity排序后计算ClosureHash；不通过目录扫描补项。

选择Definition闭包而不是维护一个新的Corin总配置文件，是因为Definition已经是Character Pipeline的装配根。额外总配置会再次形成第二作者真相，最终会和BTSMTL Document或Unity资产漂移。

### 4. Gate使用单向hash链

Gate身份按以下顺序生成：

```text
SourceManifestHash
  -> AuthoringClosureHash
  -> DocumentHash / PlanHash
  -> ProgramHash / LayoutHash / ProjectionHash
  -> SessionCompositionHash
  -> ReplayRequestHash / RuntimeDumpHash
```

每个后续Gate记录前置hash，禁止使用较旧的hash继续。Build只在Document apply成功、Definition Closure没有变化时执行；Replay只接受同一Program/Projection/Session和Fixed Input Trace身份。

### 5. Build按Numeric Target分组发布

Float32、Fixed、Rollback和Server Authority分别建立Target Product Group，组内保存Definition、Program、Projection、Session、Prefab/Scene和Network Model identity。Fixed Replay默认使用Fixed目标；Float32验证单独记录。不能用另一个worktree的Program或Projection填充缺失产物。

### 6. Replay复用现有Center，不新建执行器

Replay由既有3C Development Center change记录、正式RunHost和`character.fixed_input_trace`生命周期负责。变更前如果有同条件基线就attach，否则先run before；修改/整合完成后run after，再执行compare和review。运行期间锁定worktree、Unity实例、源码版本、输入Trace和配置hash，不能在运行中继续修改被测工作区。

Replay比较至少包含Input/Tick、ActionInstance/generation、Timeline事件、Program/Layout/Projection身份、State/Snapshot hash和Body结果。缺少Foot、Camera、Pose或性能专门分析时只报告未验证，不把Body轨迹一致推断为完整表现通过。

### 7. Gate失败不做隐式修复

来源、Document、Build、Session或Replay任一Gate失败时保留机器诊断和当前hash，停止后续Gate。禁止自动rebase、旧产物替换、Dump路径fallback、其他worktree复制或手工YAML修补。修复后从受影响的最早Gate重新运行。

## Risks / Trade-offs

- [Dump索引引用外部目录，换机器后不可用] → 运行身份只保存SourceIdentity和hash；导入期检查外部路径存在，发布/Replay不依赖该路径。
- [Corin闭包过大，单次核对耗时] → 以Definition依赖图分层hash并缓存只读结果，变化从最早受影响节点开始失效，不维护第二份配置。
- [Pose/Foot/Agent仍在其他change变动] → 只在它们产生稳定提交和正式Build/Document接口后执行下游Gate；当前工作区变化不能作为Replay基线。
- [Replay结果与配置看似相同但初始状态不同] → ReplayRequest固定初始Scene/Prefab/Actor/World状态和Trace hash，并将State/Snapshot hash作为比较前置条件。
- [Dump资源和正式AnimationClip不一致] → Source Manifest必须保存NormalizedAssetIdentity与内容hash，Build前复核引用；不允许仅凭名称或路径猜测。

## Migration Plan

1. 只读盘点Corin Dump索引及其引用，形成Source Manifest草案和来源缺口表。
2. 从精确Definition导出Authoring Closure，补齐来源identity、正式owner和生成产品引用；不写入Runtime。
3. 等Agent/Pose相关change稳定后执行v7 checkout、dry-run、apply和re-checkout，确认同一DocumentHash/`Clean`。
4. 依次执行Float32、Fixed及需要的Rollback/Server Authority Build，发布各自Product Group并核对Program/Layout/Projection hash。
5. 固定Corin运行目标、Session Composition、Prefab/Scene和Input Trace，执行Play/Skill/Timeline/中断观察与Runtime Dump。
6. 用3C Development Center attach/run before/after、replay和compare，保存RunId、RuntimeDumpHash、ReplayRequestHash和业务review。
7. 只有所有Gate通过后，才更新当前spec、tasks和最终交付记录；任何失败保留失败证据，不把部分闭环标为完成。

### 8. Camera是Presentation owner，不是场景散落参数

Camera与Pose、Timeline一样进入Corin Presentation Closure。当前Corin作者配置至少包含：`CorinCharacterCameraProfile`（schema `character-camera-profile/v1`、ProfileId `corin.camera.profile`、DefaultSequence、NearClipPlane `0.1`、FarClipPlane `2000`、CameraLocateRadius `3.75`、DefaultElevationAngle `3.4336302`、DefaultFieldOfView `50`、DefaultSmoothTime `0.15`、RotationTransitionSeconds `3`、ChangeAvatarTransitionSeconds `0.3`，以及Input/Locking/Collision/Zooms/Stretches/Shakes/Shots/Curves/TargetSlots）；`CorinCameraDefaultSequence`（schema `character-camera-sequence/v1`、SequenceId `corin.camera.default.normal`、Stages、TimeDomain）；以及当前默认Camera Curve（schema `character-camera-curve/v1`、TimeDomain、Normalized Unit、Min `0`、Max `1`、Pre/Post Wrap）。

动作相关Camera Sequence、Camera Curve和Timeline Camera/Scene请求必须通过Profile/Sequence owner与stable identity引用，不能把数值复制进Skill节点或场景Prefab。Replay同时固定Camera Profile/Sequence/Curve hash和Camera Input Trace；缺少镜头采样时只能报告Gameplay/Body结果，不能声称完整表现闭环。

### 9. 配置字段与数值清单作为交付物

配置盘点不只列资产文件。对每个正式Corin作者资产记录schema、identity、owner、引用、枚举/模式、所有标量/vector/quaternion/时间/权重/阈值/速度/角度/距离/迭代次数/容量和曲线元数据；对Animation/Pose Transition记录Blend Logic、Duration、Blend Mode、Custom Curve、Blend Profile、Slot/Layer/Mask、Inertialization、Source Slot和Animation Channel。生成Program/Projection只记录输入依赖与输出hash，不逐字段复制生成字节。

这份清单是来源和验收索引，不成为第二份配置；正式资产和metadata仍是唯一真相。
