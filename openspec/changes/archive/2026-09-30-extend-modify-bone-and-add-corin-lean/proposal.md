## Why

当前 Modify Bone 只能从节点静态配置读取位置、旋转和缩放，动态端口仅提供权重，且组件空间修改没有重建受影响后代。Corin 因而缺少可直接组合的程序侧倾能力；需要补齐通用骨骼变换，再由现有动画图表达跑动转弯 lean，而不是恢复旧姿态样本系统。

## What Changes

- 扩展现有 Modify Bone：精确类型的动态位置、旋转、缩放输入，每个通道的 Ignore／Add／Replace 模式，明确的父骨骼局部空间与角色组件空间，以及显式子骨骼传播规则。
- **BREAKING** 收口原有操作位掩码和含糊的 Local／Mesh 变换合同；使用同一正式 payload、作者字段和 C# API，清理被取代字段，不保留双配置或旧执行分支。实施前枚举实际使用点，已有明确正确的行为出现冲突时报告用户，不静默改变。
- 使用当前 Native Handler、实例绑定和可复用姿势缓冲实现通用能力；保持输入只读、单帧结果复用、统一提交和唯一 Final Writer，运行时零托管分配。
- Corin 动画 EventGraph 计算跑动转弯侧倾、速度衰减和回正；仅在适用的跑动状态子图通过通用 Modify Bone 施加，继续进入原 AnimationSlot、Foot Placement、Goal Assembly、FullBodyIK 和最终输出。
- 倾角公式、参数和适用范围由 Corin 作者图拥有。优先参考能够完成角色绑定的 ZZZ dump 证据；缺失数值明确标为本项目初始调参，不宣称完整复刻。
- 保留旧 worktree 为只读算法参考，不迁入 PoseCorrectionSet／Binding／Slot、姿态样本编辑器、旧 Document、Worker／Operation 或整角色 Projection。

## Capabilities

### New Capabilities

- `character-pose-bone-transform`：通用骨骼变换的动态 typed 输入、变换模式与空间、后代传播、作者入口和原生执行合同。
- `corin-locomotion-lean`：通过通用骨骼能力完成 Corin 跑动转弯侧倾、平滑回正、动作隔离和正式作者接入。

### Modified Capabilities

- `character-animation-event-graph`：保留已有七项派生计算语义，同时允许本次显式新增的 Corin lean 派生参数及其独立历史和平滑，消除历史迁移条款对新功能的笼统禁止。

## Impact

- 通用能力：`CharacterModifyBonePosePayload`、`CharacterPoseCanvasNativePorts`、对应节点定义／Capability／Mutation／C# authoring、`CharacterPoseNativeModifyBoneHandler`，以及其实际消费的 Rig 派生骨骼规则。
- 角色内容：Corin 正式动画 EventGraph、跑动状态子图和当前 Pose Graph 作者生成入口；不调整 Gameplay 运动、碰撞、Root Motion 或已有折返朝向修正。
- 数学职责：运动到侧倾的公式留在 EventGraph；通用 Handler 只执行骨骼变换，不认识 Corin、跑步、攻击或 lean。
- 与现行规范对照：遵守 `native-flowcanvas-pose-runtime`、`character-presentation-pose-graph`、`character-pose-graph-runtime-architecture` 的原生图、实例隔离和唯一输出约束；唯一需要改写的现行冲突是 `character-animation-event-graph` 中迁移阶段“不新增平滑或状态策略”的无范围限定措辞。本 change 提供精确 delta，不直接改写当前主规范。
- 交付包含通用能力和 Corin 正式图内容；只有节点代码或未消费的 lean 变量不算完成。
