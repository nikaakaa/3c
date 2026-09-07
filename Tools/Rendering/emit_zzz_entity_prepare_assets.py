import argparse
import json
import re
from pathlib import Path

from recover_zzz_shader import sha256


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--named-recovery", type=Path, required=True)
    parser.add_argument("--compute-json", type=Path, required=True)
    parser.add_argument("--output-root", default="3cDemo/Client/3C_Client/Assets/Render/ZZZRestored/Generated/EntityLighting")
    args = parser.parse_args()
    recovery = json.loads((args.named_recovery / "recovery.json").read_text(encoding="utf-8"))
    source_document = json.loads(args.compute_json.read_text(encoding="utf-8"))
    if recovery["compute"] != "NapEntityPrepare" or recovery["targetRenderer"] != 2:
        raise ValueError("Named recovery is not the evidenced D3D11 entity program")
    kernels = {row["kernel"]: row for row in recovery["kernels"]}
    if kernels["NapEntityPrepareDev"]["restored_sha256"] != kernels["NapEntityPrepareDev2"]["restored_sha256"]:
        raise ValueError("Default and optimize-disabled kernels differ")
    selected = args.named_recovery / "NapEntityPrepareDev2.hlsl"
    source = selected.read_text(encoding="utf-8")
    source = re.sub(r"cbuffer \w+\s*:\s*register\(b\d+\)\s*\{(.*?)\}",
                    lambda match: re.sub(r"\s*:\s*packoffset\([^)]*\)", "", match[1]), source, flags=re.S)
    source = source.replace("SamplerState ZZZSampler_1 : register(s0);", "SamplerState zzz_linear_repeat_sampler;")
    source = source.replace("SamplerComparisonState ZZZSampler_340 : register(s1);",
                            "SamplerComparisonState sampler_PointClampCompare;")
    source = source.replace("ZZZSampler_1", "zzz_linear_repeat_sampler")
    source = source.replace("ZZZSampler_340", "sampler_PointClampCompare")
    source = source.replace("void main(uint3 vThreadID : SV_DispatchThreadID)",
                            "void NapEntityPrepare(uint3 vThreadID : SV_DispatchThreadID)")
    source = "#pragma kernel NapEntityPrepare\n#pragma target 5.0\n#pragma only_renderers d3d11\n\n" + source
    root = Path(args.output_root)
    report = {
        "schemaVersion": 1,
        "sourceCompute": str(args.compute_json.resolve()),
        "sourceComputeSha256": sha256(args.compute_json.read_bytes()),
        "namedRecovery": str((args.named_recovery / "recovery.json").resolve()),
        "namedRecoverySha256": sha256((args.named_recovery / "recovery.json").read_bytes()),
        "selectedKernel": "NapEntityPrepareDev2",
        "selectionEvidence": {
            "pipelineField": "UniversalRenderPipelineAsset.m_OptimizeBlendLightOb0103",
            "pipelineFieldOffset": 712,
            "snapshotValue": False,
            "nativeSelectorRva": "0x1BC70740",
            "defaultAndSelectedProgramIdentical": True
        },
        "runtimeBound": False,
        "samplerAdaptations": {
            "1": "linear-repeat",
            "340": "point-clamp-compare"
        },
        "computeSha256": sha256(source.encode())
    }
    files = {
        root / "NapEntityPrepare.compute": source,
        root / "NapEntityPrepareSource.json": json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    }
    patch = ["*** Begin Patch"]
    for path, content in files.items():
        if path.exists():
            raise ValueError(f"Generated target already exists: {path}")
        patch.append("*** Add File: " + path.as_posix())
        patch.extend("+" + line for line in content.splitlines())
    patch.append("*** End Patch")
    print("\n".join(patch))


if __name__ == "__main__":
    main()
