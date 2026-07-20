# Implementation Inventory

> 本文是五段连段迁移完成时的资产与identity记录。后续`refactor-action-transition-eligibility-authoring`已经接管当前动作准入、typed window、Gameplay Tag与Agent schema事实；本文中的Root-owned Cancel window、旧ownership交接和编译hash只能作为迁移历史，不得作为当前authoring合同。

## Authoring roots

- Definition: `Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`
- RootTree: `Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset`
- Root graph authoring id: `79647291-0c69-4e9e-9276-96a93c3647e7`
- Locomotion StateMachine: `8968c5a0-f19d-487f-94cc-01cd191fee7d`
- Action StateMachine: `fdfba4db-d919-460a-a2f3-eb8149c7610c`
- Attack Combo StateMachine: `ba00b356-4bf5-4b38-9bec-7ac934b7a25c`

## Migration source action identities

| Element | Authoring id | Behavior graph / Timeline |
|---|---|---|
| Outer None | `671567a1-32ff-5b54-96aa-db50a54a490b` | `69a2fd13-bfd4-483e-b7a6-d3fd0cadc585` |
| Outer DodgeBack | `0db2c1af-654f-43d2-93e9-3d6d4fa76453` | `ee909991-5be0-4961-838a-a2854baca30d` / `1ec9175b-a959-4960-af6b-4177f601425f` |
| Outer DodgeForward | `9881f8a2-be18-414f-97ab-0ee161d2272f` | `b2328afd-40a8-467d-91f8-784c461f6137` / `86e3cd9c-eab8-41b0-b2f5-e9a73c3cfa27` |
| Outer Attack | `ff371195-3c10-4c47-9f93-440accbee2c3` | `3ae19d5e-dd52-4f44-a80d-e32d2474e7ec` |
| Attack Combo node | `1afcf514-7458-4e28-aaba-10b485528f91` | `ba00b356-4bf5-4b38-9bec-7ac934b7a25c` |
| Attack1 | `bc48e50c-7231-527c-8604-7a402a1c6fac` | `cf79f885-4e1f-4a3b-8bf7-8a21620959b1` / `10f4cb90-8b9a-4944-b77c-14efc9a3124d` |
| Attack2 | `f97c29b1-1f38-5549-9758-d388a4a8e976` | `cbe6f984-5d7a-48e7-89b9-f30bc6c91325` / `40908a3b-5568-459b-b4f0-b871155dc226` |

Attack1 的 AnimationTrack、MotionCurveTrack、TreeTrack 分别为 `0811fba7-c4c7-4cc3-9714-f93b9da4d4ab`、`9673f395-2c0b-46b8-bcc3-f10062581bab`、`ec923db2-74bc-40a3-8826-039fc541c948`。Attack2 对应为 `321b312e-7450-4482-b10e-11066d42129c`、`8ad6ca0d-a703-4830-b271-a8e8e7c690c6`、`760acb2d-ef04-4f3a-92c9-1bb9115de935`。这六个 track identity 在迁移中保持不变。

Attack Action Context 使用 `CorinAttackActionContextSlot`，资产 GUID 为 `2639a9c0228ef404389f9fd9a36561f5`。Dodge Action Context 使用 `CorinDodgeActionContextSlot`，资产 GUID 为 `7294b270a227b32429a8159cdbfd96b3`。

## Migration source ownership data

- `ActionOverride` state: `54586af4-c634-4d6d-b77a-d7c366ae5a32`
- `ActionOverride` behavior graph: `b69ecad6-fb41-4cbc-82bb-75224cf51561`
- `IsDodging` declaration: `bd4e6f68-fbea-479c-8341-331fc093016e`
- Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop、RunEnd 与 MovingTurn 均存在一条读取 `IsDodging` 的 `-100` priority ownership edge。
- ActionOverride 当前通过 `Not IsDodging` 分别返回 RunLoop 或 RunEnd，没有 Attack 与 Dodge 的独立返回语义。

## Animation sources

全部资源为 60 fps、非循环。Pipeline pose 来源固定为 `WithWeaponInplace`，gameplay motion 来源固定为 `WithWeaponRootmotion`。

| 段 | Inplace GUID | Rootmotion GUID | 帧数 |
|---|---|---|---:|
| Attack1 main | `93faf90a3b0029c4bb5f1ecdd8813664` | `b366720336dc71a4fb9332245a0bfc04` | 49 |
| Attack1 End | `8b4233118d240dc4d9586ec2ddd4654a` | `8c7c70840c9030e40a85ce02fd44abd2` | 119 |
| Attack2 main | `7aa15e1ae19c68c45bca62dc6872227d` | `800f4840b7a4c0249a84624240a98521` | 48 |
| Attack2 End | `7d3811193da431c4b919862f61accf12` | `691310491513fde48b1807dcece401f0` | 125 |
| Attack3 main | `2068e3da72a4f4843aaea894f48ce5d1` | `856921a1b76f1c64ab14701fd51da8a9` | 81 |
| Attack3 End | `1be4b1c800eaeed4fab812e75c6907a0` | `86f6720229044e94bae3f2df7132d170` | 125 |
| Attack4 main | `a86a098728726b344a27357e2f096ef5` | `edd3281b20c171d4e9cb04a9679fa926` | 89 |
| Attack4 End | `f5a9dd9e8df1bc54fb9b5d38a330c7aa` | `3f5ed4d8f1b6f024c8676a84939cf4e8` | 193 |
| Attack5 main | `787d994c5ba1454499149cd3f07d740b` | `a69d162548ad1ea4ba6aa784cc4ed76f` | 125 |
| Attack5 End | `8aa6c7b7f1acaa043a9895e903ff011c` | `b376aaad61175d141b11255a9b9b3625` | 87 |

现有 `Corin_Pipeline_Attack1_Inplace` 与 `Corin_Pipeline_Attack2_Inplace` 保持 GUID 不变。它们与对应 WithWeaponInplace 源拥有相同骨骼 curve path，Pipeline 版本仅按既有口径将根节点初始 X/Z 归零。

现有 `CorinAttack1RootMotionCurve` 与 `CorinAttack2RootMotionCurve` 的资产 GUID 分别为 `33a4e633163b3d443bc19c36e15a2b75` 与 `de9ac662445f14d49817610f59d6a9ab`，其 SourceClip 分别是表中 Attack1/2 main Rootmotion GUID。全部 WithWeaponRootmotion 候选都在 `Bip001` 路径上提供 `m_LocalPosition.x/y/z` 和 `m_LocalRotation.x/y/z/w` 根 transform binding；烘焙后曲线使用本地累计位移与 signed yaw。

`Normal_03_Explode` 与 `Normal_05_B` 只登记为未接入的特殊资源。本 change 不为其推断输入、状态或 transition。

## Preserved window identities

| Declaration | Authoring id | Digest |
|---|---|---:|
| Attack1Hit | `5e549c2b-b601-4fbd-a65b-cdb0f3a63f29` | 1001 |
| Attack1Cancel | `e143e75d-8bd1-4556-b7f0-514be04f9307` | 1002 |
| Attack2Hit | `21a3c2b3-7038-414d-a366-e85dd695fff9` | 2001 |
| Attack2Cancel | `8100c8f1-9f1e-4dc8-998f-a978b8b1ca1d` | 2002 |

Attack1/2 的现有 Hit、Cancel TreeClip、ActionCue、Action Context 与 lifecycle identity 必须保留。新增 Attack3/4/5 使用全新 identity，不克隆旧 authoring id。

## Final migration result

外层 Action StateMachine 最终只保留 `None`、`Attack`、`Dodge`。Attack body 内联持有五段单向 Combo StateMachine；Dodge body 内联持有方向 StateMachine，原 DodgeBack、DodgeForward state body、Timeline 与 lifecycle identity 均直接迁入，没有克隆第二份资产。

| State | State id | Behavior graph id | Timeline id | Animation track id |
|---|---|---|---|---|
| Attack1 | `bc48e50c-7231-527c-8604-7a402a1c6fac` | `cf79f885-4e1f-4a3b-8bf7-8a21620959b1` | `10f4cb90-8b9a-4944-b77c-14efc9a3124d` | `0811fba7-c4c7-4cc3-9714-f93b9da4d4ab` |
| Attack2 | `f97c29b1-1f38-5549-9758-d388a4a8e976` | `cbe6f984-5d7a-48e7-89b9-f30bc6c91325` | `40908a3b-5568-459b-b4f0-b871155dc226` | `321b312e-7450-4482-b10e-11066d42129c` |
| Attack3 | `470e8a7b-e6b3-4aa6-b7be-0783a024858f` | `451f8513-596d-416c-a685-fff57e9fe51f` | `001b250e-9f1c-44ab-a170-c43896e756ac` | `5cb744b3-207c-4c5e-8485-5b933cd7b7ad` |
| Attack4 | `3041ec1d-e44d-4a1c-88aa-a944304df042` | `87cc7381-11cd-4631-a1b1-99114073ede9` | `9fa96566-14a4-47e4-b6ea-c6c710ffbbcf` | `1819d573-4469-4f6c-a97e-fdf6640b04b1` |
| Attack5 | `d54e89e5-632f-49e8-b290-528f813deb6d` | `3dfd9a99-a63f-4df6-9689-b9b9789e2171` | `7b4f6ad5-7513-4a00-92f3-d34c3761d2f9` | `ce9ae6ae-b878-4ecf-92b5-56e3c161aaf9` |

Combo transition 最终为 `Attack1 -> Attack2 -> Attack3 -> Attack4 -> Attack5`。前四段只在对应 Cancel window 与新的 Attack request 同时成立时前进；第五段没有 Cancel declaration、没有回 Attack1 的 edge。任一阶段未收到下一段输入时，主动画与 End 动画继续在同一 Timeline 内自然播放到 `StateRootCompleted`。

方向 Dodge StateMachine authoring id 为 `ac7e681e-356d-4092-87b6-d6836bcac884`。DodgeBack 与 DodgeForward 分别保持 state id `0db2c1af-654f-43d2-93e9-3d6d4fa76453`、`9881f8a2-be18-414f-97ab-0ee161d2272f`；Entry 依据 MoveAxis 是否超过 StopThreshold 选择分支，leaf 唯一消费 Dodge request，完成或 move-cancel 后进入 nested Exit。

## Generated pose and motion assets

| Segment | Pipeline pose GUID | Motion curve GUID | Timeline main range | Timeline End range |
|---|---|---|---|---|
| Attack1 | 保持 `93faf90a3b0029c4bb5f1ecdd8813664` | 保持 `33a4e633163b3d443bc19c36e15a2b75`，End `b7568dbde92a76742a0ac08e3e9840a6` | `0..49` | `43..162` |
| Attack2 | 保持 `7aa15e1ae19c68c45bca62dc6872227d` | 保持 `de9ac662445f14d49817610f59d6a9ab`，End `dd4a620cb19adfe4594d9b569e12192e` | `0..48` | `42..167` |
| Attack3 | `928dc4fc54458fe488f6ee2bd52054a7`，End `fe89c3755a34cf64ca6ec99408ab55d6` | `3de793e7e383356409e9861db8fd51eb`，End `daeaf0e92f1d96744a9d88046fd37135` | `0..81` | `75..200` |
| Attack4 | `c0186e9f72ed34e4dae58f9f7de9ffa0`，End `67587c79547407b4a9fb97cadcf2b6e6` | `c1bfe60992a6a3c49a17520fecf5cc5e`，End `4ead0a1d84a90054abd149a23e88d41f` | `0..89` | `83..276` |
| Attack5 | `cacfffd6d16755a40bd024ab1cfd8438`，End `4cfdcca827720544e9b01db0507ad2bf` | `2a5fa81f9b807e147a8e57c011e48a55`，End `29e969e256fb7054a9eaceb4bf0ab448` | `0..125` | `119..206` |

Attack1/2 的 End pose GUID 分别为 `4bd2aa40793a8a5449b6ef7f4de23426`、`69b4dda2a99b8c441833d55ec2229928`。全部新 pose 资产保留 60 fps、完整时长、非循环和 3328 个 curve path，并使用与既有 Attack1/2 相同的初始根 X/Z 归零。全部 motion curve 使用 `FullLocalDelta`、零起点累计位移与 signed yaw。动画 track 的位移外推为 None，gameplay 位移只由 MotionCurveTrack 提交。

每段主动画与 End 动画使用 6 帧 overlap；没有 Hold 外推，也没有空采样区间。Animation 与 MotionCurve 使用相同的 main/End range。

## Final windows and ownership

| Declaration | Authoring id | WindowId | Digest |
|---|---|---|---:|
| Attack3Hit | `5073c3cb-5aec-4212-871b-a28b8d11375a` | `Attack3Hit` | 3001 |
| Attack3Cancel | `d48f059a-d177-48f2-9727-dbe3bab29ece` | `Attack3Cancel` | 3002 |
| Attack4Hit | `82cd4eac-a745-4f1b-bb85-823aea9b3af6` | `Attack4Hit` | 4001 |
| Attack4Cancel | `def08d90-d398-44ea-847e-3e1908435ac8` | `Attack4Cancel` | 4002 |
| Attack5Hit | `464f77f5-4407-4353-97fd-b457c4fa7222` | `Attack5Hit` | 5001 |

这些 declaration 均为 root-owned Frame Bool、`ActionWindow` fact projection，分类为 `Action/Windows`。`IsDodging` declaration、写入节点和 Locomotion rule 引用已删除。动作与移动的正式交接只使用 `HasActionLocomotionOwnership` 和 `ResumeLocomotionThroughRunEnd`：Action 先执行并写 ownership，Locomotion 后执行并统一进入无表现内容的 ActionOverride，再根据输入和 resume 语义恢复 RunLoop、RunEnd 或 Idle。

Presentation Profile 最终有 14 个 Base layer producer：原 7 个 Locomotion、原 Attack1/2、原 DodgeBack/Forward，加新增 Attack3/4/5。没有由 End clip 创建第二个 producer，也没有 stale Attack binding。

## Final compile identity

- ProgramId: `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- SourceRevision: `d6c0dc6fc95b2c68648c86d8a884f4629146c2b0c943bb215cbbe215c6e5e4d0`
- ProgramHash: `d8ae1f401af8cff84f6aeeae32ef30ce39231d05080b1875f7d432e93d1b67ac`
- LayoutHash: `a27fc550329b565780397569759d627e05e4657e26e5244f7905fe4dfa116b86`
- Compiled producers: 25
- Animation producers: 14

该次迁移使用的Agent样例曾全部schema-valid、compile-success，业务coverage为19/19；这些编译hash和1134个StateSlot只属于该次产物。当前正式schema为`agent-character-controller-synthesis.v9`，当前资产、Program与Projection身份以`refactor-action-transition-eligibility-authoring/implementation-inventory.md`及正式Reader输出为准。Program与Presentation Projection继续只由统一`CharacterSimulationBuildOrchestrator`发布，不手写generated产物。
