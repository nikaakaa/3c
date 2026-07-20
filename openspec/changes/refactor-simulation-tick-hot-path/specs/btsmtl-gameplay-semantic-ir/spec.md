## ADDED Requirements

### Requirement: Semantic IR Value输入必须遵守版本化Port Contract

Character Gameplay Operation Set MUST为每个operation code声明numeric-neutral且版本化的Value input/output port contract。Contract MUST描述稳定port identity、canonical order、固定value kind或受约束kind group及允许转换。Semantic Frontend MUST使用该contract解析linked Value edge和未连接input constant，并 MUST在validated Semantic IR中保存每个constant input的target operation、target port、constant index与resolved value kind。`ProgramControlFlowEdge(kind=Value)` MUST继续是linked input唯一真值；系统 MUST不保存第二份linked binding或依赖constant identity字符串推导端口。

#### Scenario: Linked Value输入通过合同解析

- **WHEN** 一个InputScalar operation连接到Compare的Left端口
- **THEN** Frontend MUST通过Operation Set合同解析source output和target input
- **AND** Semantic IR MUST保留该Value edge且确认其resolved kind满足Compare约束

#### Scenario: 未连接输入使用constant

- **WHEN** Compare的Right端口没有Value edge并在authoring中保存数值常量
- **THEN** Semantic IR MUST生成该literal及指向Compare/Right的结构化constant input binding
- **AND** constant identity MUST不承担端口寻址语义

#### Scenario: 受约束多态端口完成解析

- **WHEN** Compare、And、Or、Not、BlackboardGet或BlackboardSet使用由上下文决定的Value kind
- **THEN** Frontend MUST按operation contract、declaration reference和literal kind解析出确定的Semantic value kind
- **AND** MUST不使用Unknown、Object、运行时反射或Target专用类型作为成功结果

#### Scenario: Value来源或类型冲突

- **WHEN** 同一target port存在两个Value edge、同时存在Value edge与constant binding、source output不存在或value kind不兼容
- **THEN** Semantic artifact build MUST失败并报告source operation、target operation与port identity
- **AND** MUST不发布近似IR、跳过binding或延迟到Runtime猜测
