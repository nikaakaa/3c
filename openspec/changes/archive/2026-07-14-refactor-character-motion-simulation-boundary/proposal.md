# Change: 重构角色运动模拟执行边界

## Why

当前 `CharacterMotionStage` 同时负责合成 `MotionIntent`、调用 Unity `CharacterController.Move`、读取碰撞结果、写入逻辑位姿和应用网络校正；`CharacterPipelineHost` 也把 `CharacterController` 作为所有控制模式的必需依赖。结果是：

- 本地角色运动语义与 Unity 场景组件绑定，无法在 Unity Dedicated Server、纯 C# 服务端运动求解之间替换实现；
- `ExternalPose` 远端角色仍被迫配置不参与求解的 `CharacterController`；
- 当前 `add-local-two-client-gameplay-network-closure` 只能把客户端已经求解的位移发给服务端做限幅，属于客户端求解、服务端校验，不是独立权威运动模拟；
- 如果直接在网络 change 中增加 Unity、DotRecast 或确定性 KCC 分支，会把 Network Model、运动语义和具体碰撞实现再次耦合，并形成多条位姿写入路径。

在继续双客户端网络闭环前，需要先把“想怎么动”“结合世界约束后实际怎么动”“逻辑位姿真值如何读写”拆成正式边界，并把现有 Unity 行为完整迁入其中。

## What Changes

- 新增无 Unity 类型的运动执行合同：输入当前逻辑体状态和最终 `MotionIntent`，输出实际位移、位姿、grounded 与碰撞摘要。
- 新增逻辑位姿端口，统一读取逻辑位姿、应用外部权威位姿和执行需要的显式重定位。
- `CharacterMotionStage` 保留 contribution 仲裁、modifier、correction phase 和 `MotionResult` 生成，但不再直接持有或调用 `CharacterController`、`Transform`。
- 把现有 `CharacterController.Move` 行为迁入唯一正式 Unity motion executor；该 executor 是当前 `LocalSolver` 的实现，不是 pipeline 的抽象本身。
- `CharacterPipelineHost` 改为显式装配 logic pose adapter 与按 authority mode 需要的 motion executor；`ExternalPose` 不再要求 `CharacterController`。
- 网络校正、外部位姿、grounded 读取和 resolved motion fact 全部经过新边界，不保留 direct Transform、第二 motor 或旧 `CharacterController` 注入路径。
- 迁移 Sandbox/Corin 现有装配后删除 Host、Pipeline、MotionStage 上的旧 `CharacterController` 字段和构造参数，不增加 fallback、自动搜索或兼容字段。
- 明确后继方案边界：Unity 权威进程与纯 C# KCC 是 `ServerAuthoritativeHybrid` 下的两种服务端运动模拟实现；确定性 KCC 帧同步/回滚是另一完整 Network Model，不强塞进当前 float 运动执行合同。
- 重写 `add-local-two-client-gameplay-network-closure` 中“服务端只校验 resolved motion”的设计与任务，使其在本 change 完成前保持依赖阻塞，并在后续选择一个独立权威模拟后再 apply。

## Scope

本 change 只完成现有角色运动主线的抽象、Unity 实现迁移、资产装配迁移和网络合同纠偏。它不实现 Fantasy 双客户端、Unity Dedicated Server 启动、纯 C# KCC、DotRecast 导航、确定性物理、帧同步或 rollback。

## Current Spec Comparison

- `character-motion-semantics` 当前明确要求 `CharacterMotionStage` 直接调用 `CharacterController.Move`，与可替换执行器边界冲突；本 change 修改该要求。
- `character-pipeline-runtime` 当前要求 Host 使用 `CharacterController` 创建 pipeline，并把 `LocalSolver`/`ExternalPose` 只描述为枚举行为；本 change 改为显式 logic pose 与 executor 装配。
- `character-presentation-interpolation` 当前把 `CharacterController` 或等价 root 描述为逻辑真值；本 change 保留 logic/visual root 分离，但把逻辑真值来源改为正式 logic pose port。
- `character-network-sync-domain-contract`、`gameplay-network-model-boundary` 和 `server-authoritative-hybrid-sync-model` 当前允许把 resolved motion 直接映射成 `MotionCommand`；这可以用于预测对账，却不能被描述为服务端独立权威模拟。本 change 收紧该语义。
- `character-root-motion-curves` 的 authoring 方向不变：Timeline 仍把 root motion 曲线提交为 `MotionContribution`，不直接驱动 Transform，也不选择运动后端。
- `openspec/project.md` 当前仍把已归档的 network model boundary 写成待归档，并未记录运动执行边界；apply 阶段需要同步现行架构真相。

## Impact

- **Affected specs**: `character-motion-simulation-boundary`、`character-motion-semantics`、`character-pipeline-runtime`、`character-presentation-interpolation`、`character-network-sync-domain-contract`、`gameplay-network-model-boundary`、`server-authoritative-hybrid-sync-model`
- **Affected runtime**: `CharacterPipelineHost`、`CharacterPipeline`、`CharacterMotionStage`、motion correction、external pose、motion diagnostics、resolved motion facts
- **Affected Unity assembly**: 新的 Unity logic pose adapter 与 `CharacterController` motion executor
- **Affected assets**: Sandbox 中 Corin Host 的 logic pose / executor 显式装配
- **Affected pending change**: `add-local-two-client-gameplay-network-closure` 必须先移除 resolved-motion-as-authority 设计，再选择正式服务端模拟实现
- **Breaking changes**: 删除 pipeline 主线中的 concrete `CharacterController` 依赖和旧序列化字段；缺少正式 adapter 的 authority mode 直接配置失败

