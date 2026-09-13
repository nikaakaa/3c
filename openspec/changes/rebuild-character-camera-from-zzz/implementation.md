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
| 1.1-1.2 基础构图与唯一轨道 owner | 部分完成 | 默认轨道仍由 DefaultSequence 唯一拥有；Profile/Projection schema 已升到 v2，重复 DefaultSphere/DefaultOrbitGroup 已删除；生成 Projection 仍是旧 v1，必须正式重发布。 |
| 1.3-1.4 配置消费者与输入合同 | 部分完成 | CameraLocateRadius、RotationTransitionSeconds、Offset/AspectRatio、响应权和 Reset 原因已接入；无消费者的 Locking 与 ChangeAvatar 配置已删除；有限 PlayLength/Context 与失焦专用来源仍未形成。 |
| 2.1-2.3 平滑、裁决与连续性 | 完成 | WorldBasicHistory 普通推进保留速度；Sequence/Response/Target 同权裁决统一，零权 Sequence 不再抢占；切镜使用显式 BlendIn，未配置时使用 Profile RotationTransitionSeconds。 |
| 2.4 生命周期 | 部分完成 | Sequence/Effect 请求保留 source/generation/action/cycle/event 身份并区分结束原因；Unity Owner 销毁和正式产物运行尚未取得当前 Editor 证据。 |
| 3.1-3.2 锁定和多目标构图 | 部分完成 | 目标槽、显式目标切换、双点/多点/实体计划和失效报错已接线；不自动选敌；Entity frame/rotation policy 的来源公式未闭合。 |
| 4.1-4.3 Zoom/Stretch/Shake/Override | 部分完成 | 曲线、时钟、权重、叠加、空间和生命周期已进入 Projection/evaluator；当前 Corin 只有 Zoom/Stretch 正式资源，Shake/Override 没有来源触发闭包。 |
| 4.4-4.5 Shot 与固定效果顺序 | 部分完成 | Shot 编译要求真实 prefab 和标准 VCam，Adapter 支持显式 Shot rig binding；已知来源 prefab 尚未进入工程；顺序固定为 Override→Zoom→Stretch→Shake→Shot→Environment。 |
| 5.1-5.3 碰撞与环境约束 | 完成代码接线 | 新增正式查询接口、PhysicsScene 实现、自身过滤、层/触发器、近裁剪保护、收缩/恢复/起点重叠/无合法空间结果，并接到诊断；Corin 当前 Collision 仍关闭，未做动态运行验收。 |
| 6.1-6.2 作者、导出与生成 | 部分完成 | CameraState/Cue 字段、Graph/Timeline emitter、Projection compiler 和资源校验已接入；Shot 及效果真实作者引用仍缺少闭包。 |
| 6.3 Corin 动作资源可达性 | 未完成 | 已保留 18 Zoom/18 Stretch 资源；现有 Corin ActionCue 的 Camera 标记仍没有正式 ResourceId，不能虚构 81 Shake/4 Override 的触发关系。 |
| 6.4-6.5 正式发布与 Preview/ScenePlay | 阻塞 | 生成产物待 Character Build 正式重发布；当前 Runtime 代码检查被工作区既有 `CharacterSkillDependency`/`GameplayAbilityDefinition` 编译错误阻断，Unity MCP 资源返回 `no_unity_session`，未做动态 Console/ScenePlay 验收。 |
| 7.1-7.2 诊断与输入回放迁移 | 部分完成 | DebugSnapshot、采样帧、Reset/响应/碰撞字段和 Effect table/operator 已接入；输入回放已改读正式 Presentation CameraBasis/InitialState，保留用户已有注入改动，文件仍未单独提交。 |
| 7.3-7.4 删除与合同同步 | 部分完成 | 已删除无引用 ThirdPersonCameraController 及 meta、旧 FreeLook 朝向写入引用和无消费者 Locking/ChangeAvatar 配置；生成 Projection 和部分历史文档仍需正式发布后对账。 |

## 已确认的实施阻塞边界

- 来源证据未闭合的 Shot prefab 本体、Timeline 绑定和 compact-blend-words 字段不能用名称或占位资源补齐；对应能力必须保持明确失败，直到存在正式资源闭包。
- 来源没有证明的字段公式不能用临时近似发布为 ZZZ 还原；可以实现通用合同和已证明几何分支，但必须保留未验证边界。
- 当前工作区的输入诊断与角色 Prefab 有用户改动，不能覆盖或回退；若与旧 Controller 删除发生真实冲突，记录具体文件与调用者后发送一次 `ACTUAL_CONFLICT`。

## 修改与证据

本节随实际修改追加。编译、Unity MCP、回放和截图只记录真实执行结果及其边界。

## 本窗口证据

- `8db41c4fbcc925c1df83e66aea18f304b12368dc`：统一 Camera Projection、FramePlanner、Effect、Environment Constraint、Producer binding 和唯一默认轨道 owner。
- `ThirdPersonCamera.Contracts.csproj` 使用 `--no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，2 个 Unity 包警告，0 个错误。
- `ThirdPersonClient.Runtime.csproj` 同参数检查到工作区既有错误：`CharacterControlProgramContracts.cs(636)` 缺少 `CharacterSkillDependency`；直接项目重编译另有用户新增 `GameplayAbilityDefinition` 未实现 `IGameplayBehaviorProfile.DebugCategory/Tags`。本次未修改这些文件。
- `ThirdPersonClient.Editor.csproj` 在 Runtime 因既有错误未产出 DLL 后无法继续编译；没有把该结果解释为摄像机代码已通过 Editor 编译。
- Unity MCP 已显式查询实例 `3C_Client@e852139597e42532`，Editor/project/camera 资源均返回 `no_unity_session`；未使用 batchmode 替代当前 Editor 验收，也未声明 Console、画面或回放通过。
- 本窗口未新增测试，符合项目规则；生成 Projection 的旧 v1 产物没有手工伪造为 v2，等待正式 Character Build 发布。
