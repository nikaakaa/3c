## MODIFIED Requirements

### Requirement: Final Pose Publication必须原子拥有最终结果与Physical写入

`CharacterFinalPosePublication` MUST 唯一拥有 Committed/Pending Final Pose 物理页、完整 Physical Bone binding、Final Writer binding、整 Rig 预验证、唯一 Apply 和 Publication Result。Program Image 的 Output Family MUST 只保存稳定 `CharacterFinalPosePublicationLayoutHandle`，该 handle 只表达 Output layout slot 而不包含 Actor 页指针；Actor Runtime 创建时 MUST 由 Final Publication 把它绑定到当前 Actor 唯一 Pending 页。Program Runtime 通过 actor-local binding 写入 Output Pose 并发布只读 `ProgramOutputPoseResult`，MUST 不在 Program Workspace 分配第二 Final Pose buffer。Final Publication MUST 在写任何 Physical Bone 前验证 Pose availability、Rig、continuity、Program completion、Constraint completion 和 Frame lineage；合法时一次写入完整 Pending Pose，非法时保持 Committed Pose 并返回正式失败。

Compiler MUST 只证明唯一 OutputPose、唯一 Final Publication requirement 与唯一 layout handle；具体 Final Publication 实例、Physical Bone binding 和 Writer 唯一性 MUST 由 Runtime Factory 与 Final Publication 构造验证。系统 MUST 保持一个最终写入流程，不建立 Writer Graph 节点、第二 Writer、第二 Final Pose 页、图外 Transform 写入或运行时 Writer 选择。

当角色声明动画属性时，同一 Final Publication MUST 一并拥有属性 Committed/Pending 结果、Renderer/Mesh binding、dense 目标索引及写入 completion。最终写入流程 MUST 保持现有骨骼数学，并在同一整体预验证通过后写入骨骼和 BlendShape；属性 MUST 不创建独立 Publication、独立 Seal 或绕过根 Frame Transaction。所有目标、参数值和 lineage 必须在第一笔可见写入前完成验证。Source Graph、decoder 和 Preview MUST 不直接发布管理中的 Renderer 属性。

#### Scenario: Pending Pose完整合法

- **WHEN** 当前 Program Result、Constraint Result、Final Pose 及角色声明的属性结果全部匹配同一 lineage
- **THEN** Final Publication MUST 一次写入全部 Physical Bones 和所声明 BlendShape，并发布同一 completion 的 Result
- **AND** Writer 成功后 MUST 不再执行可能失败的动画业务计算

#### Scenario: 一个Physical binding无效

- **WHEN** 任一 Physical Bone binding 在 Apply 前无效
- **THEN** Final Publication MUST 不写入任何 Pending Physical Bone 或属性
- **AND** 当前 Frame MUST 遵守 Barrier 后的 Fault 政策而不得切换第二 Writer 或恢复后继续

#### Scenario: 一个属性binding无效

- **WHEN** 任一声明 Renderer、Mesh revision 或属性索引在整体预验证时无效
- **THEN** Final Publication MUST 阻止本次骨骼和属性的全部写入
- **AND** MUST 不先提交骨骼后丢弃属性，也不得读取旧 Renderer 数值补成当前帧
