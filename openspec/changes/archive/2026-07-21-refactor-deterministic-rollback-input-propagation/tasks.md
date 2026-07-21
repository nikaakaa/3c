## 1. 迁移清单与并行改动对齐

- [x] 1.1 重新读取本 change 的 proposal、design、tasks 和全部 spec delta。
- [x] 1.2 检查并行 Agent 对 Rollback runtime、Presentation、Build workflow 和 Corin 输入资产的最新改动。
- [x] 1.3 建立 Unity Host code、scene、role、argument、serialized field、diagnostics 和 build manifest 的精确删除清单。
- [x] 1.4 建立 `InputDelayTicks` 在 policy、identity、pipeline、overlay、asset 和文档中的精确引用清单。
- [x] 1.5 建立 ActorInputBatch、canonical bundle、confirmation、snapshot routing 的当前协议 identity 清单。
- [x] 1.6 确认 portable DeterministicRollback 与 Endpoint source set 不引用 UnityEngine、Fantasy 或 ServerAuthoritative。
- [x] 1.7 确认目标修改可沿现有 Session Source、Pipeline、Committer 和 Presentation 主链完成，不需要第二套 Gameplay 路径。
- [x] 1.8 记录两个Network Model与三个Network Test Product的精确归属。
- [x] 1.9 记录三个产品当前Player、Server、manifest、进程角色与固定输出目录。
- [x] 1.10 记录schema v1 `player/server/hostIdentity`与`ServerShape/NoServer`全部引用。

## 2. 输入 Timing Authoring

- [x] 2.1 为动作 request 定义稳定的 `Immediate` 与 `Offensive` timing class。
- [x] 2.2 将 timing class 加入 `CharacterActionRequestDefinition` 的唯一 serialized authoring。
- [x] 2.3 将 timing class 暴露为只读运行配置，不写入 BTSMTL node 或 Program operation。
- [x] 2.4 在 `CharacterInputProfile` 配置校验中拒绝未定义 timing class。
- [x] 2.5 在输入配置 Inspector 中显示 timing class，并保持 request id、source action、buffer 和 priority 的现有编辑语义。
- [x] 2.6 更新 Agent snapshot/export schema，使动作 request timing class 可读。
- [x] 2.7 更新 Agent patch schema、lowerer、handler、emitter 和 validator，使 timing class 可正式写入。
- [x] 2.8 更新 Agent MCP bridge 的 CharacterInputProfile 合同，不增加字符串 fallback。
- [x] 2.9 将 Corin Attack request 配置为 `Offensive`。
- [x] 2.10 将 Corin 非进攻 request 按业务事实配置为 `Immediate`，不得按 request 名在运行时代码猜测。

## 3. Rollback Timing Policy

- [x] 3.1 从 `DeterministicRollbackModelPolicy` 删除 `InputDelayTicks`。
- [x] 3.2 增加 `OffensiveRequestDelayTicks` 并限制为非负 Tick 数。
- [x] 3.3 保留独立 `ConfirmationDelayTicks`，删除其必须大于等于旧 input delay 的耦合校验。
- [x] 3.4 将 request timing 与 confirmation timing 分别写入 model configuration hash。
- [x] 3.5 更新 Rollback model、pipeline、endpoint 和 protocol revision identity。
- [x] 3.6 从 `DeterministicRollbackPipelineDefinition` 删除 `m_InputDelayTicks`。
- [x] 3.7 在 `DeterministicRollbackPipelineDefinition` 增加正式 `m_OffensiveRequestDelayTicks`。
- [x] 3.8 将 Corin Rollback pipeline 的 offensive delay 配置为 2 Tick。
- [x] 3.9 保持 Corin Rollback pipeline 的 confirmation delay 为独立正式值。
- [x] 3.10 删除旧 asset 字段和旧值迁移 fallback，直接保存新正式配置。

## 4. 单一输入阶段身份

- [x] 4.1 定义 Captured、Relayed Explicit、Predicted、Canonical 和 Confirmed 的输入阶段合同。
- [x] 4.2 将 `ActorId + SimulationTick + InputSequence + GameplayHash` 固定为跨阶段输入身份。
- [x] 4.3 收敛 `RollbackInputProvenance`，明确显式输入与预测输入的区别。
- [x] 4.4 明确同 GameplayHash 的 provenance 晋升不构成 Gameplay change。
- [x] 4.5 明确同一 Actor/Tick/Sequence 的不同 GameplayHash 构成协议冲突。
- [x] 4.6 让 protocol codec 编解码新的阶段身份和消息种类。
- [x] 4.7 删除旧 protocol version decoder 和兼容分支。
- [x] 4.8 更新 protocol schema hash，使旧 Player 与新 Relay 在 handshake 时直接拒绝互连。

## 5. 原始输入立即中继

- [x] 5.1 将 portable host runtime 重命名并收敛为 `RollbackInputRelayRuntime` 语义。
- [x] 5.2 Relay 在接收 ActorInputBatch 时校验 Peer、Actor、Tick、sequence、provenance 和容量。
- [x] 5.3 Relay 按 Actor/Tick/sequence/hash 去重冗余 frame。
- [x] 5.4 Relay 在校验后立即向其它 Peer 转发 relayed explicit frame。
- [x] 5.5 Relay 转发不得等待同 Tick roster 输入齐备。
- [x] 5.6 Relay 转发不得等待 canonical clock 或 confirmation delay。
- [x] 5.7 Relay 将同一 frame identity 提交给 canonical assembler，不创建第二份 command。
- [x] 5.8 Relay 拒绝一个 Peer 提交其它 Actor 的输入。
- [x] 5.9 Relay 对同身份不同 GameplayHash 关闭 session并记录冲突事实。
- [x] 5.10 保留现有输入批次冗余，确保后续 batch 能补发前序 Tick。
- [x] 5.11 将输入冗余数定义为上限，并按单个 unreliable datagram 预算发送包含当前 Tick 的最大连续后缀。
- [x] 5.12 当前 Tick 单帧超过 datagram payload 预算时精确失败，不调大 MTU、不分片 unreliable input、不静默丢字段。

## 6. Canonical 与 Confirmation 解耦

- [x] 6.1 删除 canonical epoch 对 `NextTick + InputDelayTicks` 的 lead 检查。
- [x] 6.2 Canonical assembler 只在当前 NextTick 的完整 roster 显式输入齐备时生成 bundle。
- [x] 6.3 Canonical bundle 固定按 Tick 和 stable ActorId 排序。
- [x] 6.4 Canonical bundle 生成后保持不可变，不保留普通 revision queue。
- [x] 6.5 删除因 provenance/sequence 变化产生普通 revision broadcast 的旧逻辑。
- [x] 6.6 保留 canonical contiguous frontier，并在缺少任一 Actor input 时停止推进。
- [x] 6.7 Peer 允许在 MaximumRollbackDepth 内越过暂时停住的 canonical frontier继续预测。
- [x] 6.8 Confirmation 只从完整不可变 canonical 区间推进。
- [x] 6.9 Confirmation 继续可靠携带完整最终 bundle 区间。
- [x] 6.10 Relay 拒绝 confirmed Tick 的任何后续 GameplayHash 变化。
- [x] 6.11 Snapshot request/response 继续由 Relay 路由，不让 Relay 持有 WorldState。

## 7. Peer Explicit Input History

- [x] 7.1 在 Rollback Endpoint 接收 relayed explicit frame 消息。
- [x] 7.2 为每个远端 Actor 建立有界、按 Tick 索引的 explicit input history。
- [x] 7.3 对乱序和冗余 relayed frame 做稳定去重。
- [x] 7.4 对同 Tick 更新记录 earliest affected tick。
- [x] 7.5 构造当前 predicted bundle 时优先使用目标 Tick 的 exact relayed input。
- [x] 7.6 exact input 缺失时只延续最近连续 values。
- [x] 7.7 exact input 缺失时始终生成空 request 列表，不预测离散动作。
- [x] 7.8 canonical bundle 到达时将相同 GameplayHash 的 explicit frame晋升为 canonical。
- [x] 7.9 provenance-only 晋升不得产生 replay directive。
- [x] 7.10 canonical GameplayHash 与已执行 predicted input 不同时，从最早受影响 Tick 请求 restore/replay。
- [x] 7.11 confirmed frontier 推进时裁剪不再需要的 explicit/canonical history。
- [x] 7.12 history capacity 耗尽时明确失败，不改成无界容器或静默丢弃未确认输入。

## 8. 进攻 Request 调度

- [x] 8.1 在 Unity Fixed Rollback input adapter 中建立模型专属 pending request schedule。
- [x] 8.2 捕获 Immediate request 时计算当前 eligible tick。
- [x] 8.3 捕获 Offensive request 时按 policy 计算 `capture tick + OffensiveRequestDelayTicks`。
- [x] 8.4 为 pending request 保留稳定 request id、capture sequence、capture tick 和 eligible tick。
- [x] 8.5 同一离散 request 序列按 capture sequence 排序。
- [x] 8.6 后捕获 request 不得越过尚未到期的前序 Offensive request。
- [x] 8.7 连续 Move、Look 和 held values 不进入 pending request schedule。
- [x] 8.8 只有 eligible request 才写入当前正式 Fixed CharacterSimulationInput。
- [x] 8.9 pending request schedule 进入 Rollback Source checkpoint/restore 所有权。
- [x] 8.10 replay 只消费已经写入 input history 的 request，不重新读取 InputAction。
- [x] 8.11 Local Float32 input adapter 保持现有即时 request 行为。
- [x] 8.12 删除按 `Attack`、`Dodge` 或其它 request 字符串判断 timing 的可能路径。

## 9. Pipeline 与原子 Replay

- [x] 9.1 更新 Rollback ingress batch，使 relayed explicit arrival 与 canonical/confirmation 具有明确顺序。
- [x] 9.2 在同一个 Source Read 中先登记 explicit frame，再计算 affected tick。
- [x] 9.3 Schedule 对 earliest affected tick 生成唯一 restore directive。
- [x] 9.4 同一 outer transaction 合并多个晚到 frame，只恢复到最早受影响 Tick一次。
- [x] 9.5 Replay 与 current step 继续复用同一 Fixed Program、Kernel、KCC 和 world transaction。
- [x] 9.6 相同 GameplayHash 的阶段晋升只推进 frontier，不执行 world replay。
- [x] 9.7 预测领先达到 MaximumRollbackDepth 时返回 NoStep，不丢 input history。
- [x] 9.8 Source snapshot 保存 explicit frontier、canonical frontier、confirmed frontier 和 request schedule。
- [x] 9.9 Restore 不回退 confirmed frontier 和已确认 output 边界。
- [x] 9.10 删除 canonical-only 首次输入投递的旧 Source 分支。

## 10. Body 与动画表现

- [x] 10.1 保持 Body 和动画只消费 Fixed simulation output，不读取网络 packet。
- [x] 10.2 Rollback output adapter 在 replay outer transaction结束后只提交最终 Body 分支。
- [x] 10.3 Rollback output adapter 在同一事务提交最终 animation producer lifecycle。
- [x] 10.4 provenance-only canonical 晋升不得生成 Body replacement。
- [x] 10.5 provenance-only canonical 晋升不得生成 animation replace/retire。
- [x] 10.6 真正 GameplayHash 修正时保持 Body 与动画原子替换。
- [x] 10.7 远端移动循环动画继续从 predicted current producer 与高帧率 sample time 推进。
- [x] 10.8 confirmed-only cue 继续等待 confirmed horizon。
- [x] 10.9 `CharacterVisualTrajectoryFollower` 只消费最终 branch replacement 误差。
- [x] 10.10 删除任何以固定 confirmed delay 驱动远端 Body 或动画的路径。

## 11. 纯.NET Dedicated Relay Server产品

- [x] 11.1 在`3cDemo/Server/Products/DeterministicRollback`建立受版本控制的`ThirdPerson.DeterministicRollback.Server` .NET 8 executable project。
- [x] 11.2 Server project只引用portable Core、Fixed identity、DeterministicRollback和Endpoint source set。
- [x] 11.3 Server project不引用UnityEngine、Unity assemblies、Fantasy、ServerAuthoritative或DotRecast产品。
- [x] 11.4 建立Dedicated Relay Server process entrypoint、参数解析和明确退出码。
- [x] 11.5 建立portable `DeterministicRollbackServerManifest` schema与canonical hash。
- [x] 11.6 Manifest写入session、endpoint、Client/Actor roster、model/protocol和deterministic identities。
- [x] 11.7 Manifest写入confirmation、capacity和snapshot source policy。
- [x] 11.8 Server启动时一次性读取并严格校验manifest。
- [x] 11.9 Server不读取Unity Asset、Scene、Program bytes、Collision artifact或prefab。
- [x] 11.10 Server不执行SimulationTick、Program、KCC、Presentation或Gameplay output。
- [x] 11.11 Server文件日志写入RunId对应日志目录，不修改ProductRoot。
- [x] 11.12 将`RollbackCanonicalInputHost`重命名并收敛为唯一`RollbackInputRelayRuntime`。
- [x] 11.13 将`HostPeerId`、`m_HostHandshake`及Rollback协议身份迁移为RelayServer命名。
- [x] 11.14 删除旧Host类型、字段、序列化键和协议身份别名。

## 12. Network Test Product schema v2与Build

- [x] 12.1 定义model-neutral Runtime Artifact RoleId、Kind、ProductId、root、entrypoint和configuration identity。
- [x] 12.2 将Artifact Kind限制为`UnityPlayer`、`ManagedExecutable`等启动载体，不表达具体产品或Network Model。
- [x] 12.3 定义artifact manifest path/hash、字段稳定排序和路径约束。
- [x] 12.4 将Network Test Product manifest升级为schema v2 `artifacts[]`。
- [x] 12.5 增加顶层NetworkModelIdentity与RuntimeTopologyIdentity。
- [x] 12.6 删除顶层`hostIdentity`、固定`player/server`字段和`ServerShape`。
- [x] 12.7 删除`NetworkTestServerProductResult`与`NoServer`语义对象。
- [x] 12.8 将adapter接口的`BuildServer`替换为零到多个附加artifact发布合同。
- [x] 12.9 将公共Network Test Product类型迁出`UnityAuthority`命名文件。
- [x] 12.10 公共workflow只拥有Unity Player build、staging、candidate validation、exact closure和原子替换。
- [x] 12.11 公共workflow只校验adapter返回的artifact identity、entrypoint、closure和hash。
- [x] 12.12 公共workflow拒绝重复RoleId、ProductId、输出root和路径逃逸。
- [x] 12.13 公共workflow不引用具体Server Product、Rollback Relay concrete type或产品目录常量。
- [x] 12.14 将Unity Authority Player登记为`unity-player` artifact。
- [x] 12.15 将Unity Authority Fantasy产品登记为`unity-authority-gate-server` artifact。
- [x] 12.16 保持Unity Authority ServerProductId、四进程拓扑与业务行为不变。
- [x] 12.17 将DotRecast Client Player登记为`unity-client-player` artifact。
- [x] 12.18 将DotRecast Fantasy产品登记为`dotrecast-authority-server` artifact。
- [x] 12.19 保持DotRecast ServerProductId、三进程拓扑与业务行为不变。
- [x] 12.20 Rollback adapter负责publish Server executable到临时`Server`目录。
- [x] 12.21 Rollback adapter负责从正式authoring导出Server runtime manifest。
- [x] 12.22 Rollback adapter将Player和`deterministic-relay-server`两个artifact写入schema v2 manifest。
- [x] 12.23 Rollback Player scene closure只保留Bootstrap与Peer Scene。
- [x] 12.24 Build继续使用临时目录和原子替换，同种产品覆盖且三个产品互不覆盖。
- [x] 12.25 Run只接受schema v2 manifest及匹配的Server executable与Player closure。
- [x] 12.26 删除schema v1 parser、自动迁移和兼容读取。
- [x] 12.27 Run不编译、publish、复制、导出配置或自动修复缺失文件。

## 13. Unity Demo 资产与启动

- [x] 13.1 从 Bootstrap role enum 删除 `CanonicalHost`。
- [x] 13.2 从 Bootstrap serialized config 删除 canonical host scene 名称。
- [x] 13.3 Bootstrap 只根据 peer profile进入唯一 Peer Scene。
- [x] 13.4 删除 `DeterministicRollbackCanonicalHostBehaviour` 及其 `.meta`。
- [x] 13.5 删除 `DeterministicRollbackCanonicalHost.unity` 及其 `.meta`。
- [x] 13.6 删除 Editor 的 `BuildCanonicalHostScene` 和 Host Scene 配置逻辑。
- [x] 13.7 删除 Player 参数 `--deterministic-rollback-role=host`。
- [x] 13.8 保留并严格要求 Peer A/B 的显式 profile 参数。
- [x] 13.9 更新启动脚本，先启动Dedicated Relay Server，再启动Client A与Client B Player。
- [x] 13.10 更新StopExisting，只清理本产品Server与带Rollback peer profile的两个Client Player。
- [x] 13.11 更新进程存活和endpoint ready校验为Server、Client A、Client B。
- [x] 13.12 更新日志命名，删除 host Unity log并增加 relay log。
- [x] 13.13 更新 ExpectedScenes 和产品 manifest，拒绝包含旧 Host Scene 的产物。

## 14. Diagnostics 收敛

- [x] 14.1 删除含糊的单一 `InputDelayTicks` overlay 字段。
- [x] 14.2 显示 OffensiveRequestDelayTicks 与 ConfirmationDelayTicks。
- [x] 14.3 显示 pending offensive request count、oldest capture tick 和 eligible tick。
- [x] 14.4 显示 relayed explicit input 的 arrival lead/late tick。
- [x] 14.5 显示每个远端 Actor 的 exact-input hit 与 predicted fallback 计数。
- [x] 14.6 显示 provenance-only canonical promotion 计数。
- [x] 14.7 显示 explicit GameplayHash correction、earliest affected tick 和 replay depth。
- [x] 14.8 显示 Body/animation branch replacement 与 follower correction magnitude。
- [x] 14.9 Relay 显示 rx、forward、dedupe、invalid、canonical 和 confirmed frontier。
- [x] 14.10 Diagnostics 保持只读，不改变 input、history、simulation 或 presentation。

## 15. 废弃路径删除与文档统一

- [x] 15.1 删除 `RollbackCanonicalInputHost` 旧类型和命名引用。
- [x] 15.2 删除 pending canonical revision queue 和 revision broadcast 旧路径。
- [x] 15.3 删除 canonical epoch input-delay lead 旧字段和代码。
- [x] 15.4 删除 Unity Host profile、scene、role、argument 和 inspector 残留。
- [x] 15.5 删除旧 protocol codec、identity 和 handshake 兼容路径。
- [x] 15.6 删除任何 ServerAuthoritative/Fantasy 类型进入 Rollback Relay 的引用。
- [x] 15.7 删除任何 remote Transform/snapshot correction 或动画网络状态路径。
- [x] 15.8 更新 `openspec/project.md` 的 Rollback 产品、进程拓扑和输入生命周期描述。
- [x] 15.9 更新受影响的 current architecture 文档，删除“三个 Unity 进程”和“全局 4 Tick input delay”描述。
- [x] 15.10 全局搜索确认旧 Host 名称、旧 scene、旧 role 和旧 `InputDelayTicks` 不再存在。
- [x] 15.11 全局搜索确认 Rollback Relay 不引用 UnityEngine、Fantasy 或 ServerAuthoritative。
- [x] 15.12 更新`openspec/project.md`的两个Network Model、三个Network Test Product与DS进程拓扑。
- [x] 15.13 更新受影响current specs中的Canonical Host、NoServer与schema v1过时描述。
- [x] 15.14 全局搜索确认公共Build Workflow不引用具体adapter、Server Product或Network Model。
- [x] 15.15 全局搜索确认顶层`hostIdentity`、`ServerShape`与`NoServer`不再存在。

## 16. 编译与规范校验

- [x] 16.1 构建 portable Core、Fixed、DeterministicRollback 和 Endpoint 工程，使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 16.2 构建`ThirdPerson.DeterministicRollback.Server`，使用相同build server参数。
- [x] 16.3 构建受影响的 Unity C# runtime/editor 工程，使用相同 build server 参数。
- [x] 16.4 每次编译后立即执行 `dotnet build-server shutdown`。
- [x] 16.5 确认 Rollback product exact closure 不包含 Host Scene 或第三个 Unity Player role。
- [x] 16.6 确认 tasks 勾选与实际统一代码链路一致。
- [x] 16.7 运行 `openspec validate refactor-deterministic-rollback-input-propagation --strict --no-interactive`。
