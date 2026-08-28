# Change: 稳定角色脚步Path换代与Landing可达

## Why

当前Corin Foot Placement已经完成唯一深Module、每脚唯一State Context、唯一Goal Set、唯一FBBIK和根Bank事务重构，但仍保留`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`的两项行为问题：Ground Path更新时脚部Correction会出现可见跳变，Landing前脚目标可能超出腿链可达范围并把膝盖拉到接近完全伸直。

封口诊断包已经把问题分开，但当前Path结论需要收紧：`Releasing -> Swing`同帧Floor顺序修复和取消Ground Path identity单独触发Residual重置，清除了错误执行条件，却没有让用户观察到明显的整体抖动改善。最新`20260826-203655`诊断中178次有效Path变化仍有42次Correction单帧跳变超过2厘米，P90为0.03016米、P99为0.12405米、最大值为0.14170米；1042至1049帧的下一Landing端点只变化0.01063米，Correction却跳变0.12282米，说明当前主要问题不是Residual缺少Landing截止时间，而是Raw Path目标之后仍存在同帧放大或输出所有权切换。Landing腿伸直则继续伴随Foot Goal满权、Landing只有一帧、Primary Support缺失和目标压缩余量归零。

`build-character-foot-motion-data-foundation`已经由用户验收并归档，现行spec已经安装可审查的Step Time/Distance、Foot Height、Contact、Lock Mode/Weight与Support作者数据，并继续禁止没有正式行为change时的Runtime消费。本change建立唯一正式消费者，按独立小步迁移行为，不恢复旧隐藏Foot Feature、旧PlantConfidence政策、第二状态机或Goal后处理器。

当前`CharacterFootStateMachine`仍是传统中央状态机：同一个`Evaluate`与状态分支同时判断Transition、创建或释放Anchor、计算State Target、推进`SwingResidual`/`AcquireResidual`/`ReleaseResidual`、写`EffectiveCorrection`、执行Safety Floor并推导Support。结果是一次Landing或Release问题同时跨越离散状态、连续插值和硬约束，任一行为修改都可能把误差从Swing搬到Landing、Locked或Release。正式Foot Motion接入前必须先把这三类责任拆开，否则新Contact、Lock Weight与Support只会继续塞进旧状态分支。

## Current Diagnostic Record

- 3.1修正了`Releasing -> Swing`同帧漏过Swing Ground Floor的执行顺序；该修正属于地面安全一致性，不足以解释整体Path抖动。
- 3.2删除了Ground Path identity单独变化造成的Residual重置；用户端到端观察为抖动基本不变，因此不得把它记录为可见问题的主修复。
- 正式Step Time只提供Landing前Residual欠账的截止时间。它不能解释Raw Landing端点变化约1厘米而Correction同帧跳变约12厘米的放大，不得在首个不连续阶段尚未定位时作为当前抖动修复接入。
- 逐阶段诊断已经把`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Safety Floor -> Encoded Goal`放在同Frame与Event lineage下对账，并以typed失败阻止缺失阶段时继续生成伪结论。
- `20260826-231414`在右脚4367到4368帧证明旧Event的Promoted Contact Landing遮住同帧新Event的Accepted Swing Landing，使State Machine错误发布`PathAvailable 1 -> 0 -> 1`。Swing Path Landing与Contact Landing必须拆成两个typed输入；该修复不改变后续Residual截止衰减政策。
- canonical nearest对照已经证明，上一Committed `SurfaceIdentity`通过`PreferredSurfaceIdentity`参与下一帧候选选择时，相同查询几何会因历史不同得到不同Surface；超过5厘米的23次CurrentFloor catchup中18次、超过10厘米的14次中13次来自旧Surface覆盖nearest。FutureLanding必须先收敛为相同canonical Observation Key复用不可变结果、新Key只查询一次的纯世界事实，历史只允许参与后续5毫米Acceptance。

## What Changes

- 从当前选中动画Source的原生Foot Motion Curve生成唯一typed Runtime Foot Motion Frame，并保持Source、Cycle、Contribution、Completion、Clip与Landing Event lineage。
- 保留已经完成的`Releasing -> Swing`顺序修正和identity触发清理，先对Ground Path到Encoded Goal的逐阶段Correction做同帧归因，修复首个已证明的不连续阶段；之后才接入Residual截止收敛与真实Envelope安全边界。
- 只把Step Time/Distance接入Landing Prediction，保持世界落点仍由正式Future Body Translation、RootLocalLanding与唯一SphereCast生成。
- 为每脚FutureLanding建立根事务所有的Committed/Pending Observation Page；每帧继续重新投影Raw Landing，但相同canonical Observation Key不重复SphereCast，新Key始终选择canonical最近合法Surface并删除历史Surface偏好。
- 把每脚约束执行固定为`不可变输入与Observation -> Pre-Interpolation Transition -> State Target -> 统一Interpolation -> Post-Interpolation Transition -> Hard Constraint -> Resolved Foot`。同一根事务按固定顺序执行一次，不建立第二状态机或第二输出路径。
- 用独立typed `CharacterFootTransitionResolver`声明固定Transition边、判定阶段和优先级。Resolver只生成不可变Decision；唯一Transition Runtime应用State与Anchor命令，不执行插值、不查询世界、不写Goal。
- 用纯`CharacterFootStateTargetResolver`按Transition后的离散State生成Correction Target、接触引用、Goal/Ownership目标和Interpolation Policy Request。State Target不得保存时间状态、推进Residual或跳转到另一State。
- 用唯一typed `CharacterFootInterpolationRuntime`拥有Effective Correction、唯一Residual、上一Target与Completion。Swing Path换代、Landing Acquire和Release都提交固定Policy Request给它执行；删除分散在State分支中的`SwingResidual`、`AcquireResidual`、`ReleaseResidual`、`ContactProgress`与重复`Advance`数学。
- Swing/UnlockedSupport的State Target只使用正式Ground Path、Envelope与Foot Height；插值后的Hard Constraint复用同一Ground Path Envelope作为安全下界。删除逐帧CurrentSwingFloor Query及全部结果、诊断和Owner语义。Releasing继续只回到原始Swing目标。
- 只把Foot Height接入Swing，使动画抬脚高度叠加到Runtime Ground Envelope，删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`提前改脚目标的旧政策。
- 只把Support接入Resolved Foot、Primary Support与Pelvis，使承重意图不再依赖Lock资格，并为Landing腿提供独立Reach请求。
- 增加必须显式序列化的米制最小Landing腿压缩余量；缺失即typed invalid，不提供默认值。Pelvis优先求双腿可达交集，无法同时满足时夹紧Foot Goal并发布typed不可达结果，不允许完全伸直后继续进入Full Lock。
- 最后用Contact、Lock Mode和Lock Weight替换旧PlantConfidence的Landing、Locked、Sliding与Release生命周期；由独立Transition、State Target与统一Interpolation链执行，Anchor与Interpolation各自只有一个typed Owner。
- 每迁移一个消费者就删除对应旧输入与旧解释，不保留新旧双读、fallback、运行开关或兼容reader。
- Foot诊断采样包把每Frame/Side唯一阶段事实与一对多Ground Contact/Envelope几何拆成正式主表和几何表；停止录制后由唯一后台Finalizer排空、封存、分析并发布，不在Unity主线程等待Writer或扫描CSV。

## Dependency And Ordering

- `build-character-foot-motion-data-foundation`已经由用户验收并归档；本change不得反向修改Analyzer候选、Motion Reference或AnimationClip作者数据。
- `refactor-character-pose-graph-architecture`只改变Program Operation、Constraint Bank与Final Publication所有权；本change先固定Foot模块的typed输入、唯一Result和根事务边界，Pose Graph重构把Foot视为不透明Constraint能力，不规定其内部Transition与Interpolation布局。
- 行为迁移固定按`Path逐阶段归因 -> 首个不连续阶段修复 -> 拆分State/Transition/Interpolation/Hard Constraint -> Step Time/Distance -> Foot Height -> Support/Pelvis -> Landing Reach -> Contact/Lock`执行。Step Time不得用来掩盖同帧Correction放大；后一步不得在前一步仍有双主线时开始。

## Impact

- Affected specs: `character-foot-placement-presentation`、`character-animation-pipeline`
- Affected runtime: AnimationClip/Source Foot Motion采样、Presentation Projection、Foot Placement Pose Input、Landing Prediction、Ground Path、Foot Discrete State/Transition/State Target/Interpolation/Hard Constraint、Resolved Foot、Primary Support、Pelvis与Goal编码
- Affected editor/build: Projection Compiler、正式Curve/Event lineage校验、Corin Float32/Fixed产品显式重建
- Affected diagnostics: 正规化采样包、后台Finalizing生命周期、Raw Landing/Path Target、Swing Target、Residual Capture、State Output、Safety Floor、Encoded Goal、Envelope clearance、Landing Reach、Support来源、Solved/Physical结果与typed拒绝原因
- 不改变Gameplay State、World State、KCC、网络packet、rollback snapshot或Simulation Program行为

## Current Spec Comparison

- current `character-foot-placement-presentation`已经删除`8fc704a`公式、PlantConfidence生命周期和单体`CharacterFootStateMachine/Context`实现约束，只保留一个权威Foot结果、typed状态单一写入、根事务和下游隔离边界。本change通过delta把内部实现收紧为独立Transition、纯State Target、统一Interpolation与Hard Constraint固定顺序。
- current spec只要求时间连续化、Transition和Hard Constraint不能互相越权，不规定具体策略。本change安装正式Foot Motion输入、Transition边、Interpolation Policy和迁移完成后的旧字段删除要求。
- current Resolved Foot与Pelvis合同只规定下游不得读取Foot内部状态。本change进一步安装与Lock分离的正式Support Intent、Landing Reach和无交集时的Goal夹紧政策。
- current `character-animation-pipeline`规定新增22条Curve在没有正式消费者时不进入Runtime；本change通过新的Animation Pipeline requirement建立唯一Runtime Frame，并在消费者迁移时取代对应的旧Runtime输入。
- active `refactor-character-pose-graph-architecture`对Foot的delta只调整Constraint/Final Publication所有权，与本change不矛盾；实施时仍需按其最终Program lineage重新对账。

## Non-Goals

- 不实现Heel/Toe双点IK、脚掌旋转、移动平台、Reactive Foot或第二种Ground Query。
- 不增加第二Foot State Machine、第二Goal Set、第二FBBIK、全局Goal低通或图外骨骼修正。
- 不创建字符串Channel、Dictionary字段、任意Tween注册表或跨Foot/PoseState/AnimationSlot共享的全局插值服务。统一Interpolation只服务Foot Constraint的固定typed通道，不复用Pose Transition Routing。
- 不用膝盖最小角度直接覆盖FBBIK结果，不用降低Goal Weight掩盖不可达。
- 不修改Foot Motion Analyzer算法、正式AnimationClip曲线或Motion Reference绑定。
- 不处理TrainingEnemy，不顺手迁移其Pose Graph、曲线、Projection或Foot配置。
- 不新增自动测试；实施只复用现有编译、严格OpenSpec校验和封口诊断重放。

## Success Criteria

```text
每帧只有一个正式Foot Motion Runtime Frame且lineage匹配选中动画贡献
Step、Foot Height、Support、Contact/Lock各自只有一个正式消费者
Releasing完成并回到Swing的同一帧执行新Swing Envelope保护
普通Path Revision保持Correction连续且向上/向下响应使用同一Residual政策
Raw Landing或Swing Target的小变化不得在State Output、Safety Floor或Encoded Goal阶段被无依据放大
真实Ground Envelope最低安全高度不被平滑穿过
首个同帧不连续阶段修复后，Landing前Residual才按剩余时间收敛到LandingUpdateDistance以内或发布明确不可达
Swing目标使用Runtime Envelope加正式Foot Height
Support Intent不由Contact或Lock门控且Pelvis不出现无依据支撑空洞
Landing腿与支撑腿Reach区间有交集时Pelvis输出位于交集内
无交集时Foot Goal夹紧并拒绝Full Lock，不产生超长腿目标
Contact/Lock生命周期只读取正式Contact、Lock Mode与Lock Weight
每条Foot Transition只由唯一typed Resolver判定并由唯一Transition Runtime应用
State Target不拥有Transition、Residual、HalfLife、时间推进或Hard Constraint
Swing、Landing Acquire与Release只通过一个Interpolation State、一个Residual和一个Effective Correction Owner连续化
Pre/Post Transition、Interpolation与Hard Constraint顺序固定且每帧各执行一次
Ground Path Envelope与Reach只约束插值后的结果，不反向修改State、Transition、Target或Residual
Swing Hard Constraint只消费已接受预测Path；预测输入不变时不得执行实时地面查询或逐踏面切换输出
旧隐藏Step、Constraint、PlantConfidence和Support消费者被删除
不存在fallback、旧新双读、第二状态机、第二Goal链或TrainingEnemy变化
```
