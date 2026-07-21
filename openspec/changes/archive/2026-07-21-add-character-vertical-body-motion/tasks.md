## 1. 基线清单与实施边界

- [x] 1.1 使用UTF-8读取本change的proposal、design、tasks与全部spec delta
- [x] 1.2 记录当前Semantic IR、Operation Set、Float32 Program ABI与Fixed Program ABI identity
- [x] 1.3 记录当前Float32与Fixed Character State、WorldState和WorldSnapshot codec identity
- [x] 1.4 记录当前ServerAuthoritative Prediction State、History、Baseline、Checkpoint与Egress codec identity
- [x] 1.5 记录当前Deterministic Rollback Snapshot、History、Hash与Recovery identity
- [x] 1.6 记录当前WorldCapability、WorldFeature与三个正式Solver descriptor
- [x] 1.7 记录当前CharacterPipelineDefinition全部正式Config引用和Inspector字段
- [x] 1.8 记录当前Corin Definition GUID、source revision、ProgramHash、ContractHash与ProjectionRevision
- [x] 1.9 记录当前Corin Float32与Fixed生成产物路径和产品manifest引用
- [x] 1.10 记录当前Motion accumulator从channel合成到CharacterMotionRequest的唯一调用链
- [x] 1.11 记录当前Float32和Fixed World request创建、ResolveBatch与Finalize调用链
- [x] 1.12 记录Unity CharacterController Solver计算final body的位置
- [x] 1.13 记录Deterministic KCC Solver计算final body和KCC state的位置
- [x] 1.14 记录DotRecast Solver对Y位移、height projection与Grounded的当前语义
- [x] 1.15 确认没有现存Gravity node、Gravity contribution、Body Motion Profile或其它垂直动力owner需要迁移

## 2. Body Motion Profile作者配置

- [x] 2.1 定义唯一`CharacterBodyMotionProfile` authoring类型
- [x] 2.2 为Profile定义显式`GravityAcceleration`字段
- [x] 2.3 为Profile定义显式`MaximumFallSpeed`字段
- [x] 2.4 要求GravityAcceleration为有限负数
- [x] 2.5 要求MaximumFallSpeed为有限正数
- [x] 2.6 为Profile建立稳定asset identity和content revision输入
- [x] 2.7 在`CharacterPipelineDefinition`增加唯一Body Motion Profile引用
- [x] 2.8 在Definition配置校验中拒绝缺失Profile
- [x] 2.9 在Definition配置校验中合并Profile字段错误
- [x] 2.10 在Definition source revision中加入Profile GUID与content revision
- [x] 2.11 在Definition Inspector的作者配置区显示Profile引用
- [x] 2.12 保持Definition Inspector不内联Gravity和MaximumFallSpeed
- [x] 2.13 为Profile提供只编辑正式字段的Inspector
- [x] 2.14 删除任何缺Profile时创建默认重力配置的可能路径
- [x] 2.15 删除任何Host、Scene或Network Model上的重复重力字段

## 3. Semantic IR与Program descriptor

- [x] 3.1 定义numeric-neutral Body Motion semantic descriptor
- [x] 3.2 在descriptor中保存GravityAcceleration
- [x] 3.3 在descriptor中保存MaximumFallSpeed
- [x] 3.4 在descriptor中保存Body Motion semantic version
- [x] 3.5 在descriptor中声明`AirborneVerticalMotion` required world capability
- [x] 3.6 让Frontend从Definition唯一Profile发射descriptor
- [x] 3.7 让Frontend拒绝缺失、重复或非法descriptor
- [x] 3.8 将Profile source identity关联到Semantic IR SourceMap
- [x] 3.9 将descriptor写入Semantic IR canonical codec
- [x] 3.10 将descriptor从Semantic IR canonical codec读回
- [x] 3.11 将descriptor纳入SemanticHash
- [x] 3.12 将descriptor纳入普通.NET Semantic IR Reader输出
- [x] 3.13 将descriptor降低为Float32 Program Body Motion descriptor
- [x] 3.14 将descriptor降低为Fixed Program Body Motion descriptor
- [x] 3.15 让两个Target再次校验参数范围和semantic version
- [x] 3.16 将Body Motion descriptor纳入Float32 Program canonical bytes与ProgramHash
- [x] 3.17 将Body Motion descriptor纳入Fixed Program canonical bytes与ProgramHash
- [x] 3.18 将`AirborneVerticalMotion`纳入两个Target的Program required capabilities
- [x] 3.19 提升受影响的Semantic IR与Float32/Fixed Program ABI identity
- [x] 3.20 删除旧Program reader、缺descriptor默认和兼容payload路径
- [x] 3.21 更新Program Inspector与普通.NET Reader显示Body Motion descriptor和required capability

## 4. WorldBodyState与canonical状态升级

- [x] 4.1 在Float32 `WorldBodyState`增加独立`VerticalVelocity`
- [x] 4.2 在Fixed `WorldBodyState`增加独立`VerticalVelocity`
- [x] 4.3 保持现有`Velocity`字段为actual applied velocity
- [x] 4.4 禁止从`Velocity.Y`构造或恢复`VerticalVelocity`
- [x] 4.5 更新Float32 Body equality包含VerticalVelocity
- [x] 4.6 更新Fixed Body equality包含VerticalVelocity
- [x] 4.7 更新Float32 WorldState codec写入VerticalVelocity
- [x] 4.8 更新Float32 WorldState codec读取VerticalVelocity
- [x] 4.9 更新Fixed WorldState codec写入VerticalVelocity
- [x] 4.10 更新Fixed WorldState codec读取VerticalVelocity
- [x] 4.11 更新Float32 WorldSolve request/result canonical hash包含VerticalVelocity
- [x] 4.12 更新Fixed WorldSolve request/result canonical hash包含VerticalVelocity
- [x] 4.13 更新Float32 WorldSnapshot capture与restore包含VerticalVelocity
- [x] 4.14 更新Fixed WorldSnapshot capture与restore包含VerticalVelocity
- [x] 4.15 更新Float32 WorldStateHash包含VerticalVelocity
- [x] 4.16 更新Fixed WorldStateHash包含VerticalVelocity
- [x] 4.17 提升Float32 WorldState与Snapshot codec identity
- [x] 4.18 提升Fixed WorldState与Snapshot codec identity
- [x] 4.19 删除旧WorldState/Snapshot reader与缺字段默认值
- [x] 4.20 更新全部初始Body创建点显式提供VerticalVelocity
- [x] 4.21 更新全部Body复制、插值、比较和诊断投影点区分actual Velocity与VerticalVelocity

## 5. Motion accumulator输出边界

- [x] 5.1 定义Float32 `ResolvedGameplayMotion`
- [x] 5.2 定义Fixed `ResolvedGameplayMotion`
- [x] 5.3 让ResolvedGameplayMotion保存最终玩法displacement
- [x] 5.4 让ResolvedGameplayMotion保存最终玩法requested velocity
- [x] 5.5 让ResolvedGameplayMotion保存yaw、has-motion与必要provenance
- [x] 5.6 让Float32 Motion accumulator返回ResolvedGameplayMotion
- [x] 5.7 让Fixed Motion accumulator返回ResolvedGameplayMotion
- [x] 5.8 保持Locomotion、Action、GameplayResult的channel顺序不变
- [x] 5.9 保持Additive、WeightedBlend、Override与ConsumeLowerChannels语义不变
- [x] 5.10 保持Program Motion Modifier发生在ResolvedGameplayMotion形成前
- [x] 5.11 删除Float32 accumulator直接构造CharacterMotionRequest的旧路径
- [x] 5.12 删除Fixed accumulator直接构造CharacterMotionRequest的旧路径
- [x] 5.13 禁止Gravity作为MotionContribution进入任一channel
- [x] 5.14 禁止Motion Modifier读取或覆盖环境gravity delta
- [x] 5.15 更新Motion Trace区分resolved gameplay motion与最终world request

## 6. Float32 Body Motion Integration

- [x] 6.1 定义Float32 Body Motion descriptor runtime类型
- [x] 6.2 定义Float32 Body Motion integration plan
- [x] 6.3 让plan保存previous vertical velocity
- [x] 6.4 让plan保存candidate vertical velocity
- [x] 6.5 让plan保存gameplay Y与gravity delta
- [x] 6.6 让plan保存最终requested displacement与identity
- [x] 6.7 实现Float32先更新candidate速度再以`candidate * TickDelta`生成位移的半隐式Prepare公式
- [x] 6.8 在Prepare中按MaximumFallSpeed限制向下速度
- [x] 6.9 在Prepare中将gravity delta与完整gameplay displacement相加
- [x] 6.10 保持gameplay yaw不受Body Motion改变
- [x] 6.11 让Grounded body以零初始垂直动力继续生成向下重力位移
- [x] 6.12 实现Float32碰撞后Finalize规则
- [x] 6.13 只让稳定Grounded清零向下VerticalVelocity
- [x] 6.14 保证Below接触在Grounded为false时不清零向下VerticalVelocity
- [x] 6.15 让Above碰撞清零向上VerticalVelocity
- [x] 6.16 让仍Airborne的body保存candidate VerticalVelocity
- [x] 6.17 从applied displacement独立计算actual Velocity
- [x] 6.18 拒绝TickDelta、descriptor、plan identity或数值非法
- [x] 6.19 保持plan为同Step transient且不进入Character State
- [x] 6.20 删除Float32 Host、Solver或Network Model中的重复积分公式

## 7. Fixed Body Motion Integration

- [x] 7.1 定义Fixed Body Motion descriptor runtime类型
- [x] 7.2 定义Fixed Body Motion integration plan
- [x] 7.3 让Fixed plan字段与Float32保持同语义
- [x] 7.4 以Q32.32实现先更新candidate速度再生成位移的固定半隐式Prepare公式
- [x] 7.5 以Q32.32应用MaximumFallSpeed限制
- [x] 7.6 以Q32.32计算`candidate * TickDelta`
- [x] 7.7 将Fixed gravity delta与完整gameplay displacement相加
- [x] 7.8 保持Fixed gameplay yaw不受Body Motion改变
- [x] 7.9 让Grounded Fixed body同Tick生成向下重力位移
- [x] 7.10 实现Fixed碰撞后Finalize规则
- [x] 7.11 只让稳定Grounded清零向下VerticalVelocity
- [x] 7.12 保证Below接触在Grounded为false时不清零向下VerticalVelocity
- [x] 7.13 让Above碰撞清零向上VerticalVelocity
- [x] 7.14 让仍Airborne的body保存candidate VerticalVelocity
- [x] 7.15 从Fixed applied displacement独立计算actual Velocity
- [x] 7.16 拒绝Fixed溢出、非法descriptor、plan identity或TickDelta
- [x] 7.17 保持Fixed plan为同Step transient且不进入Snapshot
- [x] 7.18 禁止Fixed实现回退float、Unity Time或Unity Physics
- [x] 7.19 删除Fixed KCC、Source或Network Model中的重复积分公式

## 8. Kernel、WorldRequest与统一Finalize接入

- [x] 8.1 让Float32 Evaluate在Motion accumulator后调用Body Motion Prepare
- [x] 8.2 让Fixed Evaluate在Motion accumulator后调用Body Motion Prepare
- [x] 8.3 让两个Target只从Prepare结果构造唯一CharacterMotionRequest
- [x] 8.4 将integration plan以typed transient关联到CharacterWorldSolveRequest
- [x] 8.5 将plan identity纳入World request一致性校验
- [x] 8.6 保持World request不包含Action、Timeline、Graph或Profile对象引用
- [x] 8.7 让Float32 World result builder调用唯一Float32 Finalize
- [x] 8.8 让Fixed World result builder调用唯一Fixed Finalize
- [x] 8.9 保持concrete Solver只提供applied displacement、稳定Grounded和方向性Collision事实
- [x] 8.10 让Finalize后的VerticalVelocity只写入NextWorldState
- [x] 8.11 保持pending integration plan不进入Snapshot、History或packet
- [x] 8.12 让outer transaction abort同时丢弃pending integration plan
- [x] 8.13 让restore后的下一次Prepare只读取恢复后的WorldBodyState
- [x] 8.14 更新WorldSolve request/result codec和Reader显示integration摘要但不持久化plan
- [x] 8.15 删除旧Evaluate直接传递accumulator request的调用链

## 9. Unity CharacterController与Fixed KCC能力接入

- [x] 9.1 为通用WorldCapability增加稳定`AirborneVerticalMotion` identity
- [x] 9.2 更新Capability codec、descriptor、manifest和Inspector显示
- [x] 9.3 让Unity CharacterController Solver消费Prepare后的完整XYZ displacement
- [x] 9.4 让Unity Solver保持CharacterController.Move为唯一场景移动调用
- [x] 9.5 让Unity Solver把CollisionFlags Above/Below准确映射为方向性portable Collision
- [x] 9.6 让Unity Solver独立确认稳定Grounded且不以Below标志代替稳定支撑
- [x] 9.7 让Unity Solver通过Float32唯一Finalize构造VerticalVelocity
- [x] 9.8 让Unity Solver完成后声明AirborneVerticalMotion
- [x] 9.9 让Deterministic KCC消费Prepare后的完整Fixed XYZ displacement
- [x] 9.10 保持KCC Motor不读取GravityAcceleration或MaximumFallSpeed
- [x] 9.11 保持KCC Ground Snap只受最终request Y和上一support约束
- [x] 9.12 只把KCC `IsStableOnGround`映射为portable Grounded
- [x] 9.13 保持KCC非稳定下方接触与稳定Grounded语义分离
- [x] 9.14 让KCC上方阻挡准确映射Above
- [x] 9.15 让KCC Solver通过Fixed唯一Finalize构造VerticalVelocity
- [x] 9.16 让Deterministic KCC完成后声明AirborneVerticalMotion
- [x] 9.17 将Body Motion semantic version与Capability纳入相关Solver/World compatibility校验
- [x] 9.18 搜索确认Unity与Fixed Solver都没有私有重力常量或第二套Finalize

## 10. DotRecast明确拒绝边界

- [x] 10.1 保持DotRecast descriptor不声明AirborneVerticalMotion
- [x] 10.2 保持DotRecast capability identity不包含空中碰撞
- [x] 10.3 让Composition在创建DotRecast runtime前检测Program缺失能力
- [x] 10.4 让拒绝错误明确列出Program、Solver与AirborneVerticalMotion
- [x] 10.5 禁止DotRecast丢弃request Y后继续返回成功
- [x] 10.6 禁止DotRecast通过NavMesh height projection伪造空中落地
- [x] 10.7 禁止DotRecast按Network Model关闭Body Motion descriptor
- [x] 10.8 禁止DotRecast组合隐藏调用Unity Physics或Fixed KCC fallback
- [x] 10.9 更新DotRecast产品manifest与诊断准确声明当前能力缺口
- [x] 10.10 更新DotRecast current documentation指向后续正式空中World backend需求

## 11. ServerAuthoritative状态与纠偏升级

- [x] 11.1 更新Prediction working state保存VerticalVelocity
- [x] 11.2 更新Prediction History entry保存VerticalVelocity
- [x] 11.3 更新Prediction History codec写入VerticalVelocity
- [x] 11.4 更新Prediction History codec读取VerticalVelocity
- [x] 11.5 更新Authority Baseline body写入VerticalVelocity
- [x] 11.6 更新Authority Baseline body读取VerticalVelocity
- [x] 11.7 更新Authority Checkpoint保存VerticalVelocity
- [x] 11.8 更新Canonical Egress与Observation payload保存VerticalVelocity
- [x] 11.9 更新Prediction与Authority body equality比较VerticalVelocity
- [x] 11.10 更新Baseline merge与HardRecovery替换VerticalVelocity
- [x] 11.11 更新restore/replay从baseline VerticalVelocity继续Prepare
- [x] 11.12 提升Prediction、History、Baseline、Checkpoint与Egress codec identity
- [x] 11.13 更新协议或portable canonical payload的正式生成入口
- [x] 11.14 删除旧网络payload reader、缺字段默认和双写
- [x] 11.15 更新handshake与产品manifest引用最终Program/World identity

## 12. Deterministic Rollback状态、Hash与Replay升级

- [x] 12.1 让Fixed完整World Snapshot保存VerticalVelocity
- [x] 12.2 让Rollback History保存升级后的完整World Snapshot
- [x] 12.3 让restore原子恢复每个Actor的VerticalVelocity
- [x] 12.4 让replay下一Tick从恢复速度执行Fixed Prepare
- [x] 12.5 将VerticalVelocity纳入WorldStateHash
- [x] 12.6 将VerticalVelocity纳入分层KccHash或Body dynamics hash的正式归属
- [x] 12.7 保持Kcc support state与VerticalVelocity同时恢复但职责分离
- [x] 12.8 提升Rollback snapshot、history、hash与recovery identity
- [x] 12.9 更新Relay/runtime manifest与peer handshake引用最终identity
- [x] 12.10 删除旧Fixed snapshot reader与缺字段默认
- [x] 12.11 搜索确认Rollback没有从actual Velocity.Y重建VerticalVelocity
- [x] 12.12 搜索确认Rollback没有peer-local或presentation-local垂直动力cache

## 13. Presentation、Diagnostics与工具链

- [x] 13.1 保持Character Body Presentation使用actual Position、Velocity与Grounded
- [x] 13.2 禁止Presentation用VerticalVelocity反写VisualRoot或Gameplay
- [x] 13.3 让Presentation诊断可选显示VerticalVelocity但不取得mutable plan
- [x] 13.4 更新Body Motion Trace记录gameplay Y
- [x] 13.5 更新Trace记录previous VerticalVelocity
- [x] 13.6 更新Trace记录GravityAcceleration与gravity delta
- [x] 13.7 更新Trace记录candidate VerticalVelocity与requested Y
- [x] 13.8 更新Trace记录applied Y、Grounded与Collision
- [x] 13.9 更新Trace记录committed VerticalVelocity
- [x] 13.10 让Trace关联Program descriptor、ActorId、Tick与WorldRequestId
- [x] 13.11 更新Runtime Diagnostics inspector显示Prepare、Solve与Finalize三段
- [x] 13.12 更新Semantic IR Inspector显示Body Motion descriptor来源
- [x] 13.13 更新Program Reader显示两个Target最终Body Motion参数和capability
- [x] 13.14 保持Timeline纯动画Preview不生成重力或假Body
- [x] 13.15 让完整Gameplay Preview只通过正式Session与支持能力的WorldSolver执行Body Motion
- [x] 13.16 删除Preview、Host或Editor工具中的默认重力模拟路径

## 14. Corin配置、产物迁移与旧路径删除

- [x] 14.1 创建唯一正式Corin Body Motion Profile资产
- [x] 14.2 为Corin显式配置GravityAcceleration
- [x] 14.3 为Corin显式配置MaximumFallSpeed
- [x] 14.4 将Profile绑定到Corin CharacterPipelineDefinition
- [x] 14.5 确认Corin Definition不内联重复重力字段
- [x] 14.6 重新生成Corin validated Semantic IR artifact
- [x] 14.7 重新生成Corin Float32 Program artifact
- [x] 14.8 重新生成Corin Presentation Projection
- [x] 14.9 重新生成Corin Fixed Program artifact
- [x] 14.10 更新Local与Unity Authority产品manifest中的Program/World identity
- [x] 14.11 更新Deterministic Rollback产品manifest中的Program/World identity
- [x] 14.12 让DotRecast产品manifest保留真实缺失capability而不生成兼容配置
- [x] 14.13 删除旧Program、WorldState、Snapshot与网络codec reader
- [x] 14.14 删除任何缺Profile默认、Network Model重力开关或Solver私有重力字段
- [x] 14.15 搜索确认不存在Gravity MotionContribution、Graph gravity node或Blackboard gravity分裂路径
- [x] 14.16 搜索确认不存在从Velocity.Y推导VerticalVelocity的路径

## 15. Agent只读投影与统一校验

- [x] 15.1 盘点Agent v13 Snapshot当前Definition与Profile投影边界
- [x] 15.2 在Agent compact Snapshot增加Body Motion Profile identity摘要
- [x] 15.3 在Agent full Snapshot增加GravityAcceleration与MaximumFallSpeed只读值
- [x] 15.4 在Snapshot增加Body Motion semantic version与source revision摘要
- [x] 15.5 在Snapshot增加Program required AirborneVerticalMotion摘要
- [x] 15.6 让Snapshot从当前Definition精确引用读取Profile而不扫描目录
- [x] 15.7 让Agent Validator复用Definition/Profile正式配置校验
- [x] 15.8 让Validator报告缺Profile、非法Gravity与非法MaximumFallSpeed
- [x] 15.9 保持Agent schema v13 Patch不增加Profile mutation operation
- [x] 15.10 保持MCP bridge不增加Body Motion专用action或任意字段写入口
- [x] 15.11 确认Profile仍只由正式Inspector或未来独立批准capability修改
- [x] 15.12 更新Agent Snapshot模型、Exporter和报告字段而不输出runtime VerticalVelocity
- [x] 15.13 更新Agent current contract/skill引用说明Body Motion Profile为只读投影
- [x] 15.14 使用正式Agent export_snapshot核对Corin Body Motion摘要
- [x] 15.15 使用正式Agent validate核对Profile与Simulation Compiler报告一致

## 16. 文档、静态验证与收口

- [x] 16.1 更新`openspec/project.md`的Motion链路加入ResolvedGameplayMotion与Body Motion Prepare/Finalize
- [x] 16.2 更新`openspec/project.md`准确记录Unity、Fixed与DotRecast垂直能力
- [x] 16.3 更新current specs中的最终Program ABI、WorldState/Snapshot与网络codec identity
- [x] 16.4 删除current specs中Motion accumulator直接生成最终Request的过时描述
- [x] 16.5 删除current specs中Ground Snap或NavMesh投影等价于完整重力的歧义描述
- [x] 16.6 对照active changes确认没有覆盖其未完成业务字段或保留双版本codec
- [x] 16.7 确认Float32与Fixed descriptor、公式、状态转换和trace字段保持同语义
- [x] 16.8 确认DotRecast只通过正式capability拒绝且没有运行时fallback
- [x] 16.9 确认没有旧reader、兼容字段、缺失默认、双写或第二Body Motion路径
- [x] 16.10 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的正式命令编译portable Core/Float32/Fixed
- [x] 16.11 编译后立即执行`dotnet build-server shutdown`
- [x] 16.12 使用同样参数编译Deterministic KCC与Deterministic Rollback程序集
- [x] 16.13 编译后立即执行`dotnet build-server shutdown`
- [x] 16.14 使用同样参数编译Assembly-CSharp
- [x] 16.15 编译后立即执行`dotnet build-server shutdown`
- [x] 16.16 使用同样参数编译Assembly-CSharp-Editor
- [x] 16.17 编译后立即执行`dotnet build-server shutdown`
- [x] 16.18 使用正式Reader读取Corin Semantic IR、Float32 Program与Fixed Program并核对descriptor/identity
- [x] 16.19 运行`openspec validate add-character-vertical-body-motion --strict --no-interactive`
- [x] 16.20 确认没有运行Unity batchmode且没有新增测试或人工验证task
- [x] 16.21 全部实现和清理真实完成后才将本checklist逐项标记为`[x]`
