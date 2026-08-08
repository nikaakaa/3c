# 双端远端 Locomotion 骨骼抽搐排查

## 状态

- 日期：2026-08-08
- 状态：代码根因已确认并修复，等待新 Build 双端复测
- 复现产品：Deterministic Rollback Network Test
- 当前 BuildId：`20260807-144419`
- 当前 RunId：`20260807-224742`

## 现象

- 本地角色的 Locomotion 正常。
- 每个客户端看到的远端角色在 Locomotion 中严重骨骼抽搐。
- 攻击和闪避等 Action 动画正常。
- 问题曾表现为单向，当前可在双端远端角色上出现。

## 已确认事实

- 双端 Gameplay Tick、角色位置和世界哈希可以对齐。
- 当前采样中 `body rollback=0`，根节点 follower 修正为 0。
- 两端动画分支修正计数不对称，曾观测到约 `46` 与 `221`。
- 运行时的对象释放、Action lifecycle journal 和重复 Presentation Fact 报错已经分别修复；这些报错不是 Locomotion 抽搐根因。
- 近期未提交的运行时代码改动集中在 Foot Placement 和 FinalIK 链路。
- `EvaluateFullBodyIk` 会通过 `TryCopyValue` 完整复制输入 Pose，`SolvePrepared` 会再次校验整页，FinalIK 输入页不完整假设已证伪。
- 压缩 descendant index 中每个 child 只沿唯一 parent 链登记一次，重复传播假设已证伪。
- 远端 Committed Body branch replacement 次数明显多于本地，且每次 replacement 都推进了被 Pose Runtime 当成硬重置依据的 generation。

## 已排除或降低优先级

| 候选原因 | 当前判断 | 证据 |
| --- | --- | --- |
| 网络位置不同步 | 降低优先级 | 双端位置与 Gameplay 哈希对齐，`body rollback=0` |
| 根 Transform 反复校正 | 降低优先级 | follower position/yaw correction 为 0 |
| Action Timeline 自身错误 | 基本排除 | 攻击、闪避正常，错误只集中在 Locomotion |
| 模型骨架资源整体损坏 | 降低优先级 | 同一角色本地表现正常 |
| FinalIK 输入 Pose Page 未完整初始化 | 已排除 | `EvaluateFullBodyIk` 完整复制输入 Pose，`SolvePrepared` 校验整页 |
| FinalIK descendant 压缩索引重复传播 | 已排除 | 每个 child 只沿唯一 parent 链登记一次 |

## 最终根因

Committed Body branch replacement 同时承担了两种不同语义：一是 Body/Intent history revision，二是 Pose 硬不连续。旧链路在普通 replay 分支替换时推进 Body `ResetSequence`，Presentation Fact把它直接输出为 `BodyDiscontinuityGeneration`，随后 `CharacterPoseStateMachineRuntime.PrepareFrame` 把 Walk/Run 当成新流执行硬重置。

完整触发链为：

`Committed Body branch replacement`
→ Body `ResetSequence` 变化
→ Fact `BodyDiscontinuityGeneration` 变化
→ `CharacterPoseStateMachineRuntime.PrepareFrame` Reset
→ Walk/Run Sequence Player 与 transition clock 重启
→ Foot Placement 状态重置
→ FinalIK 接收到已经不连续的 Locomotion Pose

远端角色的 branch replacement 多于本地，因此远端 Walk/Run 抽搐更严重。Action 使用独立 playback lifecycle，不依赖这条 Pose generation 重置链，所以攻击和闪避保持正常。

## 假设结论

### H1：FinalIK 输入或输出 Pose Page 没有在每帧完整初始化

近期 `CharacterFinalIkFullBodySolver` 从“复制输入 Pose 后求解”改成“直接求解已准备的输出 Pose”。如果调用方没有在所有远端重放路径上先完整复制 Pose，FinalIK 会在旧帧或部分未写入的骨骼上迭代。

结论：已证伪。`EvaluateFullBodyIk` 通过 `TryCopyValue` 完整复制输入 Pose，`SolvePrepared` 再校验整页。

### H2：Foot Placement 的远端目标在重放或分支重定向时不连续

本地角色没有远端 Presentation Fact 重放；远端角色会经历动画分支重定向。若 contact、surface anchor、pelvis spring 或 previous/current offset 的缓存归属错误，目标会在旧分支与新分支之间跳变。

结论：不是第一错误阶段。Foot Placement 在旧链路中响应 Body generation 变化执行重置，是上游错误语义的受影响模块。

### H3：FinalIK Pose Buffer 的 component-space 层级传播错误

近期 descendant 传播从二维布尔表改为压缩索引。若 parent/descendant 索引、虚拟骨骼边界或同一子节点被重复传播，旋转/平移会被放大到整条腿和骨盆。

结论：已证伪。压缩 descendant index 每个 child 只沿唯一 parent 链登记一次。

### H4：Locomotion 本地表现时钟和远端动画分支重定向共同触发 IK

Locomotion 使用连续 Presentation 时间，Action 使用命令锚点。动画分支重定向本身不应造成骨骼炸开，但可能频繁改变 Foot Placement/FinalIK 的输入连续性。

结论：已确认。Locomotion 源 Pose在进入Foot Placement与FinalIK前，已经因PoseStateMachine和Sequence Player硬重置产生不连续。

## 修复

- `CharacterBodyPresentationRuntime` 只有在replacement真正改变Body运动学数据时才推进branch sequence；canonical provenance变化但Body相同不再触发重定向。
- `CharacterPresentationFactProjector` 将Body branch sequence与Pose discontinuity generation拆开。普通Committed branch replacement只替换Body/Intent history，不清空上一Fact、Presentation time或Pose generation。
- `CharacterSimulationPresentationRuntime` 在Body提交后显式同步branch身份。Committed branch replacement只调用Animation、Foot Placement和Motion Matching的retarget路径，不再调用Pose hard reset。
- 只有Initialization和Selected Stream Reset推进Pose discontinuity generation并重置PoseStateMachine、Sequence Player、Root Orientation Warp、Foot Placement与相关表现时钟。

## 排查记录

### 2026-08-07 初始证据

- 现有双端进程运行稳定，没有新的 Exception、Error 或 Failure。
- 抽搐与 body/root correction 无同步关系。
- 工作区 IK 相关差异涉及：
  - `CharacterFinalIkFullBodySolver.SolvePrepared`
  - `CharacterFinalIkPoseBufferBackend` descendant 压缩索引
  - `CharacterPoseBoneIkGoalSource` Rig identity 类型
  - `CharacterPredictiveFootPlacementGoalSource.RetargetBodyBranch`
- 下一步首先检查 Pose Page 初始化和 FinalIK staging 的所有调用路径。

### 2026-08-08 根因与代码修复

- H1、H3通过代码链路对账证伪。
- 第一个错误阶段定位到Presentation Fact把Body history revision投影成Pose discontinuity generation。
- branch sequence和Pose discontinuity generation已经拆分，普通replay replacement只重基Body/Intent并retarget依赖模块。
- 目标运行时程序集已编译通过；仍需使用新Build执行双端Walk、Run、Attack和Dodge端到端复测。
