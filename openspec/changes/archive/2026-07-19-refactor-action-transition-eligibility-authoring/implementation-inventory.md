# Implementation Inventory: 动作切换准入与时间窗口作者链路

## Apply 前 Baseline

本节到 `Confirmed Authoring Gaps Fixed During Apply` 之前记录迁移输入，用于证明 stable identity 如何被保留；它不是当前资产状态。

- Definition：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`
- Snapshot schema：`agent-character-controller-synthesis.v9`
- Snapshot mode：`Full`
- Root Graph：`79647291-0c69-4e9e-9276-96a93c3647e7`
- Program hash：`5dd8826446be026237fb97bb55d16d77e54fcdf9682a24db523370961708645d`
- Locomotion StateMachine：`8968c5a0-f19d-487f-94cc-01cd191fee7d`
- Action StateMachine：`fdfba4db-d919-460a-a2f3-eb8149c7610c`
- Attack Combo StateMachine：`ba00b356-4bf5-4b38-9bec-7ac934b7a25c`
- Dodge Direction StateMachine：`ac7e681e-356d-4092-87b6-d6836bcac884`

## Action Profiles

| Action | Asset | GUID | Granted | Cancel query |
|---|---|---|---|---|
| Attack | `Assets/Configs/Character/Corin/Pipeline/Actions/Attack/CorinAttackActionProfile.asset` | `450722665af79c2468ff212811859458` | 空 | 空 |
| Dodge | `Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset` | `add8a25636588a543a6770231bea14b6` | `Dodge` | 空 |

## Leaf State Bodies

| State | State body Graph | Activation node | Existing Action Exit selector |
|---|---|---|---|
| Attack1 | `cf79f885-4e1f-4a3b-8bf7-8a21620959b1` | `935605a3-a912-5969-9d09-9140c6a67a83` | `c9526ffd-0532-45c0-a631-57adbf04d992` |
| Attack2 | `cbe6f984-5d7a-48e7-89b9-f30bc6c91325` | `5ca1d7d5-06c7-5311-86a0-6caa5f729947` | `f0d18709-fc87-4347-9c69-a9f7ff061474` |
| Attack3 | `451f8513-596d-416c-a685-fff57e9fe51f` | `edf8c28e-db08-4916-8780-e293867ecd7f` | `c81a9328-f702-4e3b-8d4e-c72bed923930` |
| Attack4 | `87cc7381-11cd-4631-a1b1-99114073ede9` | `a5ec1c18-1d81-4da8-ac27-267fd162b50e` | `6480c9d0-cc6b-42e3-b467-d2e3bab3d836` |
| Attack5 | `3dfd9a99-a63f-4df6-9689-b9b9789e2171` | `6a021735-d216-4710-bf73-cb596dcbdad4` | `4ba37705-5a0b-4e3d-97c3-da9a269dccf9` |
| RushAttack（legacy，删除） | `d60b5872-7e2a-4a65-b899-1c2195c8bc66` | `9cba9c47-9632-4c6c-9b28-f5db1ffda426` | `8b16d33f-1685-46e0-90be-5ee44a4021da` |
| DodgeBack | `ee909991-5be0-4961-838a-a2854baca30d` | `ff10ace1-68cb-433b-a188-67636d72cfca` | `9c18c4a5-ea3f-4e3b-986b-e8820a23d0d7` |
| DodgeForward | `b2328afd-40a8-467d-91f8-784c461f6137` | `cb819416-9c25-4f1c-bc2f-921bbd8d9970` | `64597f03-12df-4495-aa3c-803938ba2d96` |

## Timeline Window Identity

| State | Timeline | Tree track | Combo clip | Move/Recovery clip | Combo declaration | Move/Recovery declaration |
|---|---|---|---|---|---|---|
| Attack1 | `10f4cb90` | `ec923db2` | `ffc20627` 50..93 | `de283c70` 73..162 | `e143e75d-8bd1-4556-b7f0-514be04f9307` | `78a0f37c-b725-4f8f-a885-22232133d509` |
| Attack2 | `40908a3b` | `760acb2d` | `a5b14e00` 49..92 | `12c83914` 72..167 | `8100c8f1-9f1e-4dc8-998f-a978b8b1ca1d` | `c16acd64-20b8-493e-80b7-970e30910716` |
| Attack3 | `001b250e` | `e31b63e5` | `a9b9776e` 82..125 | `16532453` 105..200 | `d48f059a-d177-48f2-9727-dbe3bab29ece` | `187f8710-6235-4b68-be90-caae212fbe8e` |
| Attack4 | `9fa96566` | `c8e64134` | `b4b57cb6` 90..133 | `a519e919` 113..276 | `def08d90-d398-44ea-847e-3e1908435ac8` | `b51ef93b-1f67-469b-aa2a-84d0604e1852` |
| Attack5 | `7b4f6ad5` | `d440e3bf` | `05a8ffe7` 126..169 | `487f2658` 149..206 | `51ea3d21-2b19-4af2-8549-4c2f3cda15a7` | `6447351c-b610-4535-966d-60784b278f04` |
| RushAttack（legacy，删除） | `48340869` | `bcfb4fbb` | `a783ad3c` 72..115 | `e59894f2` 95..145 | `41ef81d3-a359-4256-9284-97d024f028fa` | `0e654ec8-b7b1-45ce-9450-9fc05ca36682` |
| DodgeBack | `1ec9175b` | `04d58a49` | 不适用 | `cfc6c09a` 45..141 | 不适用 | `8a0057af-9fcd-46fc-8951-74deb437ee51` |
| DodgeForward | `86e3cd9c` | `73641288` | 不适用 | `33043955` 46..142 | 不适用 | `8a0057af-9fcd-46fc-8951-74deb437ee51` |

完整 Timeline/Track/Clip identity 以本次 Full Snapshot 为准；表内 Timeline/Track/Clip 使用可读短前缀，declaration 使用完整稳定 identity。

## Root Declarations To Remove Or Move

- 保留：`HasActionLocomotionOwnership` / `84a5c3f0-04e7-41cb-8898-515f2ebd3a7f`。
- 迁移：Attack1..4 Cancel declarations 到对应 ComboAccept local owner；Attack5 不保留循环连段路由。
- 迁移：Attack1..5 MoveCancel declarations 到对应 RecoveryLate local owner。
- 迁移：`DodgeRecoveryCancel` / `8a0057af-9fcd-46fc-8951-74deb437ee51` 到 Dodge RecoveryOpen local owner；DodgeBack 与 DodgeForward 不能继续共享一个 owner-local declaration，迁移时必须保留一个 identity 并为另一个 owner 创建新 identity。
- 删除：`ResumeLocomotionThroughRunEnd` / `49b435e0-3522-4fa9-989e-8107ae704469`。

## Confirmed Authoring Gaps Fixed During Apply

- MCP `export_snapshot` 已改为 Full Snapshot；节点位置改用纯 `{x,y}` DTO，避免 Newtonsoft 递归序列化 `Vector2.normalized`。
- Snapshot 已输出 activation/lifecycle stable node identity。
- 新增 `delete_flow_edge`，支持保留节点 identity 的状态 body flow 重接。
- transition command 已改为显式 stable edge identity，不再按端点合并语义不同的边。
- 新增 `move_blackboard_declaration`，支持同事务保留 identity 的 owner 迁移。

## 已关闭的设计分支：Dodge -> RushAttack

`Corin_Attack_Counter_WithWeaponRootmotion` 只应在 Combat Resolution 确认成功闪避或成功格挡后使用。当前仓库没有 PerfectDodge/PerfectGuard 成功事实，上一状态为 Dodge 不能证明业务成功，因此本 change 删除 legacy RushAttack state/route，不增加 parent-transition provenance、Opportunity、派生模块或历史窗口读取。

当前 Dodge 恢复期收到普通 Attack request 时，外层 `Dodge -> Attack` edge 在 source Window/Admission 成立时 replacement；source Dodge 正常关闭后，target Attack 的 nested StateMachine 从普通 Enter 路由进入 Attack1。未来反击必须由正式 Combat Resolution 授予限时 Gameplay Effect Tag，再由另一个 change 配置 CounterAttack。

## Applied Result

- Agent Patch：v9，主迁移 `135` 项；condition 命名收口 `39` 项；两次均完成 dry-run、transaction apply、save、重新 export 与 validate。
- 最终 SourceRevision：`8a2caacbc038d192c281ab7678a215e30588d48e0a96bd7456ee5ccfae0b54f1`。
- 最终 SemanticHash：`4d8383b5b2c7ec1f92a02f12e0cf26d921804061547ecc93eb9a85dabfe479d5`。
- 最终 Float32 ProgramHash：`816015c8e2a1b28ad05ac6bada1c411096541e632af158117d55dc60b375718d`，LayoutHash：`9abc84099a603f265ba8e7ae8d4f0d4b23219ed89318eadeb6b017c6ad3c726d`。
- 最终 Fixed ProgramHash：`961cc6d89db86ab26bb1bced8299c26474919d3fdc583e1375f40d4d0b0785fa`，LayoutHash：`3b5ec9877839e87b6401001b0362989861986288973ded984cc2af633d4e49eb`。
- 最终 Snapshot：`163` graphs、`4` StateMachines、`14` Timelines、`28` Blackboard declarations，schema 为 `agent-character-controller-synthesis.v9`。
- Attack Profile granted `Attack`；Dodge Profile granted `Dodge`；两者 cancel query 均匹配 active `Attack` 或 `Dodge`。
- Attack1..4 各有 `ComboAccept`，Attack1..5 各有 `RecoveryEarly` 与 `RecoveryLate`；DodgeBack/DodgeForward 各有 owner-local `RecoveryOpen`。
- `Attack1Cancel` 等旧文本只作为保留 identity 的 `ActionWindowId` provenance 存在，不再是 Blackboard key、ConditionRule 名或路由入口。
- RushAttack state、Timeline、transition、producer binding 与 generated projection 均已删除；原动画和 motion 素材不属于本 change 的可达 authoring，不作为运行时 fallback。
- Agent Snapshot 先索引完整 topology declaration，再导出 TreeClip write，因此 local window 的 projection、WindowType、WindowId 与 Digest 不再受遍历顺序影响。
- Agent Patch 不拥有 Animation Presentation 写入口；动画绑定仍只由正式 Presentation authoring 边界维护。

## 最终输入、处理、输出与 Owner

- 输入：Input request buffer、source State body 当前 tick 的 Decision TreeClip staged write、target ActionProfile、唯一 Gameplay Effect Tag Container 和 current active ActionInstance。
- 处理：ConditionRuleGraph 使用 `ActionWindowActive(WindowType)`、`CanActivateAction(ActionProfile)` 与现有 Bool 节点选择唯一 transition；StateMachine stop barrier 先关闭 source，source OnExit 提交 terminal lifecycle 并撤销 `action:<ActionInstanceId>` tags，再允许 target activation。
- 输出：唯一 selected edge、source lifecycle result、新 target ActionInstance、对应 Timeline gameplay/motion/presentation 输出，以及 EndFrame 形成的 ActionWindowFact。
- Authoring owner：State body 拥有 local Frame declaration、inline Timeline 与 transition 条件；ActionProfile 拥有 granted/cancel/block policy；GameplayTagCatalog 拥有 tag identity；Presentation profile 继续独立拥有动画 producer binding。
- Runtime owner：portable `ActionAdmissionControl` 拥有准入语义；Gameplay Effect aggregate 拥有 tag 状态；Float32/Fixed 只提供窄端口和 numeric leaf；不存在 ActionWindow registry、私有 Action tag store 或 target activation 隐式取消。

## Spec Delta 对照

- `character-action-authoring-closure` 与 `character-pipeline-blackboard` 用 typed `ActionWindow` local projection 替换按动作命名的 RootTree Cancel/MoveCancel key。
- `character-state-interruption-authoring` 与 `character-state-timeline-authoring-loop` 用 source leaf window 决定离开时机、outer category completion 决定目标，并删除 Action 完成强制 RunEnd。
- `character-action-activation-flow` 与 `gameplay-tag-runtime` 用唯一 portable admission 和唯一 Gameplay Effect Tag Container 替换隐式 source cancel 与私有 tag 合并。
- `agent-character-controller-synthesis` 与 `btsmtl-gameplay-semantic-ir` 将唯一外部合同提升到 Agent v9 和 operation set `/5`，旧 v8、`/4` reader 与兼容分派均不保留。
