# gameplay-simulation-session-composition Specification

## MODIFIED Requirements

### Requirement: Target-specific Composer 必须唯一创建完整 Runtime

每个已安装 Program Runtime/Execution Backend组合 MUST通过唯一强类型 Composer集中校验并创建ProgramCatalog、compiled Pipeline plan、roster、initial state、Source ports、Kernel services、WorldSolver、Snapshot codec、Committer与diagnostics。当前Float32 Pass Backend MUST只有一个位于portable source set的正式Composer入口。Unity target adapter MUST只把五项显式Composition与Actor registration降低为一个完整portable request，并通过Prepared Source显式提供的Runtime Launcher调用该Composer。Runtime Launcher MAY增加模型专属启动约束，但 MUST不复制Runtime构造、Pipeline compile、LaunchPlan、identity或capability校验。Common Host、Unity Composer、Character Host、Preview和Demo MUST不识别具体Network Model、Prepared Source或Pipeline Definition类型。

#### Scenario: Local 与 ServerAuthoritative Prediction 共用 Float32 基座

- **WHEN** Local Pipeline与ServerAuthoritative Prediction Pipeline都选择Float32 Program Runtime和Float32 Pass Backend
- **THEN** 两者 MUST通过Standard Runtime Launcher进入同一个target-specific Composer
- **AND** 差异 MUST存在于Source和Pipeline Pass，不得存在两份Float32 Session构造器

#### Scenario: ServerAuthoritative Authority增加Host约束

- **WHEN** Authority Prepared Source提供带Source policy与locked roster的Authority Runtime Launcher
- **THEN** Launcher MUST完成模型专属启动校验后调用同一个portable Float32 Composer
- **AND** 公共Unity Composer MUST不引用Authority Prepared Source、Authority Pipeline Definition或Host launch具体类型

#### Scenario: Fantasy DotRecast Authority Scene装配Float32 Session

- **WHEN** Fantasy Server内DotRecast Authority Scene提供合法Float32 Program Runtime、Runtime Package、Source ports、Launcher、Solver与输出端口
- **THEN** MUST调用与Unity相同的模型Launcher和portable Float32 Composer
- **AND** MUST不复制Unity Composer、Pipeline compiler或LaunchPlan构造逻辑
