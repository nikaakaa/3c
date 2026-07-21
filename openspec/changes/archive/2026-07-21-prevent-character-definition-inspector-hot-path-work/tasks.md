# Tasks

## 1. 固化状态边界

- [x] 1.1 在 Character Simulation Editor 构建服务中增加只读取发布 Header 的轻量检查入口。
- [x] 1.2 让现有完整发布元数据校验复用轻量 Header 前置检查，保持唯一字段规则。
- [x] 1.3 保持 `IsStale` 的完整 SourceRevision、ProjectionRevision 与 Target Artifact 检查语义不变。

## 2. 重构 Definition Inspector 状态机

- [x] 2.1 为 Inspector 建立 `Missing/Invalid/Unchecked/NeedsCompile/Ready/Stale` 会话状态。
- [x] 2.2 在 `OnEnable` 只通过轻量 Header 初始化状态。
- [x] 2.3 删除 `OnInspectorGUI`、状态绘制与 foldout 路径对 `IsStale` 的调用。
- [x] 2.4 增加显式 `Refresh Status` 命令，并只在该命令中运行精确 stale 检查。
- [x] 2.5 让 Definition 序列化修改把状态切换为 `Needs Compile`。
- [x] 2.6 让 Compile 成功把状态切换为 `Ready`，失败时不伪造成功状态。
- [x] 2.7 让 HelpBox 的文字与严重级别准确区分未检查、待编译、失效和错误。

## 3. 收口与校验

- [x] 3.1 检查 Definition Inspector 热路径不再引用 `IsStale`、Program decode 或完整 revision 入口。
- [x] 3.2 使用独立 build server 参数编译 ThirdPersonClient.Editor，并在结束后关闭 build server。
- [x] 3.3 严格校验 OpenSpec change，并核对任务状态与最终实现一致。
