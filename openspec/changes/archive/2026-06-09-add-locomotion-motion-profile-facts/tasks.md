## 0. 审批前检查
- [x] 0.1 确认 `add-animation-phase-timeline-facts` 已通过用户验证或明确允许作为依赖继续规划实施。
- [x] 0.2 确认本变更第一版只覆盖 `MoveStop / RunEnd` 的烘焙运动贡献。
- [x] 0.3 确认本变更不启用完整 `Animator.applyRootMotion` 驱动基础移动。
- [x] 0.4 确认本变更不新增第二套角色控制器路径。
- [x] 0.5 确认本变更不复制 BBB 的主控、状态机或运行时 namespace。

## 1. 参考与现状复核
- [x] 1.1 读取 `AGENT.md` Root Motion 策略。
- [x] 1.2 读取 `openspec/specs/basic-locomotion-animation/spec.md` 的位移权威要求。
- [x] 1.3 读取 `openspec/specs/unityhfsm-locomotion/spec.md` 的运动执行端口要求。
- [x] 1.4 读取 `openspec/changes/add-animation-phase-timeline-facts/design.md` 的 playback progress 边界。
- [x] 1.5 读取 `Ref/BBB-Nexus/Editor/RootMotionExtractor.cs`，只记录可复用采样算法。
- [x] 1.6 读取 `Ref/BBB-Nexus/Character/ConfigData/CharacterDataDefinitions.cs`，只记录可复用数据字段。
- [x] 1.7 读取 `Ref/BBB-Nexus/Character/Core/Driver/MotionDriver.cs`，只记录曲线驱动思想。
- [x] 1.8 静态搜索当前 `CharacterController.Move` 位置，确认唯一运动出口。
- [x] 1.9 静态搜索当前 `applyRootMotion` 配置，确认基础移动仍关闭完整 Root Motion。

## 2. Profile 数据模型
- [x] 2.1 在当前项目命名空间内规划 `LocomotionMotionProfileSO`。
- [x] 2.2 Profile 记录 `BasicMovementPhase`。
- [x] 2.3 Profile 记录 alias key。
- [x] 2.4 Profile 记录烘焙时长。
- [x] 2.5 Profile 记录累计本地 X 位移曲线。
- [x] 2.6 Profile 记录累计本地 Z 位移曲线。
- [x] 2.7 Profile 记录累计本地 yaw 曲线。
- [x] 2.8 Profile 记录 source clip 名称用于诊断。
- [x] 2.9 Profile 记录 source clip guid 或等价稳定标识用于校验。
- [x] 2.10 Profile 不保存 Animancer runtime 对象。
- [x] 2.11 Profile 不保存 BBB runtime 类型。
- [x] 2.12 Profile 提供空数据或 invalid 状态。

## 3. Profile 绑定模型
- [x] 3.1 规划 `LocomotionPhaseMotionProfileBinding`。
- [x] 3.2 Binding 记录 phase。
- [x] 3.3 Binding 记录 alias key。
- [x] 3.4 Binding 记录 profile 引用。
- [x] 3.5 在 Run 配置中增加 profile binding 集合或等价结构。
- [x] 3.6 Resolver 必须同时匹配 phase 和 alias。
- [x] 3.7 Resolver 在 profile 缺失时返回空结果。
- [x] 3.8 Resolver 在 alias 不匹配时拒绝 profile。
- [x] 3.9 默认只给 `MoveStop / RunEnd` 绑定 profile。
- [x] 3.10 保持 Animancer TransitionLibrary 继续管理 clip/fade/speed。

## 4. 播放窗口模型
- [x] 4.1 规划 `AnimationMotionPlaybackWindow` 或等价纯数据模型。
- [x] 4.2 播放窗口记录 phase。
- [x] 4.3 播放窗口记录 alias key。
- [x] 4.4 播放窗口记录 previous normalized time。
- [x] 4.5 播放窗口记录 current normalized time。
- [x] 4.6 播放窗口记录是否有效。
- [x] 4.7 phase 改变时重置 previous normalized time。
- [x] 4.8 alias 改变时重置 previous normalized time。
- [x] 4.9 播放重启时重置 previous normalized time。
- [x] 4.10 播放窗口不携带 AnimancerState。
- [x] 4.11 播放窗口不携带 AnimationClip。

## 5. Motion Facts 模型
- [x] 5.1 规划 `AnimationMotionProfileSample`。
- [x] 5.2 Sample 记录是否有有效运动贡献。
- [x] 5.3 Sample 记录本地平面 delta。
- [x] 5.4 Sample 记录 yaw delta。
- [x] 5.5 Sample 记录来源 phase。
- [x] 5.6 Sample 记录来源 alias key。
- [x] 5.7 Sample 默认值不贡献运动。
- [x] 5.8 规划 `BasicMovementMotionFacts` 或等价 Movement 层纯数据模型。
- [x] 5.9 Movement facts 不引用 `ThirdPersonAnimation` 类型。
- [x] 5.10 Movement facts 不引用 Animancer。
- [x] 5.11 Movement facts 不引用 AnimationCurve。
- [x] 5.12 Movement facts 不引用 Unity 场景实例。

## 6. Sampler
- [x] 6.1 规划 `AnimationMotionProfileSampler`。
- [x] 6.2 Sampler 输入 profile。
- [x] 6.3 Sampler 输入播放窗口。
- [x] 6.4 Sampler 在 profile 为空时输出无贡献。
- [x] 6.5 Sampler 在播放窗口无效时输出无贡献。
- [x] 6.6 Sampler 在 phase 不匹配时输出无贡献。
- [x] 6.7 Sampler 在 alias 不匹配时输出无贡献。
- [x] 6.8 Sampler 将 normalized time clamp 到 profile 范围内。
- [x] 6.9 Sampler 通过累计曲线差值计算本帧 local planar delta。
- [x] 6.10 Sampler 通过累计 yaw 曲线差值计算本帧 yaw delta。
- [x] 6.11 Sampler 不读取 Animancer runtime 对象。
- [x] 6.12 Sampler 不读取 AnimationClip。
- [x] 6.13 Sampler 不调用 CharacterController。
- [x] 6.14 Sampler 不写 Transform。

## 7. Controller / Pipeline 组装
- [x] 7.1 在 `PlayerLocomotionController` 或等价组装层解析当前 phase。
- [x] 7.2 读取当前播放进度快照。
- [x] 7.3 维护上一帧播放进度窗口。
- [x] 7.4 根据 phase + alias 从 Run 配置解析 profile。
- [x] 7.5 调用 sampler 生成 animation motion sample。
- [x] 7.6 将 animation motion sample 转换为 Movement facts。
- [x] 7.7 将 Movement facts 传入 `BasicLocomotionPipeline` 或 command builder。
- [x] 7.8 保持 phase facts 和 motion facts 分开。
- [x] 7.9 保持状态图切换逻辑不读取 Profile 资产。
- [x] 7.10 保持没有新增第二个 Update 驱动入口。

## 8. MovementCommand / Executor
- [x] 8.1 扩展 `MovementCommand` 或等价命令以携带动画烘焙本地平面 delta。
- [x] 8.2 扩展命令以携带动画烘焙 yaw delta。
- [x] 8.3 命令默认不包含动画运动贡献。
- [x] 8.4 `MovementCommandBuilder` 接收 Movement motion facts。
- [x] 8.5 无输入且 `MoveStop` 有 RunEnd sample 时，命令包含 RunEnd delta。
- [x] 8.6 有输入切回 `MoveStart` 时，命令不继续包含旧 RunEnd delta。
- [x] 8.7 CharacterController executor 把本地 planar delta 转成 world delta。
- [x] 8.8 CharacterController executor 合成输入驱动位移和动画烘焙位移。
- [x] 8.9 CharacterController executor 继续统一处理重力。
- [x] 8.10 `CharacterController.Move` 仍只在 executor 或 adapter 内调用。

## 9. 编辑器烘焙工具
- [x] 9.1 规划 `LocomotionMotionProfileBakerWindow`。
- [x] 9.2 Baker 支持选择目标 prefab。
- [x] 9.3 Baker 支持选择 AnimationClip。
- [x] 9.4 Baker 支持选择 phase。
- [x] 9.5 Baker 支持输入 alias key。
- [x] 9.6 Baker 采样 root transform 位移。
- [x] 9.7 Baker 采样 root yaw。
- [x] 9.8 Baker 写入累计本地 X 曲线。
- [x] 9.9 Baker 写入累计本地 Z 曲线。
- [x] 9.10 Baker 写入累计 yaw 曲线。
- [x] 9.11 Baker 写入 source clip 诊断信息。
- [x] 9.12 Baker 不修改 Animancer TransitionLibrary。
- [x] 9.13 Baker 不修改场景实例。
- [x] 9.14 Baker 不依赖 BBB runtime 类型。

## 10. 资产和配置
- [x] 10.1 为 `RunEnd` 创建或更新 motion profile 资产。
- [x] 10.2 将 `RunEnd` profile 放到项目约定的配置目录。
- [x] 10.3 在 Run 配置中绑定 `MoveStop + RunEnd -> profile`。
- [x] 10.4 校验 profile phase 与 binding phase 一致。
- [x] 10.5 校验 profile alias 与 binding alias 一致。
- [x] 10.6 校验 profile 曲线非空。
- [x] 10.7 校验 profile duration 为正。
- [x] 10.8 确认 `DefaultRunLocomotionAnimationConfig` 不重复配置 clip/fade/speed。

## 11. 自动测试：Profile 和 Sampler
- [x] 11.1 测试空 profile 输出无运动贡献。
- [x] 11.2 测试无效播放窗口输出无运动贡献。
- [x] 11.3 测试 phase 不匹配输出无运动贡献。
- [x] 11.4 测试 alias 不匹配输出无运动贡献。
- [x] 11.5 测试累计 X/Z 曲线差值输出本帧 delta。
- [x] 11.6 测试累计 yaw 曲线差值输出 yaw delta。
- [x] 11.7 测试 normalized time clamp。
- [x] 11.8 测试 phase/alias 变化后不携带上一段 delta。

## 12. 自动测试：Controller / Pipeline
- [x] 12.1 使用 fake playback progress 驱动 `MoveStop / RunEnd`。
- [x] 12.2 使用 fake profile 验证 controller 生成 motion facts。
- [x] 12.3 验证 pipeline 把 motion facts 交给 command builder。
- [x] 12.4 验证 command 默认不含动画运动贡献。
- [x] 12.5 验证 `MoveStop` 无输入时 command 包含 RunEnd delta。
- [x] 12.6 验证 `MoveStop` 中途输入切 `MoveStart` 后不继续消费 RunEnd delta。
- [x] 12.7 使用 fake motion executor 验证可接收到动画运动贡献。

## 13. 自动测试：运动执行端口
- [x] 13.1 用 fake 或轻量 CharacterController 场景验证本地 delta 转 world delta。
- [x] 13.2 验证无动画 delta 时原基础移动行为不变。
- [x] 13.3 验证有动画 delta 时角色发生额外平面位移。
- [x] 13.4 验证重力处理仍在 executor 内。
- [x] 13.5 验证 `CurrentSpeed` 诊断仍合理。

## 14. 静态边界验证
- [x] 14.1 运行 `rg -n "BBBNexus" 3cDemo/Client/3C_Client/Assets/Scripts/Character` 并确认新增运行时代码不引用 BBB。
- [x] 14.2 运行 `rg -n "CharacterController\\.Move" 3cDemo/Client/3C_Client/Assets/Scripts/Character` 并确认只在 executor 或 adapter 内。
- [x] 14.3 运行 `rg -n "applyRootMotion\\s*=\\s*true" 3cDemo/Client/3C_Client/Assets/Scripts/Character` 并确认基础移动未启用完整 Root Motion。
- [x] 14.4 运行 `rg -n "Animancer|AnimationClip|AnimationCurve" 3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver 3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Model`。
- [x] 14.5 运行 `openspec validate add-locomotion-motion-profile-facts --strict --no-interactive`。

## 15. Unity 验证
- [x] 15.1 让 Unity Editor 完成脚本编译。
- [x] 15.2 检查 Console 没有新增编译错误。
- [x] 15.3 运行定向 EditMode 测试 `PlayerLocomotionControllerTests`。
- [x] 15.4 记录是否使用 Unity MCP；不得使用 Unity batchmode。

## 16. 手动验证
- [x] 16.1 打开当前 3C 演示场景。
- [x] 16.2 确认角色绑定 Run 配置。
- [x] 16.3 确认 `MoveStop / RunEnd` 绑定 motion profile。
- [x] 16.4 进入 Play Mode。
- [x] 16.5 按住移动键进入 `MoveLoop`。
- [x] 16.6 松开移动键进入 `MoveStop`。
- [x] 16.7 确认播放 `RunEnd`。
- [x] 16.8 观察胶囊随 RunEnd 烘焙位移继续刹车。
- [x] 16.9 确认 RunEnd 播完后进入 `Idle`。
- [x] 16.10 再次进入 RunEnd 后中途输入移动。
- [x] 16.11 确认立即进入 `MoveStart`。
- [x] 16.12 确认旧 RunEnd 剩余位移不再继续推动角色。
- [x] 16.13 确认没有额外角色控制器或第二套状态机参与。

## 17. 文档更新
- [x] 17.1 更新 `docs/agents/character-animation-state-roadmap.md` 当前基线。
- [x] 17.2 说明 RunEnd 已从“播完退出”升级到“烘焙位移 facts + 播完退出”。
- [x] 17.3 说明 BBB 代码复用边界：算法可复制，主链路不可依赖。
- [x] 17.4 说明后续 Motion Warping、IK window、foot lock 需要单独 OpenSpec。

## 18. 完成状态
- [x] 18.1 确认所有实现任务完成后再更新本清单。
- [x] 18.2 确认自动测试、静态验证和手动验证结果已记录。
- [x] 18.3 将所有已完成任务标记为 `- [x]`。

## 验证记录
- 自动测试：已通过 Unity MCP 运行 EditMode `ThirdPersonMovement.Tests.PlayerLocomotionControllerTests`，结果 succeeded。
- 静态验证：已确认新增运行时代码不引用 `BBBNexus`；`CharacterController.Move` 仍只出现在 `CharacterControllerBasicMotionExecutor`；Movement model/solver 未引用 `Animancer`、`AnimationClip`、`AnimationCurve`。
- OpenSpec：已通过 `openspec validate add-locomotion-motion-profile-facts --strict --no-interactive`。
- Unity 编译：已通过 Unity Editor refresh/compile，Console 未发现新增 error。
- 工具使用：使用 Unity MCP 执行刷新、资产烘焙和 EditMode 测试；未使用 Unity batchmode。
- 手动验证：未执行 Play Mode 手感验证，第 16 节保持未勾选。

