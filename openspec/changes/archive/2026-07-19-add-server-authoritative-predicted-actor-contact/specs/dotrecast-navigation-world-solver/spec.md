## ADDED Requirements

### Requirement: DotRecast Actor接触必须区分Active与Observed参与者

`ActorContactSolver` MUST在同一candidate集合中显式区分`ActiveSimulated`与`ObservedKinematic` mobility。Active/Active MUST保持对称连续扫掠、闭合法向裁剪、切向保留和有界去穿透；Active/Observed MUST使用双方前后位置计算相对轨迹与TOI，只允许修改Active一侧；Observed/Observed MUST不产生可提交修正。Mobility MUST只表达本次World batch的可改写性，MUST不表达Gameplay priority、阵营、霸体、攻击或网络权威枚举。Observed candidate MUST引用Solver锁定canonical contact shape的configuration hash；Solver MUST验证一致后使用自己的Radius、Height与SkinWidth，MUST不从网络Body或默认值构造第二份形状。

#### Scenario: 本地owner撞向静止远端观察体

- **WHEN** Active owner的candidate轨迹与Observed remote Body相交
- **THEN** Solver MUST裁剪owner的闭合法向位移并保留合法切向位移
- **AND** MUST不移动Observed Body

#### Scenario: 远端观察体沿轨迹靠近owner

- **WHEN** Observed remote的前后Body轨迹主动闭合到Active owner
- **THEN** Solver MUST使用相对轨迹计算接触
- **AND** 需要分离时 MUST只修正Active owner

#### Scenario: Observed接触形状身份不一致

- **WHEN** 观察frame声明的contact shape hash与Prediction Solver锁定shape不一致
- **THEN** World batch MUST在接触求解前失败
- **AND** MUST不使用owner shape、网络字段或默认半径继续求解

### Requirement: DotRecastWorldSolver必须只提交Active参与者

`DotRecastWorldSolver.ResolveBatch` MUST对active requests生成Navigation Surface candidate，对observed constraints读取已选择Body轨迹，将两者按ActorId合并后调用唯一`ActorContactSolver`。接触后 MUST只对active位置执行Surface reconstraint，只为active request生成FinalBody、WorldSolveResult与NextWorldState；Observed参与者 MUST不进入committed World state。任一active/observed pair在固定迭代后不能同时满足Surface与最小间距时整个batch MUST失败。

#### Scenario: Observed约束参与Prediction batch

- **WHEN** World batch包含一个active owner request与一个observed remote constraint
- **THEN** 接触计算 MUST同时看到双方轨迹
- **AND** 结果roster与NextWorldState MUST仍只包含active owner

#### Scenario: Authority完整roster batch

- **WHEN** Authority以两个active Program actor执行同一World batch
- **THEN** 两个Actor MUST继续都产生FinalBody与WorldSolveResult
- **AND** Observed合同 MUST不改变Authority对称求解

### Requirement: Observed Actor接触必须通过World Feature锁定

支持ObservedKinematic约束的Solver MUST声明`ActorCollision`与`ObservedKinematicActorContact` World Feature，并将feature、Solver version和观察约束codec identity纳入组合兼容性。需要该能力的Prediction Composition MUST在Session Active前显式要求并验证；不支持该合同的Solver MUST不得忽略观察frame或伪装成功。

#### Scenario: Prediction选择不支持观察接触的Solver

- **WHEN** Composition要求ObservedKinematicActorContact但Solver未声明该feature
- **THEN** Session preparation MUST失败并报告缺失feature
- **AND** MUST不退化为只有静态Surface的预测
