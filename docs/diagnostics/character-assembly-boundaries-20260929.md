# 角色程序集边界整理（2026-09-29）

## 本轮范围

把角色运行与编辑器的大程序集按现有业务依赖拆开，保留技能、TreeClip、Timeline 与原生 Pose 的执行行为。不执行 replay，不新增或修改测试代码，不改 OpenSpec。

## 实际编译边界

| 程序集 | 源文件数 | 职责 |
| --- | ---: | --- |
| ThirdPersonClient.Runtime | 75 | 角色内容装配、技能作者合同、会话接入、表现领域组合 |
| ThirdPersonCamera.Runtime | 21 | Camera 求解、效果执行、环境查询和 Cinemachine 接入 |
| ThirdPersonCharacter.Animation | 315 | 原生 Pose、动画来源、Timeline 动画生命周期、Foot/IK、姿势发布和身体表现合同 |
| ThirdPersonClient.Editor | 68 | 产品构建、网络工具、性能工具、场景接入和诊断入口 |
| ThirdPersonCharacter.Content.Editor | 262 | 角色内容作者操作、资源准备、Foot 分析和 ACL 资源发布，其中包含现有生成作者代码 |
| ThirdPersonCharacter.SkillCompiler.Editor | 27 | Skill/TreeClip 语义编译、目标后端和执行数据发布 |

拆分前 ThirdPersonClient.Runtime 为 410 个文件，ThirdPersonClient.Editor 为 357 个文件。动画程序集新增一个内部访问声明文件，因此拆分后运行文件总数增加一个。

Camera 与 Animation 不引用 ThirdPersonClient.Runtime；Content.Editor 和 SkillCompiler.Editor 不引用 ThirdPersonClient.Editor，两个编辑器能力程序集也互不引用。最终装配和工具位于这些模块之上。

Animation 中保留共同参与帧事务的图、采样、混合、约束和发布实现。Content.Editor 中保留共同参与内容制作与资源发布的操作；没有为缩小文件数把紧密耦合的事务继续切开。

分散目录通过 Unity 原生 asmref 归入同一程序集，不增加运行包装或第二条实现。18 个搬迁脚本及其 meta 同步移动，原 GUID 保留。仅清除迁移后不再使用的三个 namespace import，没有改动这些脚本的业务执行体。

CameraEnvironmentConstraintSolver 是表现领域实际调用的求解入口，因此公开该类型。Animation 内部实现保持 internal，明确授权角色装配、内容作者和现有诊断工具三个程序集访问；没有整体公开 Pose 实现。原角色运行程序集删除不再需要的 RootMotion、Burst、Collections、Cinemachine 等直接引用及 unsafe 权限。原编辑器程序集删除已归属内容/编译模块的直接引用。

## 资产与工具接线

- CorinPoseNativeDomainResourceSet 的四处 AnimationClipPhasePlan managed reference 改为 ThirdPersonCharacter.Animation。
- CorinAnimationEventGraph 的五处 CharacterPresentationMotionPhase 泛型程序集限定名同步迁移。
- ActionTargetSnapshot 仍属于 ThirdPersonClient.Runtime，技能资产中的对应限定名保留。
- 性能 IL 织入名单加入 ThirdPersonCharacter.Animation，防止 Pose Prepare/Evaluate/Commit 探针随程序集迁移而漏掉。
- Fixed、Rollback、ServerAuthoritative 角色宿主、诊断采样与分析、Timeline 编辑器、性能采集和 GameLogic 增加其实际使用的新程序集引用。

## 检查与限制

- 145 个项目 Assets/Packages 程序集的名称和 GUID 引用图无循环。
- 搬迁的 18 个脚本执行体与原件一致，meta GUID 全部保留。
- 使用目标 Unity 2022.3.62f2c1 生成的编译 response file 和 C# 编译器进行独立检查：四个新程序集、原 Runtime/Editor、三种角色宿主、性能、Timeline 编辑器、诊断程序集、GameLogic、Assembly-CSharp 和 Assembly-CSharp-Editor 编译通过。
- 这项独立检查对 ThirdPersonSimulation.Core 使用当前源码生成的 metadata-only 参考程序集，以核对当前类型和调用合同；输出只放在系统临时目录，没有替换 Unity Library 产物。它不证明 Core 方法体可编译，也不等价于 Unity 完整编译通过。
- 目标 Editor 已退出 Play，正式刷新后的整体编译仍被既存的 OperationStateMachineRuntime.cs:267 两处 int 到 ulong 参数错误阻塞。该文件不在本轮改动范围，未修改。
- 未执行 Player 构建、运行检查、资产加载验收、性能采集或 replay。程序集拆分没有新增每帧执行逻辑，未据此宣称整体零 GC 或性能改善。
