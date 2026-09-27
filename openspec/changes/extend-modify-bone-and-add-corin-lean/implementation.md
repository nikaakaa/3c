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
