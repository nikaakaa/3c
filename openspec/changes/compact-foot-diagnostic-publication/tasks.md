## 1. 单一存储与发布

- [x] 1.1 将 Analyzer 与 Publisher 改为内存事实交接，删除展开 facts.json 写回及全文重读。
- [x] 1.2 实现紧凑明细、版本化清单和索引 Reader，完整保留派生观察与原始帧引用。
- [x] 1.3 报告删除全量复制并限制代表预览，评分与全部统计保持不变。
- [x] 1.4 同步采样、Launcher、MCP、Replay 的产物引用及已有测试的失效 API。

## 2. 验证和交付

- [x] 2.1 对已有原始包在独立目录离线执行唯一 Analyzer/Publisher，核对全部 Target、coverage、评分和索引。
- [x] 2.2 记录前后文件大小和阶段耗时，执行 Editor 规定参数构建并 shutdown build server。
- [x] 2.3 执行 change/all 严格校验与 diff check，核对本次提交文件范围。
