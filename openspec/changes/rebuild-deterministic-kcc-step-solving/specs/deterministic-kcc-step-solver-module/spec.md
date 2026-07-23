## ADDED Requirements

### Requirement: Step Solver 模块必须只返回确定性原子候选

`DeterministicKccStepSolver` MUST只消费调用方提供的 Fixed capsule query contract、canonical contact、previous stable support、requested displacement 与 immutable Step policy。模块 MUST不拥有 world state，不读取 Graph、Action、Input、Network、Presentation 或 Unity 对象，不注册 WorldSolver、Composition 或 Session。模块 MUST在验证完成前保持输入 pose、remaining、support、constraint plane 和 snapshot 不变；成功时只可返回完整 `StepCandidate`，失败时只可返回唯一 `StepRejection`，不得返回部分提交状态。

#### Scenario: Step Up 中途验证失败

- **WHEN** 顶部、净空、前移、landing 关联、稳定性或最终 overlap 任一阶段失败
- **THEN** 模块 MUST返回唯一 Step Up rejection
- **AND** MUST不修改调用方 pose、remaining 或 stable support

#### Scenario: Step Down 中途验证失败

- **WHEN** 下级 landing 超高、非稳定、路径受阻或最终 pose 无效
- **THEN** 模块 MUST返回唯一 Step Down rejection
- **AND** MUST不返回部分下移位置

### Requirement: Step Up 模块必须绑定真实 blocker 并按真实高度验证

Step Up MUST只由 canonical movement contact set 中真实阻挡当前平面位移的闭合侧面 contact 触发。模块 MUST从 blocker 内侧执行 outer/inner 向下探测：outer MUST证明关联顶部存在，inner MUST证明顶部沿内侧至少保有 `MinimumStepDepth` 稳定踏面。模块 MUST从 canonical landing 计算 `actualStepHeight`，并且只按真实高度验证向上 clearance、受请求幅度约束的前移、向下贴合、blocker/landing 关联和最终无重叠。Box landing MUST与 blocker primitive 相同；Triangle landing MUST为 blocker 自身或共享边相邻 primitive。成功 candidate MUST返回实际消费的平面位移和未消费 remaining。

#### Scenario: 合法低台阶位于受限头顶空间下

- **WHEN** outer/inner probe证明关联顶部有效且 `actualStepHeight` 小于可用净空
- **THEN** 模块 MUST只按 `actualStepHeight` 验证向上路径
- **AND** MUST不因 `MaximumStepHeight` 大于净空而拒绝候选

#### Scenario: 狭窄障碍没有最小踏面深度

- **WHEN** outer probe发现顶部但 inner probe 在 `MinimumStepDepth` 处离开关联顶部
- **THEN** 模块 MUST拒绝 Step Up
- **AND** MUST不通过跨过障碍后的原高度地面构造 landing

#### Scenario: Step Up 只消费部分请求

- **WHEN** 合法 candidate 只需要消费当前 remaining 的一部分平面位移
- **THEN** candidate MUST分别返回 consumed planar displacement 和 remaining displacement
- **AND** accepted movement MUST不超过请求幅度

### Requirement: Step Support Evaluator 必须用内外证据判断台阶鼻部

Step Support Evaluator MUST区分普通稳定 face、双稳定 seam、台阶顶部/立面共享边和 unsupported ledge。只有 outer 位于空侧或非稳定侧、inner 在 `MinimumStepDepth` 内获得关联稳定顶部、previous support 与 movement direction 仍允许保持内侧支持时，evaluator 才可使用 inner 顶部法线和 support identity 返回 stable step support。角色朝空侧离开、inner 证据失效或 primitive 关联断开后 MUST取消该支持。evaluator MUST只返回 ground report，不修改当前 Grounding。

#### Scenario: 胶囊位于台阶鼻部

- **WHEN** outer 非稳定且 inner 获得关联稳定顶部
- **THEN** evaluator MUST返回 inner 顶部法线与 support identity
- **AND** MUST保留明确 ledge state

#### Scenario: 角色离开内侧支持范围

- **WHEN** 角色朝空侧移动且 inner 支持证据失效
- **THEN** evaluator MUST返回非稳定支持
- **AND** MUST不跨不相邻 feature 保留 previous support

### Requirement: Step Down 模块必须独立于 Ground Snap

Step Down MUST只在 previous stable Grounded、当前存在最小平面进展、Ground Snap 未成功且最终 request 没有明确向上分量时建立候选。模块 MUST在 `MaximumStepHeight` 内寻找稳定 landing，要求实际下降超过 `GroundSnapDistance` 的微小贴地范围，并验证连续下探路径、台阶边缘支持和最终无重叠。成功时 MUST返回完整 Step Down candidate；失败时 MUST只返回 rejection。模块 MUST不扩大 Ground Snap，也 MUST不写入或推导 `VerticalVelocity`。

#### Scenario: Snap 范围外存在合法下级踏面

- **WHEN** Ground Snap 未成功且 `MaximumStepHeight` 内存在稳定下级踏面
- **THEN** 模块 MUST返回完整 Step Down candidate

#### Scenario: 平台落差超过最大台阶高度

- **WHEN** `MaximumStepHeight` 内没有稳定 landing
- **THEN** 模块 MUST拒绝 Step Down
- **AND** MUST不保留 previous support或返回部分向下吸附

### Requirement: 隔离模块不得提前成为当前运行时

本 change 的实现 MUST位于隔离分支或独立工作树，MUST不单独合入当前闭环分支或单独 archive。实现 MUST不替换当前 `DeterministicKccMotor` 调用点，不修改 `DeterministicKccWorldSolver`、Solver descriptor、Composition、Session、正式 Solver Definition、KCC asset、Motor/Solver version、KCC identity 或 snapshot。运行时开关、第二个 KCC、第二份配置、兼容字段和 fallback MUST不存在。只有 `integrate-deterministic-kcc-step-solving` 获得用户放行并执行原子切换后，该模块才可成为当前运行时。

#### Scenario: 并行模块实现完成

- **WHEN** Step Solver 模块在隔离工作树完成
- **THEN** 当前闭环分支和已安装 KCC 行为 MUST保持不变
- **AND** 本 change MUST保持未合并、未归档，等待接入 change
