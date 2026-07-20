## MODIFIED Requirements

### Requirement: Program 必须是不可变 portable 数据

CharacterSimulationProgram MUST只包含稳定 identity、SemanticHash、typed operation/data table、Character state layout、portable catalog、source map、required world capability manifest、NumericProfile、operation-set version、Target ABI、ProgramHash与 LayoutHash。Target Compiler MUST将 Program编码为独立 canonical `.csim` artifact；该 artifact MUST不包含 UnityEngine.Object、GameObject、AnimationClip、Animancer state、Pipeline Definition、Pass、Execution Backend、Session Source、Endpoint、Transport、Network Model或 mutable World state，并 MUST可由 Unity与普通 .NET Host使用同一 canonical codec读取。

#### Scenario: 纯 CSharp 加载 Program

- **WHEN** 普通 .NET Host加载 Float32 `.csim` bytes
- **THEN** MUST不需要 UnityEngine、ScriptableObject、CharacterPipelineDefinition或 Pipeline asset才可解析 Program
- **AND** MUST得到与 Unity ProgramAsset相同的 ProgramHash与 LayoutHash

### Requirement: Program Artifact 必须与 Source Revision 严格对齐

正式 Target Program artifact MUST记录 compiler version、operation-set version、source revision、SemanticHash、TickRate、NumericProfile、Target ABI、ProgramId、ProgramHash、LayoutHash与 capability manifest。Unity ProgramAsset MUST只包装经过正式 store重读校验的 exact `.csim` bytes与轻量 metadata。Host MUST在 artifact stale、Program缺失、Target ABI不匹配、ProgramAsset metadata不匹配或 required capability不满足时创建失败，MUST不在运行时重新编译、读取 `.csir`、重新编码 Program或使用旧 interpreter。

#### Scenario: Authoring 已修改但 Program 未重建

- **WHEN** Host检测到 source revision与 Program artifact不同
- **THEN** Host MUST拒绝创建 Session并报告 stale source
- **AND** MUST不从 ProgramAsset metadata、旧 `.csim`或 `.csir`选择近似匹配结果

## ADDED Requirements

### Requirement: Target Program 必须作为正式独立 Artifact 原子发布

Editor build MUST将每个 Numeric Target的 canonical Program写入 `Library/CharacterSimulation/Programs/<definition-guid>/<numeric-profile>-abi<version>.csim`，并使用同目录临时文件、完整 flush、重新读取、header/ProgramHash/LayoutHash校验和原子替换。路径 MUST只来自合法 Definition GUID、NumericProfileId与 ABI version，不得按 Definition名称、asset path、ProgramId显示字符串或 fallback名称生成。

#### Scenario: 生成 Corin Float32 Program

- **WHEN** Float32 Target成功降低 Corin validated `.csir`
- **THEN** build MUST发布一份可由普通 .NET Reader读取的正式 Float32 `.csim`
- **AND** Corin ProgramAsset MUST包装从该 store重读的同一 bytes

#### Scenario: Program Artifact 写入中断

- **WHEN** `.csim` 临时写入、重读校验、Unity Asset publish或 Definition reference更新失败
- **THEN** build transaction MUST恢复旧 `.csim`、ProgramAsset、Projection与 Definition references
- **AND** MUST不留下新 Program与旧 Projection或旧 ProgramAsset的混合组合

### Requirement: Program Identity 与 Session Pipeline Identity 必须分离

ProgramHash MUST只覆盖 Numeric Target Program语义和 ABI，MUST不包含 PipelineId、PipelineHash、BackendId、Session Source、Solver或 Network Model。同一 Program MAY进入多个合法 Session Pipeline；Session composition、Snapshot、diagnostics与后续 handshake MUST另外锁定 Pipeline/Backend/Source/Solver identity。Pipeline不同 MUST不要求重新编译 BTSMTL Program，也 MUST不允许两个不同 Pipeline snapshot互换。

#### Scenario: Corin 复用在 Local 与 Prediction Pipeline

- **WHEN** 两个 Session使用同一 Corin Float32 `.csim`但选择不同 Pipeline
- **THEN** 两者 ProgramHash MUST保持相同
- **AND** 两者 PipelineHash与 Session composition identity MUST不同

