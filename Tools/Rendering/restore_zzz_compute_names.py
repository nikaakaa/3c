import argparse
import json
import re
from pathlib import Path

from recover_dxbc_hlsl import compile_hlsl
from recover_zzz_shader import restore_names, sha256, write_json


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--compute-json", type=Path, required=True)
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--variant", type=int, default=0)
    args = parser.parse_args()
    document = json.loads(args.compute_json.read_text(encoding="utf-8"))
    variant = document["m_Variants"][args.variant]
    if variant["m_TargetRenderer"] != 2 or not variant["m_ResourcesResolved"]:
        raise ValueError("Selected compute variant is not resolved D3D11")
    args.output.mkdir(parents=True, exist_ok=False)
    records = []
    for index, kernel in enumerate(variant["m_Kernels"]):
        source_path = args.source_dir / f"v{args.variant}-k{index}.uint-registers.hlsl"
        source = source_path.read_text(encoding="utf-8")
        names = {}

        def name_index(name):
            current = next((key for key, value in names.items() if value == name), None)
            if current is not None:
                return current
            current = len(names)
            names[current] = name
            return current

        cb_slots = {row["m_Name"]: row["m_BindPoint"] for row in kernel["m_Cbs"]}
        constant_buffers = []
        constant_bindings = []
        for buffer in variant["m_ConstantBuffers"]:
            buffer_name_index = name_index(buffer["m_Name"])
            constant_bindings.append({"m_NameIndex": buffer_name_index, "m_Index": cb_slots[buffer["m_Name"]]})
            vectors, matrices = [], []
            for parameter in buffer["m_Params"]:
                converted = {
                    "m_NameIndex": name_index(parameter["m_Name"]),
                    "m_Index": parameter["m_Offset"],
                    "m_ArraySize": parameter["m_ArraySize"],
                    "m_Type": parameter["m_Type"]
                }
                if parameter["m_RowCount"] == 1:
                    converted["m_Dim"] = parameter["m_ColCount"]
                    vectors.append(converted)
                elif parameter["m_RowCount"] == 4 and parameter["m_ColCount"] == 4:
                    converted["m_RowCount"] = 4
                    matrices.append(converted)
                else:
                    raise ValueError("Unsupported compute constant layout")
            constant_buffers.append({"m_NameIndex": buffer_name_index, "m_VectorParams": vectors,
                                     "m_MatrixParams": matrices, "m_StructParams": []})

        def resources(rows):
            return [{"m_NameIndex": name_index(row["m_Name"]), "m_Index": row["m_BindPoint"],
                     "m_ArraySize": 0} for row in rows]

        inputs = kernel["m_InBuffers"]
        textures = kernel["m_Textures"]
        subprogram = {
            "m_ConstantBufferBindings": constant_bindings,
            "m_ConstantBuffers": constant_buffers,
            "m_TextureParams": resources(textures),
            "m_BufferParams": resources(inputs),
            "m_Samplers": [{"bindPoint": row["m_BindPoint"], "sampler": row["m_Sampler"]}
                           for row in kernel["m_BuiltinSamplers"]]
        }
        restored, bindings = restore_names(source, {"names": names, "subprogram": subprogram})
        outputs = []
        for output in kernel["m_OutBuffers"]:
            old, name = f"u{output['m_BindPoint']}", output["m_Name"]
            restored = re.sub(rf"(?<!register\()\b{old}\b", name, restored)
            restored = re.sub(rf"\b{old}_Element\b", name + "_Element", restored)
            outputs.append({"name": name, "bindPoint": output["m_BindPoint"]})
        target = args.output / kernel["m_Name"]
        target.with_suffix(".hlsl").write_text(restored, encoding="utf-8")
        code, messages = compile_hlsl(restored, "cs_5_0")
        if messages:
            raise RuntimeError(messages)
        target.with_suffix(".dxbc").write_bytes(code)
        bindings["outputs"] = outputs
        write_json(target.with_suffix(".bindings.json"), bindings)
        records.append({"kernel": kernel["m_Name"], "source": str(source_path),
                        "source_sha256": sha256(source_path.read_bytes()),
                        "restored_sha256": sha256(restored.encode()), "compiler_messages": messages,
                        "inputs": [row["m_Name"] for row in inputs],
                        "textures": [row["m_Name"] for row in textures],
                        "outputs": [row["m_Name"] for row in kernel["m_OutBuffers"]]})
    write_json(args.output / "recovery.json", {
        "schemaVersion": 1,
        "compute": document["m_Name"],
        "variant": args.variant,
        "targetRenderer": variant["m_TargetRenderer"],
        "runtimeBound": False,
        "source": str(args.compute_json.resolve()),
        "sourceSha256": sha256(args.compute_json.read_bytes()),
        "toolSha256": sha256(Path(__file__).read_bytes()),
        "kernels": records
    })
    print(json.dumps(records, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
