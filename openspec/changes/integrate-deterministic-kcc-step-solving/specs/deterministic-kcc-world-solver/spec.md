## MODIFIED Requirements

### Requirement: Step Up/Down 必须作为 blocker-linked 完整候选事务

Step Up MUST只由当前 continuous cast 的真实闭合侧面 contact 触发，并使用该 blocker 的 primitive/feature、safe position、requested remaining 与 previous stable support 建立候选。Step Solver MUST先在不修改 Motor pose 的只读阶段，从 blocker 内侧执行 outer/inner 向下探测：outer MUST证明关联顶部存在，inner MUST证明顶部沿障碍内侧至少保有`MinimumStepDepth`稳定踏面，然后 MUST从 canonical landing计算真实`actualStepHeight`。只有真实高度大于零且不超过`MaximumStepHeight`时，Solver才可依次按`actualStepHeight`验证向上 clearance、受请求幅度约束的前向移动、向下贴合、blocker/landing 几何关联和最终无重叠；MUST不先把完整胶囊上抬`MaximumStepHeight`再决定候选。landing MUST与 Box blocker 为同一 primitive，或与 Triangle blocker 为自身/共享边相邻 primitive。Step Up只可消费已验证的部分平面位移，未消费 remaining MUST继续进入同一 Motor collide-and-slide，不得直接清零。旧`MinimumStepForwardDistance` MUST删除并单路迁移为`MinimumStepDepth`；普通 movement progress MUST继续使用`MinimumMovementDistance`。

Step Down MUST是独立于 Ground Snap 的候选，只允许 previous stable Grounded、当前存在最小平面进展且最终 request 没有明确向上分量时执行。Step Down MUST在 `MaximumStepHeight` 内找到稳定 landing，验证实际下降高度、连续下探路径与最终无重叠后原子提交；失败时 MUST保留常规 movement 结果并报告 Airborne，不得部分下移。Step Up/Down只影响 applied displacement、stable support与collision结果，MUST不写入或推导`VerticalVelocity`。

#### Scenario: 走上合法台阶

- **WHEN** 当前真实 blocker 为低高度侧面、outer/inner probe证明关联顶部具有`MinimumStepDepth`、按真实`actualStepHeight`检查的上方净空充足且请求允许实际前移
- **THEN** KCC MUST接受完整 Step Up candidate
- **AND** 最终 pose MUST位于关联稳定地面且无 overlap
- **AND** 未消费的平面位移 MUST继续由同一 Motor 处理

#### Scenario: 低台阶位于受限头顶空间下

- **WHEN** `MaximumStepHeight`大于可用头顶空间，但 outer/inner probe算出的`actualStepHeight`小于可用空间
- **THEN** KCC MUST只按`actualStepHeight`执行向上clearance并允许合法Step Up
- **AND** MUST不因最大配置高度本身拒绝候选

#### Scenario: 垂直墙被误判为台阶

- **WHEN** blocker 不满足低障碍资格、上抬后仍受阻或无法获得关联稳定 landing
- **THEN** KCC MUST拒绝完整 Step Up candidate
- **AND** MUST从原 safe position与原 contact set继续普通阻挡/slide

#### Scenario: 快速移动越过狭窄低障碍

- **WHEN** outer probe发现低障碍顶部，但inner probe在`MinimumStepDepth`处已离开关联顶部或回到原高度地面
- **THEN** KCC MUST拒绝 Step Up candidate
- **AND** MUST不通过清零 remaining 或跨越障碍返回成功

#### Scenario: 连续走下合法台阶

- **WHEN** previous stable Grounded角色产生平面进展、Ground Snap范围内没有落点，但在 `MaximumStepHeight` 内存在稳定下级踏面
- **THEN** KCC MUST接受完整 Step Down candidate
- **AND** MUST报告`SteppedDown`、稳定Grounded与新support identity

#### Scenario: 离开超过最大高度的平台

- **WHEN** Ground Snap范围和`MaximumStepHeight`内都没有稳定落点
- **THEN** KCC MUST拒绝 Step Down candidate并报告Airborne
- **AND** MUST不跨断崖保留previous support或执行部分向下吸附

### Requirement: Grounding 必须区分任意地面命中与稳定支持面

Ground query MUST分别输出 `FoundAnyGround` 与 `IsStableOnGround`，并记录 stable support primitive/feature、ground normal、distance 与 ledge state。稳定性 MUST考虑坡度、胶囊底部支持区域、triangle adjacency、边缘类型和 previous support；陡坡 MAY作为 collision contact，但 MUST不成为 stable ground。顶部与立面的共享边不得仅因 edge contact直接成为stable ground，也不得无条件返回`UnsupportedEdge`：当outer probe位于空侧或非稳定侧、inner probe沿顶部内侧至少`MinimumStepDepth`仍获得与blocker/landing连续的稳定支持、且角色没有离开内侧支持范围时，Grounding MUST使用inner顶部法线和support identity报告稳定step support；inner证据失效或角色朝空侧离开后 MUST取消该support。

#### Scenario: 角色移动到坡顶共享边

- **WHEN** 胶囊从一个可站立 triangle 移到相邻可站立 triangle
- **THEN** KCC MUST使用 adjacency 保持稳定 support 连续性
- **AND** grounded MUST不因 primitive id 改变而无依据闪断

#### Scenario: 胶囊跨过台阶鼻部

- **WHEN** closest feature 为顶部与立面的共享边、outer侧没有稳定支持，但inner probe在`MinimumStepDepth`内获得关联稳定顶部
- **THEN** Grounding MUST使用inner顶部法线和support identity保持稳定Grounded
- **AND** MUST记录明确ledge state而不使用共享边混合法线

#### Scenario: 角色离开悬崖

- **WHEN** 胶囊底部已没有足够稳定支持区域或inner probe证据已经失效
- **THEN** `IsStableOnGround` MUST变为 false
- **AND** MUST不跨不相邻 feature 保留 previous support

### Requirement: Ground Snap 必须受上一支持面和当前运动意图约束

Ground Snap MUST只在上一 Tick 稳定 grounded、当前没有明确向上位移、目标落点在 `GroundSnapDistance` 内且为稳定地面时执行。Snap path MUST经过连续查询，MUST不穿过陡坡、断崖或其它阻挡。Ground Snap MUST只表达微小连续贴地，不得使用 `MaximumStepHeight` 扩大范围，不得输出`SteppedDown`，也不得替代独立 Step Down candidate。

#### Scenario: 连续下坡

- **WHEN** 角色上一 Tick 稳定 grounded 且下坡落点在 `GroundSnapDistance` 内
- **THEN** KCC MAY向稳定落点 snap
- **AND** MUST更新新的 support identity
- **AND** MUST不把该结果标记为`SteppedDown`

#### Scenario: 向上攻击位移

- **WHEN** MotionRequest 包含明确向上位移
- **THEN** KCC MUST不执行 Ground Snap或Step Down

#### Scenario: Actor接触修正后重新施加静态约束

- **WHEN** Actor接触批处理修改了角色候选位置并要求再次执行静态去穿透与Ground query
- **THEN** 静态重约束 MUST复用原Motor movement依据上一稳定支持面和当前请求Y确定的Ground probe资格与距离
- **AND** MUST不把初始化放置使用的完整`GroundSnapDistance`或`MaximumStepHeight`无条件应用到Airborne或明确向上移动的角色
