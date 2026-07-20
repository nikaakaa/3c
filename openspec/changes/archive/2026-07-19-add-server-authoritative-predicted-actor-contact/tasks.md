## 1. 现状锁定与依赖收口

- [x] 1.1 确认`add-dotrecast-authoritative-server-backend`已归档且current spec包含唯一DotRecast ActorContactSolver合同。
- [x] 1.2 记录当前Prediction Source只注册本地owner的正式代码入口。
- [x] 1.3 记录当前Remote Body从Observation直接进入Remote Presentation的旧入口。
- [x] 1.4 记录当前Float32 Step、World batch request与request hash字节布局。
- [x] 1.5 记录当前Prediction Correction v3、History v1、Journal v2 participant顺序。
- [x] 1.6 记录当前Corin DotRecast Prediction Composition的World feature与Model policy资产字段。
- [x] 1.7 确认Fixed Rollback源码、资产、spec与构建产品不在本change修改范围。

## 2. Float32观察约束合同

- [x] 2.1 定义观察World参与者的model-neutral ActorId与Body轨迹合同。
- [x] 2.2 定义Exact、Interpolation与ConstantVelocityExtrapolation采样类型。
- [x] 2.3 定义按SimulationTick绑定的单Actor观察约束值。
- [x] 2.3.1 将Solver锁定的接触形状configuration hash加入单Actor观察约束。
- [x] 2.4 定义按ActorId稳定排序的`ObservedWorldConstraintFrame`。
- [x] 2.5 拒绝无效ActorId、重复ActorId、tick不匹配和不连续Body。
- [x] 2.6 提供带明确tick的正式空frame。
- [x] 2.7 将观察frame加入`Float32SimulationStep`构造与只读公开合同。
- [x] 2.8 更新所有Local Step创建点显式传入空frame。
- [x] 2.9 更新所有Authority Step创建点显式传入空frame。
- [x] 2.10 更新所有Preview与Editor Step创建点显式传入空frame。
- [x] 2.11 删除以`null`或缺省构造表示观察约束关闭的路径。

## 3. World batch request与identity

- [x] 3.1 扩展`WorldSolveBatchRequest`保存观察frame。
- [x] 3.2 保持active request与`BeforeWorldState.Bodies`一一对应验证。
- [x] 3.3 验证observed ActorId不得与active roster重复。
- [x] 3.4 验证observed frame tick必须等于batch tick。
- [x] 3.5 将观察frame canonical bytes加入World request codec。
- [x] 3.6 将观察frame加入`RequestHash`。
- [x] 3.7 保持observed参与者不产生`CharacterWorldSolveResult`。
- [x] 3.8 保持observed参与者不进入`NextWorldState`。
- [x] 3.9 更新Float32 Program Evaluate Pass将Step观察frame传给唯一World batch。
- [x] 3.10 更新World request codec版本与相关identity常量。
- [x] 3.11 删除任何在WorldSolve Pass外单独处理观察碰撞的实现。

## 4. DotRecast参与语义

- [x] 4.1 定义`ActiveSimulated`与`ObservedKinematic`接触mobility。
- [x] 4.2 将mobility加入`ActorContactCandidate`并完成构造验证。
- [x] 4.3 保持Authority active/active pair的现有对称扫掠行为。
- [x] 4.4 为active/observed pair计算相对轨迹与连续TOI。
- [x] 4.5 只裁剪active一侧的闭合法向位移。
- [x] 4.6 保留active一侧合法切向位移。
- [x] 4.7 初始重叠时只对active执行有界去穿透。
- [x] 4.8 observed一侧不得接收correction或depentration位移。
- [x] 4.9 observed/observed pair不得生成可提交修正。
- [x] 4.10 最终间距验证同时覆盖active/active与active/observed。
- [x] 4.11 扩展接触trace记录双方mobility和单侧修正原因。
- [x] 4.12 保持ActorContactSolver不引用Network Model、Presentation、Graph或Unity。

## 5. DotRecast WorldSolver组合

- [x] 5.1 将active Surface candidate与observed轨迹合并为稳定ActorId接触candidate集合。
- [x] 5.2 只对active request执行nearest-poly、MoveAlongSurface与height projection。
- [x] 5.3 对observed轨迹验证Actor、tick与World configuration identity。
- [x] 5.3.1 验证observed接触形状hash与Solver canonical contact shape一致。
- [x] 5.4 调用唯一ActorContactSolver处理完整active/observed candidate集合。
- [x] 5.5 只对active接触修正执行Surface reconstraint。
- [x] 5.6 只为active request构造FinalBody与WorldSolveResult。
- [x] 5.7 只把active FinalBody写入NextWorldState。
- [x] 5.8 active/observed无法同时满足Surface和间距时拒绝整个batch。
- [x] 5.9 为DotRecast Solver声明`ActorCollision` feature。
- [x] 5.10 新增并声明`ObservedKinematicActorContact` feature。
- [x] 5.11 更新DotRecast Solver definition、version与configuration identity。
- [x] 5.12 保持Unity CharacterController Solver不声明未实现的观察接触feature。

## 6. Remote Body Timeline

- [x] 6.1 在Prediction History模块内建立唯一`ServerAuthoritativeRemoteBodyTimeline`。
- [x] 6.2 按ActorId与authority tick稳定保存原始权威Body样本。
- [x] 6.3 验证BeforeBody、FinalBody和连续authority tick区间。
- [x] 6.4 验证Actor、Solver、World与Compatibility identity。
- [x] 6.5 定义有界容量与只淘汰不再被History引用的样本规则。
- [x] 6.6 将Observation Ingress的remote Body写入该timeline。
- [x] 6.7 保持producer sample与reliable EventId继续按原authority tick进入正式产品。
- [x] 6.8 删除Observation到Remote Presentation的原始Body旁路。
- [x] 6.9 将timeline纳入Prediction aggregate事务checkpoint。
- [x] 6.10 将timeline纳入Prediction aggregate capture、restore与hash。
- [x] 6.11 保持timeline不引用Unity Transform、MonoBehaviour或Presentation runtime。
- [x] 6.12 从locked handshake roster建立必须完成预热的remote Actor集合。
- [x] 6.13 为全部remote Actor建立首个合法Body anchor后才结束Observation Priming。

## 7. Current与Replay采样

- [x] 7.1 在Model policy中增加`MaximumRemoteBodyExtrapolationTicks`正式字段。
- [x] 7.2 将最大外推tick加入policy validation与configuration hash。
- [x] 7.3 删除旧`RemoteInterpolationDelayTicks`字段、序列化入口和默认值。
- [x] 7.3.1 在首个remote Body集合到达前让Schedule显式保持`RemoteObservationPriming`。
- [x] 7.3.2 Priming期间产生零Current step并复用既有pending request保存规则。
- [x] 7.3.3 正常调度开始后的remote样本缺口不得退回Priming。
- [x] 7.4 为Current step实现Exact Body选择。
- [x] 7.5 为Current step实现区间内确定性Interpolation。
- [x] 7.6 为Current step实现有界ConstantVelocityExtrapolation。
- [x] 7.7 超过外推上限时拒绝Current plan而不是生成空观察frame。
- [x] 7.8 为要求观察接触能力的每个Current step构造完整`ObservedWorldConstraintFrame`，其他Composition构造正式空frame。
- [x] 7.9 将frame hash和采样来源写入step diagnostics。
- [x] 7.10 为Replay step读取History record保存的精确frame。
- [x] 7.11 禁止Replay使用当前timeline重新采样过去tick。
- [x] 7.12 让Reconciler在restore merge中保留当前合法RemoteBodyTimeline。
- [x] 7.13 缺少Replay frame时进入既有formal failure或HardRecovery。
- [x] 7.14 保持零步、单步和双步Schedule都只由唯一Schedule构造frame。

## 8. Prediction History v2

- [x] 8.1 将History schema version从v1提升为v2。
- [x] 8.2 在History record中保存观察frame canonical value。
- [x] 8.3 在History record中保存观察frame hash和采样类型。
- [x] 8.4 在History participant payload中保存RemoteBodyTimeline canonical bytes。
- [x] 8.5 更新History v2 canonical writer字段顺序与count上限。
- [x] 8.6 更新History v2 canonical reader严格校验全部identity。
- [x] 8.7 更新History state hash覆盖timeline与每tick frame。
- [x] 8.8 更新History capacity计算覆盖远端样本引用。
- [x] 8.9 删除History v1 magic、reader和exact-byte断言。
- [x] 8.10 保持Correction v3与Journal v2 schema和participant顺序不变。
- [x] 8.11 删除任何History v1/v2双写或兼容分支。

## 9. Remote Presentation收口

- [x] 9.1 定义Schedule已选择远端Body frame的正式Egress产品。
- [x] 9.2 只在outer transaction成功Commit后发布selected Body frame。
- [x] 9.2.1 只发布成功Current step的selected Body frame。
- [x] 9.2.2 Replay frame不得重新发布到实时Remote Presentation。
- [x] 9.2.3 零Current step时完成或保持既有表现区间，不选择新Body frame。
- [x] 9.2.4 HardRecovery通过正式Egress重置selected Body stream，后续成功Current step提交新anchor。
- [x] 9.3 让Remote Presentation registration缓存selected frame而非原始权威Body。
- [x] 9.4 保持渲染帧只在相邻selected frame之间插值。
- [x] 9.5 同tick selected frame被替换时复用现有visual convergence边界。
- [x] 9.6 可靠Select、Complete、Release不得早于对应selected Body tick发布。
- [x] 9.7 SampleProducer继续按authority tick进入动画采样缓存。
- [x] 9.8 删除Remote Presentation独立Body authority cursor。
- [x] 9.9 删除Remote Presentation独立Body delay配置。
- [x] 9.10 保持Presentation不写World state、Solver input或Prediction history。

## 10. Composition、资产与握手

- [x] 10.1 在Corin DotRecast Prediction Composition要求`ActorCollision` feature。
- [x] 10.2 在Corin DotRecast Prediction Composition要求`ObservedKinematicActorContact` feature。
- [x] 10.3 更新ServerAuthoritative Model资产保存最大远端Body外推tick。
- [x] 10.4 删除Corin资产中的旧Remote Body interpolation delay字段。
- [x] 10.5 更新PipelineHash覆盖采样策略与观察frame合同版本。
- [x] 10.5.1 将6个标准Float32 Pass与7个ServerAuthoritative Prediction Pass资产迁移到canonical implementation v2。
- [x] 10.5.2 保持ServerAuthoritative Authority专属Pass implementation v1不变。
- [x] 10.5.3 将Corin复制策略迁移为精确覆盖当前25个Program Producer，纳入Attack3、Attack4、Attack5新增的9个Producer。
- [x] 10.5.4 将`SelectedRemoteBodyBatch`注册进Pass-authored Float32 Runtime Package的唯一Product Slot目录。
- [x] 10.6 更新handshake compatibility覆盖History v2与World feature。
- [x] 10.6.1 更新handshake World identity验证覆盖observed contact shape hash。
- [x] 10.7 在要求观察接触feature的Prediction Active前拒绝不支持该能力的Solver。
- [x] 10.8 保持Unity Authority Composition与DotRecast Authority完整roster配置不变。
- [x] 10.9 保持Fixed Rollback Composition与资产不变。

## 11. 诊断与清理

- [x] 11.1 增加Remote Body timeline容量、首尾tick和淘汰诊断。
- [x] 11.2 增加Current/Replay采样种类、目标tick与来源tick诊断。
- [x] 11.3 增加外推跨度与上限拒绝诊断。
- [x] 11.4 增加观察frame hash与World request hash关联。
- [x] 11.5 增加active/observed pair、TOI、normal clip与depentration诊断。
- [x] 11.6 增加baseline correction是否由远端接触frame差异触发的诊断。
- [x] 11.7 删除旧Remote Body独立缓冲诊断字段。
- [x] 11.8 删除废弃schema、字段、reader、serializer和Inspector引用。
- [x] 11.9 搜索并删除第二Remote Body timeline、第二碰撞调用和Transform输入路径。
- [x] 11.10 更新implementation inventory记录唯一数据与调用链。

## 12. 文档与静态校验

- [x] 12.1 更新`openspec/project.md`中的ServerAuthoritative Prediction远端Actor口径。
- [x] 12.2 更新current specs与本change delta保持一致。
- [x] 12.3 使用`rg`确认History v1与`RemoteInterpolationDelayTicks`无运行时残留。
- [x] 12.4 使用`rg`确认Fixed Rollback专属文件未被修改。
- [x] 12.5 编译portable Float32、ServerAuthoritative与DotRecast工程并携带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 12.6 编译Unity生成的Runtime与Editor工程并携带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 12.7 编译后立即执行`dotnet build-server shutdown`。
- [x] 12.8 运行`openspec validate add-server-authoritative-predicted-actor-contact --strict --no-interactive`。
- [x] 12.9 静态核对所有canonical v2 Pass资产身份，确认不存在代码v2与资产v1分裂。
- [x] 12.10 静态核对Corin复制策略与Presentation Projection的Producer集合完全一致且无废弃ID。
- [x] 12.11 静态核对`SelectedRemoteBodyBatch`的Product Contract、Schedule writer、Egress reader与Runtime Package slot注册闭合。
