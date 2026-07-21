# Change: 重构 MotionWarp 目标姿态与累计轨迹解算

## Why

当前 `TimelineMotionWarp` 已经位于正确的统一运动链：它只修正成为 Action channel resolved owner 的源 `MotionCurve`，随后仍由 Body Motion Integrator、WorldSolver 和 Presentation 消费唯一结果。问题不在管线位置，而在 Modifier 内部模型过窄：

- `MotionWarpClip` 只有 `MatchTargetPlanarPosition`、`FaceTarget`、`MatchTargetYaw`，所有位置偏移只能按目标 yaw 解释。
- Runtime 在窗口进入时冻结一个世界空间位置残差和一个 yaw 残差，再用两条曲线分别累加；源 ActorLocal 根轨迹后续又按变化中的 Body yaw 旋转，导致预计终点与实际弯曲轨迹不一致。
- Warp 终点按源 MotionCurve 的 `CurveEndFrame`计算，而不是按 MotionWarp 窗口结束时的源累计姿态计算，作者不能直观表达“命中帧到达目标，后续动作继续播放”。
- `PositionWeight`与`YawWeight`会让“对齐目标”退化成含糊的部分修正；最大修正只做静默Clamp，作者很难区分正确对齐、受限部分对齐和未执行。
- Corin Attack1到Attack5目前使用同一套全窗口线性参数，无法按各段动作的蓄力、接近、命中和落脚节奏调节，因此会出现滑步、横向吸附和转向不足。

Motion Warping 应当把“目标姿态怎么生成”“源轨迹怎么映射”“最终朝向怎么生成”“修正如何随时间发生”拆成正交作者语义，同时继续只产生唯一 Gameplay motion。继续在现有残差算法上增加攻击专用判断会把手感问题固化为特殊分支。

## What Changes

- 将位置 authoring 从单一`MatchTargetPlanarPosition`重构为`Disabled`、`ScaleToTarget`、`SkewToTarget`与`LinearToTarget`四种明确的平面轨迹模式。
- 增加`TargetLocal`、`ApproachDirection`、`ActorStartLocal`与`World`四种目标平面offset空间；位置offset与yaw offset继续只定义目标姿态，不承担解算方法。
- 保留`Disabled`、`FaceTarget`与`MatchTargetYaw`旋转目标模式，并增加`ProgressCurve`、`ConstantRate`与`ScaleSourceYaw`三种旋转方法。
- 删除`PositionWeight`与`YawWeight`；保留最大平面修正和最大yaw修正，但把`ApplyClamped`与`PreserveSource`定义为显式编译策略并输出typed结果，禁止静默部分对齐。
- 把Warp窗口语义改为：窗口开始时固定Action target snapshot、Body起始姿态、源窗口起始累计姿态与目标姿态；每Tick按窗口内源累计轨迹求出唯一warped cumulative pose，再与上一累计结果做差生成当前Tick delta。
- 让平移解算消费同一累计yaw结果，禁止源位移再按变化中的Body yaw进行第二次增量旋转；Modifier只把`warped source delta - resolved raw source delta`作为修正加入Action channel，保留同channel其它已仲裁贡献。
- WorldSolver阻挡的位移不在后续Tick、Finalize或Presentation补偿。
- Warp source必须使用无Ease的单位Gameplay motion weight；动画淡入淡出继续属于Presentation，避免目标终点被Gameplay权重改变。
- 逻辑Tick跨越Warp窗口边界时，只替换该Tick与Warp窗口交集内的源delta，窗口外源轨迹继续保留。
- 更新MotionWarp authoring校验、Timeline Inspector、Curve Catalog、Semantic IR、Float32/Fixed Program descriptor、state schema、codec、Reader、Inspector、Trace与generated identity。
- 在唯一Agent schema v15上更新MotionWarp Snapshot、Patch、lowerer、handler、validator与技能，不建立v14兼容或MotionWarp专用工具。
- 通过正式Agent事务重新配置Corin Attack1到Attack5：普通攻击使用`SkewToTarget + ApproachDirection + FaceTarget/ProgressCurve`，每段只覆盖自身接近目标到命中阶段，并删除当前五份通用全窗口线性配置。

## Scope

### In Scope

- Root级平面MotionCurve轨迹的Scale、Skew与Linear目标映射。
- 目标位置offset空间、目标yaw offset、旋转目标和旋转方法。
- Float32与Fixed Q32.32对同一portable语义的独立数值实现。
- Rollback/Network identity所需Operation Set、Target ABI、Program、State codec与hash升级。
- Timeline人类编辑入口、Agent v15自动编辑入口和Corin五段攻击迁移。
- 旧位置模式、weight字段、冻结总修正state与旧Trace字段的删除。

### Out of Scope

- Bone或Static animation Warp Point。该能力需要动画接触点烘焙与独立source curve合同，后续单独规划。
- 动作期间持续追踪移动目标。当前仍使用ActionInstance激活时的不可变target snapshot，避免改变输入、快照和网络重放语义。
- Foot IK、Pose Warping、Distance Matching、Motion Matching或动画播放速度调整。
- 瞬时Teleport。`LinearToTarget`仍在Timeline窗口内产生逐Tick Gameplay motion，不允许一帧写Transform。
- 命中、伤害、受击、目标选择评分或完整Combat authority。

## Impact

- Affected specs:
  - `character-motion-warp-authoring`
  - `character-motion-semantics`
  - `btsmtl-gameplay-semantic-ir`
  - `agent-character-controller-synthesis`
  - `character-targeted-motion-warp-demo`
- Affected authoring:
  - `MotionWarpClip`、Timeline Inspector、Curve Channel Catalog与正式validator。
  - Corin Attack1到Attack5 inline Timeline资产。
- Affected compiler/runtime:
  - Timeline emitter、Semantic IR validator、portable descriptor、Float32/Fixed lowering、Program codec、state layout与Motion runtime。
  - Program/State/Network composition identity与generated artifacts。
- Breaking changes:
  - 删除旧`MatchTargetPlanarPosition`序列化语义、`TargetLocalPlanarOffset`字段名、`PositionWeight`、`YawWeight`和旧冻结总修正state。
  - 旧`.csir`、Float32 `.csim`、Fixed Program、State Snapshot和Network baseline均不兼容，必须从正式authoring重编译，不提供reader或converter。

## Current Spec Comparison

- `character-motion-warp-authoring`当前明确要求target-local offset、position/yaw weight、最大总修正和两条累计修正曲线。本change将它改为目标姿态、轨迹solver、旋转method和显式限制结果；旧weight语义必须删除。
- `character-motion-semantics`当前要求窗口进入时固定总position/yaw correction并按进度增量应用。该要求正是变化Body yaw与冻结world residual不一致的来源，本change将其替换为固定累计轨迹上下文和逐Tick累计pose差分。
- `btsmtl-gameplay-semantic-ir`当前已经保证numeric-neutral Frontend和Target-specific lowering。本change只扩充`TimelineMotionWarp`的typed descriptor，不新增第二operation或Target专用业务节点。
- `agent-character-controller-synthesis`当前v14能读写旧MotionWarp字段；active change `extend-agent-authoring-for-ai-controller`会原子升级为v15。本change必须基于最终v15扩展同一Character domain命令，不能并行创建v14分支。
- `character-targeted-motion-warp-demo`当前只保证五段攻击预置可调Warp，并明确不承诺最终手感。本change将其收敛为五段独立窗口与普通近战目标空间的正式样例。
- `openspec/project.md`当前描述固定总修正与累计progress state；实现完成后必须同步更新为累计轨迹上下文，不能继续保留旧算法口径。

## Dependencies And Sequencing

- 依赖已经安装的`add-program-motion-modifier-warping`与`add-corin-targeted-motion-warp-demo`提供唯一Modifier、target snapshot和训练敌人闭环。
- Agent任务硬依赖`extend-agent-authoring-for-ai-controller`完成v15原子升级；核心authoring/Program/Runtime可以先实施，但Corin资产迁移与最终清理必须基于唯一v15串行完成。
- 与Timeline Marker、Foot Placement和AI change共享文件时必须基于最新代码重读并保留其最终语义，不能回退并行改动。
- 实施顺序必须先安装新正式authoring和Program合同，再通过Agent迁移全部可达资产，最后删除旧字段、旧state与旧codec；最终仓库不得保留双读、双写或一次性migrator。

## Success Criteria

- MotionWarp authoring能独立选择目标offset空间、平移solver、旋转目标、旋转method、限制策略和适用curve。
- Warp目标姿态按窗口开始时的不可变Action target snapshot与明确空间唯一计算。
- Float32与Fixed都从窗口内源累计轨迹生成warped cumulative pose，并用相邻累计结果之差输出当前Tick唯一Action motion delta。
- Warp只替换匹配source owner的运动部分，不重复源位移，也不覆盖同Action channel的其它合法贡献。
- Warp窗口边界Tick不丢失窗口外的前段或后段source delta。
- 大角度yaw修正时，源局部位移不再使用变化Body yaw二次积分；平移终点与yaw解算共享同一窗口上下文。
- Warp窗口结束时达到有效目标姿态；窗口后的源MotionCurve继续按自身剩余轨迹运行。
- 受最大修正限制时，Trace明确区分`Applied`、`AppliedClamped`与`PreservedByLimitPolicy`，不静默冒充完整对齐。
- Corin五段普通攻击使用独立Warp窗口和`ApproachDirection`站位，后摇不Warp；无目标挥空与WorldSolver碰撞权威保持不变。
- Agent v15能够export、dry-run、apply、re-export和validate全部新字段；旧schema与旧MotionWarp字段不存在。
- OpenSpec strict validation、portable编译和Unity静态程序集编译通过，不运行Unity batchmode。
