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
| 1.1-1.4 基础构图与输入合同 | 未开始 | 已确认默认轨道与鼠标偏移链需要保留，单位/重置/响应合同仍需落代码。 |
| 2.1-2.4 平滑、裁决与生命周期 | 未开始 | 已定位 History 清零速度和 Response 同权前置判断问题。 |
| 3.1-3.2 锁定和多目标构图 | 未开始 | 已有点绑定解析，缺少正式双点、多点、实体构图和失效结束。 |
| 4.1-4.5 效果完整实现 | 未开始 | Zoom/Stretch 有有限求值；Override/Shake/Shot 编译与求值仍有拒绝点。 |
| 5.1-5.3 碰撞与环境约束 | 未开始 | Profile/Projection 有碰撞字段，但当前没有查询端口和求解阶段。 |
| 6.1-6.5 作者、动作资源与正式发布 | 未开始 | Camera producer 适配和资源编译入口仍需闭合；Shot 来源 prefab 尚未形成当前工程引用闭包。 |
| 7.1-7.4 诊断、迁移与删除 | 未开始 | 快照字段不完整，输入回放仍搜索旧 Controller，旧 Controller 尚未删除。 |

## 已确认的实施阻塞边界

- 来源证据未闭合的 Shot prefab 本体、Timeline 绑定和 compact-blend-words 字段不能用名称或占位资源补齐；对应能力必须保持明确失败，直到存在正式资源闭包。
- 来源没有证明的字段公式不能用临时近似发布为 ZZZ 还原；可以实现通用合同和已证明几何分支，但必须保留未验证边界。
- 当前工作区的输入诊断与角色 Prefab 有用户改动，不能覆盖或回退；若与旧 Controller 删除发生真实冲突，记录具体文件与调用者后发送一次 `ACTUAL_CONFLICT`。

## 修改与证据

本节随实际修改追加。编译、Unity MCP、回放和截图只记录真实执行结果及其边界。

