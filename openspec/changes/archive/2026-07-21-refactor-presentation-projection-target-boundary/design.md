# Design: Target-Neutral Presentation Projection

## Context

当前编译树在 Numeric Target 分叉后生成 Projection：

```text
CharacterPipelineDefinition
  -> Semantic Frontend
  -> validated Semantic IR
  -> Float32 Target Program
  -> CharacterPresentationProjection.Build(Float32 Program, Authoring, Analysis)
```

这条路径最初利用 Float32 Program 已经整理好的 producer、source map、reference 和 constant，避免 Projection 再读取 Semantic IR。加入 Fixed 后，表现资产仍然应该复用，于是 Fixed 运行时只校验 ProgramId、SourceRevision、SemanticHash 与 producer identity，跳过 Projection 保存的 Float32 ProgramHash、NumericProfile 与 ABI。

问题不是 Unity Presentation 最终使用 `float`。动画时间、相机 blend 与 Unity Transform 使用 `float` 是表现层正常实现。问题是 Projection 的编译输入和身份依赖某一个 Gameplay Numeric Target，导致 Fixed 的完整产品无法只依赖公共语义和自己的 Target Program。

## Goals

- Projection 在 Float32/Fixed 分叉之前形成唯一 target-neutral 表现产物。
- Gameplay Numeric Program 与 Unity Presentation Projection 分别拥有清楚、不可重叠的 identity。
- Runtime 只有一个 Projection 校验 Interface；Float32 与 Fixed 通过两个真实 Adapter 接入同一 Seam。
- Editor 只有一个 Projection Compiler，集中 Animation、Camera、Cue、Equipment、Marker Sync 与 Analysis artifact join。
- Build workflow 只有一个 Frontend、一个 Projection 编译路径和一个发布事务，不保留 Fixed/Float 特例或兼容分支。
- 目标 Program 的 ProgramHash、LayoutHash、NumericProfile、ABI、State codec 与 Session compatibility 继续严格校验。
- 生成资产迁移后删除旧字段、旧 overload、旧 revision 与手工 producer identity 拼装。

## Non-Goals

- 不改变 Gameplay Semantic IR 的业务 operation、控制流、State layout 或 Numeric Target lowering 规则。
- 不让 Runtime 读取 Semantic IR、Authoring Graph、Timeline asset 或 Editor Analysis artifact。
- 不改变 Unity Presentation 内部使用 `float` 的实现。
- 不新增 Numeric Target，也不把 Fixed ABI 引入 Float32 Session。
- 不重做 Timeline Animation Analysis；只消费 `refactor-timeline-animation-authoring-boundary` 提供的正式 artifact resolver 输出。
- 不为兼容旧 Projection asset 增加 reader、字段迁移器、默认 identity 或 fallback。
- 本 change 不为了单一依赖强行拆出新的 Runtime asmdef；先通过 Module Interface 和 source-set ownership 清理职责。

## Target Architecture

```text
CharacterPipelineDefinition
        |
        v
Character Semantic Frontend
        |
        v
Validated Semantic IR Artifact
        |
        +------------------------------+
        |                              |
        v                              v
Presentation Semantic Contract     Numeric Target Build Adapters
        |                              |                |
        |                              v                v
Presentation Authoring         Float32 Program      Fixed Program
Analysis Artifact Set                |                |
        |                             v                v
        v                        Float32 Contract   Fixed Contract
CharacterPresentationProjectionCompiler      \      /
        |                                     \    /
        v                                      \  /
Target-Neutral Projection  <---- exact contract equality
        |
        v
Atomic Artifact Publish Transaction
```

Projection Compiler 与 Numeric Target Adapter 都消费同一个 validated Semantic IR identity，但互不调用。Target Adapter 不认识 Presentation authoring；Projection Compiler 不认识目标 Program。

## Module 1: CharacterPresentationSemanticContract

### Interface

Contract 是不可变 target-neutral 值，至少包含：

- ProgramId
- Gameplay SourceRevision
- SemanticHash
- Presentation contract schema version
- 按 producer index 排序的 `Index / Identity / LayerId / SourceIdentity / ChannelKind`
- 由上述字段 canonical 计算的 ContractHash

构造时必须拒绝空 identity、重复 index、非连续 index、重复 producer identity、非法 LayerId/SourceIdentity 和调用方提供的伪造 ContractHash。

### Implementation

- Frontend Adapter 从 validated Semantic IR artifact 创建 contract。
- Float32 Adapter 从已完成 exact artifact load 的 Float32 Program 创建 contract。
- Fixed Adapter 从已完成 exact artifact load 的 Fixed Program 创建 contract。
- 三者使用同一个 canonical contract builder；Adapter 只负责选择字段，不复制 hash 算法。

### Input / Output

```text
Input: validated Semantic IR 或已严格加载的目标 Program
Output: immutable CharacterPresentationSemanticContract
```

Contract 不包含 ProgramHash、LayoutHash、NumericProfile、Target ABI、State codec 或 Unity Object。

## Module 2: CharacterPresentationProjectionCompiler

### Interface

唯一公共 Editor 编译 Interface 接收一个完整 request：

- validated Semantic IR artifact
- Character authoring compilation model 中的 Presentation inventory
- Animation Presentation Profile
- Equipment Presentation Profile
- 已解析 Animation Analysis artifact set
- publish/dry-run 所需的显式选项

返回：

- 完整 `CharacterPresentationProjection`
- Presentation Semantic Contract
- ProjectionRevision
- 结构化编译 diagnostics

调用方不需要知道 producer/source map join、Camera literal 解码、Marker Sync call-site 收集、Equipment Visual 编译或 revision token 排序。

### Implementation

现有 `CharacterPresentationProjection.Build`、`BuildProducer`、`ResolveKind`、`ResolveSource`、Camera/Cue builder、Marker Sync authoring validation、Equipment Projection compile 和相关 Authoring join 移入该 Editor Module。Runtime Projection 不再引用 `CharacterSimulationProgram`，也不再公开 Authoring 编译入口。

Editor Module 通过 Runtime assembly 已存在的 `InternalsVisibleTo("ThirdPersonClient.Editor")` 调用窄的 internal payload factory。该 factory 只接收已完成验证和稳定排序的 payload，不接收 Authoring object 或目标 Program。

### Input / Output

```text
Input: Semantic contract + Presentation inventory + Analysis artifacts
Output: validated target-neutral Projection payload
```

## Module 3: CharacterPresentationSemanticReader

该 Editor-only Module 只负责从 validated Semantic IR 查询 Projection 需要的公共语义：

- ordered producer table
- producer source map
- producer reference 对应的 source operation
- Camera/Cue operation schema
- numeric-neutral literal

Camera Graph producer 不再读取 Float32 `SimulationOperation` 或 `ProgramConstant`。Reader 使用 `CameraProgramOperationSchema` 校验 Semantic operation，并从 `SemanticLiteral` 读取 Int32、String 与 numeric-neutral Number。Presentation Compiler 在明确的 Unity Presentation 数值 Seam 把 Number 转成 `float`，错误必须包含 source identity、字段名和原始 literal。

Timeline Camera Clip 继续以唯一 Timeline authoring inventory 生成表现 binding；Graph Camera 与 Timeline Camera 不得互相 fallback。一个 producer 同时出现多个合法 source operation时直接失败。

## Module 4: CharacterPresentationProjection Runtime Payload

Runtime `CharacterPresentationProjection` 只负责：

- 保存 Presentation Semantic Contract identity 与 ProjectionRevision
- 保存 Layer、Producer、Animation、Camera、Cue、Equipment Visual、Marker Sync 和 Foot Analysis payload
- 提供只读 producer/binding 查询
- 校验传入 ContractHash 与必要的 producer entries
- 拒绝损坏或过期 payload

它不再负责：

- 编译 Authoring
- 解码 Float32/Fixed operation
- 计算 AssetDatabase dependency
- 生成 Analysis artifact
- 判断 NumericProfile/ABI
- 校验 ProgramHash

`CharacterPresentationProjectionAsset` 只保留一个 `Load(CharacterPresentationSemanticContract)` Interface。Preview、Host、Animation binding index 与 Runtime Factory 不得再选择“exact Program”或“semantic Program”分支。

## Module 5: Numeric Target Presentation Contract Adapters

两个 Adapter 位于各自已有 Target 可见的 Unity source set：

- Float32 Presentation Contract Adapter
- Fixed Presentation Contract Adapter

Adapter 的唯一职责是从已经通过目标 ProgramAsset/codec 校验的 Program 创建 `CharacterPresentationSemanticContract`。Adapter 不加载 Projection、不读取 Authoring、不重算 ProjectionRevision，也不改变 Session 的 Program identity。

这两个 Adapter 形成真实 Seam：

```text
Float32 Program --+
                  +--> CharacterPresentationSemanticContract --> Projection.Load
Fixed Program ----+
```

远端 Presentation 如果只有正式的 semantic producer manifest，也必须通过同一个 canonical contract builder创建 contract；不得维护第三套 identity 类型。

## Module 6: CharacterSimulationBuildOrchestrator

Orchestrator 改为显式 Build Request 驱动。Request 必须声明：

- CharacterPipelineDefinition
- publish 或 dry-run
- 有序且非空的 Numeric Target Build Adapter 集
- 每个 Target 的正式 artifact/wrapper destination

顺序固定为：

1. 运行唯一 Semantic Frontend。
2. 写入并重读 validated Semantic IR artifact。
3. 从 artifact 建立 Presentation Semantic Contract。
4. 解析正式 Animation Analysis artifacts。
5. 编译唯一 target-neutral Projection。
6. 每个请求的 Numeric Target Adapter 独立编译 Program。
7. 从每个目标 Program 重新建立 Presentation contract，并与 Frontend contract 精确比较。
8. stage Projection、全部请求的目标 artifact、wrapper 与 generated references。
9. 原子提交；任一阶段失败时全部恢复旧发布组。

Projection 和 Numeric Target 的执行先后不构成依赖。实现可以顺序执行，但任何编译方法参数都不能包含另一方产物。

默认 Editor“编译角色”入口使用显式安装 Target catalog，不从旧资产、场景或最近一次选择猜测 target。Product Build Adapter 必须显式声明所需 target；没有默认 Float32 fallback。

## Module 7: Artifact Publish Transaction

发布事务拥有一次 build 中请求的完整产物集合：

- Semantic IR cache descriptor
- target-neutral Projection asset
- 一个或多个 Numeric Target canonical artifact
- 对应 Unity wrapper/generated references

Target Program descriptor 各自保存 ProgramHash/LayoutHash/NumericProfile/ABI。Projection descriptor 保存 ContractHash/ProjectionRevision，不复制任何目标 ProgramHash。

事务提交前验证：

- 所有目标 Program 的 ProgramId、SourceRevision、SemanticHash 与 producer contract等于 Frontend contract。
- Projection ContractHash 等于 Frontend contract。
- ProjectionRevision 等于 schema、contract、Presentation dependency 和 Analysis artifact token的 canonical 结果。
- 请求 target 的 wrapper 与 exact canonical bytes一致。

失败不得留下新 Projection + 旧 Program、旧 Projection + 新 Program 或只提交部分 Numeric Target 的组合。

## Identity Model

| Identity | 归属 | Float32/Fixed 是否相同 | 覆盖内容 |
|---|---|---:|---|
| SemanticHash | Semantic IR | 是 | Gameplay operation、数据表与公共业务语义 |
| Presentation ContractHash | Semantic/Presentation Seam | 是 | ProgramId、SourceRevision、SemanticHash、ordered producer contract |
| ProgramHash | Numeric Target Program | 否 | target-specific numeric payload与Program ABI |
| LayoutHash | Numeric Target Program | 否 | target-specific state/program layout |
| ProjectionRevision | Presentation Projection | 是 | ContractHash、Projection schema、表现 authoring、Analysis artifact identity/content |
| Catalog/Session identity | Session composition | 否 | 精确 Program、Pipeline、Backend、Source、Solver 与 roster |

纯 AnimationClip、Analysis Source、Rig Calibration 或 Presentation Profile变化只改变 ProjectionRevision。纯 NumericProfile、rounding、Target ABI 或状态编码变化只改变对应 ProgramHash/LayoutHash。Gameplay producer contract变化同时改变 SemanticHash、ContractHash、各目标 ProgramHash 和 ProjectionRevision。

## Stale Detection

`CharacterSimulationProgramBuildService` 不再用 `projection.ProgramHash == floatProgram.ProgramHash` 判断 Projection current。

Projection current 条件为：

- ProgramId 与当前 Definition一致。
- Gameplay SourceRevision 与当前 Frontend一致。
- SemanticHash 与当前 validated artifact一致。
- ContractHash 与当前 ordered producer contract一致。
- ProjectionRevision 与当前 Presentation dependencies和Analysis artifact tokens一致。

目标 Program current 条件继续由自己的 exact artifact expectation判断。Projection stale 不得使未变化的 ProgramHash变化；单个 Target Program stale 也不得伪造 Projection stale。

## Runtime Composition

Float32/Fixed Host 的顺序统一为：

1. 严格加载目标 Program artifact并校验 ProgramHash/LayoutHash/NumericProfile/ABI。
2. 由目标 Adapter创建 Presentation Semantic Contract。
3. 以唯一 `ProjectionAsset.Load(contract)` 加载 Projection。
4. 创建 Presentation Runtime。
5. 把精确 Program identity 与独立 Presentation identity写入 Actor registration/diagnostics。

Presentation identity 不进入 ProgramCatalog、Snapshot、Network gameplay hash 或 State codec。ProgramHash 继续进入 Actor registration 与 Session compatibility，但不进入 Projection payload。

## Build Product Behaviour

- Character authoring 变化只把已发布产物变为 stale，不在域重载、资产导入或退出 Play Mode 后自动扫描和编译。作者通过显式角色编译、显式 Compile All Stale 或 Product Build 发起完整 Build Request；Host/Product validation 继续拒绝 stale 产物。
- Float32 Local/ServerAuthoritative product请求 Float32 Target Adapter与唯一 Projection。
- Local Fixed与Deterministic Rollback product请求 Fixed Target Adapter与同一 Projection。
- 同一 Definition 同时请求两个 target时，Frontend、Analysis resolver与Projection Compiler只执行一次。
- Fixed-only product不得为了 Projection 生成 Float32 Program。
- Float32-only product不得生成空 Fixed wrapper或默认 Fixed artifact。
- Product workflow 不得直接调用 target compiler并复制文件写入逻辑；必须通过唯一 Orchestrator和正式 Target Build Adapter。

## Migration and Deletion

实施按原子迁移处理，不保留中间双路径：

1. 先建立 Semantic Contract、Projection Compiler 和 target Adapter。
2. 在同一代码迁移中把全部 Runtime/Editor调用方切换到唯一 contract Interface。
3. 删除 Runtime Projection build implementation和两个旧 Load/Require分支。
4. 删除 Projection 的 ProgramHash、NumericProfileId、TargetAbiVersion serialized field与 Inspector 展示。
5. 删除 ProjectionRevision 中的 ProgramHash token并提升 schema。
6. 删除 Rollback/Fixed 手工 producer数组与重复 Fixed artifact发布实现。
7. 重新生成 Corin Projection、Float32 Program wrapper与Fixed Program asset。
8. 清理旧 generated data；不提供 `FormerlySerializedAs`、旧 reader、字段猜测或自动补值。

## Tradeoffs

### 选择：一份 Target-Neutral Projection

业务收益：同一角色在 Float32 Local、ServerAuthoritative、Fixed Local和Rollback下使用同一套动画、相机、Cue、Equipment Visual与Foot Analysis，不会因 Numeric Target复制或漂移表现资产。编译树在真正的公共语义层复用。

技术代价：Projection 不能用 ProgramHash作为现成 stale token，必须正式定义 Presentation ContractHash，并让 Semantic IR reader承担当前 Float32 Program reader做的 Camera/Cue解析。

### 未选择：每个 Numeric Target 生成独立 Projection

收益是 Projection 可以继续精确保存目标 ProgramHash，单个产物配对直观。代价是目标无关的动画、Foot Analysis、Camera、Cue与Equipment数据重复；新增 Target 就增加一份 Projection和stale/publish闭包。它也不符合当前 Local Fixed与Rollback复用同一表现的业务口径。

### 未选择：共享 Projection加每 Target Binding Manifest

收益是大 payload只保存一次，同时用一个小 manifest保存 `ProgramHash -> ProjectionRevision` 精确配对。代价是新增一个几乎只转抄两个 identity 的浅 Module，Program exact identity本来已经由ProgramAsset、Catalog与Session composition校验。当前没有独立部署 Projection/Program组合的业务需求，因此不增加该层；如果未来内容分发要求独立热切 Presentation bundle，应以新的正式 change重新评估。

### 未选择：立即拆分 Runtime asmdef

单独 Presentation assembly可以从编译层强制禁止 Float32/Fixed引用，但当前 `ThirdPersonClient.Runtime` 内仍存在合法 Float32 Host、Input和Presentation协调关系，立即拆分会扩大到大量程序集循环治理。本 change 先把 Interface和Implementation依赖清干净；不创建只为形式隔离的浅 assembly。

## Spec Conflicts and Resolution

- `character-animation-layer-runtime` 的“Projection匹配 ProgramHash”改为“Projection匹配 Presentation Semantic Contract与SourceRevision”。ProgramHash继续由目标Program/Session校验。
- `add-local-fixed-gameplay-lab` 的“复用同一Projection”保留，但实现从手工 semantic bypass改为正式 Fixed Adapter。
- `btsmtl-compiled-simulation-program` 的 Build 顺序改为 Projection 与请求 Numeric Target从同一 artifact独立编译，不再表达 Numeric Program是Projection输入。
- `deterministic-rollback-two-client-demo` 明确 Fixed Build不得先生成 Float32 Program只为得到Projection。
- `refactor-timeline-animation-authoring-boundary` 的Analysis artifact resolver与Projection payload继续保留；本 change只改变Projection Compiler输入和identity，不复制其artifact状态机。
- `add-character-presentation-pose-graph` 是本change完成后的后续Presentation schema迁移。它会把当前producer contract中的`LayerId`替换为`AnimationChannelId`，并扩展Projection的PoseSlot/BlendStack/Rig/PoseProgram payload；这些变化继续只影响ContractHash、ProjectionRevision与Presentation payload，不改变本change确立的Numeric Target分离规则。

## Risks

- Semantic IR Camera literal reader若与Target lowerer使用不同字段规则，可能形成第二套schema解释。处理方式是复用 `CameraProgramOperationSchema` 与 Semantic IR已校验的 literal/reference表，不复制Operation Code清单。
- 并行 active change正在修改Projection Analysis与生成资产。实施前必须重新读取current specs和最终字段，所有新编译逻辑以已安装artifact resolver为准。
- Projection serialized schema删除目标字段后所有旧asset失效。该风险被接受，唯一处理方式是正式重建，不提供兼容。
- 多Target发布事务扩大回滚范围。事务必须先stage并完成全部identity验证，再修改任何generated reference。
