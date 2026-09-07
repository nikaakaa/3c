import argparse
import hashlib
import json
import re
import struct
from pathlib import Path

from trace_zzz_dump_render_graph import virtual_read


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b""):
            value.update(chunk)
    return value.hexdigest()


def compact(value):
    if value is None:
        return "未读到"
    if isinstance(value, bool):
        return "是" if value else "否"
    if isinstance(value, float):
        return f"{value:.7g}"
    if isinstance(value, list):
        if len(value) > 4 and len(value) % 4 == 0:
            return "<br>".join(compact(value[i:i + 4]) for i in range(0, len(value), 4))
        return "(" + ", ".join(compact(item) for item in value) + ")"
    if isinstance(value, dict):
        if all(key in value for key in ("r", "g", "b", "a")):
            return compact([value[key] for key in ("r", "g", "b", "a")])
        return json.dumps(value, ensure_ascii=False, separators=(",", ":")).replace("|", "\\|")
    return str(value).replace("|", "\\|").replace("\n", " ")


def table(headers, rows):
    return "\n".join(["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"] +
                     ["| " + " | ".join(compact(value) for value in row) + " |" for row in rows])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path, required=True)
    parser.add_argument("--project-audit", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists; preserve prior catalog")
    repo = Path(__file__).resolve().parents[2]
    client = repo / "3cDemo/Client/3C_Client"
    resources = client / "Assets/AssetArt/Model/ZZZ/可琳/可琳tex/ZZZ导出"
    evidence = args.evidence_root.resolve()
    sources = {}

    def load(key, path, kind):
        sources[key] = {"path": str(path.resolve()), "sha256": digest(path), "kind": kind}
        return json.loads(path.read_text(encoding="utf-8-sig"))

    texture_contract = load("纹理合同", resources / "Raw/texture-raw-export.json", "磁盘资源")
    if texture_contract["Schema"] != "zzz-texture-source-contract/1":
        raise ValueError("Texture contract schema differs")
    materials = {}
    for name in ("Body", "Hair", "Face", "Eye", "Weapon", "HairShadow"):
        filename = "MAT_HairShadow_ZZZ.json" if name == "HairShadow" else f"MAT_Corin_{name}_ZZZ.json"
        source = load("材质-" + name, resources / filename, "磁盘材质导出")
        materials[name] = {"source": "材质-" + name, "original_name": source["m_Name"],
                           "shader_reference": source["m_Shader"],
                           "keywords": source.get("m_ValidKeywords", []),
                           "render_queue": source.get("m_CustomRenderQueue"),
                           "disabled_passes": source.get("disabledShaderPasses", []),
                           "enabled_pass_mask_raw": source.get("enabledPassMask"),
                           "properties": source["m_SavedProperties"]}

    generated = client / "Assets/Render/ZZZRestored/Generated"
    body_manifest = load("主体Shader来源", generated / "Character/NapAvatarStandardSource.json", "恢复来源清单")
    specialized = load("面部眼部Shader来源", generated / "Character/SpecializedShaderSource.json", "恢复来源清单")
    shadow_manifest = load("HairShadow来源", generated / "HairShadow/NapStencilShadowCasterSource.json", "恢复来源清单")
    shaders = {}
    for name, manifest in (("Standard", body_manifest), ("Face", specialized["face"]),
                           ("Eye", specialized["eye"]), ("HairShadow", shadow_manifest)):
        key = "原Shader-" + name
        original = load(key, Path(manifest["source"]), "原磁盘Shader导出")
        if sources[key]["sha256"] != manifest["sourceSha256"]:
            raise ValueError("Original shader hash differs: " + name)
        parsed = original["m_ParsedForm"]
        shaders[name] = {"source": key, "properties": parsed["m_PropInfo"]["m_Props"],
                         "passes": [{"subshader": i, "pass": j, "state": p["m_State"]}
                                    for i, subshader in enumerate(parsed["m_SubShaders"])
                                    for j, p in enumerate(subshader["m_Passes"])]}
        del original, parsed
    shader_by_material = {"Body": "Standard", "Hair": "Standard", "Weapon": "Standard", "Face": "Face",
                          "Eye": "Eye", "HairShadow": "HairShadow"}
    for name, material in materials.items():
        material["shader_catalog"] = shader_by_material[name]

    global_sets = {}
    for key, directory in (("829-主体及合成", "20260905_shader_parameter_snapshot_v5"),
                           ("829-眼部环境", "20260905_eye_environment_snapshot_v1"),
                           ("829-描边", "20260906_outline_globals_v1")):
        global_sets[key] = load(key, evidence / directory / "shader-parameters.json", "829非原子全局缓存")
    graph = load("0905-渲染对象图", evidence / "20260905_render_graph_v1/render-graph.json", "0905物理快照")
    residents = load("0905-角色对象", evidence / "20260905_nap_render_entity_resident_v1/resident-objects.json",
                     "0905物理快照")
    inputs = load("0905-光照输入", evidence / "20260905_entity_gpu_input_snapshot_v3/entity-gpu-input.json",
                  "0905身份未归属的光照记录")
    audit = load("3C-实际运行", args.project_audit, "项目观测，不是ZZZ")
    entity_address = "0x7005f028640"
    entity = next(item for item in graph["entities"] if item["virtual"] == entity_address)
    resident = next(item for item in residents["objects"] if item["virtual"] == entity_address)

    bindings = {}
    for key, directory, entry in (("主体PS", "20260904_typed_named_shader_v3", 2705),
                                  ("主体VS", "20260904_typed_named_shader_v3", 65),
                                  ("描边PS", "20260904_typed_named_shader_v3", 48),
                                  ("描边VS", "20260904_typed_named_shader_v3", 24),
                                  ("面部PS", "20260905_corin_face_shader_typed_v2", 1980),
                                  ("眼部PS", "20260905_corin_eye_shader_typed_v3", 480)):
        bindings[key] = load("参数表-" + key, evidence / directory / f"entry{entry}.bindings.json", "原DXBC参数绑定")
    load("NapCB索引读取", evidence / "20260906_napcb_catalog_getter_v1/native_evidence.json", "原PE汇编")
    load("NapCB包装读取", evidence / "20260905_napcb_getset_native_v1/native_evidence.json", "原PE汇编")

    dump = Path(graph["dump"])
    size = dump.stat().st_size
    cr3 = int(graph["cr3"], 16)
    reads = []
    with dump.open("rb") as stream:
        def read(address, count):
            value = virtual_read(stream, size, cr3, address, count)
            if value is None:
                raise ValueError(f"Required offline bytes are absent: {address:#x}")
            reads.append({"virtual": hex(address), "bytes": count, "raw_hex": value.hex()})
            return value

        wrapper = int(entity["napCBuffer"], 16)
        buffer = struct.unpack("<Q", read(wrapper + 16, 8))[0]
        array = struct.unpack("<Q", read(buffer + 24, 8))[0]
        count = struct.unpack("<I", read(array + 24, 4))[0]
        if count != 41:
            raise ValueError(f"NapCB array count differs from the proved getter range: {count}")
        raw = read(array + 32, count * 16)

    names = {}
    for key, binding in bindings.items():
        for item in binding["parameters"]:
            if item["buffer"] != "UnityNapCB":
                continue
            slots = 4 if item["kind"] == "matrix" else max(1, item["m_ArraySize"])
            for index in range(slots):
                offset = item["m_Index"] + index * 16
                label = item["name"] + (f"[{index}]" if slots > 1 else "")
                names.setdefault(offset, set()).add(label)
    vectors = [{"index": index, "byte_offset": index * 16,
                "names": sorted(names.get(index * 16, [])),
                "float_view": list(struct.unpack_from("<4f", raw, index * 16)),
                "uint_view": list(struct.unpack_from("<4I", raw, index * 16)),
                "raw_hex": raw[index * 16:(index + 1) * 16].hex()} for index in range(count)]
    napcb = {"dump": str(dump), "dump_bytes": size, "dump_modified_ns": dump.stat().st_mtime_ns,
             "cr3": hex(cr3), "entity": entity_address, "wrapper": hex(wrapper), "buffer": hex(buffer),
             "array": hex(array), "payload": hex(array + 32), "vector_count": count,
             "payload_sha256": hashlib.sha256(raw).hexdigest(), "vectors": vectors, "reads": reads,
             "boundary": "CPU数组内容，不等于同帧GPU已上传值；不能与829或其它更新阶段合并"}

    project_calls = []
    runtime = client / "Assets/Render/ZZZRestored/Runtime"
    patterns = (r'command\.(SetGlobal\w+)\("([^"]+)",\s*(.*?)\);',
                r'command\.(SetCompute\w+)\(shader,\s*(?:kernel,\s*)?"([^"]+)",\s*(.*?)\);')
    for filename in ("CorinRestoredRenderProfile.cs", "CorinRestoredEntityLighting.cs"):
        path = runtime / filename
        sources["项目源码-" + filename] = {"path": str(path), "sha256": digest(path), "kind": "项目实现"}
        code = path.read_text(encoding="utf-8-sig")
        for pattern in patterns:
            for match in re.finditer(pattern, code, re.S):
                project_calls.append({"file": filename, "line": code[:match.start()].count("\n") + 1,
                                      "api": match[1], "name": match[2],
                                      "expression": " ".join(match[3].split())})
    for filename in ("CorinRestoredRendererFeature.cs", "CorinRestoredRenderEntity.cs"):
        path = runtime / filename
        sources["项目源码-" + filename] = {"path": str(path), "sha256": digest(path), "kind": "项目实现"}
    count_summary = {"materials": len(materials), "textures": len(texture_contract["Textures"]),
                     "shader_property_declarations": sum(len(value["properties"]) for value in shaders.values()),
                     "stored_material_scalars": sum(len(value["properties"].get("m_Floats", {})) for value in materials.values()),
                     "stored_material_colors_vectors": sum(len(value["properties"].get("m_Colors", {})) for value in materials.values()),
                     "global_observations": sum(len(value["properties"]) for value in global_sets.values()),
                     "global_names": len({p["name"] for value in global_sets.values() for p in value["properties"]}),
                     "napcb_vectors": count, "prepare_input_records": len(inputs["records"])}
    result = {"schema": "zzz-corin-render-data-catalog/1", "scope": "可琳角色渲染已有证据，不是整个游戏全量数据",
              "live_process_read": False, "atomic_frame_capture": False, "counts": count_summary,
              "sources": sources, "textures": texture_contract["Textures"], "materials": materials, "shaders": shaders,
              "global_sets": global_sets, "corin_entity": entity, "corin_entity_cache": resident["napRenderEntity"],
              "corin_napcb_cpu": napcb, "unattributed_prepare_input": inputs,
              "project_audit": audit, "project_bindings": project_calls,
              "generator_sha256": digest(Path(__file__))}
    args.output.mkdir(parents=True, exist_ok=False)
    (args.output / "整理数据.json").write_text(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n",
                                               encoding="utf-8")

    lines = ["# 原始参数明细", "", "只汇集 dump、原磁盘资源及原参数表。当前项目数据在末尾独立列出。",
             "阅读数值约保留 7 位有效数字；完整 float/uint 视图、原字节、来源与哈希见同目录 `整理数据.json`。",
             "快照中的零仅是那一记录的值，不是游戏永久默认值。矩阵每行显示连续四个存储元素，不擅自转置。", "",
             "## 1. 原纹理", "", table(["名称", "大小", "格式", "mip", "颜色空间", "过滤 / Wrap"],
               [[t["Name"], f'{t["Width"]}×{t["Height"]}', t["Format"], t["MipCount"],
                 "线性" if t["ColorSpace"] == 0 else "sRGB", f'{t["FilterMode"]} / {t["WrapU"]},{t["WrapV"]}']
                for t in texture_contract["Textures"]]), "", "FilterMode 1=Bilinear；Wrap 0=Repeat、1=Clamp。", "",
             "## 2. 材质", "", "这里是材质文件保存的覆盖值，不冒充完整 GPU 最终输入。Shader 默认值在下一节，运行时覆盖需要另看 NapCB 和全局记录。", ""]
    selected = ("_UseOverlayTex", "_OverlayTexScale", "_Metallic", "_Glossiness", "_BumpScale", "_SpecIntensity",
                "_OutlineWidth", "_Color", "_DecolorizationContrast", "_UseMatCapMask")
    regional = ("_ShallowColor", "_ShadowColor", "_SpecularColor", "_OutlineColor", "_AlbedoSmoothness",
                "_ToonSpecular", "_SpecularRange", "_MatCapColorTint", "_MatCapBlendMode", "_MatCapAlphaBurst")
    for name, material in materials.items():
        props = material["properties"]
        floats, colors, textures = props.get("m_Floats", {}), props.get("m_Colors", {}), props.get("m_TexEnvs", {})
        lines.extend([f"### {name} / {material['original_name']}", "",
                      f"关键字：{compact(material['keywords'])}；队列：{material['render_queue']}；禁用 Pass：{compact(material['disabled_passes'])}。",
                      f"原字段数：{len(floats)} 个标量、{len(colors)} 个颜色/向量、{len(textures)} 个纹理槽。以下是常用字段，完整字段均在 JSON。", "",
                      table(["字段", "原值"], [[field, floats.get(field, colors.get(field))] for field in selected
                                              if field in floats or field in colors]), ""])
        rows = []
        for field in regional:
            if field not in floats and field not in colors:
                continue
            rows.append([field] + [floats.get(key, colors.get(key)) for key in
                                  [field] + [field + str(i) for i in range(2, 6)]])
        if rows:
            lines.extend(["五组参数按字段后缀排列，不能把序号直接理解为某块身体；需按原 M 图 R 通道选择。", "",
                          table(["参数", "无后缀", "2", "3", "4", "5"], rows), ""])

    lines.extend(["## 3. 原 Shader 默认声明", "", "这是默认声明，不是材质覆盖值，也不是某帧 GPU 实际值。保留原 flags；例如 HDR/颜色属性不能一律当普通向量处理。", ""])
    for name, shader in shaders.items():
        rows = []
        for prop in shader["properties"]:
            kind = prop["m_Type"]
            value = prop["m_DefTexture"] if kind == "Texture" else prop["m_DefValue"]
            if kind == "Float":
                value = value[0]
            elif kind == "Range":
                value = f"默认 {compact(value[0])}；范围 {compact(value[1])}~{compact(value[2])}"
            rows.append([prop["m_Name"], kind, prop.get("m_Flags"), value])
        lines.extend([f"### {name}", "", table(["名称", "类型", "原 flags", "默认声明"], rows), ""])

    lines.extend(["## 4. 829 全局参数", "", "三份文件来自同一旧快照，但均不是原子 Draw 捕获。重名值保留各自来源，不以“最新文件”覆盖。", ""])
    for key, snapshot in global_sets.items():
        lines.extend([f"### {key}", "", table(["字段", "读取状态", "数值视图"],
                     [[p["name"], p["status"], p.get("float_view")] for p in snapshot["properties"]]), ""])

    lines.extend(["## 5. 0905 可琳对象与 CPU 参数缓冲", "",
                  f"实体 {entity_address}；包装 {hex(wrapper)}；数组 {hex(array)}；{count} 个 Vector4。", "",
                  "对象缓存与 CPU 参数数组是两处数据；非同次更新读取，数值不等不能直接判为错误。参数名称只采用本次汇集的原绑定表，未覆盖的槽保留为未命名。", "",
                  table(["对象缓存字段", "值"], list(resident["napRenderEntity"].items())), "",
                  table(["组号 / 字节偏移", "原绑定表名称", "float4 视图"],
                        [[f'{v["index"]} / 0x{v["byte_offset"]:X}', "; ".join(v["names"]) or "未命名槽", v["float_view"]]
                         for v in vectors]), "", "## 6. 光照输入记录：尚未对应实体", "",
                  "两条记录的 entity 均为空；不可自动当作可琳数据，也不能把第二条的 -1 标记当正常头部方向。", ""])
    for item in inputs["records"]:
        lines.extend([f"### 记录 {item['index']}", "", table(["字段", "原值"], list(item["values"].items())), ""])
    lines.extend(["## 7. 当前项目绑定：不是 ZZZ 原始值", "",
                  f"运行 audit：{args.project_audit.name}；UTC={audit.get('utc')}；Initialize 参数数={audit.get('loadedProfileInitializeParameterCount')}。", "",
                  table(["文件:行", "字段", "项目表达式"],
                        [[f'{c["file"]}:{c["line"]}', c["name"], c["expression"]] for c in project_calls]), "",
                  "## 8. 来源索引", "", table(["来源ID", "类别", "文件"],
                     [[key, value["kind"], f'[{Path(value["path"]).name}](<{value["path"].replace(chr(92), "/")}>)']
                      for key, value in sources.items()]), ""])
    (args.output / "参数明细.md").write_text("\n".join(lines), encoding="utf-8")
    print(json.dumps({"output": str(args.output.resolve()), **count_summary,
                      "napcb_sha256": napcb["payload_sha256"], "source_files": len(sources)}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
