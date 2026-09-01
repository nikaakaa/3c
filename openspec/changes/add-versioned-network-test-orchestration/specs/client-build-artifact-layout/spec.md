## MODIFIED Requirements

### Requirement: Network Test Product 必须保留现行 Build/Network 合同

三个 Network Test Product MUST分别使用`Build/Network/UnityAuthority/<CandidateId>`、`Build/Network/DotRecastAuthority/<CandidateId>`与`Build/Network/DeterministicRollback/<CandidateId>`保存不可变正式Candidate。`Build/Network/RunLogs/<Product>/<RunId>` MUST保存RunManifest、RunStatus、运行配置和日志。Product根 MUST只作为Candidate容器，不得直接保存可运行Player、Server或Product manifest。Network workflow MUST从公共ClientBuildArtifactLayout取得NetworkRoot，并保持Content、普通Player与Network分区互不写入。

#### Scenario: 构建两个Rollback候选

- **WHEN** 作者从两个不同干净提交分别构建DeterministicRollback Candidate
- **THEN** 两份完整产物 MUST位于各自CandidateId目录并同时保留
- **AND** 后一次Build MUST不修改前一Candidate或普通Content/Player

#### Scenario: 旧固定根仍包含schema v2产物

- **WHEN** Product根直接存在旧Player、Server或schema v2 manifest
- **THEN** Candidate Catalog与Run MUST拒绝把它解释为正式Candidate
- **AND** MUST不自动迁移、复制到版本目录或创建latest链接
