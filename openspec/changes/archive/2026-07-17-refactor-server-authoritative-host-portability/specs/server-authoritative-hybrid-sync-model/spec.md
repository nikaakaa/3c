# server-authoritative-hybrid-sync-model Specification

## ADDED Requirements

### Requirement: Authority Host外壳不得拥有模型运行语义

Unity Authority Worker外壳 MUST只负责Unity authoring lowering、Unity transport adapter、显式WorldSolver输入和lifecycle装配。Authority Pipeline、Source policy、queue、clock、checkpoint baseline和replication lowering MUST位于portable ServerAuthoritative模块。未来普通.NET Host MUST复用这些实现，MUST不复制模型运行语义。

#### Scenario: Unity Adapter迁移完成

- **WHEN** Unity Worker进入Active
- **THEN** Authority Source与Pipeline MUST来自portable实现
- **AND** Unity外壳 MUST不保留并行旧queue或factory路径
