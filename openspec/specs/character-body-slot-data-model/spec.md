# character-body-slot-data-model Specification

## Purpose
定义角色身体仲裁结果的正式 slot contract，区分 Source、Action、Claim、Slot、Channel 和 Presentation Layer，并固定 BaseSlot / UpperBodySlot 的读取语义。
## Requirements
### Requirement: 身体仲裁结果必须使用 Slot Contract
系统 MUST 用明确的 slot contract 表达角色身体仲裁结果。slot contract 至少 MUST 能表达 `BaseSlot` 和 `UpperBodySlot` 的 owner、是否被压制、以及产生该结果的 source step 或等价调试信息。当前正式读取面为 `BaseSlotOwner`、`UpperBodySlotOwner` 和 `UpperBodySlotSuppressed`，或批准的等价类型名；MUST NOT 使用 animation layer 名称作为正式 gameplay contract。

#### Scenario: Frame plan 暴露 BaseSlot
- **WHEN** `BodyArbiter` 为一帧生成 `CharacterFramePlan`
- **THEN** plan MUST 暴露 `BaseSlot` owner 或批准的等价读取面
- **AND** 新 runtime、compiler、editor adapter 和测试 MUST 使用 slot 口径读取该结果

#### Scenario: Frame plan 暴露 UpperBodySlot
- **WHEN** `BodyArbiter` 为一帧生成 `CharacterFramePlan`
- **THEN** plan MUST 暴露 `UpperBodySlot` owner 或批准的等价读取面
- **AND** plan MUST 能表达该 slot 被 FullBody claim 压制

### Requirement: Claim 与 Slot Owner 必须分离
系统 MUST 区分 body claim 和 slot owner。`FullBody`、`UpperBody` 或批准的等价词只能描述 claim kind；slot owner MUST 描述最终由哪个 source 或领域拥有该 slot。

#### Scenario: FullBody claim 映射到 BaseSlot owner
- **GIVEN** CommittedAction source 提交 FullBody claim
- **AND** 该 claim 被采纳
- **WHEN** 角色级计划完成
- **THEN** `BaseSlot` owner MUST 是 CommittedAction、Action-side owner 或批准的等价 owner
- **AND** `FullBody` MUST NOT 作为 slot owner 输出

#### Scenario: UpperBody claim 映射到 UpperBodySlot owner
- **GIVEN** 某个 source 提交 UpperBody claim
- **AND** 没有 FullBody claim 压制它
- **WHEN** 角色级计划完成
- **THEN** `UpperBodySlot` owner MAY 是 UpperBody、UpperBodyAction 或批准的等价 owner
- **AND** `BaseSlot` owner MUST NOT 因 UpperBody claim 被隐式替换

### Requirement: Layer 命名不得作为正式身体结果契约
系统 MUST 将 `BaseLayerOwner`、`UpperBodyOwner` 或等价 layer 口径命名从正式身体结果契约中删除。系统 MUST 使用 `BaseSlotOwner`、`UpperBodySlotOwner` 和 `UpperBodySlotSuppressed`，不得维护 layer 口径兼容属性。

#### Scenario: BaseLayerOwner 只是旧错词
- **WHEN** 文档、测试或代码需要表达 Base 身体资源位的 gameplay owner
- **THEN** 它 MUST 使用 `BaseSlotOwner` 或批准的等价 slot contract
- **AND** MUST NOT 使用 `BaseLayerOwner` 表达 gameplay 仲裁结果

#### Scenario: 不保留兼容属性
- **WHEN** 检查 `BodyOccupancyDecision` 和 `CharacterFramePlan`
- **THEN** 它们 MUST NOT 暴露 `BaseLayerOwner`、`UpperBodyOwner` 或等价旧属性
- **AND** 调用方 MUST 读取 slot contract

#### Scenario: 新代码不扩散旧命名
- **WHEN** 新增测试、compiler、editor adapter 或 runtime consumer
- **THEN** 它 MUST 优先读取 slot contract
- **AND** MUST NOT 把 `BaseLayerOwner` 当作新的设计术语

### Requirement: FullBody 不得成为节点、Slot 或 Source
系统 MUST 将 `FullBody` 限定为 claim kind 或历史迁移语境。`FullBody` MUST NOT 成为 behavior graph node、runtime source、slot id、slot owner、Locomotion parent、FramePipeline owner 或 presentation layer。

#### Scenario: Dodge 不需要 FullBody 节点
- **GIVEN** `Action.Dodge` 需要全身接管
- **WHEN** authoring graph、runtime branch 或 timeline 表达 Dodge
- **THEN** Dodge MUST 通过 Action / CommittedAction source 提交 FullBody claim
- **AND** MUST NOT 要求存在 `FullBody` gameplay node

#### Scenario: Locomotion 不归 FullBody 所有
- **GIVEN** Locomotion source 已提交基础移动候选
- **WHEN** FullBody claim 被其它 source 提交
- **THEN** 是否采用 Locomotion 输出 MUST 由 slot contract 决定
- **AND** FullBody MUST NOT 直接拥有、停止或改写 Locomotion runtime 私有状态

### Requirement: UpperBody 和 Facial 不得借本变更扩范围
系统 MUST 保留 UpperBody 作为 slot/claim 扩展边界，但本变更 MUST NOT 实现 UpperBody runtime source、masked animation layer、UpperBody gameplay tick 或 UpperBody editor workflow。系统 MUST NOT 在未审批前新增 Facial、FaceBody 或等价身体仲裁 slot。

#### Scenario: UpperBody 只作为扩展位
- **WHEN** 检查当前身体仲裁模型
- **THEN** 系统 MAY 保留 UpperBody claim、candidate、slot owner 或等价合同
- **AND** MUST 明确它们不代表 UpperBody runtime source 已完成

#### Scenario: 当前模型不包含 Facial slot
- **WHEN** 检查角色身体仲裁模型、frame plan 和 runtime boundary
- **THEN** 系统 MUST NOT 包含 `FaceBody`、`FacialOwner`、`FacialCandidate`、`FacialClaim`、`FacialSlot` 或等价未审批字段
- **AND** 表情能力不得默认参与 BodyArbiter

### Requirement: Presentation Layer 只能消费 Slot Contract
Animancer layer、AvatarMask、Timeline track、GraphView lane、VFX/SFX presenter、camera adapter、IK presenter 或等价表现层 MUST 只消费 slot contract、channel output 或 compiled definition。表现层 MUST NOT 决定 claim 是否采纳或 slot owner 是谁。

#### Scenario: 动画层只消费结果
- **GIVEN** `CharacterFramePlan` 已选择 `BaseSlot` 和 `UpperBodySlot` owner
- **WHEN** Animation Presenter 或 Animancer adapter 播放本帧动画
- **THEN** 它 MUST 只消费最终 animation request、slot owner 或等价 frame output
- **AND** MUST NOT 反向决定 BodyArbiter 的 claim 采纳结果

#### Scenario: Editor view 不是 gameplay slot
- **WHEN** Character Behavior Editor 或 Committed Action Timeline Editor 展示节点、lane 或 track
- **THEN** UI 元素 MUST 映射到 authoring data、claim、slot 或 channel 中的明确一层
- **AND** MUST NOT 因视觉 lane 名称创建新的 gameplay slot
