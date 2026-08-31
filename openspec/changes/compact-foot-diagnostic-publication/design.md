# Design

## 唯一链路

Sealed CSV 与几何经过现有 Analyzer 完整校验和诊断，得到同一次内存事实。Publisher直接消费它，不经大型 JSON 磁盘中转。后台 Finalizer继续唯一拥有完成与失败状态。

## 存储和查询

`diagnoses/analysis.json` 保存 schema、输入 SHA-256、样本与程序身份、Analyzer 版本、coverage、明细文件和索引身份、发布性能数据。`details.jsonl` 每条保存一种事件或派生观察；`details-index.json` 保存唯一 ID、类别、脚侧、帧范围、字节位置与长度。所有文件在同一个 staging 目录完整写入后才发布。

诊断 JSON 保持每问题一文件；质量总览继续是 `quality-score.json`。不再包含全量骨盆帧和 Step 候选数组；全量观察通过明细类别索引定位。同一个事件被多个 Target 引用时只存一份明细；报告保留最多五条代表摘要，不复制完整阶段对象。Reader根据索引只读指定记录，校验身份和完整性，不能静默扫描或读取旧 facts.json。

CSV和geometry的正式解析同时生成按Frame/Side分组的UTF-8字节范围及校验和；查询原始帧只读取该范围。只读`character.foot_diagnostics`入口提供summary、events分页、detail、frame，不触发录制、分析或Unity状态变化。Frame查询允许显式列筛选，未请求完整列时不返回宽行镜像。

## 不变项

规则、阈值、eligible/matched、完整分布、Health/Evidence 与七维评分数学保持。格式升级不表示行为改善。原始采样和历史报告不覆盖；仅在独立新输出目录离线分析。缺字段、非法值、缺证据继续显式失败或按既有业务域发布 Unavailable，不补默认值。

## 性能与验证

记录读取/校验、分析、发布耗时和输出字节数。验收对比已有同一 raw 的全部 Target 指标、评分、coverage及事件总数，校验全部索引可读与摘要引用有效。精确统计沿用现有算法，不引入近似分位数或降采样。额外取舍是详细证据需要一次索引读取，换取报告大小不再随全部采样帧线性膨胀。

## 已执行的离线对账

输入为`20260831-092855-825-6211bdaf960f461a8d4e96a533d38f58`的原始2086条Foot行、1043帧与50195条geometry。使用同一普通.NET宿主分别调用既有Unity编译Editor DLL中的旧Analyzer和本次构建Editor DLL中的新Analyzer，不启动Unity、不复制诊断公式。旧控制输出位于`ArchivedAnalyses/facts65-offline-storage-control-092855`，新输出位于`ArchivedAnalyses/facts66-compact-092855`。

37个Target的规则、eligible/matched、分布、Health/Evidence、coverage与61.9分完全一致；保留的14787事件、2086 Landing Reach、1043 Pelvis、2086 Step候选共20002条明细逐值完全一致，全部明细校验和与3873个源字节范围校验通过。原Unity生成报告与两次.NET重算存在相同的浮点末位及PrimaryProbe平局选择差异，不能描述成跨宿主所有浮点完全一致，也没有修改阈值消除差异。

单次冷调用参考：旧Analyzer约9.77秒，新链约4.75秒；新链读取校验约0.89秒、分析约2.62秒、发布约1.19秒，不作为通用耗时保证。9份报告共459686字节；Step Time约8.7KiB、Landing Leg约9.5KiB、Swing约117.4KiB。完整明细59076981字节、索引4864961字节、manifest4543字节，默认看报告不加载这些明细。生产只读查询入口已在普通.NET进程直接调用验证summary、事件分页、detail及样本/几何帧查询；首轮约74–122毫秒，最终复查事件分页约405毫秒，其余约75–113毫秒，不作为延迟保证。非法detail ID明确返回InvalidDataException。未操作Unity或Replay。
