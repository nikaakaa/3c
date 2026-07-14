# gameplay-sync-runtime Specification

## Purpose
定义通用 `GameplaySyncRuntime`：它管理 gameplay sync packet、peer、tick、history 和 debug，作为 Character、Objective、PvE 或后续 backend 的同步边界，不直接 tick Graph、调用角色 runtime 或修改 Unity Transform。
## Requirements
### Requirement: Common Network Session 必须只管理模型生命周期

系统 MUST 提供 model-neutral Session composition boundary，用于持有唯一 model definition、创建 model session 并管理 dispose。该 boundary MUST NOT 定义 packet kind、history 内容、prediction、correction 或 snapshot 语义。

#### Scenario: 创建当前模型

- **WHEN** SessionHost 读取 ServerAuthoritative model definition
- **THEN** 它 MUST 创建对应 model session
- **AND** common host MUST 不读取模型 packet

### Requirement: 同步 Runtime、Packet、History 和 Debug 必须声明模型归属

任何管理 gameplay 网络 packet、history、queue 和 debug 的 runtime MUST 属于一个明确 Network Model。系统 MUST NOT 再用无模型限定的 `GameplaySync*` 类型承载 ServerAuthoritative 专用语义。

#### Scenario: 搜索通用类型

- **WHEN** 实现完成后搜索正式运行时代码
- **THEN** `GameplaySyncRuntime`、`GameplaySyncPacket`、`IGameplaySyncPeer` 和通用 GameplaySyncHistory MUST 为零定义
- **AND** 对应能力 MUST 只存在于 ServerAuthoritative model 模块

