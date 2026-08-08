## ADDED Requirements

### Requirement: Action Playback Runtime 必须原子消费最终分支重基

Character Action Playback Runtime MUST通过唯一typed branch revision边界接收已完成外层EventId合并的最终Action playback分支。Branch revision MUST按AnimationChannel和PlaybackId/generation表达最终selection、committed sample anchor与已确认terminal，MUST不暴露replay中间命令。Runtime MUST在现有PresentationFrame Evaluate Barrier前事务内原子更新lifecycle、sample history、Slot usage、source continuity和release ownership，并且MUST不通过重置Physical Bones或整个Animation Runtime完成重基。

回滚撤销已消费的未确认Select/Sample MUST属于branch revision，MUST不转换为业务Complete/Release。Confirmed terminal提交后的同generation Sample MUST被拒绝并进入正式Faulted路径。

#### Scenario: Rollback 撤销已消费 Sample

- **WHEN** 最终branch revision移除一个已被PresentationFrame消费但未确认的Sample
- **THEN** Runtime MUST原子恢复该playback的最终sample基线
- **AND** MUST不因该撤销将generation设为terminal

#### Scenario: 最终分支恢复同 generation

- **WHEN** 一个未确认playback分支被撤销后又被最终branch revision恢复
- **THEN** Runtime MUST以同一PlaybackId/generation恢复selection与sample
- **AND** MUST不报告`Sample follows a terminal command`

#### Scenario: Confirmed terminal 后出现 Sample

- **WHEN** 同PlaybackId/generation的Complete或Release已经confirmed并提交成功
- **AND** 后续branch revision包含该generation的Sample
- **THEN** Lifecycle Registry MUST报告结构化generation、EventId、Tick和phase错误
- **AND** Actor Animation Runtime MUST进入Faulted
- **AND** MUST不吞掉异常、恢复骨骼快照或切换fallback路径
