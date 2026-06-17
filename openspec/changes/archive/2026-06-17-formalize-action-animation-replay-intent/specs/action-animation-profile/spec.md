## ADDED Requirements

### Requirement: Action 动画播放意图身份
系统 MUST 区分 Action 动画稳定语义 key 与 Action 动画播放意图身份。`ActionAnimationKey` 只表达要解析和播放的动作动画语义；播放意图身份 MUST 表达当前请求属于哪一次 Action 播放实例。Action 动画 Presenter MUST 使用播放意图身份决定是否复用当前播放段或重新播放，不得只凭相同 key 判断为同一次播放。

#### Scenario: 连续同 key Dodge 重播
- **GIVEN** 第一段 accepted Dodge 已经播放 `Action.Dodge.Directional`
- **AND** 第二段 accepted Dodge 也解析为 `Action.Dodge.Directional`
- **WHEN** 第二段 Dodge 的播放意图身份不同于第一段
- **THEN** Presenter MUST 将第二段 Dodge 视为新的播放意图
- **AND** MUST 重新播放该 key 对应动画
- **AND** MUST 将 Action 动画 normalized time 重置到新播放段起点

#### Scenario: 同一播放意图重复提交不重启
- **GIVEN** 当前 Action 动画 key 为 `Action.Dodge.Directional`
- **AND** 当前播放意图身份为 `A`
- **WHEN** 后续帧再次提交相同 key 和相同播放意图身份 `A`
- **THEN** Presenter MUST 保持当前播放段
- **AND** MUST NOT 每帧重新播放或重置 normalized time

#### Scenario: Restore 后同一播放意图不重启
- **GIVEN** Action 动画播放进度从 restore state 恢复到 key `Action.Dodge.Directional` 和播放意图身份 `A`
- **WHEN** 恢复后的同一次 Action 再次提交相同 key 和播放意图身份 `A`
- **THEN** Presenter MUST 保持恢复后的播放进度
- **AND** MUST NOT 把该请求当作新的 Dodge 播放段归零

#### Scenario: Presenter 不生成业务身份
- **WHEN** Presenter 接收 Action 动画播放请求
- **THEN** 播放意图身份 MUST 来自 Action 生命周期、状态机输出或等价纯数据上游
- **AND** Presenter MUST NOT 调用 Action 仲裁、读取输入缓冲或检查 Dodge 配置来生成播放意图身份

### Requirement: Action 动画重播保持配置边界
Action 动画重播语义 MUST 不改变动作动画 Profile 的配置职责。Profile 继续只负责将稳定 action animation key 解析为具体动画表现资源；播放意图身份 MUST NOT 写入 Profile entry，也不得要求设计者为连续 Dodge 配置第二份动画 key 或第二条播放路径。

#### Scenario: Profile 不复制连续 Dodge key
- **GIVEN** Profile 中存在 `Action.Dodge.Directional`
- **WHEN** 玩家连续两次进入 Directional Dodge
- **THEN** 系统 MUST 复用同一个稳定 key 解析动画资源
- **AND** MUST 通过不同播放意图身份触发第二次播放
- **AND** MUST NOT 要求新增 `Action.Dodge.Directional.2` 或等价重复 key

#### Scenario: 不新增 fallback 播放配置
- **WHEN** Action 动画播放意图身份缺失或无效
- **THEN** 系统 MUST 通过正式错误、拒绝播放或测试失败暴露问题
- **AND** MUST NOT 自动查找备用 Profile、备用 Presenter 或代码内置动画 key 继续运行
