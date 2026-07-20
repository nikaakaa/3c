# gameplay-network-model-boundary Specification

## MODIFIED Requirements

### Requirement: Gameplay Network Model 必须是 Session 级唯一装配

Gameplay Network Model MUST作为`SimulationSessionSourceDefinition`的一种实现，通过实际runtime factory创建model session、Endpoint、history、显式Source ports与匹配Target ABI的Runtime Launcher。唯一`SimulationSessionHost` MUST使用同一Composition Definition将该Source与显式Program Runtime、Execution Backend、Pipeline Definition、WorldSolver、ProgramCatalog、roster、Committer和diagnostics组合。Common Host、target-specific Unity Composer与通用Pipeline runtime package builder MUST不硬编码已知Model、Prepared Source或Pipeline Definition具体类型。Character、Graph、Program、Kernel、Pipeline Backend和WorldSolver MUST不保存Model selection。Local Source MUST可独立使用同一Session Host，但 MUST不被声明为Network Model。

#### Scenario: 新增完整Float32 Network Model

- **WHEN** 新模型提供Source preparation、Endpoint、Pipeline Runtime Package、Pass factories与Runtime Launcher
- **THEN** MUST可通过现有五项Composition和公共Unity Float32 request lowering进入唯一portable Composer
- **AND** MUST不修改公共Session Host、Unity Float32 Composer或通用Package Builder

#### Scenario: 当前核心运行Local Session

- **WHEN** 已安装Network Model都没有完整Source factory、Runtime Launcher与合法Pipeline Runtime Package
- **THEN** Local Session MUST通过显式Local Source、Standard Launcher和Standard Local Pipeline正常创建
- **AND** Host MUST不创建GameplayNetworkModelSession或把Local当作fallback Model

