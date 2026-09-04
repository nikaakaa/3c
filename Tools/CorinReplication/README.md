# Corin 复刻资料生成器

输入是 `sources.json` 明确指定的 Corin 控制器、动画清单与 Odin 解码结果；输出是同一份外部资料包中的动作页、镜头参数、窗口、事件、来源哈希和机器可读 JSON。

```powershell
python -X utf8 Tools/CorinReplication/build_guide.py
```

正式阅读入口：`D:/ZZZ_Dump/output/corin_replication/replication-guide/README.md`。

复用 `D:/ZZZ_Dump/kern_tools/OdinBinaryDecoder.py` 的 materialize，不另建二进制解析器。生成前对原始文件 SHA-256 对账，内部 Odin 引用按原 identity 解析。未知外部引用、条件枚举、负时间与不同时间域保持原值；同名资源保留重复来源对账，不按名称混用。

重点动作包括普攻、分支、冲刺攻击、反击和闪避。切人保留在完整控制器数据中，本轮不创建动作阅读页。数据只用于分析，不作为 3C 的运行时配置或第二条导入路径。

`analysis/` 由后续研究工具和分析记录维护，重复生成阅读投影不会覆盖该目录。默认不运行 Unity、不构建项目、不修改作者资产。
