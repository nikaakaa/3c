## ADDED Requirements

### Requirement: Scoped 快照比较结果
本地 synctest snapshot comparison MUST 支持 scoped comparison result。结果 MUST 至少包含 strict gameplay differences 和 presentation differences 两组字段；`Matches` 或等价 success 判定 MUST 只由 strict gameplay differences 决定。

#### Scenario: 只有表现漂移时通过
- **GIVEN** replay 后没有 strict gameplay differences
- **AND** 存在 animation normalized time presentation drift
- **WHEN** synctest runner 生成结果
- **THEN** result MUST 为成功
- **AND** result MUST 保留 presentation differences

#### Scenario: Strict 差异时失败
- **GIVEN** replay 后存在 position、yaw、状态机或 motion executor strict 差异
- **WHEN** synctest runner 生成结果
- **THEN** result MUST 为失败
- **AND** failure reason MUST 不被 presentation drift 覆盖

#### Scenario: First drift 不覆盖 first mismatch
- **GIVEN** replay 区间内先出现 presentation drift，后出现 strict mismatch
- **WHEN** runner 记录 first difference
- **THEN** strict mismatch MUST 作为失败依据
- **AND** presentation drift MAY 作为辅助诊断保留

### Requirement: Scoped F6/F8 诊断日志
本地 F6/F8 诊断日志 MUST 明确输出 strict differences 与 presentation differences。只有 presentation drift 时，日志 MAY 输出 PASS 但 MUST 附带 drift 字段；strict mismatch 时日志 MUST 输出 FAIL 和 strict differences。

#### Scenario: F6 输出 presentation drift
- **GIVEN** F6 replay 只有视觉动画 drift
- **WHEN** debug runner 输出结果
- **THEN** Console MUST 包含 `presentationDifferences` 或等价字段
- **AND** MUST NOT 输出 strict failure

#### Scenario: F6 输出 strict failure
- **GIVEN** F6 replay 存在 gameplay mismatch
- **WHEN** debug runner 输出结果
- **THEN** Console MUST 包含 strict differences
- **AND** MUST 标记 `[rollback-synctest] FAIL`

#### Scenario: F8 汇总 drift
- **GIVEN** F8 soak 的某些窗口只有 presentation drift
- **WHEN** soak 输出结果
- **THEN** 输出 MUST 能诊断 drift 窗口
- **AND** MUST NOT 将其计入 strict failure

### Requirement: Scope Resolver 可测试
本地 synctest 的字段分类 MUST 通过可测试的 resolver、policy 或等价纯数据表完成。Resolver MUST 能解释当前字段属于 strict gameplay、presentation drift、predictive gameplay 或 ignored，并且 MUST 支持后续状态/动画配置扩展。

#### Scenario: TurnBack 字段归类 strict
- **WHEN** resolver 收到 TurnBack profile-driven playback progress 字段
- **THEN** resolver MUST 返回 strict gameplay scope

#### Scenario: MoveLoop 字段归类 presentation
- **WHEN** resolver 收到 MoveLoop 视觉 playback normalized time 字段
- **THEN** resolver MUST 返回 presentation drift scope

#### Scenario: Resolver 不依赖表现层对象
- **WHEN** 检查 resolver 模型
- **THEN** resolver MUST NOT 保存 Animancer state、Animator、AnimationClip、TransitionAsset 或 Unity 场景对象引用
