# Change: 稳定角色脚步Path换代与Landing可达

## Why

当前Corin Foot Placement已经完成唯一深Module、每脚唯一State Context、唯一Goal Set、唯一FBBIK和根Bank事务重构，但仍保留`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`的两项行为问题：Ground Path更新时脚部Correction会出现可见跳变，Landing前脚目标可能超出腿链可达范围并把膝盖拉到接近完全伸直。

封口诊断包已经把问题分开，但当前Path结论需要收紧：`Releasing -> Swing`同帧Floor顺序修复和取消Ground Path identity单独触发Residual重置，清除了错误执行条件，却没有让用户观察到明显的整体抖动改善。最新`20260826-203655`诊断中178次有效Path变化仍有42次Correction单帧跳变超过2厘米，P90为0.03016米、P99为0.12405米、最大值为0.14170米；1042至1049帧的下一Landing端点只变化0.01063米，Correction却跳变0.12282米，说明当前主要问题不是Residual缺少Landing截止时间，而是Raw Path目标之后仍存在同帧放大或输出所有权切换。Landing腿伸直则继续伴随Foot Goal满权、Landing只有一帧、Primary Support缺失和目标压缩余量归零。

`build-character-foot-motion-data-foundation`已经由用户验收并归档，现行spec已经安装可审查的Step Time/Distance、Foot Height、Contact、Lock Mode/Weight与Support作者数据，并继续禁止没有正式行为change时的Runtime消费。本change建立唯一正式消费者，按独立小步迁移行为，不恢复旧隐藏Foot Feature、旧PlantConfidence政策、第二状态机或Goal后处理器。

## Current Diagnostic Record

- 3.1修正了`Releasing -> Swing`同帧漏过Swing Ground Floor的执行顺序；该修正属于地面安全一致性，不足以解释整体Path抖动。
- 3.2删除了Ground Path identity单独变化造成的Residual重置；用户端到端观察为抖动基本不变，因此不得把它记录为可见问题的主修复。
- 正式Step Time只提供Landing前Residual欠账的截止时间。它不能解释Raw Landing端点变化约1厘米而Correction同帧跳变约12厘米的放大，不得在首个不连续阶段尚未定位时作为当前抖动修复接入。
- 逐阶段诊断已经把`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Safety Floor -> Encoded Goal`放在同Frame与Event lineage下对账，并以typed失败阻止缺失阶段时继续生成伪结论。
- `20260826-231414`在右脚4367到4368帧证明旧Event的Promoted Contact Landing遮住同帧新Event的Accepted Swing Landing，使State Machine错误发布`PathAvailable 1 -> 0 -> 1`。Swing Path Landing与Contact Landing必须拆成两个typed输入；该修复不改变后续Residual截止衰减政策。
- `20260826-235506`证明上述假Path换代已经消失；117个唯一无Anchor Swing Path事件仍有13个非Safety Floor跳变超过2厘米，且13个全部启用了Deadline加速。直接替换Formal Step Time会让其中12个截止更短，因此正式政策不得缩短HalfLife；Formal Step Time只计算均匀Required Step，基础HalfLife定义单帧最大响应，来不及偿还时发布`Unavailable`并继续基础响应。

## What Changes

- 从当前选中动画Source的原生Foot Motion Curve生成唯一typed Runtime Foot Motion Frame，并保持Source、Cycle、Contribution、Completion、Clip与Landing Event lineage。
- 保留已经完成的`Releasing -> Swing`顺序修正和identity触发清理，先对Ground Path到Encoded Goal的逐阶段Correction做同帧归因，修复首个已证明的不连续阶段；之后才接入Residual截止收敛与真实Envelope安全边界。
- 先让Formal Step Time成为Swing Residual唯一截止时域，用均匀Required Step和基础HalfLife速度上限生成Scheduled、WithinTolerance或Unavailable结果；之后再把Step Time/Distance接入Landing Prediction，保持世界落点仍由正式Future Body Translation、RootLocalLanding与唯一SphereCast生成。
- 只把Foot Height接入Swing，使动画抬脚高度叠加到Runtime Ground Envelope，不再用旧`LandingConstraintWeight * BaselineHeightError`提前把脚拉向地面。
- 只把Support接入Resolved Foot、Primary Support与Pelvis，使承重意图不再依赖Lock资格，并为Landing腿提供独立Reach请求。
- 增加米制最小Landing腿压缩余量；Pelvis优先求双腿可达交集，无法同时满足时夹紧Foot Goal并发布typed不可达结果，不允许完全伸直后继续进入Full Lock。
- 最后用Contact、Lock Mode和Lock Weight替换旧PlantConfidence的Landing、Locked、Sliding与Release生命周期；仍由唯一`CharacterFootStateMachine`和同一个Anchor/Residual Owner执行。
- 每迁移一个消费者就删除对应旧输入与旧解释，不保留新旧双读、fallback、运行开关或兼容reader。
- Foot诊断采样包把每Frame/Side唯一阶段事实与一对多Ground Contact/Envelope几何拆成正式主表和几何表；停止录制后由唯一后台Finalizer排空、封存、分析并发布，不在Unity主线程等待Writer或扫描CSV。

## Dependency And Ordering

- `build-character-foot-motion-data-foundation`已经由用户验收并归档；本change不得反向修改Analyzer候选、Motion Reference或AnimationClip作者数据。
- `refactor-character-pose-graph-architecture`只改变Program Operation、Constraint Bank与Final Publication所有权；本change继续使用其唯一Foot Module和根事务，不建立并行执行链。
- `add-character-foot-penetration-diagnostics`若继续实施，只拥有只读诊断基础设施；本change可以要求正式事实，但不得复制Analyzer、Reporter或CSV写入架构。
- 行为迁移固定按`Path逐阶段归因 -> 首个不连续阶段修复 -> Step Time/Distance -> Foot Height -> Support/Pelvis -> Landing Reach -> Contact/Lock`执行。Step Time不得用来掩盖同帧Correction放大；后一步不得在前一步仍有双主线时开始。

## Impact

- Affected specs: `character-foot-placement-presentation`、`character-animation-pipeline`
- Affected runtime: AnimationClip/Source Foot Motion采样、Presentation Projection、Foot Placement Pose Input、Landing Prediction、Ground Path、Swing、Foot State Context、Resolved Foot、Primary Support、Pelvis与Goal编码
- Affected editor/build: Projection Compiler、正式Curve/Event lineage校验、Corin Float32/Fixed产品显式重建
- Affected diagnostics: 正规化采样包、后台Finalizing生命周期、Raw Landing/Path Target、Swing Target、Residual Capture、State Output、Safety Floor、Encoded Goal、Envelope clearance、Landing Reach、Support来源、Solved/Physical结果与typed拒绝原因
- 不改变Gameplay State、World State、KCC、网络packet、rollback snapshot或Simulation Program行为

## Current Spec Comparison

- current `character-foot-placement-presentation`要求Swing、Landing、Support、Pelvis和Goal对`8fc704a`逐帧等价，并固定用PlantConfidence驱动Landing、让Landing发布`SupportEligibility=None`。这些约束与本change的行为修复直接冲突，必须由delta替换。
- current spec把完整Swing Correction当作`RaiseToFloor`下界；本change把连续目标与真实Envelope最低安全高度分开，只有后者是硬约束。
- current spec只让Pelvis消费已锁定Primary Support的Reach Reference，并明确禁止提前接入Landing腿；本change增加与Lock分离的Support Intent和Landing Reach合同，因此必须修改该要求。
- current `character-animation-pipeline`规定新增22条Curve在没有正式消费者时不进入Runtime；本change通过新的Animation Pipeline requirement建立唯一Runtime Frame，并在消费者迁移时取代对应的旧Runtime输入。
- active `refactor-character-pose-graph-architecture`对Foot的delta只调整Constraint/Final Publication所有权，与本change不矛盾；实施时仍需按其最终Program lineage重新对账。

## Non-Goals

- 不实现Heel/Toe双点IK、脚掌旋转、移动平台、Reactive Foot或第二种Ground Query。
- 不增加第二Foot State Machine、第二Goal Set、第二FBBIK、全局Goal低通或图外骨骼修正。
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
旧隐藏Step、Constraint、PlantConfidence和Support消费者被删除
不存在fallback、旧新双读、第二状态机、第二Goal链或TrainingEnemy变化
```
