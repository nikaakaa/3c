## ADDED Requirements

### Requirement: Target Program必须以结构化Binding保存Constant Value输入

每个Numeric Target Program MUST从validated Semantic IR降低结构化constant input binding table。每条binding MUST保存target operation、target port、target-specific constant index与resolved value kind，并 MUST进入Program canonical bytes与ProgramHash。Linked input MUST继续只来自`ProgramControlFlowEdge(kind=Value)`。Program constructor、codec、artifact store与composition MUST拒绝重复target port、linked/constant双source、非法constant index、kind不兼容和不支持该table的旧ABI；Runtime MUST不解析`/constant/port:`或其它constant identity约定。

#### Scenario: Compare同时读取连线与常量

- **WHEN** Compare的Left来自Value edge而Right来自authoring constant
- **THEN** Target Program MUST分别保存linked edge和Right constant binding
- **AND** ProgramExecutionLayout MUST能在不解析字符串的情况下合并两者

#### Scenario: Float32与Fixed来自同一Semantic binding

- **WHEN** 同一validated Semantic IR分别生成Float32与Fixed Program
- **THEN** 两个Program MUST保存相同target operation/port语义与resolved value kind
- **AND** constant index和值 MUST按各自Target ABI降低，ProgramHash MUST彼此不同

#### Scenario: 同一端口存在多个source

- **WHEN** Target Program table为同一operation/port包含重复binding或与Value edge冲突
- **THEN** Target build或Program load MUST在composition前失败
- **AND** Runtime MUST不选择第一个、最后一个或任意source继续执行

#### Scenario: Host读取旧字符串端口Program

- **WHEN** Host读取缺少结构化binding table的旧`.csim`、`.fixed-program`或ProgramAsset metadata
- **THEN** artifact/ABI validation MUST明确拒绝
- **AND** MUST不启用legacy parser、migrator、fallback artifact或双版本runtime
