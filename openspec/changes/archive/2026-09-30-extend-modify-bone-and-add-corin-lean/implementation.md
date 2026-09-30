# 实施说明

## 功能入口

- 通用骨骼变换：`CharacterModifyBonePosePayload` 定义模式、空间和来源，`CharacterPoseNativeModifyBoneHandler` 消费同帧参数并输出独立 Component Pose。
- Corin 参数与公式：`Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/CorinAnimationEventGraphAuthoringCode/Lean.cs`；正式资产为 `Assets/Configs/Character/Corin/Pipeline/Presentation/EventGraphs/CorinAnimationEventGraph.asset`。
- 姿势消费：`Assets/Configs/Animation/Presentation/PoseGraphs/LocomotionFullBodyPoseGraph.asset` 中 Run Start、Run Loop 调用 `corin.pose.spine-transform`。子图使用 LocalToComponent、Modify Bone、ComponentToLocal，目标为正式 Spine 骨骼。
- 采样：`CharacterPoseDiagnosticFrame` 的 `lean` 字段组，沿现有 Foot IK Generated Sampling Core/Full 程序输出。

## 当前初始参数

| 图参数 | 值 |
| --- | --- |
| animation.lean.max-angle | 16 度 |
| animation.lean.tilt-seconds | 0.3 秒 |
| animation.lean.recover-seconds | 0.3 秒 |
| animation.lean.full-turn-rate | 180 度/秒 |
| animation.lean.full-speed | 6 米/秒 |

16 度和两项 0.3 秒参考 dump 默认跑配置；转向敏感度和速度权重为本项目初始调参。默认配置的输入键单位与原版曲线未确认，不宣称完全复刻。

实际速度方向的有符号变化决定左右倾斜，速度决定权重。换边先回零；首次、离地、无方向和反向歧义不产生新倾斜目标。现行 RunLoop 运动身份也驱动跑步起步姿势。Spine 在组件空间绕 +Z 旋转：右转产生负角，身体向 +X 倾；左转相反。

## 测试与采样入口

进入现有 GameplayLab 后，可直接测试跑动转弯。直行应逐渐回正；动作完全覆盖时不保留底层跑动 lean；走路、停步和折返仍走原姿势。

现有完整采样器 `character-foot-ik/full` 已准备，采样编译开关保持启用。本次没有启动采样或 replay。

- 开始：`Tools/3C/Diagnostics/Foot IK Generated Sampling/Start`
- 保存：同菜单下 `Stop and Save`
- 查看文件：同菜单下 `Reveal Last Capture`

CSV 字段前缀为 `character-foot-ik/main/frame/lean-`。重点字段：`available`、`eligible`、`horizontal-speed`、`movement-x/z`、`turn-rate`、`target-angle`、`angle`、`rotation-x/y/z/w`。旋转是实际传给骨骼节点的输入参数，不是额外测得的最终脊柱欧拉角。所有字段读取同帧正式结果，未另外计算 lean。

## 已完成的检查与边界

- C# 编译覆盖正式运行代码、作者入口与采样生成器；最终编译 0 错误，没有新增测试。
- Pose 正式资产已完成导出 → C# 编译 → 重新生成，保存成功且无生成诊断。
- EventGraph 正式资产校验和 Native Pose Graph 校验通过。
- 原 EventGraph 的 50 条连接保留，原逻辑节点仅把 Update Split 从 8 路增加到 9 路；新增 lean 分支独立保存历史。
- Pose 资产只修改 Run Start、Run Loop 的内容版本与连接，新增一个普通 Subgraph 和对应目录引用。生成前后的 Profile、Definition 字节一致，原状态、Slot、Body Control Rig 内容未改变。
- Core/Full 的实际生成采样 schema 均包含 12 个 lean 字段，capability revision 为 3。
- 原作者拓扑校验仍报告 Body Control Rig 的 FullBodyIK 缺少直接 Goal Contribution；该图内容与生成前一致，Native 校验通过。本次未修改这条既有 IK 校验及 IK 算法。
- 未执行 Play 视觉验收、replay 或输入回放；最终手感由用户直接测试。

## 接入时一并收口

普通 Subgraph 的多个调用引用不再被误当作多个资产所有者；每个调用仍有独立原生实例，状态子图和 Control Rig 仍维持独占规则。通用 C# 导出器从正式字段合同重建骨骼 payload，并正确传递在最终步骤使用的外部资源引用，没有角色专用的手工补引用。

## 2026-09-28 采样故障与修正

初次交付时的编译、生成和结构校验不足以证明运行成功。用户采样 `20260928-005351-b173cb6e53594670915f08aa88ba4f7a` 的后半段 960 帧中，身体实际移动而动画水平速度始终为零。运行实例的 36 个变量节点均未绑定：无 Agent、无父 Blackboard 时，图初始化的引用相等判断跳过了首次绑定。修正首次初始化与重新载入的绑定，并删除 Lean 显示名中的 `/`，避免它被解释为全局 Blackboard 路径，提交 `f3c03a8dc`。

后续采样 `20260928-010834-346fb6ad442f4da2ad6bf3c3e4e2a6a5` 的速度已更新，但 384 帧 lean 均未启用。正式作者接口误将实例常量同时设为节点默认值，序列化因而省略跑步字符串、限幅及旋转轴。修正 `ConfigureValueInput` 只写作者覆盖值并重新生成 Corin 资产，提交 `a10d86bb1`；保存后反序列化确认常量恢复。未执行 replay，尚无修正后的最终 Spine 视觉验收证据。

架构审查另发现：变量 ID 校验与底层名称解析口径不同，缺变量写入部分只记日志；运行结果成功本身不能证明目标变量被写入。正式 authoring skill 已补充这些边界，提交 `530f4ee3f`。规则更新不等于这些通用缺口已经全部修复。

## EventGraph 零 GC 发布链

本次范围为原生 EventGraph 执行、变量发布、Character 包装及同帧 Pose 读取；不宣称整个角色、编辑器、所有第三方节点零 GC。初始化、重置后的重建和错误诊断允许分配，正常连续帧不创建输出数组、合同、结果对象，不经 object 读取值类型。

- 图实例初始化时将正式声明解析为类型化变量读取器；每帧按布局读取，删除逐帧 LINQ、装箱和变量数组复制。枚举保留类型身份，以底层整数转换，避免 `Enum.ToObject` 装箱。
- 实例持有两页输出。完整采集到写入页后才切换发布页，失败不发布部分内容。只读帧和执行结果使用值类型，Character 合同每实例共享。
- 帧是当前成功发布结果的只读借用；下一次成功发布使旧帧失效，Reset、Replacement、Dispose、Fault 同样失效。旧帧 `TryRead` 返回 false，强制读取明确报错，不允许旧帧身份读取新内容。需要长期记录的诊断在同帧复制所需标量。
- 当前 Pose 的执行、Commit/Discard 都在同一表现帧内完成，Pending 不保留帧读取任务到下一次更新。Pending 后保留 Blackboard 历史，下次动画更新继续前进，不回滚、不重放。输出缓冲不承担 Pose 历史快照职责。
- 同步修正重复 invocation 比较使用身份本身，以及失败销毁实例后 `finally` 的清理空引用。

检查结果：`ThirdPersonClient.Editor.csproj` 编译通过，0 错误。Unity Editor Mono 中使用正式 Corin 资产创建独立内存实例，预热 200 次后连续调用 1,000 次；原生图执行、变量发布、枚举读取合计分配 0 字节，单次最大 0 字节。再经真实 Character 宿主更新、Fact 构造、变量包装与读取测量同样为 0 字节；测量入口的反射与委托编译均在计数前完成，没有将它们混入逐帧范围。

发布版本检查确认：下一次成功发布后旧帧失效，Reset 前当前帧有效、Reset 后失效；Reset 后更新成功且首帧转向速度为 0，Character 合同引用共享。独立实例的无效输入返回结构化故障，未被 `finally` 空引用覆盖。探针输出过非零 lean 命令，但未做最终骨骼视觉验收。本次没有新增测试文件，没有 replay 或游戏输入回放，也没有 Player/IL2CPP 构建性能证据。
