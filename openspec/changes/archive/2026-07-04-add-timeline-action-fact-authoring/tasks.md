# Tasks

- [x] 1. 搜索 `ActionWindowTrack`、`ActionCueTrack`、`TimelinePlaybackScheduler` 的现有采样链路。
- [x] 2. 确认 Timeline window/cue 采样只在有效 Action Context 下提交 action-scoped 输出。
- [x] 3. 确认空 Action Context 的普通 Timeline 不自动创建 ActionInstance。
- [x] 4. 检查 Timeline window/cue inspector 字段，不允许保存完整网络策略。
- [x] 5. 调整 Corin Attack Timeline，添加 `ActionWindowTrack`。
- [x] 6. 在 Corin Attack Timeline 中添加 Hit window clip。
- [x] 7. 在 Corin Attack Timeline 中添加 Cancel window clip。
- [x] 8. 调整 Corin Attack Timeline，添加 `ActionCueTrack`。
- [x] 9. 在 Corin Attack Timeline 中添加 Gameplay cue clip。
- [x] 10. 在 Corin Attack Timeline 中添加 VFX 或 Camera cue clip。
- [x] 11. 确认 Corin Attack ActionProfile 包含 Hit、Cancel、Gameplay、VFX/Camera 对应策略。
- [x] 12. 删除 Corin RootTree 中平铺的 `Submit Attack Window` 测试节点。
- [x] 13. 删除 Corin RootTree 中平铺的 `Submit Attack Cue` 测试节点。
- [x] 14. 删除 Corin RootTree 中平铺的 `Submit Loopback Result` 测试节点。
- [x] 15. 搜索确认 Corin RootTree 不再通过平铺节点补 Timeline 攻击 window/cue。
- [x] 16. 搜索确认没有新增 per-node window/cue 网络策略字段。
- [x] 17. 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj -nologo -v:minimal`。
- [x] 18. 运行 `openspec validate add-timeline-action-fact-authoring --strict --no-interactive`。
