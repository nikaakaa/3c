# 摄像机实施记录

记录日期：2026-09-13
确认规划：v3，提交 `b2c2baa8f`
实施工作区：`D:/Unity_Project_1/3C`

## 实施边界

本记录只描述当前实现窗口实际修改、读取和验证的结果。规划合同位于同目录的 `proposal.md`、`design.md`、`tasks.md` 及 `specs/`，不在本记录中重写。

保留现有默认轨道、鼠标输入采集、同帧 Body visible pose 跟随、Presentation Runtime 调度和 Cinemachine Adapter 输出。实施只补齐相机自身未闭合的求解、资源发布、诊断和旧路径迁移。

## 工作区基线

- 工作区存在大量其他任务及用户未提交改动，尤其是 ACL 资源、角色 Prefab、输入诊断、Graph/Timeline 作者和 Presentation 编译文件。
- 摄像机核心运行目录、CameraContracts 与相机编译器在实施前为当前工作区可读基线；`CharacterFixedInputTraceWorkflow.cs`、Presentation Projection 编译文件和角色 Prefab 已有用户改动，修改时保留其现有内容。
- `implementation.md` 在本次实施前不存在。
- 历史证据说明 Unity 实例此前未完成当前路径核对；本记录不把历史 Console、编译成功或资源存在当作运行验收。

## 任务状态

以下状态以当前窗口真实代码为准，未完成项保持未完成，不以任务勾选替代证据。

| 任务 | 状态 | 说明 |
|---|---|---|
| 1.1-1.2 基础构图与唯一轨道 owner | 部分完成 | 默认轨道仍由 DefaultSequence 唯一拥有；Profile/Projection schema 已升到 v2，重复 DefaultSphere/DefaultOrbitGroup 已删除；多点/双点无消费者的 BeginCameraDataId 与 LayerMask 已删除；生成 Projection 仍是旧 v1，必须正式重发布。 |
| 1.3-1.4 配置消费者与输入合同 | 部分完成 | CameraLocateRadius、RotationTransitionSeconds、Offset/AspectRatio、响应权、最终俯仰限幅和 Reset 原因已接入；无消费者的 Locking 与 ChangeAvatar 配置已删除；无时钟/上下文消费者的 MakeContextDependent、PlayLength 已从 Camera authoring/payload/compiler 和 Corin 默认资产删除；失焦专用来源仍未形成。 |
| 2.1-2.3 平滑、裁决与连续性 | 完成 | WorldBasicHistory 普通推进保留速度；Sequence/Response/Target 同权裁决统一，零权 Sequence 不再抢占；切镜使用显式 BlendIn，未配置时使用 Profile RotationTransitionSeconds。 |
| 2.4 生命周期 | 部分完成 | Sequence/Effect 请求保留 source/generation/action/cycle/event 身份并区分结束原因；ForceTeardown 现在按 `Cancel` 退休同 scope 的效果，保留已有 FadeOut/诊断链；Unity Owner 销毁和正式产物运行尚未取得当前 Editor 证据。 |
| 3.1-3.2 锁定和多目标构图 | 部分完成 | 目标槽、显式目标切换、双点/多点/实体计划已接线；运行中目标 Transform 失效会按 `TargetInvalid` 移除对应目标请求并重新解析默认/剩余目标，启动时漏绑仍作为配置错误抛出；不自动选敌；未被 Planner 消费的 Entity frame/rotation policy 字段已删除，当前内置实体构图公式仍没有 ZZZ 来源闭包。 |
| 4.1-4.3 Zoom/Stretch/Shake/Override | 部分完成 | 曲线、时钟、权重、叠加、空间和生命周期已进入 Projection/evaluator；Camera Resource Track 已注册为 typed Camera request；当前 Corin 只有 Zoom/Stretch 正式资源，Shake/Override 没有来源触发闭包。 |
| 4.4-4.5 Shot 与固定效果顺序 | 部分完成 | Shot 编译要求真实 prefab 和标准 VCam，Adapter 支持显式 Shot rig binding；已知来源 prefab 尚未进入工程；顺序固定为 Override→Zoom→Stretch→Shake→Shot→Environment。 |
| 5.1-5.3 碰撞与环境约束 | 完成代码接线 | 新增正式查询接口、PhysicsScene 实现、自身过滤、层/触发器、近裁剪保护、收缩/恢复/起点重叠/无合法空间结果，并接到诊断；Corin 当前 Collision 仍关闭，未做动态运行验收。 |
| 6.1-6.2 作者、导出与生成 | 部分完成 | CameraState/Cue/Resource Track 字段、Graph/Timeline emitter、Projection compiler 和资源校验已接入；Presentation Projection 现在还会校验每个 Camera Producer 的 Sequence/Effect ResourceId 属于同一 Camera Projection；新增 Camera Prepare/AdoptBinding 领域合同与 Profile→payload Builder，返回资源/绑定失败码和实际采用身份；Resource Track 的 Weight/Ease 通过 Timeline curve channel 采样；Shot 及效果真实作者引用仍缺少闭包。 |
| 6.3 Corin 动作资源可达性 | 部分完成 | ZZZ 动作资料已证明 34 个 Shake、10 个 Stretch、10 个 Zoom 资源键能反查到具体动作；工程已保留 18 Zoom/18 Stretch 资源，但现有 Corin ActionCue 仍没有正式 ResourceId，尚未把这些来源映射接入正式 Camera request，也不能虚构剩余 81 Shake/4 Override 的项目触发关系。 |
| 6.4-6.5 正式发布与 Preview/ScenePlay | 暂停全量入口 | 用户明确要求删除会触发十几分钟全量 Character Build 的入口；已移除 Character Float32/Fixed MCP 注册与 CLI 入口，底层正式 Orchestrator 保留但当前没有新的增量发布入口，旧 job 不接受其产物。 |
| 7.1-7.2 诊断与输入回放迁移 | 部分完成 | DebugSnapshot、采样帧、Reset/响应/Sequence 退出/目标退出/碰撞字段和 Effect table/operator 已接入；Camera PresentationCaptureFrame 从正式 PresentationFrameContext 写入 RenderFrame/LocalLogicTick，输入回放仍改读正式 Presentation CameraBasis/InitialState，保留用户已有注入改动；Unity 重载后的实时证据仍待返回。 |
| 7.3-7.4 删除与合同同步 | 部分完成 | 已删除无引用 ThirdPersonCameraController 及 meta、旧 FreeLook 朝向写入引用和无消费者 Locking/ChangeAvatar 配置；生成 Projection 和部分历史文档仍需正式发布后对账。 |

## 已确认的实施阻塞边界

- 来源证据未闭合的 Shot prefab 本体、Timeline 绑定和 compact-blend-words 字段不能用名称或占位资源补齐；对应能力必须保持明确失败，直到存在正式资源闭包。
- 来源没有证明的字段公式不能用临时近似发布为 ZZZ 还原；可以实现通用合同和已证明几何分支，但必须保留未验证边界。
- 当前工作区的输入诊断与角色 Prefab 有用户改动，不能覆盖或回退；若与旧 Controller 删除发生真实冲突，记录具体文件与调用者后发送一次 `ACTUAL_CONFLICT`。

## 修改与证据

本节随实际修改追加。编译、Unity MCP、回放和截图只记录真实执行结果及其边界。

## 本窗口证据

- `8db41c4fbcc925c1df83e66aea18f304b12368dc`：统一 Camera Projection、FramePlanner、Effect、Environment Constraint、Producer binding 和唯一默认轨道 owner。
- `393066dda`：收口 Reset/暂停/碰撞/效果采样诊断，删除旧 Controller、无消费者 Locking/ChangeAvatar 配置，并让不闭合阶段在编译/求值时明确失败。
- `6915f39bf`：将 Camera Resource Track 接入统一 Timeline Contract/Program/Presentation 请求链，移除旧的未调用资源轨道采样路径。
- `594ea0a92`、`200cb7ed3`、`70f77421b`：补齐 Timeline 作者对象/曲线 owner、切镜平滑职责和 CameraCue 曲线采样；这些提交均未包含用户仍在修改的 `BtsmtlSkillAuthoringCodeAdapter.cs` 与输入回放文件。
- `7b9717518`、`bc5be34b1`、`b44ab51e6`：按资源叠加策略裁决 Zoom，令 Shot 接管正式 Near/Far Clip，并严格校验 Shot rig identity、重置所有 rig history；Sequence/Target 同权裁决在 SourceId 相同后继续比较 EventId。
- `055e03736`：删除 Camera 阶段没有消费者的 `MakeContextDependent/PlayLength` 字段，清理 authoring、Projection payload、compiler 和 Corin 默认相机资产；Contracts 编译 0 错误。
- `ee58359c0`：删除 Entity frame/rotation policy 的悬空 authoring/payload/compiler 字段，实体取景只保留当前正式 Planner 的显式目标输入和内置公式。
- `4a962d409`：删除双点/多点取景没有消费者的 BeginCameraDataId、LayerMask 字段，清理 authoring、payload、validator 和 compiler；Contracts 编译 0 错误。
- `41f3ba473`：补齐 `CameraRotationLastStage` 到 `CameraRotationLastPayload` 的正式 Projection compiler 分支，避免该合法阶段落入未闭合 evaluator。
- `9f6b9e025`、`f0f173ca6`、`d213c26ce`：将 Camera Contracts 引用和类型命名空间接到实际 `BTSMTL.Timeline.Tree.Editor`；外层 Timeline Editor 不保留重复依赖。
- `ThirdPersonCamera.Contracts.csproj` 使用 `--no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，2 个 Unity 包警告，0 个错误；最近一次在删除 DefaultFOV 存储和补充阶段合同后仍通过。
- `BTSMTL.Timeline.Editor.csproj` 使用 `--no-restore /p:BuildProjectReferences=false --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，31 个既有未赋值字段警告，0 个错误；新增 Camera 曲线 owner 没有编译错误。
- 最新 `BTSMTL.Timeline.Editor.csproj` 使用同样参数编译成功，34 个既有未赋值字段警告，0 个错误；Camera 作者引用与类型命名空间没有静态编译错误。
- `ThirdPersonClient.Runtime.csproj` 同参数检查到工作区既有错误：`CharacterControlProgramContracts.cs(636)` 缺少 `CharacterSkillDependency`；直接项目重编译另有用户新增 `GameplayAbilityDefinition` 未实现 `IGameplayBehaviorProfile.DebugCategory/Tags`。本次未修改这些文件。
- `ThirdPersonClient.Editor.csproj` 在 Runtime 因既有错误未产出 DLL 后无法继续编译；没有把该结果解释为摄像机代码已通过 Editor 编译。
- Unity MCP 当前发现两个同名实例 `3C_Client@e852139597e42532` 与 `3C_Client@1e41b3a3e2ded45f`；两者当前都未 ready，未把旧 Console 日志当作新的源码结论。另有 Timeline implementation 记录表明精确重导入后 Timeline/Slate/Camera 引用错误已清除，剩余为工作区 Character 文件错误；本窗口未据此宣称完整 Unity Console 或 ScenePlay 通过。
- 后续显式读取 e852 实例 Console 得到 4 条记录：`CharacterSkillAuthoringDefinition.cs(10)` 重复 `Serializable`、`GameplayAbilityDefinition.cs(71)` 缺少 `DebugCategory/Tags`，以及一次旧 ExecuteCode 长命令错误；没有 Camera/Timeline 源码错误。
- 已按精确 Definition/Wrapper 路径启动 Fixed Character Build，job `a5d9ddca7fc44bf7b863f8aedcd15629` 返回 `character_build_exception`：`Character Camera Profile 'CorinCharacterCameraProfile' is incomplete.` 原因是 Unity 因工作区既有编译错误仍使用旧域程序集，而 Profile 已删除正式 owner 字段；没有接受旧域状态并伪造发布产物。
- 在当前 Editor 状态下再次按同一 Definition/Wrapper 启动 Fixed Character Build，job `004c30412ff14008b3db55cdfd5a215c` 最终返回同一 `Character Camera Profile 'CorinCharacterCameraProfile' is incomplete.`；没有接受任何生成 Projection/Program 产物。
- 在 Camera/Timeline 编译错误清除后按同一 Definition/Wrapper 启动 Fixed Character Build，job `81133763a5a84dee83cdc91a7d57d761` 已进入 running；在终态返回前不接受任何生成 Projection/Program 产物。
- 当前 `Editor.log` 已给出 Build 阶段证据：ACL 清单扫描耗时 `19941ms`；170 个已发布动画资源的复用判定耗时 `47196ms`；动画目录阶段总计 `67141ms`，随后仍在处理 7 个 ACL 动画片段。该耗时属于全量 Character Build 的 ACL/资源发布阶段，不是 Camera 求解或 Camera Contracts 编译。
- 按用户要求删除全量 Character Build 入口：移除 `character.build_float32_products`、`character.build_fixed_products`、对应 Scheduler 的 Character BuildKind 分支和 `CorinFixedBuildCli`；Timeline 专用入口保留。删除后不再调用 Character 全量 Build。
- 外部来源 `D:/ZZZ_Dump/output/corin_replication/20260904_corin_attack_event_index_v3.json` 记录 108 个攻击事件，其中 `CameraShakeKey` 有 104 个非空引用、Zoom/Stretch 字段为空、Override 字段有数值引用；复刻资料的资源统计为 Shake 81、Zoom 18、Stretch 18、Override 4。公共 Shake 标准配置正文仍未定位，工程当前只有 18 Zoom/18 Stretch 正式资源，因此没有伪造 Shake/Override 资源或触发映射。
- 外部动作索引 `D:/ZZZ_Dump/output/corin_replication/replication-guide/data/actions/*.json` 的 `cameraKeys` 已逐动作解析：当前样本明确反查出 Shake 34 个、Stretch 10 个、Zoom 10 个资源键；其中 `sm0-011-Attack_Counter` 同时存在 Shake/Stretch/Zoom typed keys。该证据只确认来源可达性，未替代 3C Graph/Timeline 的正式 CameraCue request。
- `CharacterCameraPresentationRuntime.Present` 已把 `GameplayPresentationFrameContext.RenderFrame` 与 `LocalLogicTick` 传入 `CharacterPresentationReplicationCaptureFrame`；这是静态链路证据，不等价于 Unity Console 或运行时回放通过。该修改发生在 Unity 增量重载期间，实时结果待实例恢复后读取。
- `4fb7111ea`：Camera target resolver/runtime 已区分配置漏绑与运行时目标失效：前者继续在正式绑定校验处失败，后者移除使用失效 key 的目标请求、记录 `TargetInvalid` 到 `CameraDebugSnapshot` 并重新走当前正式目标裁决；没有新增自动选敌或默认目标补齐路径。
- `53ef18bc8`：Camera PresentationCaptureFrame 已同步采集 `TargetRetired`、`TargetStopReason`、`TargetRetiredKey`，目标失效退出原因进入同一只读采样合同；当前仍只有静态证据，未宣称 Unity 运行时通过。
- Camera Sequence transition 现在保留 `SequenceRetiring` 与最后一次 `SequenceStopReason`，并同步进入 DebugSnapshot/PresentationCaptureFrame；自然结束、取消和事件撤销不再只影响内部转场而丢失最终采样帧的退出原因。
- Camera 最终输出在 Effect 求值后统一按 Profile `Input.PitchLimit` 限制俯仰，再进入环境约束与 Adapter；避免后续 Stretch/Override/Shot 改写越过基础输入合同。
- Camera plan continuity operator 已消费并校验 `SequenceRetiring/SequenceStopReason` 与 `TargetRetired/TargetStopReason/TargetRetiredKey`，退出合同异常会进入诊断失败结果；尚未取得 Unity 运行时采样证据。
- Camera `ForceTeardown` 已从同 scope 直接清除改为 `Cancel` 退休，效果 owner 继续按各自 RetireDuration 淡出；未改变 Sequence force cut 的职责。
- 当前 Corin `CorinAttack1Timeline.asset` 的 `Attack1CameraCue` 仍是通用 `ActionCueClip`，只有 `CueId: Attack1CameraCue` 与 `CueType: Camera`，没有正式 `CameraCueClip.ResourceId`；因此外部动作资料中的 camera key 仍不能直接迁成正式 Camera request，避免伪造资源映射。
- `CharacterPresentationProjection.IsValid` 已把 Camera Producer 与 Camera Projection 的资源闭包绑定起来；旧/混合 Projection 不再能仅凭 Producer 自身字段通过有效性检查。
- v4 D10：新增 `CameraRuntimeBindingContracts.cs` 与 `CharacterCameraRuntimeBindingBuilder.cs`。Camera 现在可返回 `Ready/Missing/Invalid/Failed`、typed failure code、PreparedBinding 和 `CameraBindingAdoptedResult`（Profile/资源版本、BindingId、Actor/Instance）；当前尚未接入角色装配 owner，未宣称已采用。
- Unity 当前 `Editor.log` 的诊断编译只剩 `DGS003: Field 'ResponseMode'`；源码已改为 `int ResponseModeValue` 并保留 key `response-mode`，但 `Library/ScriptAssemblies/ThirdPersonClient.Runtime.dll` 反编译仍显示旧的 `CameraResponseMode ResponseMode`。这证明当前阻塞是旧 Runtime 程序集未更新，不能把它解释为现行 Camera 源码错误；未再触发刷新或全量构建。
- 本窗口未新增测试，符合项目规则；生成 Projection 的旧 v1 产物没有手工伪造为 v2，等待正式 Character Build 发布。
