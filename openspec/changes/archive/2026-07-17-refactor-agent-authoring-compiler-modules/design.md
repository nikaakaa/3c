## Context

当前`AgentPatchCompiler`既是schema parser、preflight validator、operation dispatcher、Graph mutation engine、ConditionRule builder、operation symbol table和dirty owner collector。它通过多个独立字符串switch分别判断支持操作、identity shape、asset reference和apply handler；`AgentPatchOperation`的宽union字段从JSON边界一直传到具体mutation方法。

当前dry-run只对部分资产引用和operation顺序做检查，apply再重新解释同一Patch并创建实际对象。两次解释依赖相同字符串约定，却没有共享一个prepared command plan。`bind_asset_reference`正是在这种结构下成为“列为支持但不产生mutation”的假操作。

`AgentGraphValidator`主体检查Graph、Timeline、ownership、identity和Character authoring语义，但末尾又按Definition名称执行Corin二连击结构检查。结果是通用合法性与业务样例覆盖混在一起。

## Goals

- 让每种operation只有一个schema定义、一个typed command lowering和一个handler归属。
- 让dry-run和apply使用同一prepared plan，避免两套解释。
- 保持唯一正式Graph mutation路径、唯一application service和现有Report结构。
- 让Compiler实例不保存跨调用状态。
- 让通用Validator不认识任何具体角色名、状态名或连招名。
- 让二连击等业务覆盖继续存在，但归Agent sample/macro evaluator所有。

## Non-Goals

- 不把Agent Patch编译成Gameplay Semantic IR或Runtime Program。
- 不建立内存Graph clone、虚拟BTSMTL runtime或第二套Node/Edge模型。
- 不把handler做成反射插件、动态脚本或运行时注册服务。
- 不顺带拆分SnapshotExporter或重写Agent JSON序列化库。

## Decision 1: Schema v8直接替换v7

Snapshot、Intent、Patch IR和Report继续共享一个`AgentAuthoringSchema.Version`，版本提升为v8。服务只接受v8；v6/v7输入直接返回unsupported schema，不提供converter或兼容reader。

v8删除独立`bind_asset_reference`。ActionProfile、TimelineAsset、ActionContext和Input引用必须存在于实际消费它们的typed ensure command中，并由对应Emitter或handler在一次操作内解析和写入。

### Tradeoff

- 保留v7可以减少外部JSON变化，但会继续保留current spec与代码版本不一致，以及一个没有行为的“支持操作”。
- 提升v8会要求Agent重新读取最新Snapshot后再生成Patch，但Agent JSON是editor-only临时输入，没有需要迁移的正式资产。选择v8更符合单一正式路径和无兼容层原则。

## Decision 2: Serialized Patch只存在于边界

`AgentPatchOperation`继续作为Unity JSON可序列化的宽union DTO，但只允许进入`AgentPatchCommandLowerer`。Lowerer通过唯一operation catalog完成：

- operation name到command kind的映射。
- 必需字段、互斥字段、identity reference和枚举值检查。
- Condition group与term的结构化降低。
- 明确资产引用的规范化。
- 前序operation reference的语法检查。

Lowering成功后生成immutable`AgentPatchCommandPlan`。后续Planner、Handler和Condition builder不得读取原始`op`字符串或宽DTO字段。

## Decision 3: Plan只保存命令与窄symbol，不复制Graph

Command plan按输入顺序保存typed command，并维护`operation id -> planned output kind/owner scope`的窄symbol table。它只用于证明后续operation可以引用前序输出，不保存Node、Edge或Graph序列化镜像。

Dry-run读取当前Graph Index并执行每个handler的preflight，输出精确planned diff。Apply在同一同步调用中消费相同plan，通过正式Graph API创建对象，再把实际对象注册到compile session的apply symbol table。

如果Definition source identity、RootTree identity或必要Graph identity在dry-run与apply之间发生变化，Service必须在mutation前失败，不重新lower或猜测目标。

## Decision 4: Compiler facade与单次Compile Session分离

`AgentPatchCompiler`保留为唯一编排入口，但自身不保存Definition、Snapshot、Resolver、Index、operation结果或dirty owner。

每次调用创建一个`AgentPatchCompileSession`，后者唯一拥有：

- 当前Definition、Snapshot和RootTree。
- `AgentAssetResolver`与`AgentGraphAuthoringIndex`。
- prepared command plan与planned/apply symbol。
- 当前Report、planned/applied diff和touched serialized owner。
- handler执行后的Index刷新。

Compiler不调用`EditorUtility.SetDirty`、Undo或`AssetDatabase.SaveAssets`。

## Decision 5: Handler按authoring所有权聚合

不为每个operation创建一个类。handler按共享不变量和正式API聚合：

- StateMachine handler：StateMachine、State、Transition和ConditionRule edge ownership。
- StateBehavior handler：State body node、Action activation/lifecycle和TimelineNode ownership。
- Node/Asset handler：Input node、Emitter白名单和资产引用配置。
- GraphLink handler：flow/property link、PortId和Graph kind检查。
- ConditionRule builder：Condition group组合、term emitter、property连接和Result输出。

唯一静态catalog把command kind映射到handler。未知kind在lowering阶段失败，不动态发现handler，也不使用fallback handler。

## Decision 6: Condition term使用正式Emitter Registry

现有`move_stop`、`move_has`、`move_run`、`move_walk`、`turn_facing_angle`、`blackboard_bool`、`state_root_completed`和`action_request`形成唯一Condition term白名单。每个term emitter声明：

- 支持的term kind。
- 必需字段和引用。
- 创建的正式Condition节点类型。
- 输出PropertyPort。

ConditionRule builder只负责AND组、OR组、Result连接和布局，不再包含按字符串创建具体业务节点的长switch。

## Decision 7: 通用Validator与业务Coverage分离

`AgentGraphValidator`只检查对任意Character Definition都成立的规则，包括Graph kind、节点位置、Condition纯度、Timeline ownership、serialized owner/path、TreeClip ownership、Action Context链、Input/ActionProfile引用、authoring identity和正式Compiler report。

它不得读取Definition名称，也不得要求`Attack1`、`Attack2`、`DodgeForward`或任何业务display name。

`two_hit_combo`等Macro的具体输出由`AgentMacroCoverageEvaluator`或等价sample evaluator检查typed command plan。该检查只在对应Macro评估中运行，不参与普通`validate` action，也不限制作者手工设计其它合法角色拓扑。

## Decision 8: Application Service继续拥有资产事务

`AgentPatchAuthoringService`执行固定顺序：

```text
Parse v8 JSON
  -> Lower typed command plan
  -> Dry-run preflight
  -> Collect complete serialized owners
  -> Register one Undo group
  -> Apply the same command plan
  -> Generic graph validation
  -> Mark touched owners dirty
  -> Collapse Undo and SaveAssets
```

任一步失败都返回现有`AgentCompileReport`并在需要时回滚。MCP Bridge和Editor Window继续只调用该Service。

## Parallel Ownership

本change只编辑AgentAuthoring editor-only代码和对应Agent spec delta。实现期间不得编辑：

- `Runtime/Networking/`。
- `Runtime/Simulation/`。
- `Runtime/Character/Pipeline/Presentation/`。
- Fantasy协议、Server代码、网络Scene和Build脚本。
- `add-dotrecast-authoritative-server-backend`文档。

网络主线不得同时修改AgentAuthoring目录。两条工作只可能在Unity生成的本地工程文件和编译状态上间接相遇，这些生成文件不得进入提交。

## Failure Policy

- Schema、command lowering或operation reference错误：mutation前失败。
- Dry-run与apply输入identity不一致：mutation前失败。
- Handler找不到正式Graph、Node、Port或资产：当前事务失败并回滚。
- Generic Validator失败：当前事务失败并回滚。
- 未知operation或term：明确拒绝，不创建placeholder、不跳过、不回退字符串路径。
- 无法覆盖serialized owner：不进入apply。

## Implementation Order

1. 锁定v7现状、调用入口、error code和删除清单。
2. 建立v8 schema、operation catalog、typed command与lowerer。
3. 建立command plan、planning symbol和单次compile session。
4. 迁移StateMachine、StateBehavior、Node/Asset和GraphLink handler。
5. 迁移ConditionRule builder与term emitter registry。
6. 切换Service到同一prepared plan和唯一事务所有权。
7. 删除旧Compiler分派、dirty路径、no-op和Corin Validator。
8. 接入Macro coverage evaluator，更新spec并完成编译和严格校验。
