## ADDED Requirements

### Requirement: Timeline 动画表现修正必须按 Clip 选择

作者 MUST能够为每个 Animation Clip 明确选择跟随修正后的逻辑进度，或让仍有效的同次播放保持本地连续进度。同一 Track 中不同 Clip MUST可以使用不同策略，Track 的通道、混合和重叠规则 MUST保持有效。保持连续播放的 Clip MUST不因同次执行重算而倒退或从头重播；跟随逻辑进度的 Clip MUST按修正后的采样时间更新。策略 MUST通过同一动画表现管线执行，不得增加独立播放器或网络恢复链。

两种策略 MUST服从最终有效动作分支和既有停止、分支撤销及确认终态规则。Skill、Timeline、TreeClip、伤害窗口和位移等影响未来模拟的状态 MUST继续按角色既有管线恢复，MUST不受动画表现策略影响。网络协议 MUST继续不传递 AnimationClip、播放头或最终 Pose。

#### Scenario: 同一轨道使用不同表现策略

- **WHEN** 同一 Track 上两个仍有效的动画 Clip 分别选择跟随逻辑进度和保持连续播放，网络修正改变其逻辑采样时间
- **THEN** 前者 MUST消费修正后的时间，后者 MUST保持本地连续播放
- **AND** MUST不强制整条 Track 使用同一种策略

#### Scenario: 保持连续播放的来源被撤销

- **WHEN** 最终动作分支不再包含已经表现的某次 Clip 播放
- **THEN** 表现 MUST通过既有分支撤销及过渡规则退出该来源
- **AND** MUST不因连续播放策略保留失效来源或伪造业务 Release

#### Scenario: 保持连续播放的动画对应技能发生恢复

- **WHEN** 角色恢复导致技能阶段、伤害窗口或位移结果改变，而某段动画选择保持连续播放
- **THEN** 模拟状态 MUST完整恢复并采用修正结果
- **AND** 动画的本地播放进度 MUST不成为伤害或位移真值

#### Scenario: 已确认终态再次收到旧样本

- **WHEN** 某次播放已经进入正式确认终态，随后到达该次执行的旧样本
- **THEN** 表现 MUST继续按既有规则拒绝旧样本，不能因连续播放策略重新激活

### Requirement: Clip 表现策略必须按本次播放区分状态

表现消费 MUST区分稳定的 Timeline、Track、Clip 来源与本次调用、激活和循环身份。共享同一 Track 或 Clip 资源的不同执行 MUST具有独立的表现策略状态；只保留 Track 标识或 Clip 资源标识不得作为完整执行身份。上述身份 MUST来自既有正式调用和输出合同，MUST不通过诊断数据或网络新增的动画资源消息建立。

#### Scenario: 同一个 Clip 被两次技能释放使用

- **WHEN** 同一角色的两次技能释放使用同一 Clip
- **THEN** 两次播放 MUST分别处理进度修正和停止，不能按资源合并本地播放状态

#### Scenario: 循环中的旧停止到达新播放

- **WHEN** 同一 Clip 的旧循环或旧激活停止信息到达，而新一次播放已经开始
- **THEN** 停止 MUST只作用于它所属的那次播放，不能撤销新播放
