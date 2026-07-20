# Change: 补齐ServerAuthoritative客户端预测中的远端Actor硬接触

## Why

DotRecast Authority当前会把完整Actor roster放进同一个`ResolveBatch`，并由唯一`ActorContactSolver`裁决角色之间的硬接触；Client Prediction却只注册并模拟本地owner，远端Actor的权威Body直接进入Remote Presentation，不会出现在本地`WorldSolveBatchRequest`中。

因此两名玩家接触时会形成固定循环：客户端预测本地owner穿过远端Actor，Authority按完整roster挡住，客户端收到baseline后恢复到权威位置，下一次Current/Replay又因为缺少远端接触体而再次穿过。位置容差、表现插值或提高快照频率只能改变抖动幅度，不能修复这条因果闭环。

本change为Float32 World step增加正式的观察运动体约束。远端Actor仍不运行Program、不拥有CharacterSimulationState、不接收伪input；ServerAuthoritative Prediction只把权威Body时间线选择出的远端轨迹作为`ObservedKinematic`参与者送入同一个DotRecast World batch。现有`ActorContactSolver`对本地`ActiveSimulated`参与者执行单侧裁剪，对远端观察体只读取轨迹而不改写、不提交。

## Dependencies

- `add-dotrecast-authoritative-server-backend` 已归档，DotRecast静态Surface、完整active roster与portable `ActorContactSolver`已经是current truth。本change MUST只扩展其观察参与语义，不重写active/active接触。
- `refactor-server-authoritative-hybrid-runtime`与`refactor-server-authoritative-prediction-state-modules` MUST保持已归档状态，现有Prediction aggregate、History、Correction、Remote Presentation和事务边界为唯一扩展点。
- 本change与Deterministic Rollback的Fixed Program、Fixed Step、KCC、Snapshot和网络协议无关；实施时 MUST不修改Rollback专属源码或资产。
- 若现有Float32 Step、World request或Prediction aggregate不能承载正式观察约束，MUST扩展这些公共合同；不得在Endpoint、MonoBehaviour或Presentation中直接调用第二次碰撞求解。

## What Changes

- 在Float32执行合同中增加按SimulationTick绑定、按ActorId稳定排序并进入request hash的`ObservedWorldConstraintFrame`；每个Step必须显式携带该frame，Local/Authority/Preview与未声明观察接触能力的Prediction使用正式空frame，声明该能力的ServerAuthoritative Prediction使用远端观察frame。
- `ObservedWorldConstraintFrame`只表达ActorId、前后Body、采样来源tick、采样方式、参与语义与已锁定接触形状hash，不包含Network packet、Presentation Transform、DotRecast query或Character gameplay state。Solver MUST使用自己World configuration中的正式接触形状，并验证frame hash一致，不能从远端包猜测半径或高度。
- 将远端权威Body样本收口到Prediction History模块拥有的唯一`RemoteBodyTimeline`。Observation Ingress只向该时间线追加合法样本，不再把原始Body样本旁路给独立的表现采样器。
- Prediction Source握手只证明route与data plane Ready，不证明首个remote Body区间已经到达。Schedule MUST具有显式`RemoteObservationPriming`阶段：locked roster中的全部非owner Actor拥有合法采样anchor前产生零Current step并保留输入请求，不能启动无远端约束的World预测，也不能把正常首帧等待当作Session故障。
- Prediction Schedule为每个Current step按目标SimulationTick选择远端Body：区间内使用确定性插值，超出最新样本时只允许在显式上限内按最后权威速度做短时外推；无法证明合法样本时当前事务失败，不能生成缺少远端约束的step。
- Replay step必须复用原History record保存的精确观察frame，不能用“现在最新的远端Body”重新采样过去，也不能在重放时丢弃远端碰撞。
- 将Prediction History canonical schema从v1一次性升级为v2，保存Remote Body timeline、每tick已选择的观察frame及其hash。删除v1 reader和旧字节合同，不增加兼容reader、双写或迁移桥接。
- 扩展Float32 `WorldSolveBatchRequest`，使active request与observed constraint在一个request identity中进入唯一WorldSolver。Observed参与者不进入`BeforeWorldState`、不产生`CharacterWorldSolveResult`、不进入`NextWorldState`。
- 为ActorContact candidate增加明确的参与语义：Authority完整roster全部为`ActiveSimulated`；Prediction中的本地owner为`ActiveSimulated`，远端Body为`ObservedKinematic`。接触层不得把该语义命名或实现为Gameplay priority。
- `ActorContactSolver`继续只有一份实现。Active/Active保持现有对称接触；Active/Observed按相对轨迹执行连续扫掠、只裁剪或去穿透Active一侧并保留切向分量；Observed/Observed不产生可提交修正。
- DotRecast WorldSolver只对active request执行Surface candidate、surface reconstraint和FinalBody提交；Observed轨迹只参与同批接触和最终间距验证。任一约束无法同时满足时整个batch失败。
- 为支持该合同的Solver增加明确`WorldFeature`；启用远端硬接触预测的Prediction Composition必须要求该feature。Unity CharacterController Solver或仅支持active roster接触的Solver不能伪装可用，但仍可消费同一selected Body流完成远端表现并向World提交正式空观察frame。
- 远端可见Body不再独立选择另一条权威时间线。Prediction Schedule选出的Body frame作为Remote Presentation的唯一Body sample来源；只有成功outer transaction中的Current frame可以按tick提交给实时表现，Replay frame只用于重建过去World request，不能让可见远端角色倒退。零Current step时Presentation继续完成或保持已提交区间，不创建新Body选择。HardRecovery需要显式重置selected Body stream。Presentation不能反向给WorldSolver提供Transform。
- Remote动画sample与可靠事件继续使用既有authority tick与EventId合同；其发布horizon必须以同一已选择Body tick推进，不能维护另一套Body cursor。
- 将现有`RemoteInterpolationDelayTicks`破坏性替换为明确的远端Body采样策略配置，包括最大外推tick；配置进入Model/Pipeline identity。旧字段、旧默认值和独立Presentation Body delay删除。
- 增加只读诊断：原始远端样本tick、Current/Replay选择tick、Interpolation/Extrapolation方式、外推跨度、frame hash、Actor pair、参与语义、接触裁剪和reconciliation原因。诊断不得驱动采样或求解。
- 更新Corin DotRecast Prediction Composition与Model正式资产，使其显式要求观察Actor接触能力并保存唯一采样策略。

## Non-Goals

- 不在客户端运行远端Actor的Program、StateMachine、Timeline、GameplayEffect、Blackboard或Action。
- 不复制远端输入，不实现完整远端预测，也不把远端Actor加入Client Character simulation roster。
- 不修改Deterministic Rollback、Fixed Program、Fixed KCC或其Snapshot ABI。
- 不增加Unity Collider、CharacterController、Physics Scene或Transform proxy作为客户端Actor碰撞路径。
- 不实现软碰撞、无碰撞、RVO、DetourCrowd、动态障碍、推挤、击退、ghost、队伍穿透或攻击专属接触策略。
- 不通过增大correction tolerance、降低快照频率、动画平滑或visual root补偿掩盖World差异。
- 不承诺在未知远端未来输入下完全消除所有权威纠偏。远端突然转向或冲刺仍可能产生一次新baseline纠偏；目标是消除“客户端永远看不见远端碰撞体”造成的周期性穿透与拉回。

## Current Spec Comparison

- current `server-authoritative-prediction-correction-pipeline`要求Client simulation roster只包含本地owner，并规定Remote actor只进入Presentation。前半条继续保留；后半条过度限制了World约束。本change将其改为：远端仍不是Program actor，但权威Body可作为`ObservedKinematic`进入唯一WorldSolver。
- 同一current spec冻结Correction v3、History v1与Journal v2 exact bytes。Remote frame必须参加Replay，因此History v1无法继续表达完整恢复输入。本change只升级History到v2并删除v1 reader；Correction与Journal schema不变。
- current `gameplay-network-model-boundary`规定world constraint与body result必须由唯一WorldSolver产生。本change遵守该边界，并补充Model可以通过正式Step提供model-neutral观察约束，但不得自行求解或提交Body。
- current `character-presentation-interpolation`允许Remote Presentation独立缓存稀疏Body样本。本change保留渲染帧插值，但删除其对原始权威Body的独立选择权；Body选择归Prediction Schedule，Presentation只消费同一选择流。
- current `fantasy-unity-authoritative-session`仍把`RemoteInterpolationDelayTicks`列为正式策略并要求独立Body缓冲。本change以`MaximumRemoteBodyExtrapolationTicks`和Schedule selected Body流替换该旧口径，不改变Simulation、Command与Snapshot各自频率。
- current `dotrecast-navigation-world-solver`已经要求完整active roster在唯一`ResolveBatch`内执行静态Surface与Actor硬接触，并要求接触形状进入World identity。本change不再删除旧要求，只为同一接触求解器增加`ObservedKinematic`参与者、单侧修正和接触形状hash校验。
- `add-deterministic-rollback-kcc-model`拥有Fixed KCC与Rollback同步语义。本change不修改其spec、源码、资产或构建产品，两个网络模型继续通过不同Composition隔离。

## Impact

- 新change-id：`add-server-authoritative-predicted-actor-contact`。
- 修改能力：`server-authoritative-prediction-correction-pipeline`、`gameplay-network-model-boundary`、`dotrecast-navigation-world-solver`、`character-presentation-interpolation`、`fantasy-unity-authoritative-session`。
- 公共Float32合同：Step观察约束、World batch request/hash和Solver feature identity。
- ServerAuthoritative portable状态：Remote Body timeline、采样策略、History v2、Current/Replay frame选择。
- DotRecast：同一ActorContactSolver中的Active/Observed参与语义与单侧修正。
- Unity客户端：Remote Presentation只消费Schedule选出的Body frame；Corin DotRecast Prediction资产更新。
- 保持不变：Authority完整roster模拟、网络输入协议、Character Program、Fixed Rollback、动画播放生命周期和Fantasy Host产品。
