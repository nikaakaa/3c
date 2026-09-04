# Corin 复刻资料生成器

输入是 `sources.json` 明确指定的 Corin 控制器、动画清单与 Odin 解码结果；输出是同一份外部资料包中的动作页、镜头参数、窗口、事件、来源哈希和机器可读 JSON。

```powershell
python -X utf8 Tools/CorinReplication/build_guide.py
```

正式阅读入口：`D:/ZZZ_Dump/output/corin_replication/replication-guide/README.md`。

复用 `D:/ZZZ_Dump/kern_tools/OdinBinaryDecoder.py` 的 materialize，不另建二进制解析器。生成前对原始文件 SHA-256 对账，内部 Odin 引用按原 identity 解析。未知外部引用、条件枚举、负时间与不同时间域保持原值；同名资源保留重复来源对账，不按名称混用。

重点动作包括普攻、分支、冲刺攻击、反击和闪避。切人保留在完整控制器数据中，本轮不创建动作阅读页。数据只用于分析，不作为 3C 的运行时配置或第二条导入路径。

`analysis/` 由后续研究工具和分析记录维护，重复生成阅读投影不会覆盖该目录。默认不运行 Unity、不构建项目、不修改作者资产。

## 已有资料的重建顺序

公共资源的定位、原始导出及 Odin 解码结果已保存在资料包 `analysis/camera-resource-search/`。无需重新扫描游戏即可重建阅读资料和证据：

```powershell
python -X utf8 Tools/CorinReplication/build_guide.py
python -X utf8 Tools/CorinReplication/analyze_guide.py
python -X utf8 Tools/CorinReplication/decode_native_curves.py
python -X utf8 Tools/CorinReplication/complete_camera_data.py
python -X utf8 Tools/CorinReplication/build_guide.py
```

`analyze_guide.py` 比较来源变体，导出相机字段和枚举声明，并在校验 GameAssembly SHA-256 后读取有限函数体。无异常目录范围的函数只登记未解析，不冒充缺失代码。

`decode_native_curves.py` 只读取本批已确认的 Unity 原生 AnimationCurveLibrary 格式，验证空 Odin 基类字段、关键帧布局和完整文件消费。它与 Odin 字典是两种不同序列化格式；不尝试猜测其它原生对象。

`complete_camera_data.py` 输出公共曲线、基础镜头组与全部变体，使用原始 Odin 节点重新投影，避免旧 JSON 的简化 value 部分遗漏未命名分量。

本轮对既有外部工具的正式修正保存在 `analysis/camera-resource-search/locator-terms.diff` 和 `odin-positional-fields.diff`：定位器增加 `--terms-file`；同一 Odin materialize 补齐 Vector/Quaternion/Color 分量、内嵌 AnimationCurve 及其它未命名字段。原始资源未改写。
