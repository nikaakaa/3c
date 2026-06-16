## Context
项目当前已经有 `InputRequestKind.Attack`、`InputButtonKind.Attack`、`ActionRequestType.Attack` 和回滚输入帧里的 Attack 按钮事实。实际 FullBody Action 闭环目前只落地了 Dodge：`PlayerFullBodyActionController` 只构建 Dodge 请求事实，`ActionStateIds` 只包含 `Action.None` 和 `Action.Dodge`，`ActionAnimationKeys` 只包含两个 Dodge key。

这次变更先实现攻击动作本身，不实现伤害判定。攻击必须继续进入现有 Character frame pipeline、FullBody submission、统一状态机、输入缓冲、Action 仲裁和动画 Presenter，不新增 per-action controller 或第二套状态权威。

## Goals / Non-Goals
- Goals:
  - 提供三段轻攻击动作：Attack01、Attack02、Attack03。
  - 支持 Attack 预输入在合法连段窗口内衔接下一段。
  - 使用稳定动作状态 ID 和稳定动作动画 key。
  - 攻击期间保持 FullBody Action submission 获胜，压制 Locomotion 平面位移和 base layer 动画输出。
  - 提供 EditMode 测试、静态边界验证，并给出 Play Mode 验证方式。
- Non-Goals:
  - 不实现 hitbox、hurtbox、伤害、命中停顿、击退、受击状态或死亡。
  - 不实现完整动作 Timeline 编辑器。
  - 不实现 VFX、SFX、Camera event、IK 或动画事件轨道。
  - 不实现锁敌、自动朝向目标、武器碰撞或目标选择。
  - 不修改 Fantasy 协议，不接真实网络。
  - 不让 Root Motion、Animancer callback 或 Transform 写入成为位移权威。

## Decisions
- Decision: 第一版固定为三段轻攻击链。
  - Reason: 用户当前明确先做动作，伤害判定以后再说。三段轻攻击能验证输入、状态、连段窗口、动画 key 和 FullBody Action submission，不需要先引入任意连段图。
  - Alternative considered: 直接做通用 Combo Graph。暂不采用，因为会把编辑器、分支、派生攻击类型和未来伤害判定提前混进来。

- Decision: 攻击状态使用 `Action.Attack01`、`Action.Attack02`、`Action.Attack03`。
  - Reason: 这些 ID 与已有 `Action.Attack01` 文档例子一致，能进入 `ActionRuntimeStateSnapshot`、统一状态机快照和未来回滚映射。
  - Alternative considered: 用 `Action.Attack.Light` 加 stage index。暂不作为外部状态 ID，避免状态机路径和日志不直观。

- Decision: 攻击输入只从 `InputRequestBuffer` 读取 `InputRequestKind.Attack`。
  - Reason: 本地预输入规格已经要求 Attack pressed 只记录请求，不记录未来动作结果。
  - Alternative considered: 在 `UnityInputSystemLocomotionInputSource` 或 MonoBehaviour 中直接触发攻击。禁止，因为会绕过现有输入缓冲和 Action 仲裁。

- Decision: 攻击进入和连段进入都要经过统一 request submission 和 Action 仲裁。
  - Reason: priority、resistance、force 和 timing window 的准入权威已经收口到 `ActionInterruptArbiter`。
  - Alternative considered: 状态机 transition 直接判断 `RequestPriorityAtLeast`。禁止，因为已有规格要求默认 FullBody Action 入口不得让请求优先级回流状态机。

- Decision: 连段窗口第一版是纯数据窗口事实，不做完整 Timeline 编辑器。
  - Reason: 只需要判断当前攻击段是否允许消费 Attack 请求进入下一段；hitbox、cancel、IK、VFX/SFX 和 camera event 留到后续能力。
  - Alternative considered: 用 Animancer event 或 OnEnd 触发连段。禁止，因为动画外观层不能成为逻辑权威。

- Decision: 第一版连段窗口以当前 action state elapsed normalized time 采样。
  - Reason: 该事实可测试、可回滚、可由统一状态机快照恢复，不依赖 Animancer runtime。动画播放进度可继续作为只读调试事实。
  - Alternative considered: 直接读取 Animancer normalized time。禁止，因为逻辑层不能读取 Animancer runtime。

- Decision: 攻击动作配置必须是正式配置。
  - Reason: 项目要求不要 fallback 配置；缺少攻击 stage、动画 key、duration、priority、resistance 或 combo window 时必须校验报错。
  - Alternative considered: 代码里给默认攻击手感参数。禁止，避免隐藏配置缺口。

- Decision: 攻击可以有可选轻微动作位移和转向，但必须经统一 motion executor。
  - Reason: 动作表现可能需要前踏或锁定朝向，但平面位移权威仍然只能在 motion executor。
  - Alternative considered: 让动画 Root Motion 直接推动角色。禁止；如必须改 Root Motion 权威，需要另开 OpenSpec。

## Risks / Trade-offs
- Risk: 活跃的 Locomotion 和回滚重构变更可能同时触碰状态机上下文。
  - Mitigation: 本变更只定义 Attack Action 接入边界，实施时先读活跃 change，再沿统一状态机主线扩展；如果需要绕过当前系统，必须停止并补 proposal。
- Risk: 没有攻击动画资源会导致 Play Mode 只能验证状态和 key。
  - Mitigation: 配置校验必须报告缺失动画引用；Play Mode 验证要求绑定正式攻击动画后再验动作表现。
- Risk: 三段固定链后续不够表达分支连招。
  - Mitigation: 本变更只验证第一条轻攻击垂直切片；分支、重攻击、空中攻击和派生技后续另开能力。

## Migration Plan
1. 先扩展纯数据 ID、配置和校验，不接运行时输出。
2. 再接 Attack request submission 和仲裁，保证 rejected 请求不被消费。
3. 再接统一状态机状态和输出，保证 FullBody Action submission 单一。
4. 再接动作动画 key 和 Presenter 配置校验。
5. 最后补 EditMode 测试、静态边界验证和 Play Mode 验证说明。

## Open Questions
- 第一版默认按三段轻攻击规划：Attack01、Attack02、Attack03。
- 伤害、hitbox、受击和表现事件明确不在本变更范围内。
