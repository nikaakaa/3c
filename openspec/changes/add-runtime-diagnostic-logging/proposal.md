# Change: 增加运行时诊断日志系统

## Why
当前项目已经有相机等局部 `debugLog` 开关，但缺少统一的运行时日志抽象。状态机调试主要依赖 Inspector 字段和测试断言，Play Mode 中无法稳定观察 FullBody/Locomotion 状态路径、Action 进入退出和拒绝原因，也无法通过统一宏定义在打包时裁切诊断日志。

## What Changes
- 新增 `runtime-diagnostic-logging` 能力，提供项目统一的诊断日志入口、日志分类、等级和格式约定。
- 日志调用必须受编译宏 `THIRDPERSON_DIAGNOSTIC_LOGS` 控制，未定义时常规诊断日志在构建产物中可被裁切。
- 支持运行时按分类开关，第一版至少覆盖 FullBody 状态树、Locomotion 状态机和 Action/Dodge 仲裁结果。
- 新增场景内 Inspector 开关控制器，允许在 Play Mode 中开关不同日志通道 key，并通过前缀/后缀/包含文本快速批量筛选通道。
- 将状态机日志接入现有 `FullBodyStateSnapshot`、`BasicLocomotionStateMachine.ActivePath`、`ActionRuntimeStateTracker` 等只读事实，不新增第二状态权威。
- 保留现有局部 log，不删除、不强制迁移；后续只在审批范围内逐步合流。

## Impact
- Affected specs: `runtime-diagnostic-logging`
- Related specs/changes: `unityhfsm-locomotion`, `action-runtime-state-tracker`, `add-fullbody-hfsm-state-tree`, `add-fullbody-action-framework`, `add-dodge-action-profile`
- Affected code: `Assets/Scripts/Diagnostics/*`, `Assets/Editor/Diagnostics/*`, `PlayerFullBodyActionController`, `FullBodyHfsmStateTreeDriver`, `BasicLocomotionStateMachine` 或其调用方、相关 EditMode 测试
- Non-goals: 不接入第三方日志框架，不删除现有 `Debug.Log`，不改变状态机 transition，不新增独立状态机或控制器路径
