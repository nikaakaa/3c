## 1. 实施基线与迁移清单

- [x] 1.1 使用 UTF-8 重新读取本 change 的 proposal、design、tasks 和全部 spec delta，确认实施边界未变化
- [x] 1.2 确认current Action/Combo authoring与代码基线已完成编译修复，且本 change 不接管 combo 业务配置
- [x] 1.3 记录当前 Float32/Fixed Program ABI、operation-set version、ProgramHash 与 LayoutHash 生成入口
- [x] 1.4 记录 Float32/Fixed Timeline runtime 的公共方法、Target-specific 数值访问和输出 owner
- [x] 1.5 记录 Float32/Fixed GameplayEffect runtime/state 的公共生命周期、堆叠、period、prediction 与 Target-specific 数值访问
- [x] 1.6 记录 Float32/Fixed Pipeline Transaction 的公共阶段顺序、working state、rollback 与 output disposition
- [x] 1.7 记录当前 ProgramExecutionLayout、Session workspace、Actor workspace 和 Tick 临时集合的创建位置
- [x] 1.8 记录 neutral Core 中所有具体 UnityAuthority/DotRecast Host Profile、factory、manifest 和 identity 引用
- [x] 1.9 记录 Fantasy Unity endpoint 的 control、datagram、checkpoint、prediction evidence、failure 与 dispose 字段/方法归属
- [x] 1.10 记录 Remote selected Body、contact observation、reliable event horizon 和 visual root 的现有数据流
- [x] 1.11 记录四类 Camera Graph node 的 serialized type、字段、端口、emitter 缺口和 PresentationCommand 输出类型
- [x] 1.12 记录三种 Network Test Product 的 Build、Run、output root、server product、manifest 和 launch script 差异
- [x] 1.13 记录 Timeline Editor 的 selection、move、resize、geometry、rendering、preview/live binding 状态
- [x] 1.14 记录 Tree Editor 的 mutation、selection inspector、data catalog、navigation、runtime overlay 和 domain reload 状态
- [x] 1.15 建立 `implementation-inventory.md`，保存迁移前 owner、调用方、待删除类型和产物 identity

## 2. Portable Runtime 合同

- [x] 2.1 定义 portable Timeline control state port，限制其只暴露 Timeline 业务推进所需的 typed 操作
- [x] 2.2 定义 Timeline Target leaf port，承载数值时间、曲线采样、Target state access 和 typed output sink
- [x] 2.3 定义 portable GameplayEffect control state port，覆盖 apply、stack、period、expire、prediction bookkeeping
- [x] 2.4 定义 GameplayEffect Target leaf port，承载 magnitude、modifier 数值运算、typed attribute/state access 和 codec
- [x] 2.5 定义 portable Pipeline Transaction coordinator port，覆盖 ingress、schedule、restore、step、egress、publish、commit
- [x] 2.6 定义 Target transaction port，承载 working state、snapshot、Evaluate、World request/result、Finalize 和 output freeze
- [x] 2.7 定义 ProgramExecutionServices 的 immutable 生命周期和 identity 校验
- [x] 2.8 定义 SessionExecutionWorkspace 的 owner、容量增长、reset 和禁止持久 Gameplay state 规则
- [x] 2.9 定义 ActorExecutionWorkspace 的 facts、trace、timeline segment、GE scratch 和 motion scratch 生命周期
- [x] 2.10 定义 workspace 到 Snapshot/history/published output/diagnostics 的 freeze 或 copy 边界
- [x] 2.11 明确禁止 portable runtime 使用 `object`、reflection、字符串 dispatch 和 Target fallback
- [x] 2.12 将新合同放入 neutral portable source set，并保持 Unity、Float32、Fixed 和普通 .NET 程序集依赖方向

## 3. Timeline Shared Control 迁移

- [x] 3.1 从 Float32/Fixed Timeline runtime 提取 segment 定位和边界顺序的数值无关规则
- [x] 3.2 提取 loop/cycle 进入、推进、结束和跨 cycle 规则
- [x] 3.3 提取 TreeClip/window/cue 的进入、保持、退出和 terminal 顺序
- [x] 3.4 提取 Timeline playback generation、source identity 和 EventId 生成输入顺序
- [x] 3.5 实现唯一 portable Timeline control module
- [x] 3.6 实现 Float32 Timeline Target leaf port
- [x] 3.7 将 Float32 Timeline evaluator 切换到 portable Timeline control module
- [x] 3.8 删除 Float32 runtime 中已迁出的 segment/cycle/window/cue 控制实现
- [x] 3.9 实现 Fixed Timeline Target leaf port
- [x] 3.10 将 Fixed Timeline evaluator 切换到同一 portable Timeline control module
- [x] 3.11 删除 Fixed runtime 中已迁出的 segment/cycle/window/cue 控制实现
- [x] 3.12 对比两种 Target 的 operation trace、terminal、cue 和 source identity 输出结构，消除业务顺序差异
- [x] 3.13 删除旧 Timeline shared helper、复制的控制状态和未被调用的兼容入口
- [x] 3.14 编译 portable Core、Float32 Target 与 Fixed Target，并立即执行 `dotnet build-server shutdown`

## 4. GameplayEffect Shared Control 迁移

- [x] 4.1 从 Float32/Fixed GE runtime 提取 application admission 和 source/target identity 规则
- [x] 4.2 提取 duration、period、expire 和 remove 的数值无关生命周期顺序
- [x] 4.3 提取 stack key、stack limit、refresh、overflow 和 replacement 规则
- [x] 4.4 提取 prediction key、authority confirmation、reject、rollback 和 replay bookkeeping
- [x] 4.5 提取 GameplayEffect cue/fact/EventId 的顺序与输出权限
- [x] 4.6 提取 attribute capture、modifier phase 与 Target 数值计算之间的正式边界
- [x] 4.7 实现唯一 portable GameplayEffect control module
- [x] 4.8 实现 Float32 GameplayEffect Target leaf port
- [x] 4.9 将 Float32 GE runtime/state 切换到 portable control module
- [x] 4.10 删除 Float32 runtime/state 中已迁出的 lifecycle、stack、period 和 prediction 控制实现
- [x] 4.11 实现 Fixed GameplayEffect Target leaf port
- [x] 4.12 将 Fixed GE runtime/state 切换到同一 portable control module
- [x] 4.13 删除 Fixed runtime/state 中已迁出的 lifecycle、stack、period 和 prediction 控制实现
- [x] 4.14 对比 Float32/Fixed 的 GE state transition、fact、cue、prediction identity 与 terminal 输出结构
- [x] 4.15 删除旧 GE bridge、复制 descriptor builder 和未被调用的 Target-specific control helper
- [x] 4.16 编译 portable Core、Float32 Target 与 Fixed Target，并立即执行 `dotnet build-server shutdown`

## 5. Pipeline Transaction Shared Coordinator 迁移

- [x] 5.1 从 Float32/Fixed transaction 提取 ingress 和唯一 schedule producer 顺序
- [x] 5.2 提取 optional restore、零到多个 replay/current step 和 stable ActorId 遍历顺序
- [x] 5.3 提取每 Step 的 Evaluate actors、ResolveBatch、Finalize actors 阶段
- [x] 5.4 提取 egress、atomic publish、external commit 和 OutputDisposition 顺序
- [x] 5.5 提取任一阶段失败时的 working state discard 与 external output 抑制规则
- [x] 5.6 实现唯一 portable Pipeline Transaction coordinator
- [x] 5.7 实现 Float32 Target transaction port
- [x] 5.8 将 Float32 pipeline transaction 切换到 portable coordinator
- [x] 5.9 删除 Float32 transaction 中已迁出的阶段编排和失败处理
- [x] 5.10 实现 Fixed Target transaction port
- [x] 5.11 将 Fixed pipeline transaction 切换到同一 portable coordinator
- [x] 5.12 删除 Fixed transaction 中已迁出的阶段编排和失败处理
- [x] 5.13 确认 coordinator 不读取 Numeric payload、不选择 WorldSolver、不拥有 Network Model policy
- [x] 5.14 删除旧 transaction helper、重复 output collector 和第二 publish/commit 入口
- [x] 5.15 编译 portable Core、Float32 Target 与 Fixed Target，并立即执行 `dotnet build-server shutdown`

## 6. Execution Services 与 Tick Workspace

- [x] 6.1 将 operation topology、SourceMap index、Timeline lookup、GE descriptor/index 和 state access policy 收入 ProgramExecutionServices
- [x] 6.2 在 Program/Layout composition 时一次性构建并校验 ProgramExecutionServices
- [x] 6.3 让同 Program 的多个 Actor 复用同一 immutable ProgramExecutionServices
- [x] 6.4 将 immutable roster 和 stable Actor order 缓存到 Session execution layout
- [x] 6.5 移除 `LocalSingleStepSchedulePass` 每 Tick 创建 ActorId 数组的路径
- [x] 6.6 为 outer transaction 建立 SessionExecutionWorkspace 并定义每次 transaction 的 reset
- [x] 6.7 为每 Actor 建立 ActorExecutionWorkspace 并定义每次 Evaluate 的 reset
- [x] 6.8 将 Kernel facts 与 trace collection 迁入 Actor workspace
- [x] 6.9 将 Timeline segment scratch 迁入 Actor workspace
- [x] 6.10 实现 Timeline 单 segment 无集合 fast path
- [x] 6.11 将 GE 临时排序、reference 和 serialization scratch 迁入明确 workspace
- [x] 6.12 删除稳定 GE descriptor/index 的 Tick 重建
- [x] 6.13 将 transaction completed actors、egress 和 actor-state staging collection 迁入 Session workspace
- [x] 6.14 移除热路径中仅为只读投影创建的 `AsReadOnly`、`ToArray` 和 LINQ materialization
- [x] 6.15 在 published state、Snapshot、history、egress 和 diagnostics 越过 transaction 边界前冻结或复制数据
- [x] 6.16 确认任何 Snapshot/hash/rollback state 都不引用下 Tick 会清空的 workspace memory
- [x] 6.17 为 workspace 容量增长和异常路径建立确定释放/reset 顺序
- [x] 6.18 删除旧临时 collection owner、重复 cache 和跨 Actor 可变共享路径
- [x] 6.19 编译 Simulation Core、Float32、Fixed、Unity Runtime 和 Server portable 程序集，并立即执行 `dotnet build-server shutdown`

## 7. Authority Host Profile 产品所有权

- [x] 7.1 定义 Core 可接受的通用 Host product identity/token 合同
- [x] 7.2 从 neutral Core identity 中移除 `UnityAuthorityWorker` 枚举/常量/factory
- [x] 7.3 从 neutral Core identity 中移除 `DotRecastAuthorityScene` 枚举/常量/factory
- [x] 7.4 将 Unity Authority Host Profile 移入 Unity Authority Product 模块
- [x] 7.5 将 DotRecast Authority Host Profile 移入 DotRecast Authority Product 模块
- [x] 7.6 让 ServerAuthoritative Model 只拥有协议、prediction/authority compatibility 与 gameplay policy identity
- [x] 7.7 让 Unity Product adapter 显式提供 worker host identity、solver capability、launch lowering 和 manifest fields
- [x] 7.8 让 DotRecast Product adapter 显式提供 scene host identity、solver capability、launch lowering 和 manifest fields
- [x] 7.9 分离 client prediction solver compatibility 与 authority backend capability 校验
- [x] 7.10 更新 handshake/room identity 比较以消费 product-owned Host identity
- [x] 7.11 更新 Program/Host/Product manifest 生成和读取 schema
- [x] 7.12 重建 Unity Authority 与 DotRecast 产品 manifest/hash，拒绝旧 schema
- [x] 7.13 删除 Core 中的具体 Profile reader、兼容映射和未使用 factory
- [x] 7.14 搜索 Core 对 ServerAuthoritative/UnityAuthority/DotRecast 具体产品的反向引用并清零
- [x] 7.15 编译 Simulation Core、ServerAuthoritative portable、Unity Authority 与 DotRecast 产品，并立即执行 `dotnet build-server shutdown`

## 8. Fantasy Unity Endpoint 内部模块

- [x] 8.1 定义唯一 `ConnectionCoordinator` 的 endpoint state、failure 和 dispose 所有权
- [x] 8.2 定义 `ControlSessionModule` 的 connect/register/join/roster/heartbeat/leave 输入输出
- [x] 8.3 从 endpoint 迁移 Fantasy control session 建立与 handler registration
- [x] 8.4 从 endpoint 迁移 worker register、client join、roster lock 和 heartbeat 推进
- [x] 8.5 定义 `DatagramChannelModule` 的 ticket、handshake、command 和 snapshot 输入输出
- [x] 8.6 从 endpoint 迁移 UDP socket/channel 创建、ticket 激活和 data-plane handshake
- [x] 8.7 从 endpoint 迁移 command datagram ingress 与 snapshot datagram egress/ingress
- [x] 8.8 定义 `CheckpointReconstructionModule` 的 baseline、delta、full checkpoint 和 sequence 输入输出
- [x] 8.9 从 endpoint 迁移 routine delta checkpoint 重建
- [x] 8.10 从 endpoint 迁移 full checkpoint replacement 与 baseline reset
- [x] 8.11 定义 `PredictionEvidenceModule` 的 ack、observation、liveness 和 metrics 输入输出
- [x] 8.12 从 endpoint 迁移 owner ack、remote body、reliable event horizon 和 prediction evidence 处理
- [x] 8.13 从 endpoint 迁移 endpoint/channel capacity、latency 和 liveness diagnostics
- [x] 8.14 让所有子模块只向 coordinator 返回 typed result/event，不直接切换 endpoint state
- [x] 8.15 让 coordinator 成为共享 session、socket、queue 和 module 的唯一释放入口
- [x] 8.16 保持 Fantasy callback 只验证消息外壳并写 Source queue
- [x] 8.17 删除 endpoint 大类中已迁出的字段、helper、重复 failure/dispose 和第二状态转换入口
- [x] 8.18 确认 endpoint interface、Source port、Fantasy protocol 和唯一 control/datagram 路径未变化
- [x] 8.19 编译 Unity Client、Fantasy Client adapter 与 Server Hotfix 程序集，并立即执行 `dotnet build-server shutdown`

## 9. Remote Visual Pose Convergence

- [x] 9.1 定义 presentation-only remote visual pose state、target、error 和 reset identity
- [x] 9.2 定义 convergence 参数的正式 Presentation Profile 所有权和校验规则
- [x] 9.3 让 filter 只消费已提交 selected Body interval 和 PresentationFrame delta
- [x] 9.4 实现相邻 selected Body frame 的视觉插值
- [x] 9.5 实现 selected target replacement 后从当前 visual pose 有界收敛
- [x] 9.6 实现零 Current step 时继续朝既有 committed target 收敛
- [x] 9.7 实现 HardRecovery/stream reset 清理旧 target、velocity 和 error state
- [x] 9.8 保持 canonical contact 立即使用新 selected Body，不读取 visual pose
- [x] 9.9 保持 reliable Select/Complete/Release/Fact/Cue 继续服从 selected Body horizon
- [x] 9.10 增加 selected tick、target pose、visual pose、error 和 reset reason 的 structured diagnostics
- [x] 9.11 搜索并拒绝 raw authority Body re-selection、独立 Body delay cursor 和 visual-to-WorldSolver writeback
- [x] 9.12 删除旧 remote visual 直接 snap/重复插值 helper，保留唯一 filter 入口
- [x] 9.13 编译 Client Runtime 与 Presentation 程序集，并立即执行 `dotnet build-server shutdown`

## 10. Camera Graph Node 编译闭环

- [x] 10.1 为 RequestCameraState 定义 versioned Program operation payload 和字段校验
- [x] 10.2 为 EmitCameraCue 定义 versioned Program operation payload 和字段校验
- [x] 10.3 为 SetCameraResponse 定义 versioned Program operation payload 和字段校验
- [x] 10.4 为 SetCameraTarget 定义 versioned Program operation payload 和字段校验
- [x] 10.5 为四类 Camera node 注册唯一 CharacterSimulation emitter
- [x] 10.6 将 node authoring identity、Graph/Node source 和端口写入稳定 Source Map
- [x] 10.7 让 Compiler preflight 对缺失字段、未知枚举和不支持 Target 明确失败
- [x] 10.8 在 Float32 Target leaf 中将 Camera operation 转换为强类型 PresentationCommand
- [x] 10.9 在 Fixed Target leaf 中按同一 operation 语义输出对应 PresentationCommand
- [x] 10.10 保持 Camera operation 不写 Character/World state、不调用 Unity Camera/Cinemachine
- [x] 10.11 更新 operation-set version、Program codec/hash 和生成资产 schema
- [x] 10.12 重新生成 Corin Float32/Fixed Program 与 Presentation Projection 正式产物
- [x] 10.13 删除 Camera node 的 runtime throw-only 路径和任何未注册旧 helper
- [x] 10.14 编译 Compiler、Float32、Fixed、Editor 与 Client Runtime 程序集，并立即执行 `dotnet build-server shutdown`
- [x] 10.15 修正Compiler Value端口采集边界：忽略未连接输出、由Value Edge推导已连接泛型输入、只把未连接输入编译为constant binding。

## 11. Network Test Product Build Workflow

- [x] 11.1 定义 Editor-only `INetworkTestProductBuildAdapter` 的产品 identity、输入、输出与 manifest 合同
- [x] 11.2 定义 Build request 与 Run request 为互不隐式调用的独立命令
- [x] 11.3 实现统一外部进程执行器和 stdout/stderr/exit code 报告
- [x] 11.4 为 dotnet/msbuild 固定 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`
- [x] 11.5 在每次 dotnet/msbuild 完成后立即执行 `dotnet build-server shutdown`
- [x] 11.6 实现唯一临时输出目录创建与同产品原子替换流程
- [x] 11.7 实现不同 Product 使用互斥正式 output root 的校验
- [x] 11.8 实现 exact file closure 和未声明文件拒绝规则
- [x] 11.9 提取独立 `ServerProductBuildManifestUtility`
- [x] 11.10 实现 build manifest identity、scene list、server product 和 launch script 校验
- [x] 11.11 实现 Unity Authority Product adapter
- [x] 11.12 将 Unity Authority Build 菜单迁入统一 workflow
- [x] 11.13 将 Unity Authority Run 菜单改为只消费已校验 build
- [x] 11.14 实现 DotRecast Authority Product adapter
- [x] 11.15 将 DotRecast Build 菜单迁入统一 workflow
- [x] 11.16 将 DotRecast Run 菜单改为只消费已校验 build
- [x] 11.17 实现 Deterministic Rollback Product adapter 的 no-Fantasy-server 明确形态
- [x] 11.18 将 Deterministic Rollback Build 菜单迁入统一 workflow
- [x] 11.19 将 Deterministic Rollback Run 菜单改为只消费已校验 build
- [x] 11.20 保持同种 Product Build 覆盖自身输出且不同 Product 不互相覆盖
- [x] 11.21 删除三个旧工具中的 ReplaceDirectory、RunProcess、manifest、exact-file 和隐式 Build/Run 重复实现
- [x] 11.22 搜索并删除 runtime Network Model switching、shared output 和旧 build helper 路径
- [x] 11.23 编译 Editor、Unity Authority、DotRecast 与 Deterministic Rollback build tooling，并立即执行 `dotnet build-server shutdown`

## 12. Timeline Editor 内部模块

- [x] 12.1 定义 Timeline interaction state 的 selection、drag、move、resize 和 capture 所有权
- [x] 12.2 从 `TimelineFieldView` 迁移 selection 与 multi-selection 状态
- [x] 12.3 从 `TimelineFieldView` 迁移 clip move/resize 事务和 Undo 边界
- [x] 12.4 定义 frame geometry 的 time/frame conversion、clip rect、overlap 和 hit-test API
- [x] 12.5 从 `TimelineFieldView` 迁移 geometry 与 overlap 计算
- [x] 12.6 定义 Timeline rendering 的 track、clip、playhead 和 overlay 输入
- [x] 12.7 从 `TimelineFieldView` 迁移 authoring track/clip rendering
- [x] 12.8 从 `TimelineFieldView` 迁移 Authoring Preview overlay rendering
- [x] 12.9 从 `TimelineFieldView` 迁移 Live Debug overlay rendering
- [x] 12.10 保持 Timeline asset/track/clip stable identity 和 Source Map 不变
- [x] 12.11 保持 Inspector selection、右侧设置、preview/live mode 和 tab/window binding 不变
- [x] 12.12 保持 domain reload locator 恢复和无效 identity fail-stop 行为
- [x] 12.13 删除 `TimelineFieldView` 中已迁出的状态、geometry、render helper 和第二 owner
- [x] 12.14 编译 BTSMTL/Timeline Editor 程序集，并立即执行 `dotnet build-server shutdown`

## 13. Tree Editor 内部模块

- [x] 13.1 定义 graph mutation service 的 create/link/delete/paste/condition cleanup 与 Undo 所有权
- [x] 13.2 从 `BaseTreeView` 迁移 node/edge create 和 link mutation
- [x] 13.3 从 `BaseTreeView` 迁移 delete、paste 和 identity regeneration
- [x] 13.4 从 `BaseTreeView` 迁移 transition condition graph cleanup
- [x] 13.5 让 `BaseTreeView` 只负责 node/edge visual、selection forwarding 和 mutation service 调用
- [x] 13.6 定义 selection inspector content 的 Node/Edge/Graph Settings 所有权
- [x] 13.7 从 `BaseTreeInspectorView` 迁移 Node authoring inspector
- [x] 13.8 从 `BaseTreeInspectorView` 迁移 Edge/Transition authoring inspector
- [x] 13.9 从 `BaseTreeInspectorView` 迁移无选择 Graph Authoring Settings
- [x] 13.10 定义 Graph Data Catalog 的 Input/Blackboard source filter 和 editor-only view state
- [x] 13.11 从 `BaseTreeInspectorView` 迁移 Data Catalog 与窄栏筛选
- [x] 13.12 定义 TreeWindow navigation/page state、Graph locator 和 domain reload restore 所有权
- [x] 13.13 从 `BaseTreeWindow` 迁移 Graph navigation、page/tab 和 serialized locator 状态
- [x] 13.14 定义 TreeWindow runtime overlay 的 shared debug session 与 window-local binding 所有权
- [x] 13.15 从 `BaseTreeWindow` 迁移 Live Debug attach/follow/pin/capture overlay
- [x] 13.16 保持 Data/Inspector 信息架构、Authoring/Live Debug 窗口模式和只读规则不变
- [x] 13.17 保持 Graph 与 Timeline 双窗口 binding 相互独立
- [x] 13.18 保持 asset identity、property path、Undo/Redo 和无效 locator fail-stop 行为
- [x] 13.19 删除四个大类中已迁出的状态、mutation、catalog、navigation 和 overlay helper
- [x] 13.20 编译 BTSMTL Tree/Timeline Editor 与 Assembly-CSharp-Editor，并立即执行 `dotnet build-server shutdown`

## 14. 清理、文档与最终校验

- [x] 14.1 搜索并删除旧 Float32/Fixed Timeline control 复制实现
- [x] 14.2 搜索并删除旧 Float32/Fixed GameplayEffect control 复制实现
- [x] 14.3 搜索并删除旧 Float32/Fixed Pipeline Transaction coordinator 复制实现
- [x] 14.4 搜索并删除 Core 中具体 UnityAuthority/DotRecast Host Profile 与旧 manifest reader
- [x] 14.5 搜索并删除 remote Body 第二选择、旧 snap helper 和 visual writeback 路径
- [x] 14.6 搜索并删除 Camera throw-only、未注册 emitter 和旧 operation schema
- [x] 14.7 搜索并删除 Fantasy endpoint 已迁出 helper、第二 failure/dispose owner 和废弃字段
- [x] 14.8 搜索并删除三种 Build/Run 工具的重复 process/directory/manifest helper
- [x] 14.9 搜索并删除 Tree/Timeline Editor 中迁移后未引用的旧 helper 和第二状态 owner
- [x] 14.10 确认没有 fallback、compatibility、legacy parser、temporary bridge、双写入口或运行时 model switching
- [x] 14.11 更新 `implementation-inventory.md`，逐项记录新 owner、输入、输出和删除的旧路径
- [x] 14.12 更新 `openspec/project.md` 的 Numeric Target、Host Product、Remote Presentation、Editor 和 Build Workflow 边界
- [x] 14.13 更新受影响的 current architecture 文档，删除与新唯一链路冲突的过时描述
- [x] 14.14 使用带规定参数的 `dotnet build` 编译 portable Core、Float32、Fixed、Unity Runtime、Server products 和 Editor，并在每次编译后立即 shutdown build server
- [x] 14.15 使用非交互编译检查确认普通 .NET Server/reader 不引用 Unity Editor 或 UnityEngine
- [x] 14.16 运行 `openspec validate refactor-gameplay-runtime-and-tooling-modules --strict --no-interactive`
- [x] 14.17 确认全部任务真实完成后将本文件所有任务标记为 `[x]`
