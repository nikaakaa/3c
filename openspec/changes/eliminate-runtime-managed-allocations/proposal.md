## Why

项目要求运行热路径严格 0 GC。当前全项目源码盘点已在模拟装配、Pose 帧准备、Timeline 快照、世界求解、回滚提交、网络编码和诊断中找到分配点，不能仅优化值求值器就宣称达标。需要沿正式运行链统一存储所有权、容量和消费寿命，避免战斗、重放、收包或表现阶段反复产生托管垃圾。

## What Changes

- 覆盖项目客户端 Main/HotFix、模拟与表现、网络、UI/资源、服务端和共享代码，以及第三方正式调用边界；按源码证据、调用频率和待测状态建账，详见 audit.md 与 source-inventory.json。
- 将反复创建的执行上下文、工作集合和结果容器改为由现有 Session/Actor/领域 owner 准备并有界复用；保持 Evaluate/Finalize/Commit/Discard、回滚及异步消费的所有权。
- 为 Timeline、Pose/IK、相机、输入/AI、世界求解、网络和诊断建立各自容量合同，不建全局通用对象池、第二求值器或新驱动链。
- **BREAKING**：将热点中拥有新数组/列表的返回接口迁为正式借用视图或有明确释放边界的租用结果，全部消费者一起迁移；淘汰旧分配型热点入口，不保留双接口兼容路径。
- 复用既有性能采集工作流，报告运行线程分配、首次使用/容量峰值、回滚重放和诊断开启场景；不能用关闭 GC、关闭正常功能或把分配移到后台冒充 0 GC。
- 启动、加载、编辑器显式加工和离线工具单独记录分配及存活边界；战斗中触发的技能、特效、消息和角色租用属于运行热路径，不算启动豁免。

## Capabilities

### New Capabilities

- `runtime-managed-memory-ownership`：全项目运行阶段零托管分配、容量、租用、跨帧消费及归因合同。

### Modified Capabilities

- `gameplay-performance-capture-workflow`：沿现有采集入口增加 0 GC 的分场景、分线程、分阶段证据边界，不建立第二采集流程。

## Impact

涉及 Simulation/Core 两数值实现、Pipeline/WorldSolver、Timeline、原生 Pose/动画/Foot IK、Camera、输入与 AI、Rendering、HotFix/UI/资源、Rollback/Authority codec/transport、服务端宿主和开发诊断。第三方内部存在分配时先核实正式调用与插件已有 API，不凭目录名判定死代码或直接修改插件算法。

本次只做全项目静态审计与规划，不宣称已测得全项目分配字节数，也不承诺所有候选都是真实热分配。没有 CPU/GC 基线时不排优化收益优先级。求值缓存、脏传播、短路修复属于独立语义决定，不能与本次存储生命周期改造混合。
