# 实施记录

## 隔离交付

- 目标分支：`codex/kcc-step-solving`
- 独立工作树：`D:\Unity_Project_1\3C-worktrees\kcc-step-solving`
- 基线 commit：`729d376e80b5eb7d75ce208ea6605d61a3607537`
- 隔离实现 commit：`b5f84db6e7320978a3e36951bfcb7c1dcd9aa329`
- 当前 `main` 没有接收该实现提交。
- 本 change 保持 active，不单独归档。

## 只读依赖合同

### Continuous Cast Contact

`DeterministicCapsuleQueries.Cast` 提供：

- canonical 最早 TOI contact set；
- `PrimitiveId`、`SurfaceId` 与 `FeatureId`；
- contact normal；
- character/world witness；
- separation 与 penetration；
- normalized `TimeOfImpact`；
- 固定容量 `m_CastContacts`，按 TOI、primitive、feature 和 witness 排序。

Step Solver 由调用方传入现有 contact buffer 与显式 count，不复制集合，不持有跨 Tick contact cache。

### Ground 与 Support

`DeterministicKccGroundReport` 提供：

- `FoundAnyGround`；
- `IsStableOnGround`；
- support primitive/feature；
- ground normal；
- probe distance；
- ledge state。

`DeterministicCollisionWorldArtifact` 提供 dense canonical primitive/surface catalog 与 triangle adjacency。Step 模块只保存 artifact 和 query 的只读引用，不拥有 world state。

### Capsule Query

Step 模块只调用现有：

- `Cast`：顶部探测、真实高度上抬、前向 clearance、Step Down；
- `Overlap`：最终 pose validation；
- `CastContactAt` 与 `OverlapContactAt`：读取当前 query 的预分配结果。

没有修改 `DeterministicCapsuleQueries`，没有增加 Raycast、Physics fallback、动态 buffer 或新 query 注册。

### Step Policy

`DeterministicKccStepPolicy` 显式输入：

- capsule radius；
- skin width；
- `MaximumStepHeight`；
- `MinimumStepDepth`；
- `GroundSnapDistance`；
- `MinimumMovementDistance`；
- stable ground normal threshold；
- query tolerance。

并行模块不从旧 `MinimumStepForwardDistance` 构造 policy，不修改当前配置或序列化资产。正式字段迁移留给接入 change。

## 新增模块

### `DeterministicKccStepContracts`

定义 Step Up/Down request、immutable policy、surface identity、landing evidence、atomic candidate、唯一 rejection、阶段诊断和 support result。所有类型均为当前调用栈内值或只读数组引用，不进入 snapshot。

### `DeterministicKccStepGeometry`

封装 Step 模块对现有 capsule query 和 artifact adjacency 的只读使用，集中 stable normal、landing 选择、primitive 关联、ground report 和 overlap validation。

### `DeterministicKccStepSupportEvaluator`

先处理普通稳定 face 和双稳定 seam；只有普通稳定判断失败时才执行 outer/inner secondary probe。outer 非稳定、inner 稳定且角色仍朝内侧移动或 previous support 连续时，返回 inner 顶部法线与明确 ledge state。

### `DeterministicKccStepSolver`

唯一入口：

- `TryStepUp`：资格检查、canonical blocker、outer/inner 顶部、真实高度、上方净空、受请求约束的部分前移、最终落地、overlap、consumed/remaining。
- `TryStepDown`：previous stable support、平面进展、非向上请求、Snap 失败、最大高度内 landing、edge support 和原子结果。
- `SupportEvaluator`：供后续 Grounding 接入同一个台阶鼻部判断。

## 未修改边界

- `DeterministicKccMotor.cs`
- `DeterministicKccConfiguration.cs`
- `DeterministicKccWorldSolver.cs`
- `DeterministicKccWorldSolverDefinition.cs`
- `CorinDeterministicKcc.asset`
- Solver descriptor、Composition、Session
- Motor/Solver semantic version
- configuration hash、KCC identity、snapshot

没有 runtime flag、第二个 KCC、第二份配置、compatibility reader、adapter 或 fallback。

## 校验

- portable `ThirdPersonSimulation.DeterministicKcc.csproj` 编译成功。
- 编译参数：`--disable-build-servers /nr:false /p:UseSharedCompilation=false`
- 结果：0 warning，0 error。
- 编译后已执行 `dotnet build-server shutdown`。
- 没有运行 Unity batchmode。
- 没有新增或运行自动化测试。
