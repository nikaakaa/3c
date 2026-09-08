## Context

动机与范围见[proposal.md](proposal.md)。本设计中的 Editor“预览”仅表示观察 Unity Play Mode 中已经运行的真实角色，不表示创建场景、预览角色或独立播放器。

当前 Pose 已有不可变 Program Image、每 Actor Execution View／状态／帧页、Burst Kernel、跨 Actor Job 批次、Source／Constraint／Final Publication 与根帧事务。FlowCanvas 的原生节点编辑和端口显示可以复用，但它的 getter、协程与更新循环不替代上述运行实现。

已核对本地 FlowNode、BinderConnection 和 FlowScript：原生连线闪烁与传值缓存来自其自身的执行回调，不能直接冒充项目 Native runtime 的结果。所需接入是显式、只读的外部观测显示接口。

## Goals / Non-Goals

**Goals:**

- 作者只编辑一份正式 Pose 图，原生交互与编译输入使用同一节点、端口和边身份。
- Play 中打开窗口即可发现合法运行目标；绑定目标后自动随实际完成帧更新，不需要另点“运行 Pose 图”。
- 已完成的 Pose／Native 重构继续使用；修改作者图只影响后续显式 Build，不热换正在执行的产物。
- 观察只影响诊断订阅与显示，不影响角色执行次数、时间、Job 顺序或骨骼输出。

**Non-Goals:**

- 不迁移技能、AI、Gameplay／网络执行，不改 Foot、IK、混合或动画采样算法。
- 不增加 Scene Play 协调器依赖、独立预览场景、假输入、窗口播放器、seek、窗口单步或节点断点。
- 不采用 FlowCanvas runtime，不统一替换现有子图为 Macro，不为 UI 接入主动升级 Document v5。

## Decisions

### 1. 一份作者图，直接编译

```text
FlowCanvas Pose作者图
  → 唯一只读作者遍历／现有Compiler
  → Program Image与Presentation Projection
  → 原Source／Pose／Constraint／Final Publication运行链
```

FlowGraph／FlowNode／BinderConnection 是正式作者资产的基础，原生端口注册读取唯一 Capability 与 Port Shape。Compiler 读取持久化字段和连接，不能调用 getter 获取编译值，也不能构建旧 CharacterPoseCanvasGraph／旧 typed graph 的中间副本。历史类名是否保留按最终职责决定，不能靠改名冒充迁移。

业务取舍：直接采用原生 runtime 会获得执行回调，但要重做缓存、Job依赖及提交边界；本方案保留这些已有成果，代价是维护作者图到编译数据的读取和诊断映射。编译产物不是第二份可编辑作者图。

### 2. 原生交互与领域写入分责

复用原生节点框、端口已连接／未连接状态、类型显示、布局、命中、连线、框选、复制、Undo 和导航。Pose 提供字段、图角色、严格空间类型和业务菜单；状态／规则页面保持原语义和唯一存储，不凭第三方同名节点推定等价。

逐项核对 Create、Connect、Reconnect、Paste、Delete、Rename、Move 和字段修改，进入既有 typed Mutation 预检及实际序列化 owner。缺少拦截点时只在共享框架增加领域无关钩子，不复制一份编辑器或添加第二写入服务。人工操作和 Document 整包事务各有一个 Undo owner，内部 handler 不重复记账。

运行观察视图只读。退出观察回到作者模式后可编辑；如果图已改变，旧运行结果停止叠加并提示版本不匹配，不能热写运行实例。

### 3. Play Mode 自动观察，不管理场景

```text
现有游戏入口 → 真实Actor → 原编译／Native运行
                                  ↓ 已完成诊断
                     版本／实例／调用点映射
                                  ↓
                       FlowCanvas窗口显示
```

窗口监听 Play 状态和正式运行实例注册／移除。已有明确观察目标且身份有效时自动绑定；只有一个与当前 Definition 精确匹配的实例时可自动选定，多个匹配实例则显示选择器，不按名称、当前 Selection 或扫描 Transform 猜角色。仅打开裸图且无法确定角色上下文时显示“选择运行角色”。

状态固定为：未播放、等待目标、观察中、版本不匹配、目标已结束。进入 Play 或在 Play 中打开窗口均可绑定；停止 Play、关闭窗口、脚本重载、实例替换／销毁均释放订阅。Unity 自身暂停时保留最后完成帧；恢复后随新结果更新。窗口不调用 Play、Stop、Pause、Step 或场景加载来控制游戏。

### 4. 观察绑定要能区分角色与子图调用

绑定键包含 Session、Actor、实例 generation、Projection／Pose Program identity、作者 revision、Rig identity 及 call-site。只观察当前目标的同版本结果；同一子图多处调用时导航携带调用位置，不能把多个调用的状态合并。

Source Map 随对应产物发布，表达节点到多条 operation、输出端口到值以及调用位置的关系。编译优化掉的节点明确显示“已优化／无独立运行操作”，未知映射显示不可用。不能用 operation 数组位置、显示名或连线几何猜测身份。

编译算法、操作布局与 Worker 顺序保持；必要的诊断元数据增量单独说明。新旧源码或布局 Hash 不同不直接等于行为回归，但不允许版本混用。

### 5. 适配结果显示，不借原生执行造高亮

原生 renderer 通过外部只读观测源取得节点状态、边读取事实、端口值和帧号；有必要时增加共享 editor-only 显示钩子。不得调用 FlowScript、BindPorts、值 getter 或修改 Graph.isRunning 来激活高亮。

Pose“参与求值”“贡献权重非零”“等待 Source”“已完成”“不可用”分别表达。值来自运行时按兴趣冻结的结果；未订阅、覆盖、失效与数值0分开显示。不存在边读取证据时不播放执行动画，也不把两个端点都执行过当作该边被读取。

Runtime 在现有完成／Seal边界发布有界快照或受控只读租约。Editor 不直接读正在写入的 Native 页、不调用 JobHandle.Complete、不保留跨失效边界的 NativeArray。普通重绘和悬停只读已取得的显示数据，避免 OnInspectorGUI 重操作。诊断未订阅时不创建无界记录；新订阅从下一个可用完成帧生效。

业务取舍：与原生 getter 调试相比，需要少量外部观测适配，但不会为看一个值再次推进动画。成本应测量诊断开启前后的时间和分配，不能预先承诺零开销。

### 6. Document与资产迁移维持正式闭包

保持现有 v5 的图／布局、Source Slot／Binding、状态／规则等业务表达，替换底层 owner 适配；不暴露第三方私有序列化字段或运行委托。Graph及节点稳定身份可保留时保留，新增子资产引用由正式事务分配并反向导出。

迁移只处理精确 Corin Definition 的全部可达闭包。历史核对为8张Pose图、25个节点、12种实际使用能力；实施时重读，不把旧数量硬编码成迁移规则。29种注册能力按正式目录逐项核对，不能只接通Clip／Blend就宣称作者系统完整。

迁移计划覆盖根、子图、状态机、Profile引用及实际修改的共享owner，全部成功后保存与反向发布。无业务变更的Document往返应为零Mutation；失败完整回滚，不发布半迁移资产。后续技能提案若升级唯一Document合同，本提案在实施前重读并适配其正式版本，不引入双版本reader或重复迁移。

### 7. 规范与在途变更对账

| 来源 | 当前冲突或边界 | 处理 |
|---|---|---|
| project.md／Pose编译与runtime规范 | 要求Program Image、Native、Worker与唯一帧输出 | 保留，不产生原生FlowCanvas业务执行 |
| Pose工作区／UI规范 | 固定旧GraphView与专用画布 | 本提案delta改为原生作者UI，保留领域信息和稳定身份 |
| 共享领域框架 | 禁止共享序列化基类，并强制旧UI原地抽取 | 对Pose调整实现限制；共享领域合同与其它领域既有行为保留 |
| Pose Preview规范 | 允许独立Preview运行装配 | 本窗口只观察已有Play角色；不创建自己的Preview Runtime或时钟 |
| `rebuild-btsmtl-preview-with-scene-play` | 负责独立受控场景生命周期 | 本提案不依赖或实施该生命周期，其Pose观察消费者可复用本接口 |
| `refactor-btsmtl-flowcanvas-authoring` | 已由原混合提案拆成技能作者与编译观察 | 技能归原提案，Pose归本提案；两边都是普通运行加Editor观察，共享钩子按实际已提交版本复用，不同时覆盖同段源码 |
| Pose原change 22.x、23.x、24.x | 专用端口、原生runtime实验及正式runtime替换并存 | 原生runtime路线撤回，作者与观察改归本提案；未完成事项不伪造勾选或归档 |
| project／current Document旧版本文字 | 文本含v4，现有实现为v5 | 此处以已实现v5为输入并在delta中同步相关条款；不新增v4兼容，不宣布全项目版本漂移已修完 |

## Risks / Trade-offs

- [原生编辑API绕过Mutation] → 逐入口列出实际写入者和Undo owner，非法批量修改在提交前拒绝。
- [Native帧页已经复用或释放] → 消费正式完成快照／租约，目标generation变化立即解绑。
- [高亮存在但不是当前产物] → 同版本Source Map校验失败时清空叠加，保留作者图和错误来源。
- [观察引入额外计算或UI卡顿] → 检查求值／采样／输出计数及观察开关的诊断开销；显示端不执行算法、不等待Job。
- [共享文件被技能任务同时调整] → 先读最新接口和变更范围；实际冲突交给作者决定，不回退正确改动。
- [编辑器能打开但资产或交互没闭环] → 分别保存交互、Document往返、Build和Play观察证据，不用编译成功代替全部验收。

## Migration Plan

1. 固定源码／资产／现有运行基线与共享接口，登记替代关系。
2. 接通原生作者模型、唯一端口合同和事务；编译器直接消费新图。
3. 对精确Corin闭包执行迁移计划、原子保存和Document反向发布。
4. 完成Source Map及有界诊断投影，接Play生命周期和精确目标选择。
5. 接入原生只读高亮、端口值、子图与Pose Watch；从任意正常游戏入口的合法角色获得结果。
6. 通过已有正式编译、Build、回放和诊断能力保存证据，补齐实际编辑交互结果；不新增测试代码，不把人工验收写入tasks。
7. 删除无消费者的旧Pose画布、重复端口UI、窗口播放器和失效入口，同步规范及实际进度。资产失败按既有事务回滚；整体回退仅按明确批次与作者决定执行，不提供运行时fallback。
