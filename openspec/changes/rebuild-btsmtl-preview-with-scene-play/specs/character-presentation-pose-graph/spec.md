## MODIFIED Requirements

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST提供 Definition-scoped Navigator、唯一共享画布、Details 和可折叠 Bottom Dock。Authoring MUST只通过正式 Presentation Mutation 修改当前 owner；Live MUST只读取匹配正式 Pose Graph、Projection 和当前参数采用身份的 snapshot；References MUST只读显示 Source Slot、Profile binding、资源、source map、Action producer、Rig、Policy 和 call site。稳定 identity、GUID、revision、hash 与 compiled index MUST默认隐藏。

场景预览期间 MUST在作者区域允许已有正式运行调参合同支持的字段，并保持原有生效时机和 Undo；Live snapshot、状态和 Fact MUST仍只读。结构和装配编辑 MUST在 Edit Mode 完成。合法参数修改 MUST分别显示作者版本与实际采用状态；真正的拓扑或 Projection 不匹配 MUST显示过期并清空错误关联，不能将参数提交成功伪装成全部版本一致。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中 Locomotion State 的 Clip 或 BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的 Source Slot 引用和合法字段，References MUST显示资源与唯一 owner
- **AND** MUST不显示 BaseLocomotion Gameplay producer 或可编辑 Source Id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot 与正式拓扑或 Projection 不匹配
- **THEN** Live MUST显示过期并停止错误映射
- **AND** MUST不从作者默认值或 Animancer state 伪造运行结果

#### Scenario: 参数等待下一次激活

- **WHEN** 已保存作者参数但当前 Actor 尚未到达该字段的生效边界
- **THEN** Details MUST分别显示作者修改和运行待采用状态
- **AND** Live MUST继续显示当前真实采用值

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST将 Pose Graph 降低为唯一不可变 Program Image，正式 Actor MUST使用同一 Factory 装配 actor-local Execution View、Program、Source、Constraint、Final Publication、根帧事务与 Tuning Snapshot。完整角色预览 MUST在独立场景 Play 中观察这个真实 Actor，不另建 Preview Runtime 或提供外部 Fact/Action/Query fixture。场景预览与其它正式运行 MUST保持相同 Stage Schedule、Operation evaluator、source backend、world-query Adapter、FBBIK、Final Writer 与 completion 语义；每帧每个正式阶段 MUST只执行一次。Live Debug MUST只读对应 Committed Result。

#### Scenario: Graph修改后继续Preview

- **WHEN** 需要 Build 的 State、Slot、Rig、Pose 空间、节点结构或资源变化使产物过期
- **THEN** 受控预览 MUST停止使用该旧产物并要求明确 Build 与重新启动
- **AND** MUST不创建临时 Program、旧 ABI reader、隐藏空间转换或旧产物补充路径

#### Scenario: Preview缺少world context

- **WHEN** 真实预览 Actor 执行 Foot Placement 时缺少精确世界上下文
- **THEN** 正式 Runtime MUST报告 typed Unavailable 并停止依赖结果的帧发布
- **AND** 窗口 MUST只显示失败，不能跳过约束或伪造地面
