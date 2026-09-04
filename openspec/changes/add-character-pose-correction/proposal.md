## Why

本变更规划一个“给正在播放的动画追加身体姿态调整”的工具：例如 Corin 正常播放跑步动画时，根据移动情况叠加左倾、右倾或前倾，也能根据手臂等骨骼的朝向调整相关部位的外形。作者可以从现有动画取一帧或直接调整骨骼来制作样本，这些样本通过现有 PoseGraph 使用。

## 当前范围说明

这里的“姿态”是某一时刻各骨骼的位置、朝向和缩放。当前方案的工作过程是：

1. 作者准备中性姿态和几个方向样本，并选择要改变的骨骼。
2. 游戏根据运动参数或骨骼朝向，计算各样本混合多少。
3. 将混合结果相对中性的变化叠加到当前动画，继续交给现有 PoseGraph 和最终输出。

| 内容 | 当前文档的规划 |
|---|---|
| 从动画取帧、直接调整骨骼 | 两种制作入口都包含，保存为同一种样本 |
| 根据角色移动或骨骼朝向修正姿态 | 包含，分别由两个明确节点承担 |
| 在脚部贴地/IK 处理之前或之后调整 | 包含显式连接与末端影响规则，不承诺任意后置修改都能保足 |
| 对比 IK 前后两份姿态，只追加两次样本结果的差异 | 未纳入；这是聊天中进一步提出的候选方案 |
| 完整复刻 ZZZ BoneAdjust | 未纳入；角色配置、完整补偿公式及最终位移来源仍缺证据 |

本 change 的准确目标是“3C 的通用姿态修正工具”。ZZZ 和 UE 提供参考，不能据此把本设计称作 ZZZ 原算法。当前节点只读取一份输入姿态；如后续决定采用两份姿态对照，需要同步修订目标、spec 和任务，不能在实施时默默加入。

## What Changes

- 新增正式静态姿态修正样本：保存中性参考姿态、二维样本坐标、受影响骨骼及目标局部姿态。动画指定帧提取与直接调骨骼都提交同一种样本数据；提取后的样本是独立快照，重新提取必须显式执行。
- 用 Graph-owned typed Correction Slot 和 Profile-owned Binding 装配 Profile-owned Correction Set，沿用现有角色资源所有权方式。共享图不保存角色修正资源，静态样本不获得 Player、时钟、Foot Analysis 或第二 Source Runtime。
- 新增参数驱动与骨骼方向驱动两个明确的 PoseGraph 节点，共享样本混合及姿态差值计算。前者读取正式二维参数；后者读取本帧输入姿态中源骨骼相对参考骨骼的方向。两者均只产生修正后的 Component Pose。
- **BREAKING（表现参数语义）**：对齐现有 `MotorLocalVelocityX/Y` 与实际 `FromFact` 世界速度之间的坐标冲突。保留现行世界方向 Fact 的含义，在唯一 Fact 投影中增加明确的角色局部运动事实，并统一正式运行与预览的参数读取；发布前逐一核对受影响消费者，不增加新节点专用绕行参数。
- 支持由图连接决定 IK 前置或后置。前置修正必须同时供给 FootPlacement、PoseBone Goal Source 和 FullBodyIK；后置修正显式声明保留已求解末端还是允许其位移，编译检查修改骨骼及全部受影响后代，不承诺任意修改骨盆后自动保足。
- 增加样本点编辑、骨骼编辑、取帧、撤销、只读权重显示及正式场景中的效果观察。完整角色预览接入已有场景运行预览变更；静态素材提取只是编辑器资产采样，不建立完整角色预览 Runtime。
- 通过唯一 Node Definition、Capability、typed Presentation Mutation 和 Document v4 完成创建、修改、引用、删除、校验与反向导出；通过现有 Character Build 编译常量、骨骼索引、写入范围与运行页。
- **BREAKING（生成产物）**：新增节点及常量布局按实施时的正式 Program Image/Runtime ABI 统一升级，旧生成产物重新发布，不保留旧 reader 或运行时补编译。

本提案覆盖两种制作入口、两类驱动及 IK 前后组合。ZZZ 的动画/IK 双参考补偿、SmoothKnee、速度上下楼公式、抬骨与地面俯仰不进入实施任务；已确认的结构和未确认的动态语义分别列入设计依据，不以节点名称宣称 ZZZ 等价实现。

## Capabilities

### New Capabilities

- `character-pose-correction`：统一静态样本、两种制作入口、参数/骨骼方向驱动、参考空间、修正数学、作者操作及 Corin 装配范围。

### Modified Capabilities

- `character-presentation-pose-graph`：加入修正节点的显式拓扑、统一局部参数来源及 IK 前后末端约束语义。
- `character-animation-layer-runtime`：固定职责顺序中明确允许 FBBIK 后的显式纯姿态修正，并保留唯一最终发布。
- `character-animation-presentation-authoring`：补充 Correction Slot、Binding、样本子资产的唯一所有权及共享图装配规则。
- `character-pose-plan-compilation`：补充样本编译、静态写入影响检查、PurePose Kernel 和生成产物身份要求。
- `btsmtl-agent-authoring-document-sync`：在现有 Profile/Graph 分片中表达 Correction Set、Binding、Slot 与节点，保持整包事务及生成数据只读。

## Impact

- 作者与运行代码：`Main/Runtime/Character/Pipeline/Animation/Contracts`、`PoseGraph/Program`、`PoseGraph/Worker`、正式 Fact/Parameter 投影，以及 `Main/Editor/CharacterPipeline/Authoring`、`AgentAuthoring`、`Main/Editor/CharacterSimulation/Compilation/Presentation`。
- 角色内容只装配稳定的 Corin。TrainingEnemy、Gameplay、KCC、网络状态、Foot 生命周期、WorldResidual、Goal 编码、FullBodyIK 求解算法与物理 Writer 不成为本变更的算法修改对象。
- 复用现有单骨骼更新、二维混合、Rig 与局部/Component 转换；保留 `Modify Bone`、`Additive Pose`、Clip/Blend Space 的原业务用途。静态 Correction Set 自己保存中性参考，不为本功能泛化现有 Additive 的 `RigReference` 接口。
- 关联 `refactor-character-pose-graph-architecture`、`refactor-btsmtl-authoring-architecture` 和 `rebuild-btsmtl-preview-with-scene-play` 的正式接口；不覆盖它们正在修改的实现。保留 Foot IK 变更的业务所有权及已否决的膝角实验结论。
- 现行拓扑文字、省略后置阶段的运行顺序、Document 可写内容与新增资源所有权需要对应 delta；局部速度的实现冲突及并行预览合同冲突在 `design.md` 单独列明。
- 这是规划产物。完整设计、并列方案取舍、现成源码复用与剩余工作对账见 `design.md`；先前未经复用核算的工期数字已撤回，不作为实施依据。不修改 current specs、其它 active change、项目代码或 Unity 资产。
