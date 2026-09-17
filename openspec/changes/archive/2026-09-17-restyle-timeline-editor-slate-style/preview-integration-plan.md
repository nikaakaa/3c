# Timeline 与预览窗口联动实施计划

## 本次规划基线与授权

2026-09-13 按用户协调方案 camera-preview-timeline-domain-runtime-r1 更新。运行装配以 [领域运行方案](../replace-character-program-with-domain-runtimes/design.md) D1–D8 为准；MotionCurve来源以 [曲线源迁移](../../specs/character-root-motion-curves/spec.md) D1–D7 为准。本文只更新本任务拥有的共同接入要求，不修改代码、资产或产物，不向实现任务下发消息，不增加原实现授权。

[Slate源码解耦决策](slate-source-decoupling.md)继续约束原UI复用：原轨道、Clip、曲线和手势保持，TimelineData、typed接线、编辑Session/Undo保持。旧ce21aec8f/afcb90056仅是上次源码阅读的恢复基线，不是本轮重新检查的实现状态；UI解耦不等于运行装配迁移已完成。

## 1. 归属与禁止恢复的旧前提

2026-09-14 PARALLEL-20260914-DOMAIN-01：本任务新增直接Timeline Runtime规划，详见[timeline-direct-runtime.md](timeline-direct-runtime.md)，与原Slate UI线分开。这里的“分开”只表示职责和 owner 分开，不表示把 Runtime 从当前 goal 删除；Timeline Runtime 按 tasks 第12节推进，ScenePlay协调器仍拥有场景预览接入，不能因本任务接收内容Runtime就代写预览协调器。

- Timeline窗口只拥有作者编辑、帧游标、正式Undo、源导航和真实运行/历史标记；不创建Slate播放器、另一个采样器或预览Session。
- SkillGraph/Graph Shell承载共享预览控制；场景选择、准备、运行、暂停、结束和领域变化后的实际采用由rebuild-btsmtl-preview-with-scene-play原协调器及正式运行模块拥有。
- Game/Scene视图显示真实角色与相机。Timeline编辑不需要Slate Actor/Director；真实预览对象由正式场景/领域工厂管理。
- 删除本联动方案中的Character全量Build、整包CharacterPresentationProjection、统一ProgramEpoch采用与Document/v7前置。也不能把它们改名为一个新角色总包、全局版本对象或隐藏Pose Image。
- 保留独立技能数据、源资源处理、网络Pipeline/Pass、Float32/Fixed、正式Session/World和领域算法。图编辑不会触发运行时偷偷补构建。

界面与运行数据通过各领域的报告接入。UI不代写角色工厂、CameraBuilder、技能编译器或状态恢复算法；缺合同就显示具体缺项，不再等待已经删除的Character Build。

## 2. 作者可见入口

复用已有SkillGraph/Graph Shell布局，不新增一套预览窗口。场景控制、内容操作、观察和历史仍分组：

```text
预览场景：[场景资产]  目标：[正式角色或独立调用方]
[开始预览] [暂停/继续] [结束预览]
场景状态：准备中 / 运行中 / 暂停 / 失败原因

技能：[技能] [构建此技能] [执行] [打开对应Timeline]
Pose：[显式重建实例并重置历史]
Camera：[应用正式绑定并Reset]
控制/网络：[按Session规则重新准备]（需要时显示）

领域状态：配置版本 → 已准备版本 → 实际采用版本 / 失败原因
观察：[实例/调用路径] [跟随/固定] [实时/历史]
历史与录制（折叠）：诊断、输入、Tick位置、恢复/回放
```

上述是命令含义，不要求一次铺满所有按钮；按目标实际支持的领域显示，保留现有可折叠分组。按钮名称不能统一成不知构建范围的“Build并采用”。窄窗口自然换行，版本/身份详情可折叠，输入和历史Tick不能在刷新时被覆盖。

SceneAsset、context身份与目标声明是正式输入，不能要求作者先加载场景再随意拖一个GameObject才能解析目标。场景准备后由登记的context和正式运行owner确认实例；不按显示名、列表首项或项目资源扫描猜测。

## 3. 各领域的准备与生效合同

| 内容变化 | 处理命令与真实owner | 预览应显示的结果 | 不允许的替代 |
|---|---|---|---|
| Timeline Prepare/CreatePlayback | Timeline Runtime独立准备内容identity/revision、NumericTarget、外部资源/成员和TreeClip服务，再按精确调用方创建实例；不发射Track/Clip operations | Pending/Ready或分型依赖失败；实际创建后报告Playback identity/generation及内容/资源版本 | 借用Ability Prepare、旧ProgramPlan、Slate播放器或假非Skill调用方 |
| Graph/Ability及其引用Timeline | Graph 只编译图自身；Ability/调用方只准备正式引用、provider 和实例 binding；调用 Timeline 只引用其直接内容版本和入参，不展开轨道/Clip | 图/内容准备成功不等于当前实例已更新；活动实例保持启动时不可变的 binding，新版本用于后续实例；Session 玩法 identity 变化按正式规则重新准备 | Character 全量 Build；强行给活动实例换版本；用整包 Epoch 判断 |
| Pose图及其绑定 | 同一正式原生Pose Factory显式重建实例，释放旧实例并重置播放/IK等历史 | 配置版本、实际实例generation/版本、重建成功/失败、历史已重置；图仍按原表现时钟执行 | Compile Pose Image；加载时生成隐藏操作表；承诺拓扑改动无损热替换 |
| Camera配置/资源绑定 | 正式Camera绑定与Reset，由摄像机模块报告 | 目标、配置版本、实际绑定版本、Reset结果与失败原因 | UI计算采样、编相机总包或代写CameraBuilder；声称只改hash已生效 |
| C#控制/控制配置 | 正式控制模块和角色装配/Session规则处理 | 是否需要重新准备、原因、配置与实际版本、完成/失败 | 编入技能或恢复Control总catalog；旧实例偷偷读取新字段 |
| 网络/Pipeline组合或策略 | 原Pipeline/Pass、Backend与Session准备；保留Float32/Fixed合同 | 玩法内容/schema/数值目标与网络组合的正式兼容结果，需重启/重连时明确说明 | 为了不停机而跳过握手或更换网络内容版本 |
| RootMotion/ACL/MM等独立资源 | 各自正式资源owner处理真实变化，相关消费者重新检查绑定/依赖 | 资源身份/内容版本、准备结果、受影响领域及实际采用情况 | 修改Timeline局部权重就重烘焙全部资源；运行时构建fallback |

“编辑时保持同一Session”表示保存、Undo、导航不自行重启运行；不意味着所有领域变更都能原地采用。技能活动实例版本固定，Pose重建明确重置历史，控制/网络兼容性变化按Session规则处理。不得再承诺统一adoption barrier可以无损采用所有内容。

业务取舍：各领域显式处理能让作者知道改动会影响什么；需要多种准确命令，而不是一个看似方便却重新打包所有内容的按钮。预览承担呈现和转交，不复制各领域的准备实现。

## 4. 与原预览owner待对接的数据

以下是共同接口需要表达的信息，不声明已存在同名C#类型，也不授权Timeline任务去实现其它领域工厂：

| 信息 | 必须包含的语义 |
|---|---|
| 场景与目标 | 场景资产/context身份、角色或非Skill调用方身份、Session身份及generation |
| 领域准备请求 | 明确领域、内容身份、配置版本、目标、请求身份；不得以全角色artifact为唯一输入 |
| 就绪结果 | 未准备/准备中/就绪/失败，以及准确领域、失败阶段与原因；领域缺项不能假装整个角色就绪 |
| 实际采用 | 该领域配置版本、候选/已准备版本、实际采用版本或实例generation、适用实例与采用时机 |
| 运行来源 | 技能/Timeline内容身份、实际ActionInstance或非Skill playback identity、完整调用路径、generation、实际版本 |
| 生命周期结果 | 重建/Reset/重新准备是否完成、历史是否重置、是否需要结束Session及正式原因 |

来源：场景目标由原协调器/context/运行owner提供；技能版本由技能运行模块报告；Pose由同一原生Factory/实例报告；Camera由正式绑定/Reset报告；控制和网络由装配及Session规则报告。原预览协调器转发/聚合这些结果，Timeline消费与自己内容有关的只读部分。

配置fingerprint只能说明作者数据发生变化，不能证明运行采用。版本未知就是未知；缺报告显示“尚未收到实际采用结果”，不能拿UI hash、按钮点击或准备成功代替。每条结果必须属于当前目标/请求/generation，旧实例晚到报告不得覆盖新实例。

原运行owner文档不由本任务维护：其旧Build/ProgramEpoch接口应按本表迁移或替换，具体签名与实现由原owner负责。本文只记录待对接入口，不覆盖其它change，不跨窗口追问或另建Coordinator。未接入口保持明确未完成，不等待旧总包系统复活。

## 5. Timeline导航、运行观察与纯Timeline

```text
Timeline正式编辑/Undo
  → 作者内容版本变化
  → 正式调用方/领域报告依赖需要处理
  → 作者显式构建技能、重建Pose、Reset Camera或重新准备Session
  → 各领域报告实际采用结果
  → 预览显示结果；Timeline保留编辑状态与准确运行标记
```

- SkillGraph执行请求仍经正式准入；收到请求不等于技能已启动。Tree-only技能可正常试验，不要求有Timeline。
- 纯Timeline必须有真实非Skill调用方、明确内容/参数与播放身份。控制用运动素材不是天然可执行技能，不能造空Skill或假角色来播放。
- 多个合法Timeline调用要明确实例选择，不能默认第一个。共享模板以调用路径、实例、generation和实际内容版本区分。
- Timeline编辑游标、Runtime位置、History位置分别保存。拖标尺、前后帧或前后key只改变编辑位置，不执行Slate Sample，不改运行clock。
- 打开/关闭Timeline、源导航和普通字段修改不停止Scene Play；重建/重新准备只能来自明确正式命令。
- 运行时长未知Clip、开放Track生命周期是另一个正式执行合同问题，不由本轮UI解耦或版本报告补成“假延长”；不扩大当前实现授权。

## 6. MotionCurve界面接入

曲线迁移owner提供MotionCurve源引用、源区间/播放映射及MotionWarp使用配置。Timeline界面只消费其typed字段、正式校验与“打开源资产”导航，不并行修改Timeline.MotionCurve/Timeline.MotionWarp或相关源配置binding。

- RootMotionCurveAsset拥有源累计XYZ/Yaw及其时间/求值模式；源XYZ/Yaw不再注册为Timeline-local可写通道。
- Clip仍拥有一次使用的区间/映射及Weight/Ease；Warp progress仍属其正式owner，继续用原Slate曲线工具编辑。
- Timeline必须提供当前Clip使用的源XYZ/Yaw只读曲线和区间显示，明确源版本与作者/源时间对应，不能仅提供资产导航。编辑源仍由真实源owner负责，Timeline Undo不写共享源、不自动复制；当前缺口与接线见editor-wiring-audit.md A04。
- 源时间、区间裁切、末端保持和delta等使用曲线owner唯一正式映射，预览不能另造采样公式。旧CurveEndFrame到源秒的等价迁移属于曲线owner，不留UI双读。
- 源改动使哪些技能数据或控制绑定失效由正式依赖报告决定，不触发Character总Build。

运动源迁移已归档完成，现行源API和唯一时间映射直接复用，不重新安排无损迁移或恢复旧Program接入。未来共享源/区间/Warp字段变化仍归原owner。Timeline直接Runtime的新增接收范围见timeline-direct-runtime.md。

## 7. 历史、失败与恢复

浏览历史只读真实记录，不按新配置重算旧结果。恢复/回放仍由原预览owner与Simulation/Presentation能力处理，报告接受、执行中、完成或失败，不把点击按钮当成成功。

历史记录按对应领域内容/实例和状态schema解释，不再要求一份角色ProgramEpoch。Pose显式重建后历史重置，不把重建前Pose历史接到新图；控制/网络内容不兼容的旧checkpoint由正式规则拒绝。网络重放不主动推进Pose，保持现有提交后表现更新规则。

准备失败不回退作者资产，也不创建旧Program/Projection fallback；保留实际仍在运行的版本并准确显示原因。跨Session或不兼容变化要求重新准备时，保留作者窗口状态，明确运行中断原因。

## 8. 文件分工与原批次接续

| 职责 | 唯一负责方 | 本规划接入范围 |
|---|---|---|
| Timeline直接内容Runtime/portable/播放私有状态 | 本任务新增Runtime线 | 独立Prepare/CreatePlayback、Advance候选与Commit/Discard/Stop、分型Capture/Restore；核心接总Step/快照 |
| Slate编辑源码、Timeline UI适配 | Timeline任务 | 原UI、正式字段/命令、导航、只读版本与运行标记 |
| ScenePlay协调器、场景准备、运行采用/历史 | rebuild-btsmtl-preview-with-scene-play原owner | 提供/消费第4节信息，不由Timeline复制实现 |
| Timeline.MotionCurve/MotionWarp与源配置binding | unify-timeline-motion-curve-source | UI消费typed字段和源导航；保留局部Weight/Ease |
| Timeline.Camera.cs、Camera正式绑定/Reset | 摄像机任务 | UI消费正式参数和Reset结果，不代写CameraBuilder |
| 角色领域装配、独立技能、原生Pose Factory接线 | replace-character-program-with-domain-runtimes | 预览经原owner取得就绪与实际版本，不恢复角色总包 |
| 两个显式作者工具 | 原C# authoring owner | 保持现有工具语义，不恢复Document/v7、watcher或隐式Build |

原P1–P5只替换公共合同，不复制其它任务待办：

- P1继续复用共享预览表面；命令按领域说明。
- P2接场景/目标与各领域就绪，不要求Character Program就绪。
- P3保留Timeline双向导航、真实调用/版本与三种时间显示。
- P4改消费领域配置/实际采用报告，删除统一ProgramEpoch采用流程。
- P5历史/恢复按正式领域能力、版本、generation和schema处理，保持输入与释放行为。

本次只有PLAN文档调整，不下发这些步骤，不新增测试/验证tasks。接口缺失只记录对应owner的正式接入需求，不扩展本任务实现范围。

## 9. 作者操作与规范替换

人工修改和Undo仍只写正式TimelineData；现有两个显式C#操作保持，不因编译源码自动生成资产、不因生成资产自动启动预览或构建全部领域。RootMotion源只作为明确外部引用，不进入隐式删除范围。

本文件明确替代旧联动文案中的全量Build、整包Projection和统一ProgramEpoch采用。旧Document/v7不是编辑、准备或采用前置；Slate的Editor binding和被取消的角色Projection总包不是同一个概念。

current规范中旧TimelinePreviewSession、角色Program/Image/整包Projection和预览总Epoch条款尚由各自change迁移。本任务只更新自己的delta与共同接入说明，不声称其它owner已实现，也不以历史编译/任务勾选宣称本次领域联动完成。
