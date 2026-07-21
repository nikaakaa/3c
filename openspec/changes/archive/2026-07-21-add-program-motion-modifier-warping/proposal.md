# Change: 增加 Program Motion Modifier 与 Motion Warping 闭环

## Why

当前 Character Simulation 已经把 Locomotion、Timeline MotionCurve 与 GameplayResult 收敛为 `SimulationMotionContribution`，并按 `Locomotion -> Action -> GameplayResult` 固定通道解析为唯一 `CharacterMotionRequest`。WorldSolver 仍是唯一实际移动与碰撞执行者，这部分方向正确。

缺口位于通道解析与 Request 之间。当前 `Float32MotionAccumulator` 和 `FixedMotionAccumulator` 在完成通道仲裁后立即构造 Request，没有正式的 Motion 后处理阶段。因此需要根据目标修正动作轨迹时，只剩三种错误选择：

1. 让 Timeline 直接修改 MotionCurve contribution，把原始动画位移与目标修正混成一个来源。
2. 让 Unity CharacterController、KCC 或其它 Solver理解动作和 Timeline，破坏 Solver 可替换边界。
3. 在 Presentation 或 Animancer 中修 Transform，只改变画面而不改变权威 Gameplay body。

仓库中已经存在 `MotionWarpTrack`、`MotionWarpClip` 和 `TimelineMotionWarpWindow`，但它们只在 Unity authoring 文件中自行采样；Semantic IR、Float32/Fixed Program、Kernel 和 Motion accumulator 都没有对应 operation。作者能够看见配置，正式运行时却不消费它，形成了明确的半接入分裂路径。

Action 链已经具备 `ActionTargetSnapshot`：`ActivateActionInstance` 可以从 Blackboard读取目标快照，Float32/Fixed ActionInstance也会保存目标identity、position和yaw。当前缺少的是：

- ActionProfile 对“该动作是否必须有目标快照”的类型化声明。
- MotionCurve解析结果与目标修正之间的正式 Modifier合同。
- MotionWarp authoring 对源 MotionCurve、目标姿态、修正窗口和累计进度的明确表达。
- Float32、Fixed、Snapshot、Preview、Diagnostics和Agent对同一语义的完整支持。

本change恢复项目原本的管线思想，但不恢复旧 `CharacterMotionStage`：

```text
Gameplay operations
  -> MotionContribution
  -> 固定 Motion Channel 仲裁
  -> 固定 Program Motion Modifier 管线
  -> CharacterMotionRequest
  -> WorldSolver batch
  -> actual Body Result
```

`MotionWarp`作为第一个正式Modifier，只修正已被Action通道选中的Root Motion轨迹，并始终发生在WorldSolver之前。

## What Changes

- 将当前Motion accumulator拆成“按通道解析、按通道后处理、按固定顺序合成、构造Request”四个明确阶段；没有Modifier时结果必须与当前实现逐字段一致。
- 增加版本化的numeric-neutral Motion Modifier合同。Modifier只能读取当前Actor/Tick的已解析通道、Program静态配置、committed body、Action Context和自身typed state，不能查Unity场景或调用Solver。
- 增加`TimelineMotionWarp` Semantic operation和Float32/Fixed Target实现，把MotionWarp作为Action通道的正式后处理，而不是新的motion来源或第二个scheduler。
- 重构现有`MotionWarpTrack`和`MotionWarpClip`为纯authoring类型，删除`TimelineMotionWarpWindow`、`MotionWarpTrack.Sample()`、独立weight/ease采样和`TargetKey`字符串入口。
- 让`MotionWarpClip`通过稳定authoring identity显式引用同一Timeline中的一个`MotionCurveClip`。不按时间重叠、轨道名称、列表索引或CurveId猜测来源。
- 第一版只允许引用`Action` channel、`Override`语义的MotionCurve；被引用曲线必须是该Tick Action通道的resolved owner，否则Warp不执行。静态歧义在Compiler拒绝，动态出现多个eligible Warp时runtime fail-stop。
- 为MotionWarp authoring增加：平面位置模式、旋转模式、target-local平面偏移、位置/旋转权重、最大总位置/旋转修正和两条canonical累计进度曲线。第一版不处理垂直warp。
- 将ActionProfile现有自由字符串`TargetPolicy`替换为类型化`ActionTargetRequirement`：`None`或`SnapshotRequired`。`CanActivateAction`和`ActivateActionInstance`共用的唯一admission evaluator必须同时检查该要求。
- MotionWarp只读取ActionInstance在激活时捕获的固定`ActionTargetSnapshot`。不读取live Transform、scene registry、Presentation target或Network Model私有对象；同一ActionInstance运行期间目标不追踪移动。
- 在Warp窗口首次激活时，根据committed body、源MotionCurve剩余轨迹和固定目标快照计算一次总修正；每Tick按位置/旋转累计进度差提交增量修正。
- 将跨Tick所需的Warp generation、窗口起始姿态、总修正和上一累计进度存入Program typed state slot，并纳入Float32/Fixed State codec、Snapshot与Hash。当前Tick的resolved channel和最终Request继续是transient。
- 保持WorldSolver为唯一碰撞和实际位移权威。Warp请求到达墙体时，Solver实际结果可以阻止角色到达目标，Warp不得在Finalize或Presentation中补写位置。
- 提升Operation Set、Semantic IR、Float32/Fixed Target ABI、Program artifact与State codec相关identity；删除旧reader和兼容分派，不提供migrator、fallback或双写。
- 扩展Timeline Editor：编辑MotionWarp source、模式、偏移、限制和进度曲线；完整Gameplay Preview必须通过隔离Preview Session并显式提供editor-only目标快照，纯动画预览不得伪造Warp。
- 扩展Structured Trace、Semantic IR Inspector和Live Debug，显示原始Contribution、Action通道resolved结果、Warp目标/总修正/当前进度、最终Request和Solver actual result。
- 扩展Agent v9 Snapshot/Patch、typed command、handler、emitter和validator，使Agent能够读取、创建、修改和删除MotionWarp Track/Clip及其稳定source引用；不建立第二套authoring路径。
- 删除旧MotionWarp采样DTO、未消费字段、字符串TargetPolicy、旧artifact reader和所有旧Warp路径。

## Capabilities

### New Capabilities

- `character-motion-warp-authoring`：定义MotionWarp Timeline authoring、稳定源曲线绑定、目标姿态语义、累计进度曲线和配置拒绝规则。

### Modified Capabilities

- `character-motion-semantics`：在通道仲裁与CharacterMotionRequest之间增加唯一固定Motion Modifier阶段。
- `character-root-motion-curves`：明确MotionCurve是原始位移事实，允许正式Modifier在Solver前修正其resolved结果。
- `character-action-activation-flow`：将Action目标要求类型化，并让查询与激活共同拒绝缺少必需快照的动作。
- `btsmtl-gameplay-semantic-ir`：增加MotionWarp operation、source reference、capability和numeric-neutral payload。
- `btsmtl-compiled-simulation-program`：声明Motion Modifier跨Tick状态与同Tick transient边界。
- `btsmtl-timeline-editor-preview`：增加显式目标快照的正式Gameplay Preview和MotionWarp Live Debug。
- `agent-character-controller-synthesis`：让Agent v9完整理解并修改MotionWarp authoring。

## Dependencies And Sequencing

- 必须在`refactor-gameplay-runtime-and-tooling-modules`完成剩余模块边界后串行apply。本change复用其portable operation control和Float32/Fixed target port，不在旧大类中重新实现。
- 必须在`refactor-simulation-tick-hot-path`完成后串行apply。本change以Operation Set `/6`、结构化Program layout和稳定workspace所有权为输入，并升级到`/7`；不能并行修改同一Compiler、Program、layout、codec和evaluator文件。
- 依赖current `character-action-activation-flow`与`btsmtl-gameplay-semantic-ir`已经安装的唯一portable Action admission evaluator。apply前必须确认Agent Validator能读取合法owner-local declaration；不能通过跳过Agent校验接入MotionWarp。
- 必须同时覆盖Float32与Fixed。只做Float32会让Local/Server Authoritative可用而Rollback不可用，形成按Network Model分裂的Gameplay语义。
- 本change不依赖具体Unity CharacterController、Deterministic KCC、DotRecast或网络协议；所有WorldSolver只接收同一个已修正`CharacterMotionRequest`。

## Current Spec Comparison

- `character-motion-semantics`当前规定Contribution直接由accumulator解析为Request，没有表达resolved channel与Modifier阶段。本change修改该唯一链路，但保持现有channel、priority、weight、blend和consume语义。
- `character-root-motion-curves`当前规定Root Motion通过统一motion管线进入Solver，但没有区分“原始动画派生曲线”和“目标约束修正”。本change明确MotionCurve仍是唯一原始位移事实，Warp只修改其resolved结果。
- `character-action-activation-flow`已要求激活携带target snapshot，但没有定义目标是否必需，现有`TargetPolicy`只是未执行的字符串配置。本change将它替换为可编译、可准入、可诊断的typed requirement。
- `btsmtl-gameplay-semantic-ir`要求每个authoring type有唯一Emitter。当前`MotionWarpClip`没有Emitter，因此它不应继续作为看似可用的配置存在。本change补齐唯一operation或在编译时明确拒绝。
- `btsmtl-compiled-simulation-program`已区分持久typed state与同Step motion transient。本change沿用该边界：Warp累计状态持久化，resolved channel和Request不持久化。
- `btsmtl-timeline-editor-preview`要求完整Gameplay行为只能经过正式Preview Session。本change不会在Timeline窗口中增加直接改Transform的Warp preview。
- `agent-character-controller-synthesis`当前只把Timeline Track/Clip作为泛型identity投影，不能表达MotionWarp source、模式、限制和曲线。本change扩展现有v9链路，不新增独立Agent入口。
- 当前spec明确“无独立MotionWarp runtime”；本change完成后必须同步更新`openspec/project.md`和current specs，删除该过时描述。

## Impact

- Authoring：`MotionWarpTrack`、`MotionWarpClip`、Timeline Inspector与ActionProfile Inspector。
- Frontend：Timeline emitter registry、Authoring Discovery、Semantic IR operation/reference/capability、source map与diagnostics。
- Target Program：Float32/Fixed lowerer、Program schema/codec/hash、ExecutionLayout、typed state layout与artifact identity。
- Runtime：Float32/Fixed Motion channel resolver、portable modifier sequencing、MotionWarp target modules、Action admission、State codec、Snapshot/Hash与Trace。
- Tooling：Timeline Authoring Preview、Live Debug、Semantic IR Inspector、portable Reader、Agent v9 Snapshot/Patch/Validator/MCP bridge。
- Generated products：Corin Semantic IR、Float32 Program、Fixed Program、Projection和各产品manifest必须重新生成，即使Corin暂不配置MotionWarp。
- Breaking changes：旧`TargetPolicy`字符串、旧MotionWarp fields、旧Program/State/artifact payload全部删除或拒绝，不提供兼容reader或自动猜测迁移。

## Non-Goals

- 不实现目标选择、锁定、敌人registry、命中检测、格挡/闪避成功判定或Combat Solver。
- 不为Corin攻击直接伪造目标快照。当前没有正式目标提供者，因此本change不会宣称Corin业务MotionWarp已经配置完成。
- 不实现live moving-target tracking、目标切换或服务器私有目标查询。
- 不实现垂直warp、跳跃、翻越、攀爬、IK、Motion Matching或动画姿势修正。
- 不改变Locomotion、Action和GameplayResult的channel顺序，不把priority移动到动画层。
- 不恢复旧`CharacterMotionStage`、`MotionProposal`、BBB `WarpedMotionData`或Graph runtime motion路径。
- 不在Solver、Presentation、Animancer或Transform层增加warp。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Stop Conditions

- 如果必须查找场景Transform、GameObject名称或Presentation target才能得到Gameplay目标，停止并先规划正式target provider。
- 如果某个WorldSolver必须实现专用warp逻辑，停止并重新评估Modifier与Solver边界。
- 如果无Modifier时无法逐字段保持当前Motion结果，停止并修正拆分设计，不以行为变化继续迁移。
- 如果Fixed跨Tick状态无法进入正式Snapshot/Hash，停止，不提供非确定性或Float32-only路径。
- 如果shared/inline Timeline无法通过稳定identity唯一绑定源MotionCurve，停止，不按重叠区间或名称猜测。
- 如果需要让Corin直接展示目标Warp但正式target provider仍缺失，停止说明业务缺口，不创建假目标或临时Blackboard writer。

## Success Criteria

- 正式运行链唯一为`Contribution -> Channel Resolve -> Motion Modifier -> CharacterMotionRequest -> WorldSolver -> Body Result`。
- 没有MotionWarp operation的Program，其Float32与Fixed request结果与修改前逐字段一致。
- MotionWarp只作用于显式source MotionCurve成为Action channel resolved owner的Tick，不修改Locomotion或GameplayResult结果。
- ActionProfile声明`SnapshotRequired`时，`CanActivateAction`和`ActivateActionInstance`对缺失目标给出同一typed拒绝原因。
- Warp目标只来自对应ActionInstance的immutable target snapshot；运行时不访问Unity scene、Transform或网络模型对象。
- 跨TickWarp状态进入Program state、Snapshot和Hash；rollback/replay/seek对相同输入得到相同结果。
- Float32与Fixed使用同一authoring、Semantic IR、modifier顺序、模式和拒绝规则，不存在Target专用业务分支。
- WorldSolver仍是唯一实际移动和碰撞权威；Warp不在Solver之后补偿位置。
- Timeline Preview只通过正式Preview Session接收显式preview target snapshot；纯动画预览不产生Gameplay warp。
- Agent v9能稳定导出、创建、修改、删除并验证MotionWarp Track/Clip和source identity。
- 旧`TimelineMotionWarpWindow`、`MotionWarpTrack.Sample()`、字符串`TargetPolicy`、未消费字段、旧artifact reader和第二Warp路径全部删除。
