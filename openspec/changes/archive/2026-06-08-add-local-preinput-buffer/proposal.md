# Change: 增加本地预输入与输入缓冲层

## Why
当前基础移动已经通过 `IBasicLocomotionInputSource -> BasicLocomotionInputSnapshot -> BasicLocomotionPipeline -> PlayerLocomotionController` 消费 Move/Look，但还没有一层专门承载攻击、闪避、跳跃、交互这类离散输入请求。后续动作系统如果直接从 Input System 回调里切状态或发动作，就会把输入读取、预输入手感、状态仲裁和表现混在一起，也会绕过当前 Locomotion/HFSM 主线。

本变更只规划一个可以并行实施的本地输入缓冲层：采集离散按钮事实，生成带短窗口的预输入请求，并把请求暴露给未来 ActionArbiter/HFSM 消费。它不实现完整预测回滚、不实现网络协议、不实现攻击/闪避状态，也不改变现有 Move/Look 移动行为。

## What Changes
- 新增离散输入按钮事实模型，表达 Attack、Dodge、Jump、Interact 等按钮的 pressed/held/released。
- 新增本地输入请求缓冲，将 pressed 事实转换为带有效窗口的预输入请求。
- 新增输入缓冲窗口配置，第一版使用小范围 tick 或 frame 计数表达窗口，避免依赖 `Time.time`。
- 明确消费边界：输入缓冲只保存请求，只有未来 ActionArbiter、HFSM 或等价玩法仲裁层能消费请求。
- 明确并行边界：当前 `PlayerLocomotionController` 继续只消费 Move/Look，输入缓冲不接管移动、不播动画、不切状态。
- 为未来预测回滚保留方向：输入缓冲不把“已出招”写成输入事实，消费结果可在未来模拟重放时重新计算。

## Non-Goals
- 不实现完整 tick 系统、SimulationClock、预测回滚、状态快照或服务端重放。
- 不实现 Fantasy 协议、网络发包、服务器输入队列或客户端校正。
- 不实现攻击、闪避、跳跃、连招、取消窗口或完整 `ActionArbiter`。
- 不改变当前 Locomotion 的 Move/Look 行为、相机解析、运动命令或动画表现。
- 不让输入缓冲直接调用 `CharacterController.Move`、Animancer、Cinemachine 或状态切换 API。
- 不新增全局输入单例或绕过当前 `PlayerLocomotionController` 的第二套角色控制入口。

## Impact
- Affected specs: `local-preinput-buffer`
- Related changes:
  - `integrate-unityhfsm-locomotion`
  - `refactor-wasd-to-locomotion-pipeline`
  - `add-minimal-third-person-wasd`
- Affected code:
  - 可能新增 `InputButtonKind`、`InputButtonState`、`BufferedInputRequest`、`InputRequestBuffer` 等纯 C# 模型
  - 可能新增本地离散输入 adapter，用于把 Unity Input System 的按钮输入转换为按钮事实
  - 可能新增 fake consumer/fake input source 测试输入缓冲消费语义
  - 当前 `PlayerLocomotionController` 不直接消费 Attack/Dodge/Jump/Interact 请求
- Validation:
  - EditMode 测试覆盖 pressed/held/released 计算
  - EditMode 测试覆盖请求生成、有效窗口、过期、消费和重复消费保护
  - EditMode 测试覆盖同一输入序列重复构建缓冲得到一致结果
  - 静态搜索确认输入缓冲不引用 Animancer、CharacterController、Cinemachine、Camera 或具体动作状态类
  - 静态搜索确认未新增第二套角色控制器入口
