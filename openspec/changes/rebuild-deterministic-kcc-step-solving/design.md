## Context

当前楼梯链路位于单一 `DeterministicKccMotor.Move` 内：

```text
requested displacement
    -> stable ground projection
    -> continuous cast
    -> side contact
    -> old TryStep(position, remaining)
    -> collide-and-slide
    -> Ground Snap
    -> compare final Y and label SteppedDown
```

查询层已经能提供 canonical `PrimitiveId`、`FeatureId`、TOI、normal、character/world witness、separation 和 stable ground report。问题不是缺少几何事实，而是 Step 策略没有消费这些事实。

旧 Step Up 从 movement 起点抬高到最大高度并使用整个 planar remaining。它不知道哪一个 contact 触发候选，也不要求 landing 与 blocker 属于同一个 Box 或相邻 triangle。旧 Step Down 不存在，普通 Snap 成功后才按最终 Y 变化写诊断。因此需要替换 Step 策略，而不需要再造查询内核。

本 change 只设计和实现替代模块，不执行替换。当前 Motor 链路在用户收口其它管线期间保持原样；替代模块只存在于隔离分支或工作树中，不能单独进入当前闭环分支。

## Goals

- 规则楼梯上下行都保持稳定 Grounded。
- 低矮可通行不连续面由 KCC 自动处理，不要求业务输入或场景标签。
- 竖墙、薄栏、尖角、陡坡、无顶面障碍和断崖不能被当作台阶。
- 低台阶只按真实高度检查头顶空间，不因 `MaximumStepHeight` 大于实际高度而被过度拒绝。
- 胶囊跨越台阶鼻部时使用可证明的内侧踏面保持稳定 support，不在 Grounded/Airborne 间抖动。
- Step 只消费请求允许的平面位移，不瞬移到完整请求之外。
- Step 失败时不部分提交上抬、前探或下探结果。
- 相同 Fixed 输入、artifact、配置和 previous support 产生相同 candidate、拒绝原因和结果。

## Architecture

```text
StepSolveRequest
    |
    +-- canonical contacts
    +-- previous support
    +-- requested remaining
    +-- immutable Step policy
    |
    v
DeterministicKccStepSolver
    +-- SelectBlockingContact
    +-- ProbeOuterAndInnerLanding
    +-- ValidateActualStepPath
    +-- TryStepUp
    +-- TryStepDown
    +-- EvaluateStepEdgeSupport
    |
    v
StepCandidate / StepRejection
```

`DeterministicKccStepSolver` 是待接入 Motor 的唯一台阶策略模块。它不拥有 world state，不读取 Graph、Action、Input、Network、Presentation 或 Unity 对象；它只通过调用方提供的 query contract 读取 canonical collision artifact。候选成功前不修改 pose、remaining、constraint planes 或 committed support，成功后也只返回 candidate。

## Parallel Implementation Boundary

```text
当前闭环分支
    current Motor + old TryStep
    状态：保持不变

隔离 KCC 工作树
    new Step Solver module
    状态：允许实现，不接 Motor，不合并

用户放行
    -> integrate-deterministic-kcc-step-solving
    -> 一次性接入、迁移、删除旧链路
```

这里的“并行”是 Git 交付并行，不是运行时并行。隔离工作树可以暂时存在尚未被 Motor 引用的替代类型，因为它不会进入当前分支；当前分支不能出现新旧 Solver 共存、运行时选择或未使用模块。若其它闭环修改了 query、contact、ground report 或 configuration contract，接入前必须先变基并重新核对输入，不能增加 adapter 或兼容字段绕过冲突。

## Decisions

### 1. Step Up 必须绑定当前真实 blocker

Motor 在 continuous cast 到达最早 TOI 后，把该次 canonical contact set 交给 Step Solver。候选 contact 必须同时满足：

- previous state 是稳定 Grounded；
- requested planar movement 达到 `MinimumMovementDistance`；
- contact 是阻挡当前平面位移的闭合侧面；
- contact 不是稳定 ground contact；
- contact witness 位于胶囊可形成低障碍候选的下部范围；
- 多个 contact 同时成立时按现有 canonical identity 选择，不依赖遍历偶然顺序。

业务收益：只有角色这次真正撞到的低高度几何才能触发 Step，不会在附近另一个表面上凭空寻找落点。

代价：台阶表面必须由 artifact 的 stable primitive/feature 和 triangle adjacency 正确表达；无关联的重叠几何会被保守拒绝。

### 2. Step Up 先发现真实顶部，再验证实际运动路径

旧实现先把完整胶囊上抬 `MaximumStepHeight`，会把最大允许高度错误当成本次必要高度。新实现把只读几何发现与身体移动验证拆开。

第一阶段不修改 Motor position：

1. 从 blocker normal 的平面投影得到障碍内侧方向。
2. 在 blocker 上方 `MaximumStepHeight` 范围建立 outer probe，向下寻找刚越过立面的稳定顶部。
3. 沿内侧方向再偏移 `MinimumStepDepth` 建立 inner probe，确认顶部不是尖角、细栏或立即回落到原高度的窄障碍。
4. outer 与 inner landing 必须属于同一个 Box primitive，或者属于 blocker triangle 自身/共享边相邻 triangle形成的同一连续顶部。
5. 从 outer/inner 的 canonical stable landing 计算 `actualStepHeight`，要求它真实大于零且不超过 `MaximumStepHeight`。

第二阶段才从本次 cast 的 safe position验证身体路径：

1. 向上 cast `actualStepHeight`，只检查本次真正需要的头顶净空。
2. 取当前 remaining 的平面方向和幅度，计算不超过请求位移的实际前移；geometry probe MAY检查请求终点之外的最小踏面深度，但 accepted movement MUST不超过请求幅度。
3. 在实际抬高位置执行前向 cast，任何阻挡都拒绝候选。
4. 向下贴合已验证的 outer landing高度并建立 step support report。
5. 最终 capsule 必须通过 overlap；outer/inner support 证据和最终 support identity 必须一致。

业务收益：0.1m 台阶只需要 0.1m 的真实上抬空间；角色不会因为配置允许 0.3m 台阶就额外要求 0.3m 头顶空间，也不会跨过细栏后落回原地面。

代价：Step Up 至少增加 outer、inner、actual rise clearance、forward 和 landing reconstraint 查询；只在真实 blocker 出现时执行，并继续受固定 query/movement budget约束。

### 3. 台阶鼻部使用 inner/outer support 证据

胶囊刚接触台阶顶部时，closest feature 通常是顶部与立面的共享边。只按 edge adjacency 判断会把它标成 `UnsupportedEdge`，导致刚完成 Step Up 的角色下一 Tick 又失去 Grounded。

Grounding 与 Step Solver 共用以下判定：

- outer probe 表示靠近空侧或立面侧的边缘位置；
- inner probe 沿顶部内侧进入至少 `MinimumStepDepth`；
- inner landing 必须稳定、与 blocker/outer landing 连续；
- outer 为空或非稳定而 inner 稳定时，报告明确 ledge，但可使用 inner 顶部法线作为当前 step support；
- 角色离开内侧支持范围、朝空侧移动或 inner probe失效后，必须取消 stable support。

业务收益：跨过台阶鼻部的几个 Tick 保持同一个顶部 support语义，动画和实际速度不会在 Grounded/Airborne 间抖动。

代价：Grounding 会增加只在 edge/step feature 出现时执行的 secondary probes；普通平面 face contact不付出该成本。

### 4. Step Up 只消费实际使用的位移

候选接受后，Motor 提交已验证的上抬、前探和落地位置，但不会把整段 remaining 直接清零。Step Solver 返回：

```text
accepted position
consumed planar displacement
remaining planar displacement
stable landing report
step diagnostics
```

Motor 从新稳定位置继续同一个 collide-and-slide 循环处理 remaining。若后续又撞墙、到达另一个台阶或进入内角，仍由同一查询和约束循环决定最终结果。

业务收益：高速移动不会因为一次上台阶吞掉输入，也不会越过台阶后的墙体。

代价：一个 Tick 可能产生更多 bounded query iteration；达到现有固定预算仍按正式 non-convergence 失败，不动态扩容。

### 5. Step Down 是独立候选，不是较大的 Ground Snap

常规 movement 和 `GroundSnapDistance` 内 Ground Snap 都未得到稳定支撑后，Motor 才尝试 Step Down。资格固定为：

- previous state 稳定 Grounded；
- 当前 Tick 有超过 `MinimumMovementDistance` 的平面进展；
- 最终 request 没有明确向上分量；
- 从当前候选位置向下不超过 `MaximumStepHeight` 能找到稳定 landing；
- 实际下降大于 `GroundSnapDistance` 的微小贴地范围；
- 下探路径、最终 pose 和 stable support 全部有效。

接受后提交 `SteppedDown`；拒绝后保持 Airborne，由现有 VerticalVelocity 和后续 KCC movement 处理下落。

业务收益：0.14m、0.24m 的规则楼梯可以连续下行，但超过最大高度的断崖不会被吸住。

代价：KCC 无法知道“小落差是楼梯还是平台边缘”。在相同几何条件下，小于等于 `MaximumStepHeight` 的可站立落差都会被视为可连续下行，这是角色通行规则，不是场景业务分类。

### 6. Ground Snap 只保留微小连续贴地职责

Ground Snap 继续要求 previous stable support、非向上 request、稳定落点和 `GroundSnapDistance`。它用于坡面量化残差、小缝隙和微小落差，不输出 `SteppedDown`。

业务收益：Snap 参数继续表达贴地手感，不再同时决定楼梯最大下行高度。

代价：Step Down 会增加一次只在 Snap 失败且满足资格时发生的向下 query。

### 7. 上下台阶共用 MaximumStepHeight，并显式输入 MinimumStepDepth

模块合同只保留一个 `MaximumStepHeight`，同时约束上升和下降，并把 `MinimumStepDepth` 作为明确的策略输入，唯一控制 outer 到 inner probe 所要求的稳定顶部深度。普通位移是否足够继续求解使用 `MinimumMovementDistance`。现有运行时字段 `MinimumStepForwardDistance` 的破坏性迁移不在本 change 执行，由接入 change 原子完成。

业务收益：模块从一开始只有准确语义，不为了适配当前旧字段污染新接口；细栏和尖角不会因为前进了很小距离就被当成台阶。

代价：模块在接入前不能从正式资产直接构造；接入 change 必须同时完成字段迁移和 identity 升级，不提供旧字段 reader 或默认兼容值。

### 8. Step 失败完全回到普通阻挡或 Airborne

Step Up 任一验证失败时，Motor 使用原始 contact set 继续普通多平面 slide；不保留抬高或前探位置。Step Down 失败时保留常规 movement 结果并报告 Airborne；不部分下移。

业务收益：失败结果可预测，不会出现半卡进台阶、瞬间抬高或穿过薄墙。

### 9. 诊断记录真实候选事实

Step diagnostics 扩充为：

```text
phase
rejection
blocker primitive/feature
landing primitive/feature
height delta
consumed forward displacement
query summary
```

这些字段只进入 diagnostics，不进入 snapshot/hash。影响结果的算法版本、现有配置和派生规则通过 Motor semantic version 与 KCC identity 锁定。

## Alternatives And Tradeoffs

### 给楼梯增加隐藏坡面 Collider

优点：移动最平滑，普通 Grounding 即可工作。

代价：可见几何与逻辑碰撞分裂，角色脚底、Projectile、AI 查询和回滚 artifact 看到不同世界；每段楼梯还需要人工维护。该方案不采用。

### 只把 GroundSnapDistance 调到 MaximumStepHeight

优点：下楼梯代码改动最少。

代价：角色会在平台边缘、坡面缺口和断崖附近被更远距离吸地，跳跃/作者向下运动也更难区分；同时仍不修复 Step Up 与 blocker 无关的问题。该方案不采用。

### 保持上台阶整段 raise/forward/down

优点：查询次数少，代码简单。

代价：会吞掉 remaining，无法保证 landing 属于真实障碍，也无法在上台阶后继续处理墙体或第二级台阶。该方案不采用。

### 只做一个顶部落点探测

优点：查询次数较少，可以得到大致高度。

代价：无法区分有足够深度的踏面、尖角、细栏和越过障碍后的原高度地面，也无法给台阶鼻部提供持续 support 证据。采用 outer/inner 两个关联探测。

### 分开 MaximumStepUpHeight 与 MaximumStepDownHeight

优点：能独立调整攀上能力和下沿吸附手感。

代价：增加角色配置、KCC identity 和资产迁移成本；当前业务没有提出不对称规则。当前保持一个正式阈值，不预留 fallback 字段。

## Failure Policy

- Step Up blocker 不合格：按普通 contact/slide 处理。
- outer/inner landing、实际高度、按实际高度执行的上方净空、前移、关联 support、坡度或最终 pose 任一无效：放弃完整 Step Up candidate。
- Step Down 超高、无稳定 landing、路径受阻或最终 pose 无效：保持 Airborne。
- query capacity 或 movement iteration 不足：维持现有 fail-closed 语义，完整 ResolveBatch 失败。
- 不回退 Unity Physics、CharacterController、隐藏 ramp、旧 TryStep 或放大 Ground Snap。

## Isolated Delivery

1. 在隔离分支或工作树锁定 current query/contact/ground report 的只读合同。
2. 定义 Step policy、请求、candidate、rejection 和 diagnostics。
3. 实现 blocker 选择、outer/inner 顶部发现和真实高度计算。
4. 实现按真实高度执行的 Step Up 路径验证。
5. 实现可复用的台阶鼻部 support evaluator。
6. 实现 consumed/remaining 结果，但不继续 Motor iteration。
7. 实现独立 Step Down candidate，但不改 Ground Snap 调用顺序。
8. 清理模块对 Motor、WorldSolver、Composition、Unity asset、identity 和 snapshot 的写入依赖。
9. 保留隔离提交或补丁，等待 `integrate-deterministic-kcc-step-solving`。

本 change 没有运行时迁移步骤。它不能单独合入或 archive；最终运行时只有一个 Step Solver 的条件由接入 change 保证。
