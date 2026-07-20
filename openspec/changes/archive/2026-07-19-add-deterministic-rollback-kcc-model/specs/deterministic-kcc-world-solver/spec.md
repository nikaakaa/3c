# deterministic-kcc-world-solver Specification

## ADDED Requirements

### Requirement: Deterministic KCC 必须实现 Portable World Solver 合同

DeterministicKccWorldSolver MUST通过 ICharacterWorldSolver 接收 portable BodyState、MotionRequest、Tick context 和确定性 world state，并返回 portable BodyResult。Program/Kernel MUST不引用 KCC concrete type 或 Network Model。

#### Scenario: Kernel 执行 KCC Motion

- **WHEN** Kernel 产生 portable MotionRequest
- **THEN** KCC MUST返回确定 BodyResult 并由 Kernel 更新 SimulationState

### Requirement: Collision World 必须使用版本化量化 Artifact

DeterministicCollisionWorldArtifact MUST保存 MapId、quantization、bounds、surface/material catalog、canonical static primitives、stable order 和 content hash。Runtime MUST不读取 Unity Physics scene、Mesh instance id 或动态场景几何。

#### Scenario: 两端加载地图

- **WHEN** 两端加载同一 CollisionWorldHash
- **THEN** MUST获得相同 primitive order 与量化数据

### Requirement: KCC 必须使用确定数值与固定查询顺序

KCC gameplay calculation MUST使用 core fixed/quantized math、stable candidate/contact order、固定 iteration limit 和明确 overflow policy。KCC state/hash MUST不包含 float/double、Unity Vector/Quaternion、无序集合结果 或未保存随机数。

#### Scenario: 同一 Capsule 碰到两个 Surface

- **WHEN** query 同时返回多个 candidate/contact
- **THEN** KCC MUST按 canonical primitive/contact order 处理

### Requirement: KCC 必须完整实现已声明移动能力

KCC 若声明 capsule-static-world capability，MUST完整处理 capsule cast/overlap、ground probing/snap、slope limit、step up/down、wall slide、penetration resolution 和 yaw/motion 顺序。KCC 若声明 `WorldFeature.ActorCollision`，MUST同时完整实现 stable pair order、连续相对 sweep、初始重叠去穿透、接触响应、静态世界重新约束与最终 pair separation validation。未实现的 moving platform 或 dynamic body MUST不出现在 capability manifest。

#### Scenario: Program 需要 Moving Platform

- **WHEN** Program/world profile 要求 moving-platform capability
- **THEN** KCC combination MUST拒绝创建

### Requirement: KCC 必须批量解决 Actor 身体接触

DeterministicKccWorldSolver MUST在同一个 World batch中先为全部 Actor生成静态世界 candidate，再按 stable ActorId pair order解决 fixed capsule身体接触。系统 MUST使用垂直区间过滤与双方相对位移的连续平面 sweep，MUST不按 Actor逐个提交、读取 Unity Collider或在 Presentation中执行第二次碰撞。

#### Scenario: 两个 Actor 相向冲刺

- **WHEN** 两个 Actor在同一SimulationTick内高速相向移动
- **THEN** KCC MUST使用同一batch的相对sweep找到接触时刻
- **AND** 两端 MUST按相同pair顺序得到相同BodyResult与KCC hash

### Requirement: Actor 接触必须使用 SolidBodyBlock 语义

Actor contact MUST只实现运动学`SolidBodyBlock`。静止目标 MUST阻挡主动闭合的移动者而不被隐式推行；双方移动时 MUST裁剪相对闭合法向并保留切向移动。系统 MUST不计算质量、冲量、弹性或动量交换，也 MUST不按Action、Team、Animation producer或Network role改变响应。

#### Scenario: 移动 Actor 撞到静止 Actor

- **WHEN** Actor A主动移动并接触静止Actor B
- **THEN** Actor A的闭合法向位移 MUST被裁剪
- **AND** Actor B MUST不因该接触获得隐式推行位移

### Requirement: Actor 接触修正必须重新约束静态世界并原子提交

每轮 Actor pair修正后，KCC MUST重新应用静态世界约束，并在最终提交前同时验证静态 penetration与所有有效pair的最小间距。全部 Actor BodyResult与next world state MUST原子提交；pair容量溢出、固定迭代不收敛或两类约束无法同时满足时，整个Step MUST失败。

#### Scenario: Actor 被另一 Actor 挤向墙面

- **WHEN** Actor pair接触修正会让其中一方进入静态墙体
- **THEN** KCC MUST重新约束墙面并继续固定次数的pair求解
- **AND** 无法同时满足墙面与Actor间距时 MUST拒绝整个Step

### Requirement: Actor Contact 配置必须进入 Solver 身份

Fixed contact radius、height、skin、pair capacity、iteration count与策略版本 MUST进入KccId或WorldConfigurationHash。Solver只有完整实现Actor contact合同后才能声明`WorldFeature.ActorCollision`。任何影响未来Tick的contact cache MUST进入World Snapshot与StateHash；无跨Tick cache时不得伪造snapshot字段。

#### Scenario: 两端 Contact Profile 不同

- **WHEN** 两端的contact radius、iteration count或策略版本不同
- **THEN** Rollback handshake MUST因KCC/World identity不匹配而拒绝Session

### Requirement: KCC State 必须参与 World Snapshot 与 Hash

KCC actor/world state MUST全部进入 Fixed `SimulationWorldStateSet` 并由完整 `SimulationWorldSnapshot` capture/restore/hash，包含 body、velocity、grounded和stable support primitive/feature/normal，以及其它确实会影响未来Tick分支的固定状态。瞬态query candidate、contact manifold、step/ledge诊断和iteration统计 MUST不进入Snapshot或Hash。

#### Scenario: Restore 坡面上的 Actor

- **WHEN** Rollback Pipeline恢复 Actor在坡面 grounded的 world snapshot
- **THEN** KCC ground/slope state MUST与 BodyState 同时恢复

### Requirement: KCC 失败必须终止确定模拟而不回退

Overflow、query capacity、iteration non-convergence、invalid artifact 或 unsupported dynamic state MUST产生精确 deterministic solver failure。系统 MUST不回退 Unity Physics、CharacterController、float solver 或直接应用 request displacement。

#### Scenario: Contact Iteration 不收敛

- **WHEN** KCC 达到固定 iteration limit 仍无法解决 penetration
- **THEN** MUST报告明确 failure 并停止该 simulation session
