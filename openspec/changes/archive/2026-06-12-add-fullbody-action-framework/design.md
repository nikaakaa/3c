## Context
当前代码已经形成三个重要地基：

- `PlayerLocomotionController` 负责基础移动输入、Locomotion 阶段、运动命令、基础移动动画表现和相机 Resolve。
- `ActionInterruptArbiter`、`ActionInterruptPolicySetSO` 和 `ActionRuntimeStateTracker` 提供纯数据 Action 仲裁和当前 action facts。
- `PlayerDodgeActionController` 与 `DodgeActionRuntime` 把 Dodge 请求、仲裁、动作位移、动作动画命令和 Run latch 串成了最小闭环。

问题不在于这些模块本身，而在于所有权边界还没有上提成 FullBody 主行为域。现在 Dodge 通过 thin controller 压制 Locomotion 的运动和动画，这可以作为迁移状态，但如果 Roll、Jump、Vault、Attack、Hit 等继续照这个方式复制，就会出现多个 action controller 分别决定“我能不能压制基础移动”“我要不要播放 base layer”“我要不要写 Run latch”。这就是用户指出的分裂路径风险。

工业常见做法不是把所有东西做成一个巨大 MonoBehaviour，也不是让每个动作都挂一个平级控制器抢 base layer。更稳的做法是一个 FullBody coordinator 只管调度和权威归属，具体 Locomotion、Dodge、Roll、Jump 都是可测试模块；动画 Presenter 只消费命令，MotionDriver 只执行命令，Action tracker 只保存事实。

## Goals / Non-Goals
- Goals:
  - 建立 FullBody 主行为域框架，先解决调度和所有权，不先调动画转换细节。
  - 让基础 Locomotion 变成 FullBody 主层下的模块或 adapter。
  - 让 Dodge 变成 FullBody Action module 的第一条迁移对象。
  - 保持输入、仲裁、运行时事实、运动执行、动画表现互相解耦。
  - 保持配置“主入口聚合，子职责分层”。
  - 当前只实现 FullBody 层级主树，不在本变更中引入 UpperBody、Facial、IK、Additive 等并行表现层。
- Non-Goals:
  - 不复制 BBB 大型主控或状态类体系。
  - 不在本变更内完成所有动作状态。
  - 不实现动画状态机可视化编辑器。
  - 不调整具体可琳动画过渡 bug。
  - 不实现 cooldown、Root Motion 权威或网络预测。
  - 不实现并行状态层、UpperBody 状态机、AvatarMask layer 编排或 IK/Additive 状态层。

## Decisions
- Decision: 新增一个轻量 FullBody coordinator，而不是复制 BBB 主控。
  - Reason: 当前项目已经有 Locomotion pipeline、Action 仲裁、MotionDriver 和 Animancer Presenter；需要的是统一调度它们，不是重写整套角色控制器。
  - Implementation note: 名称可为 `PlayerFullBodyController`、`FullBodyActionController` 或等价项目命名。它只负责顺序、权威选择和端口连接，不把 Dodge 数值、动画资源或 Locomotion 状态图规则写死进去。

- Decision: Locomotion 保持模块化，但归属 FullBody 主层。
  - Reason: `BasicLocomotionStateMachine` 的 `Idle / MoveStart / MoveLoop / MoveStop` 仍是正确的局部状态图；问题只是它不能长期作为和全身 Action 平级的 base layer 入口。
  - Implementation note: 可以先提供 Locomotion adapter，把现有输入意图、世界方向、phase、运动命令、动画命令拆成可由 FullBody coordinator 调用的步骤；不要求一次重写 `PlayerLocomotionController` 全部逻辑。

- Decision: 全身动作通过 Action module 端口接入。
  - Reason: Dodge、Roll、Jump、Vault 等共同点是读取输入请求/意图事实，经过 Action 仲裁，active 时输出动作运动和 base layer 动画，结束时写回 action facts 或移动 facts。
  - Implementation note: module 可以是普通 C# 类、ScriptableObject 驱动的 runtime，或薄 MonoBehaviour adapter；关键是它只是 FullBody 主树 Action 叶子的行为执行单元，不自己拥有状态树拓扑、owner 选择或第二角色控制路径。

- Decision: 每帧只允许一个 FullBody 行为拥有平面位移和 base layer 动画命令。
  - Reason: 避免 Locomotion 和 Dodge 同帧都执行移动，或两个 Presenter 同时认为自己拥有 base layer。
  - Implementation note: 当前 `SuppressBasicMotionExecution` 和 `SuppressLocomotionAnimationPresentation` 可以作为迁移手段，但最终应由 FullBody coordinator 的行为选择结果决定是否提交 Locomotion 命令。

- Decision: Action runtime tracker 继续只保存事实。
  - Reason: 现有 spec 已明确 tracker 不自动退出、不消费输入、不播放动画。FullBody 框架不能把状态机职责塞回 tracker。
  - Implementation note: tracker 由 FullBody coordinator 或 Action module runner 显式推进和更新。

- Decision: 配置入口采用 FullBody Action 逻辑集和动作动画绑定集分离。
  - Reason: 设计者需要看出角色有哪些 FullBody actions、每个 action 的逻辑配置是否完整，同时不能让动画 Profile 反向成为动作逻辑或状态树权威。
  - Implementation note: `FullBodyActionSetSO` 只聚合 action id、运动参数和打断策略；`FullBodyActionAnimationSetSO` 或等价绑定集负责 `ActionStateId -> ActionAnimationProfileSO`；FullBody 主调度入口显式引用二者，不吞掉 Locomotion Walk/Run 配置。

- Decision: Dodge 变更成为该框架的第一条迁移用例。
  - Reason: Dodge 已经有最小闭环和暴露问题，适合验证框架端口是否够用。
  - Implementation note: `add-dodge-action-profile` 可先保留当前行为测试；本变更实现后应把它的独立 controller 职责收束为 FullBody action module 或迁移 adapter。

## Risks / Trade-offs
- Risk: 一次引入太完整的状态框架会拖慢当前 demo。
  - Mitigation: 第一版只做最小 coordinator、Locomotion adapter、Action module registry 和 Dodge 迁移，不做编辑器图、不做完整能力系统。

- Risk: 为了“框架”过早抽象出泛型 ability 系统。
  - Mitigation: 端口以当前 Dodge、Locomotion、Action 仲裁真实需要为边界，未来 Roll/Jump 再补充字段。

- Risk: 迁移期间 `PlayerDodgeActionController` 和新 FullBody coordinator 共存，造成双执行。
  - Mitigation: 任务要求迁移时必须有单一 owner 检查，场景或 prefab 不允许同时启用两个 FullBody 动作调度入口。

## Migration Plan
1. 定义 FullBody 每帧调度顺序和运行时上下文数据。
2. 把现有 Locomotion 运行结果拆成可供 FullBody 读取的意图/phase/命令/动画上下文，先不改动动画资源。
3. 定义 FullBody Action module 端口，先用 Dodge 作为第一条 module。
4. 建立 FullBody Action 逻辑集和动作动画绑定集，分别连接 Dodge 的 motion/interrupt 配置和 animation profile。
5. 让 FullBody coordinator 选择当前行为：无 action 时提交 Locomotion；Dodge active 时提交 Dodge motion/animation，并只保留 Locomotion 意图和 Look。
6. 将 prefab/scene 中长期入口收束到一个 FullBody coordinator；旧 Dodge controller 只能作为迁移 adapter 或被移除。
7. 运行 EditMode 测试、静态边界检查和 Play Mode 手动验证。

## Open Questions
- 第一版实现时，FullBody coordinator 的最终命名需要按代码目录现有命名确定；本提案先用职责名描述，不锁死类名。
