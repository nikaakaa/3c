# Timeline 秒制时间与时钟职责归档记录

归档日期：2026-09-30。用户已明确要求完成文档整理与归档；本 change 的 56/56 项实施任务完成，proposal、design、specs、tasks 均由正式 CLI 返回 done。

## 现行合同承接

补齐 btsmtl-timeline-clock-domain。其余旧增量按当前 btsmtl-timeline-direct-runtime、btsmtl-runnable-timeline-node、btsmtl-timeline-editor-preview 与 character-animation-pipeline 承接；不覆盖后续原生 Pose、Slate 与 TreeClip 合同。

本次先按现行架构合并有效增量并通过 `openspec validate --specs --strict`，再以 `openspec archive --skip-specs --yes` 移动已完成记录。`--skip-specs` 用于避免 CLI 再次覆盖已经人工合并或已被后续规格取代的旧 delta；没有跳过校验。原 delta 保留为当时的设计记录，不作为当前合同。

## 历史与验证边界

本目录的 implementation、execution、design、审查和旧阻塞描述保存当时事实；其中早期“尚未完成”、旧工作区或旧行号不再表示活跃待办。后续未完成工作由 `../../../../openspec/maintenance-audit.md` 所列未归档 change 拥有，不能由本 change 的完成替代。

本次只整理文档和已完成实施的规格，没有修改运行代码、Unity 资产或原始采样数据，没有新增测试，也未重新执行编译、Player 构建、replay 或性能采集。原记录中的各项验证范围继续有效，不扩大为新的行为验收结论。
