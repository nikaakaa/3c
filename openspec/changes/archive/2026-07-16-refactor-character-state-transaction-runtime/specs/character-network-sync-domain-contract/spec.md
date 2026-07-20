## MODIFIED Requirements

### Requirement: Network Model 必须只交换 Canonical Committed Character State

需要Prediction History、Authority Baseline、Correction、Restore或Rollback的Network Model MUST只保存由正式Character State codec从committed typed State生成的canonical bytes，并同时保存ProgramHash、LayoutHash、NumericProfile、Target ABI与State codec identity。Model Source、Pass、History、packet和worker MUST不保存或传递active State Transaction、PendingCharacterEvaluation、typed mutable partition、GameplayEffect working view或领域模块内部对象。恢复时 MUST先通过匹配Program/Layout的正式codec解码完整committed State，再由Pipeline restore transaction原子替换working world。

#### Scenario: ServerAuthoritative 保存 Prediction History

- **WHEN** Prediction Egress为Tick T保存owner Character state
- **THEN** history MUST保存新codec产生的canonical committed state bytes与StateHash
- **AND** MUST不保存Float32CharacterStateTransaction或Builder引用

#### Scenario: Authority Baseline ABI 不匹配

- **WHEN** 客户端收到Target ABI、LayoutHash或State codec identity不匹配的Authority Baseline
- **THEN** Baseline Merge MUST明确失败并进入模型定义的recovery/fail-stop策略
- **AND** MUST不尝试旧codec、字段级兼容或默认空state

#### Scenario: Deterministic Rollback 使用 Fixed Target

- **WHEN** Fixed rollback模型保存world snapshot
- **THEN** MUST使用Fixed Target自己的typed committed state与canonical codec
- **AND** MUST不转换或复用Float32 mutable state实现
