## 1. 基于现有模型边界固定迁移输入

- [ ] 1.1 读取已归档 `refactor-gameplay-network-model-boundary` 后的 current specs，确认 SessionHost、Session、Adapter、Binding、精确路由、MotionCommand 和 policy ownership 已是现行真相。
- [ ] 1.2 盘点 ServerAuthoritative packet、payload、identity、endpoint、EndpointDefinition 和 debug record 的程序集依赖。
- [ ] 1.3 盘点 GameLogic Fantasy bootstrap、SessionFacade、GameProto 和生成 Handler 的当前依赖方向。
- [ ] 1.4 盘点 Outer proto、client/server 生成目录、opcode cache 和旧 FrameSync 全部引用。
- [ ] 1.5 记录 Sandbox 当前 SessionHost、model definition、LocalLoopback definition、Owner binding 和 Corin root 的 GUID/scene fileID。
- [ ] 1.6 明确本 change 不创建通用 GameplaySync runtime、第二 Network Model、per-character peer 或新 policy profile。
- [ ] 1.7 确认一个独立前置 change 已在 Unity authoritative process 与 Fantasy 纯 CSharp KCC 中选择并完整实现本纵切唯一 authoritative motion backend；缺失时停止 apply，不使用客户端 resolved motion fallback。

## 2. 建立 ServerAuthoritative 专属 contracts 程序集

- [ ] 2.1 新增 `ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid.Contracts` asmdef。
- [ ] 2.2 将 ServerAuthoritative packet/envelope/payload/identity/packet enums 迁入 contracts 目录并保留 Unity meta GUID。
- [ ] 2.3 将 `IServerAuthoritativeEndpoint`、EndpointDefinition 基类和 endpoint debug record 迁入 contracts 目录并保留 Unity meta GUID。
- [ ] 2.4 让 contracts asmdef 不引用 Character、BTSMTL、Timeline、Animation、GameLogic 或 GameProto。
- [ ] 2.5 让现有 Session、LocalLoopback、model definition 和 Adapter 继续引用同一 contracts 类型，不创建镜像 DTO。
- [ ] 2.6 为 `GameLogic.asmdef` 增加 ServerAuthoritative contracts 与 GameProto 的正式引用。
- [ ] 2.7 保持 `ServerAuthoritativeHybridSession`、Character Adapter/Binding、profile/resolver 和 Inspector 在现有 ownership 中。
- [ ] 2.8 删除任何为了跨程序集访问而增加的反射、静态 packet bridge、Assembly-CSharp wrapper 或 service locator。

## 3. 替换旧 FrameSync 协议

- [ ] 3.1 从 Outer proto 删除全部 `FrameSync*`、`C2G_FrameSync*` 和 `G2C_FrameSync*` 消息。
- [ ] 3.2 定义 Join request/response、ActorDescriptor、ActorJoined 和 ActorLeft 消息。
- [ ] 3.3 定义携带 canonical input/action request 的 C2G MotionCommand 与 G2C MotionSnapshot/MotionCorrection 消息。
- [ ] 3.4 定义 C2G MotionCorrectionAck 消息。
- [ ] 3.5 定义 C2G ActionActivation/ActionLifecycle 消息。
- [ ] 3.6 定义 G2C ActionDecision/ActionReplication 消息。
- [ ] 3.7 为消息保存必要的 subject actor、sequence、local tick、server tick、canonical move/facing input、配置 identity 和稳定 action instance identity。
- [ ] 3.8 将可选 predicted pose/resolved result 标记为独立 prediction comparison metadata，不允许 authoritative backend 将其读取为 canonical displacement。
- [ ] 3.9 让协议不保存 Graph、Timeline、Track、Clip、AnimationClip、producer 或 Animancer identity。
- [ ] 3.10 更新 ProtocolExportTool client output 到 GameProto 生成目录。
- [ ] 3.11 保持 server output 位于 Server Entity 生成目录。
- [ ] 3.12 运行正式 ProtocolExportTool 生成 client/server C#，不手改生成文件。
- [ ] 3.13 删除旧生成目录、旧 opcode cache、旧 FrameSync parser 和消息别名。

## 4. 实现 Fantasy EndpointDefinition 与 Endpoint

- [ ] 4.1 新增 `FantasyServerAuthoritativeEndpointDefinition` 并显式保存 host、port、KCP、connect timeout、heartbeat 和容量。
- [ ] 4.2 校验 endpoint 配置缺失、非法端口、非法 timeout、非法 heartbeat 和非正容量时直接失败。
- [ ] 4.3 新增 `FantasyServerAuthoritativeEndpoint` 实现正式 `IServerAuthoritativeEndpoint`。
- [ ] 4.4 为 endpoint 增加 Created、Connecting、Connected、Faulted、Disconnected、Disposed 状态。
- [ ] 4.5 让 endpoint 唯一创建和持有 `FantasySessionFacade` 与 Fantasy Session。
- [ ] 4.6 从 `FantasyClientBootstrap` 删除静态 SessionFacade ownership，只保留进程级平台初始化。
- [ ] 4.7 让 endpoint 建连后发送唯一 Join request，并在 Join 完成前拒绝 gameplay outgoing flush。
- [ ] 4.8 新增强类型 outgoing mapper，将每种正式 ServerAuthoritative packet 映射为生成 C2G message。
- [ ] 4.9 新增强类型 incoming mapper，将生成 G2C gameplay message 映射为同一 ServerAuthoritative packet。
- [ ] 4.10 在 Fantasy Session 上挂载 endpoint-owned component，供 G2C Handler 精确找到当前 endpoint。
- [ ] 4.11 让 Unity G2C Handler 只做 message 映射和入队，不访问 CharacterPipeline、GameObject 或 Transform。
- [ ] 4.12 连接失败或断开时保存明确原因并清理 Session component、pending、incoming 和 facade。
- [ ] 4.13 Fantasy 失败时不创建 LocalLoopback、不自动重连、不切换 endpoint definition。

## 5. 扩展现有 Model Session 的连接与 roster 事实

- [ ] 5.1 新增模型专属 `ServerAuthoritativeSessionEvent` 合同表达 JoinCompleted、ActorJoined、ActorLeft、ClockUpdated 和 Disconnected。
- [ ] 5.2 扩展 endpoint 合同暴露连接状态、故障原因和 session event drain，不把这些字段放进 common SessionHost。
- [ ] 5.3 扩展 LocalLoopback endpoint 实现新增合同，并保持无 roster event 的正式本地语义。
- [ ] 5.4 扩展 `ServerAuthoritativeHybridSession` 保存固定容量 session event queue。
- [ ] 5.5 让 Session Pump 同时收取 endpoint gameplay packet 与 session event，且同一 local logic tick 继续幂等。
- [ ] 5.6 让 session event 不进入 per-actor incoming queue，也不伪装成 Character SyncDomain。
- [ ] 5.7 在 JoinCompleted 后保存唯一 OwnerActorId、PlayerId、TeamId、spawn pose、server tick rate 和 snapshot rate。
- [ ] 5.8 ActorLeft 只清理对应 actor binding/queue/buffer，不清空存活 Owner 或其它 actor。
- [ ] 5.9 Disconnect/Dispose 清空 session event、actor queue、history、debug 和 endpoint 状态。
- [ ] 5.10 扩展 SessionHost Inspector 只读展示 endpoint connection、OwnerActorId、roster 和 health，不复制运行状态。

## 6. 建立 Character 预激活配置与 outgoing eligibility

- [ ] 6.1 为 `CharacterPipelineHost` 暴露只读 InputSource 与 MotionAuthority。
- [ ] 6.2 新增只允许 pipeline 激活前调用的正式 host 配置入口，用于设置输入来源、运动权威和可选相机依赖。
- [ ] 6.3 pipeline 已创建或 host 已注册 tick 后修改控制模式时直接失败。
- [ ] 6.4 扩展现有 `CharacterServerAuthoritativeBinding` 支持在 inactive GameObject 上写入正式 SubjectActorId 后再激活。
- [ ] 6.5 Binding 继续只序列化 SessionHost、CharacterHost、SubjectActorId 和 SyncProfile，不增加 authority role 字段。
- [ ] 6.6 Binding 根据 LocalDevice + LocalSolver 注册 outgoing-enabled Owner。
- [ ] 6.7 Binding 根据 ExternalFacts + ExternalPose 注册 receive-only Remote Character。
- [ ] 6.8 Remote Character 的 Graph/Timeline 派生 facts 不进入 adapter outgoing，不形成 echo。
- [ ] 6.9 空 SubjectActorId、重复 actor、非法控制模式组合和激活后重配直接失败，不建立等待/fallback 路径。

## 7. 建立 Fantasy 双人 Room 生命周期

- [ ] 7.1 在 Server Entity 程序集中定义 sealed `GameplayRoomComponent`。
- [ ] 7.2 定义 sealed actor entity/component 保存 canonical pose、sequence、动作事务和 health。
- [ ] 7.3 定义挂在 Fantasy Session 上的 sealed `SessionGameplayActorComponent`。
- [ ] 7.4 在 Gate `OnCreateScene` 中唯一创建 GameplayRoomComponent。
- [ ] 7.5 为 Room 建立一个 Scene-owned 30Hz timer，不为每个 actor 建 timer。
- [ ] 7.6 在 Room DestroySystem 中取消 timer 并清理 actor/session 引用。
- [ ] 7.7 实现 Join Handler，从 Session 而不是客户端字段建立 ownership。
- [ ] 7.8 为第一和第二名客户端分配唯一 PlayerId、ActorId、TeamId 和出生 pose。
- [ ] 7.9 Join response 返回 server tick、30Hz tick rate、15Hz snapshot rate、Owner descriptor 和现有 roster。
- [ ] 7.10 第二名加入时向双方发送完整且去重的 ActorJoined/roster 数据。
- [ ] 7.11 第三名加入时返回明确 RoomFull，不替换现有 actor。
- [ ] 7.12 Session component 销毁时从 Room 删除 actor并广播 ActorLeft。
- [ ] 7.13 Room 不访问账号、数据库、匹配、Map、SubScene、Roaming 或 Unity 数据。

## 8. 接入已选定的服务端 Motion 权威 backend

- [ ] 8.1 实现 C2G MotionCommand Handler，并从 Session component 解析唯一 actor。
- [ ] 8.2 拒绝未 Join、SubjectActorId 不匹配或 ownership 不匹配的 MotionCommand。
- [ ] 8.3 拒绝 NaN、Infinity、零 sequence、重复 sequence 和倒序 sequence。
- [ ] 8.4 为每个 actor 建立固定容量 motion command queue 和峰值计数。
- [ ] 8.5 motion queue 容量不足时明确 Fault/health，不静默丢失已接受 command。
- [ ] 8.6 Room tick 按 input sequence 消费本周期积累的全部 command。
- [ ] 8.7 将接受后的 canonical move/facing input 和 accepted action state 写入该 actor 的 backend step input。
- [ ] 8.8 校验 command 配置 identity 与服务端角色 motion/action 配置一致，缺失或不一致时明确失败。
- [ ] 8.9 调用前置 change 已实现的唯一 authoritative motion backend，从当前 canonical body state 独立生成 motion intent。
- [ ] 8.10 让 backend 执行世界约束并返回 canonical position、rotation、velocity、grounded 和 collision summary。
- [ ] 8.11 禁止 Room 或 backend 累加客户端 applied displacement、接受 predicted pose 或回退 envelope validation。
- [ ] 8.12 backend 缺失、执行失败或返回非法 result 时停止该 actor 权威推进并记录明确 health fault。
- [ ] 8.13 用 backend result 更新唯一 canonical actor state 与 acknowledged sequence。
- [ ] 8.14 只使用 prediction comparison metadata 比较 Owner predicted pose 与 canonical pose，超过阈值时发送 MotionCorrection。
- [ ] 8.15 处理 MotionCorrectionAck，只记录对应 sequence/server tick 已被 Owner 应用。
- [ ] 8.16 以 15Hz 向非 Owner Session 广播每个 actor 的 MotionSnapshot。
- [ ] 8.17 snapshot 携带 canonical pose、velocity、grounded、movement summary 和 latest accepted sequence。
- [ ] 8.18 服务端权威 motion 路径不运行 Animancer、动画表现、visual Timeline sampling 或 client Presentation。

## 9. 实现服务端 Action 事务与复制

- [ ] 9.1 实现 C2G ActionActivation Handler 并验证 Session ownership。
- [ ] 9.2 拒绝空 ActionId、零 ActionInstanceId、重复 activation 和倒序 sequence。
- [ ] 9.3 为 actor 保存唯一 active transaction 和固定 lease deadline。
- [ ] 9.4 接受 activation 后向 Owner 发送 ActionDecision Confirm。
- [ ] 9.5 接受 activation 后向其它 Session 广播 ActionReplication activation。
- [ ] 9.6 拒绝 activation 时只向 Owner 发送 Reject，不广播远端动作。
- [ ] 9.7 实现 C2G ActionLifecycle Handler 并验证 action instance 与 phase 顺序。
- [ ] 9.8 将 Complete、Cancel、Interrupt、Reject、Correct 和 Abort terminal phase 广播给观察端。
- [ ] 9.9 action lease 超时时结束悬空事务并广播明确 terminal 原因。
- [ ] 9.10 使用 actor + action instance + phase + server tick 去重 replication。
- [ ] 9.11 服务端 action 状态不保存 Timeline、Animation 或 producer identity。

## 10. 闭合 Owner 收发链

- [ ] 10.1 让 JoinCompleted 在 Owner root 激活前写入 SubjectActorId 与 spawn pose。
- [ ] 10.2 Owner host 使用 LocalDevice + LocalSolver，客户端继续产生本地 prediction result。
- [ ] 10.3 更新 Character adapter，使 MotionCommand 的 authority input 来自 canonical input/action facts，resolved motion 只写 prediction comparison metadata。
- [ ] 10.4 Fantasy endpoint 将 Owner outgoing 通过现有 ServerAuthoritative packet contract 发出。
- [ ] 10.5 G2C ActionDecision 继续由现有 adapter 转换为 `ActionLifecycleTransition`。
- [ ] 10.6 prediction key、server tick 和 defense-favor metadata 只进入模型 history/debug，不新增 Character authority DTO。
- [ ] 10.7 G2C MotionCorrection 继续转换为 ExternalPoseCorrection。
- [ ] 10.8 CharacterMotionStage 继续通过正式 Motion Executor/Logic Pose Port 唯一应用 correction 并输出 application result。
- [ ] 10.9 现有 adapter 从 application result 构造 CorrectionAck 并由同一 endpoint 发出。
- [ ] 10.10 Owner 不消费自己的 remote snapshot buffer 路径。

## 11. 闭合远端 ExternalFacts 与 ExternalPose

- [ ] 11.1 扩展 ServerAuthoritative MotionSnapshot 保存 velocity、grounded、movement summary 和 accepted sequence。
- [ ] 11.2 新增 model-owned 有界 RemoteSnapshotBuffer，按 server tick 排序并拒绝重复/倒序样本。
- [ ] 11.3 让 Remote binding 保存自己的 snapshot buffer，不把 model packet放进 CharacterPipeline。
- [ ] 11.4 logic tick 前将最新合法 snapshot 转换为 Character `ExternalPoseSample`。
- [ ] 11.5 将 snapshot movement summary 转换为 `ExternalCharacterInputFact` 的 MoveAxis、速度和 facing 输入。
- [ ] 11.6 ExternalFacts InputStage 不读取或 Enable 本地 Input System。
- [ ] 11.7 ExternalPose MotionStage 只应用外部 logic pose，不结算 Graph/Timeline motion。
- [ ] 11.8 Remote Character 只通过 Logic Pose Port 应用 external pose，不配置或调用 LocalSolver Motion Executor，也不产生 correction。
- [ ] 11.9 snapshot stale timeout 后冻结最新确认 pose并提交明确 diagnostics。
- [ ] 11.10 ActorLeft 时清空该 actor 的 snapshot 与 external input 状态。

## 12. 闭合远端 Action 与现有动画链

- [ ] 12.1 新增 model-neutral `ExternalActionActivation` Character 语义合同。
- [ ] 12.2 让 CharacterNetworkReceiveStage 缓存 external activation，不引用 ActionReplication packet。
- [ ] 12.3 让 ActionRuntime 接受显式外部 ActionInstanceId，并拒绝本地冲突 id。
- [ ] 12.4 将 ActionReplication activation 转换为对应 actor 的 ExternalActionActivation。
- [ ] 12.5 让 Corin Action StateMachine 从外部 activation 进入现有 Attack 或 Dodge authoring。
- [ ] 12.6 连续 Attack activation 继续驱动现有 Attack1/Attack2 combo。
- [ ] 12.7 将 terminal replication 转换为同一实例的既有 `ActionLifecycleTransition`。
- [ ] 12.8 重复 replication 不重复激活实例或启动 Timeline。
- [ ] 12.9 远端动作继续提交 AnimationLayerSelection 并进入 Timeline sample、Queue、Lifecycle、Animancer。
- [ ] 12.10 删除任何网络专用 AnimationClip 映射、Animator Controller 或 direct Animancer Play。

## 13. 接入 server-tick 表现采样

- [ ] 13.1 为 GameplayTickSystem 的现有 target hook 增加同一 PresentationFrame 的前置/后置扩展点。
- [ ] 13.2 让 Character binding 在目标 PresentationFrame 前采样自己的 RemoteSnapshotBuffer。
- [ ] 13.3 新增 model-neutral `ExternalPresentationPose` Character 输入端口。
- [ ] 13.4 使用 Join clock、server tick rate 和显式 4 tick delay 计算目标 server time。
- [ ] 13.5 在包围目标时间的两个 snapshot 间插值 position 和 rotation。
- [ ] 13.6 样本不足两个时保持最新确认 pose，不创建无限外推或 LocalSolver fallback。
- [ ] 13.7 Character PresentationStage 在 ExternalPose 模式只把 resolved external visual pose应用到 visual root。
- [ ] 13.8 PresentationStage 不反写 logic root、不调用 Motion Executor 或 Logic Pose Port 写入、不生成 SyncFacts。
- [ ] 13.9 Timeline visual sampling 和 Animancer fade 继续由同一 PresentationFrame/delta 推进。
- [ ] 13.10 diagnostics 展示 buffer size、sample ticks、stale age 和 interpolation state。

## 14. 闭合 Sandbox roster 与相机 ownership

- [ ] 14.1 新增 model-owned NetworkCharacterRosterHost，引用现有 SessionHost、inactive Owner root 和 Corin clone source。
- [ ] 14.2 让 Owner root 在 JoinCompleted 前保持 inactive，不以临时 ActorId 注册。
- [ ] 14.3 JoinCompleted 后先配置 Owner host/binding/spawn pose，再激活 root。
- [ ] 14.4 ActorJoined 时在 inactive staging root 下克隆同一 Corin 层级。
- [ ] 14.5 远端 clone 激活前配置 ExternalFacts + ExternalPose、SubjectActorId、profile 和初始 pose。
- [ ] 14.6 远端 clone 不要求 CameraRig、camera anchors 或 look input。
- [ ] 14.7 本地 CameraRig 只绑定 Owner，不被远端 clone 注册或重置。
- [ ] 14.8 ActorLeft 时按 binding、buffers、pipeline、roster、clone 顺序释放。
- [ ] 14.9 两个角色复用同一 Corin PipelineDefinition、RootTree、Timeline、AnimationPresentation 和动画资源。
- [ ] 14.10 创建 Sandbox Fantasy EndpointDefinition 资产并让现有 model definition 显式引用。
- [ ] 14.11 更新 Sandbox 为唯一 SessionHost + roster host + inactive Owner 装配，不恢复 per-character endpoint/backend 字段。

## 15. 增加健康信息与启动入口

- [ ] 15.1 新增 endpoint connection health，保存状态、运行时长、最近收发和 disconnect reason。
- [ ] 15.2 记录 endpoint/session/actor/roster/snapshot queue 当前值与峰值。
- [ ] 15.3 记录 packet 计数、duplicate、stale、overflow 和 correction。
- [ ] 15.4 可靠 packet 或 roster event overflow 时进入明确 Faulted，不静默丢弃。
- [ ] 15.5 在 SessionHost Inspector/diagnostics 只读聚合正式 health owner。
- [ ] 15.6 在 server Room 低频输出有界 health summary。
- [ ] 15.7 Disconnect/Dispose 输出 final health并释放 timer、queue、Session component。
- [ ] 15.8 增加本地 server 启动脚本，使用正式 Fantasy.config 与 KCP 20000。
- [ ] 15.9 增加双客户端启动脚本，要求显式 client executable、两个正常窗口和独立日志。
- [ ] 15.10 启动脚本不使用 Unity batchmode、不自动重连、不自动重启、不切换 Loopback。

## 16. 清理、编译与严格校验

- [ ] 16.1 删除旧 FrameSync proto、消息、manifest、checksum、confirmed input set 和 opcode 残留。
- [ ] 16.2 删除静态 SessionFacade ownership、字符串 Fantasy mapping 和未使用 facade 路径。
- [ ] 16.3 使用 `rg` 确认没有 GameplaySyncRuntime、IGameplaySyncPeer、backend enum、per-character peer 或 generic packet 回流。
- [ ] 16.4 使用 `rg` 确认没有 legacy parser、FormerlySerializedAs、fallback endpoint、名称搜索、wrapper 或双写。
- [ ] 16.5 使用 `rg` 确认 Character/BTSMTL/Timeline/Animation 不引用 Fantasy message、Session、Room 或 endpoint。
- [ ] 16.6 使用 `rg` 确认 GameLogic Fantasy endpoint 不引用 CharacterPipeline、Graph、Timeline、Animation 或 model policy。
- [ ] 16.7 更新 `openspec/project.md` 的 Current State、Network Boundary、Code Organization 和 Open Questions。
- [ ] 16.8 运行 ProtocolExportTool 并核对生成 client/server 文件与 proto 一致。
- [ ] 16.9 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的命令编译 Server.sln。
- [ ] 16.10 服务端编译后立即执行 `dotnet build-server shutdown`。
- [ ] 16.11 使用相同参数编译 GameProto、GameLogic、Assembly-CSharp 和 Assembly-CSharp-Editor。
- [ ] 16.12 每次客户端编译后立即执行 `dotnet build-server shutdown`。
- [ ] 16.13 运行 `openspec validate add-local-two-client-gameplay-network-closure --strict --no-interactive` 并解决全部问题。
