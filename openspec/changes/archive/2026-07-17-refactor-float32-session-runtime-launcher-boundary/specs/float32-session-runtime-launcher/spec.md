# float32-session-runtime-launcher Specification

## ADDED Requirements

### Requirement: Float32 Pipeline运行输入必须形成不可拆分的Runtime Package

系统 MUST将一个Float32 Pipeline的descriptor、portable Pass factory catalog、Float32 Pass runtime factory catalog与Product runtime catalog组合为不可变Runtime Package。Package MUST在Runtime创建前校验Backend、Pass identity、phase、configuration hash与Product contract一致。公共Unity组合代码 MUST不接收可独立替换的模型专属catalog，也 MUST不识别具体Pipeline Definition类型。

#### Scenario: Authority Pipeline提供Runtime Package

- **WHEN** 已选择的ServerAuthoritative Authority Pipeline降低运行输入
- **THEN** 其portable canonical catalog MUST包装为与Standard Pipeline相同的neutral Runtime Package
- **AND** 公共builder MUST不读取`ServerAuthoritativeAuthorityPipelineDefinition`或`ServerAuthoritativeAuthorityPipelineCatalogSet`具体类型

#### Scenario: Pipeline缺少Float32 Package Provider

- **WHEN** Float32 Composition选择一个没有正式Runtime Package Provider的Pipeline Definition
- **THEN** Composition MUST在创建Solver或Runtime handle前失败
- **AND** MUST不按Pass列表猜测package或回退旧FactorySet

### Requirement: Prepared Source必须显式提供Float32 Runtime Launcher

每个`IFloat32SimulationSessionPreparedSource` MUST在Ready时提供一个非空、与Source descriptor及Target ABI匹配的Runtime Launcher。公共Composer MUST只通过Launcher接口启动Runtime，MUST不按Source具体类型、ModelId、PipelineId字符串、已安装类型或默认规则选择Launcher。

#### Scenario: Local Source完成Preparation

- **WHEN** Local Prepared Source进入Ready
- **THEN** MUST显式提供Standard Float32 Launcher
- **AND** 公共Composer MUST不因没有Model identity而搜索其它Launcher

#### Scenario: Authority Source完成Preparation

- **WHEN** ServerAuthoritative Authority Prepared Source锁定policy、roster与Source ports
- **THEN** MUST显式提供Authority Launcher
- **AND** Launcher MUST同时锁定握手或Authority Scene manifest声明的完整Authority PipelineIdentity
- **AND** 公共Composer MUST不转换为Authority Prepared Source具体类型读取这些输入

### Requirement: Runtime Launcher只能增加启动约束并委托唯一Composer

Float32 Runtime Launcher MUST只验证已经显式选择并降低完成的Composition Request。Launcher MUST不选择、替换或创建另一份Program Runtime、Execution Backend、Pipeline、Session Source或WorldSolver。所有合法Launcher最终 MUST调用唯一portable `Float32SimulationSessionComposer`创建compiled plan、Backend runtime、LaunchPlan与runtime handle。

#### Scenario: Standard Launcher启动Local Session

- **WHEN** Standard Launcher收到合法Local Float32 Composition Request
- **THEN** MUST直接委托唯一portable Float32 Composer
- **AND** MUST不复制Pipeline compile或Backend request构造

#### Scenario: Authority Launcher执行额外校验

- **WHEN** Authority Launcher收到合法Authority Request
- **THEN** MUST先校验Source policy、locked roster、canonical Runtime Package与Authority Source ports
- **AND** 唯一portable Composer MUST在同一次Pipeline编译后、创建RuntimeHandle前精确核对编译结果与expected Authority PipelineIdentity
- **AND** 校验成功后 MUST委托同一个portable Float32 Composer

### Requirement: 公共Unity Float32组合基座不得依赖具体Network Model

公共Unity Float32 Composer、Composition Request Builder与通用Pipeline Package Builder MUST只依赖portable Float32合同、Unity通用Definition合同和Runtime Launcher接口。它们 MUST不引用ServerAuthoritative、Rollback或其它具体Network Model namespace、Prepared Source类型、Pipeline Definition类型或Host launch类型。

#### Scenario: 新增另一个Float32 Network Model

- **WHEN** 后续模型提供合法Prepared Source、Pipeline Runtime Package与Runtime Launcher
- **THEN** MUST能在不修改公共Unity Float32 Composer和Package Builder的情况下接入
- **AND** 模型差异 MUST只存在于模型Source、Pipeline provider、Pass与Launcher实现

### Requirement: Launcher失败必须保持单一资源所有权与Fail-Closed

Runtime Launcher、Runtime Package或portable Composer任一阶段失败时 MUST保留原始结构化failure，并由现有Composition owner按唯一顺序释放Prepared Source、Solver与已创建资源。系统 MUST不重试另一个Launcher、不调用Standard Launcher兜底、不重复Dispose，也 MUST不发布半成品LaunchPlan。

#### Scenario: Authority Launcher校验失败

- **WHEN** locked roster与Program Runtime roster不一致
- **THEN** Authority Launcher MUST在调用portable Composer前失败
- **AND** Composition MUST释放已取得资源且不改用Standard Launcher

#### Scenario: Composition失败时资源清理也失败

- **WHEN** 原始Composition已经产生结构化failure且Solver或Prepared Source的Dispose抛出异常
- **THEN** Composition owner MUST仍然分别尝试释放Solver与Prepared Source
- **AND** MUST保留原始异常类型、failure code与堆栈，并把清理异常附着为诊断信息
