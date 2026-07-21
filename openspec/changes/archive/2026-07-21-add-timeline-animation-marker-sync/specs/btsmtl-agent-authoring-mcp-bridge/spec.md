## ADDED Requirements

### Requirement: MCP bridge 必须透传同一 v14 Marker 与 Curve 事务

BTSMTL Agent MCP bridge MUST接受并返回`agent-character-controller-synthesis.v14` Snapshot、Patch与Validation结果，并允许generic Patch事务携带configure AnimationTrack Marker Sync、SyncRole、ensure/move/delete marker以及configure registered Timeline Curve Channel typed operation。Bridge MUST只调用正式Agent Snapshot、lowerer、dry-run、apply和validator入口，不得新增SerializedProperty、YAML、反射、任意字段写入或旧v13转换工具。Timeline UI完善 MUST复用同一authoring service，不得要求Bridge新增Marker或Curve专用action。

#### Scenario: 通过bridge配置循环组

- **WHEN** 调用方通过MCP bridge提交合法v14 Patch配置WalkLoop与RunLoop的Cyclic Marker Group与CanBeLeader角色
- **THEN** bridge MUST先返回正式dry-run command plan与validation结果
- **AND** apply MUST由同一typed plan执行
- **AND** bridge MUST返回更新后的stable identities与group摘要

#### Scenario: 通过bridge配置有限序列

- **WHEN** v14 Patch为Finite AnimationTrack提交frame 0到DurationFrame的marker序列与同步角色
- **THEN** bridge MUST保留重复MarkerId occurrence的独立AuthoringId
- **AND** MUST返回call site Once与directed pair coverage结果

#### Scenario: bridge收到非法marker事务

- **WHEN** Patch包含重复AuthoringId、非法frame、Once/Loop冲突或group pair缺口
- **THEN** bridge MUST返回正式Agent validation code、path与相关identity
- **AND** MUST不绕过validator直接写Unity资产

#### Scenario: 通过bridge修改Curve Channel

- **WHEN** 调用方通过generic Patch提交registered ChannelId与完整AnimationCurve payload
- **THEN** bridge MUST原样透传owner identity、domain、wrap mode和完整Keyframe字段
- **AND** lowerer与handler MUST调用同一Catalog descriptor和owner MutationAdapter
- **AND** bridge MUST不按字段名寻找AnimationCurve

#### Scenario: bridge收到v10请求

- **WHEN** 调用方提交v10 Snapshot或Patch
- **THEN** bridge MUST返回unsupported schema错误
- **AND** MUST不转换为v14或调用旧reader
