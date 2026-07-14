# Tasks

- [x] 1. 确认当前 `TimelineNode` 的输入边、输出边、child 执行和 editor link 回调代码位置。
- [x] 2. 确认当前正式资产中 `TimelineNode` 没有非空输出边。
- [x] 3. 删除 `TimelineNode` 的 `[Output("Output", PortCapacity.Single)]` 声明。
- [x] 4. 删除 `TimelineNode` 的 `m_OutputEdgeGUID` 字段和 `OutputGUID` 属性。
- [x] 5. 删除 `TimelineNode` 的 `m_Child` 字段和 `Child` 属性。
- [x] 6. 调整 `Dispose`，不再清理 child 引用。
- [x] 7. 调整 `OnAfterDeserialize`，不再重置输出 GUID 和 child。
- [x] 8. 调整 `OnUpdate`，Timeline 播放成功后直接返回 `Success`。
- [x] 9. 调整 `OnStop`，不再停止 child。
- [x] 10. 调整 `OnReset`，不再 reset child。
- [x] 11. 调整 `ResolveFlowLinks`，只解析 `"Input"` edge。
- [x] 12. 删除 `OnOutputLinked`。
- [x] 13. 删除 `OnOutputUnlinked`。
- [x] 14. 清理当前资产中 `TimelineNode` 的空 `m_OutputEdgeGUID` 序列化残留。
- [x] 15. 搜索确认 `TimelineNode` 不再声明或引用输出控制流 port。
- [x] 16. 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj -nologo -v:minimal`。
- [x] 17. 运行 `openspec validate remove-timeline-node-output-flow --strict --no-interactive`。
