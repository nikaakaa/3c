## MODIFIED Requirements

### Requirement: Character 输入来源与运动权威必须正交

Actor control input MUST由当前Source与Ingress/Schedule Pass产生；world constraint与body result MUST由Session装配的唯一WorldSolver和正式WorldSolve Pass产生。Program与Character state MUST不使用authority总控枚举或具体Network Model分支。Network Model Schedule MAY把权威观察到的非Program Actor轨迹编译为model-neutral、tick-bound World constraint，但 MUST不自行求解接触、不提交Body，也 MUST不让Packet、Endpoint或Presentation Transform进入Solver。后续模型 MAY为不同Program Actor提供不同input/ingress，但同一SimulationStep的world mutation仍必须经过统一batch Solver。

#### Scenario: Local Session Owner

- **WHEN** Local Session创建Corin且没有外部观察Actor
- **THEN** Local Source与Local Input Pass MUST提供设备input
- **AND** Step MUST携带正式空观察frame
- **AND** Unity WorldSolver与WorldSolve Pass MUST提供body result

#### Scenario: ServerAuthoritative Prediction观察远端Actor

- **WHEN** Model Source拥有远端Actor的权威Body timeline但没有其canonical input
- **THEN** 声明观察接触能力的Schedule MAY产生ObservedKinematic World constraint
- **AND** 唯一WorldSolver MUST只为本地Program actor提交FinalBody
- **AND** MUST不把远端Actor伪装成CharacterPipeline RemoteProxy或第二Program actor

#### Scenario: Model拥有远端canonical input

- **WHEN** 另一个Network Model为远端Actor提供正式canonical input与typed ingress
- **THEN** 该Actor MAY通过完整roster进入Program执行
- **AND** MUST不与ObservedKinematic约束使用同一ActorId双重注册

## ADDED Requirements

### Requirement: 观察World约束必须是Step级正式输入

Float32 Simulation Step MUST显式携带按tick绑定、按ActorId稳定排序的`ObservedWorldConstraintFrame`。该frame MUST进入World request canonical bytes与RequestHash，并 MUST验证与active roster不重复。每个observed参与者 MUST携带Solver锁定接触形状的configuration hash；具体形状数据 MUST继续由WorldSolver configuration拥有。空frame MUST是带tick的正式值；系统 MUST不使用`null`、隐藏Source状态、MonoBehaviour集合或Presentation缓存表示约束缺失。

#### Scenario: Pipeline构造无观察Actor的Authority step

- **WHEN** Authority完整roster已经由active Character requests表达
- **THEN** Schedule MUST显式提供空观察frame
- **AND** WorldSolve Pass MUST不从Network Model类型猜测约束

#### Scenario: Pipeline构造带观察Actor的Prediction step

- **WHEN** Schedule为Actor B选择了合法远端Body轨迹且Composition声明观察接触能力
- **THEN** 观察frame MUST随该step进入唯一World batch和request hash
- **AND** Replay MUST能从History恢复同一frame
