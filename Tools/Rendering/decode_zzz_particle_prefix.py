import argparse
import json
from collections import Counter
from pathlib import Path

import UnityPy
from UnityPy.helpers import TypeTreeHelper
from UnityPy.helpers.TypeTreeHelper import TypeTreeConfig


BASE_READ_VALUE = TypeTreeHelper.read_value
CURRENT_CONTEXT = None
PARTICLE_MODULES = [
    "InitialModule",
    "ShapeModule",
    "EmissionModule",
    "SizeModule",
    "RotationModule",
    "ColorModule",
    "UVModule",
    "VelocityModule",
    "InheritVelocityModule",
    "ForceModule",
    "ExternalForcesModule",
    "ClampVelocityModule",
    "NoiseModule",
    "SizeBySpeedModule",
    "RotationBySpeedModule",
    "ColorBySpeedModule",
    "CollisionModule",
    "TriggerModule",
    "SubModule",
    "LightsModule",
    "TrailModule",
    "CustomDataModule",
    "TextModule",
]
CORIN_SOURCE_BLOCKS = {
    "1068210133",
    "1478113610",
    "1539431670",
    "2045473264",
    "2702541078",
    "2968853155",
    "350316997",
    "988944888",
}

def parse_min_max_curve(node, reader):
    children = {child.m_Name: child for child in node.m_Children}
    state = reader.read_u_short()
    reader.align_stream()
    scalar = reader.read_float()
    min_scalar = reader.read_float()
    max_curve = parse_animation_curve(children["maxCurve"], reader)
    min_curve = parse_animation_curve(children["minCurve"], reader)
    reader.align_stream()
    return {
        "minMaxState": state,
        "scalar": scalar,
        "minScalar": min_scalar,
        "maxCurve": max_curve,
        "minCurve": min_curve,
    }


def parse_animation_curve(node, reader):
    size = reader.read_int()
    if size == -1:
        return {
            "nativeSize": size,
            "m_Curve": [],
            "m_PreInfinity": 0,
            "m_PostInfinity": 0,
            "m_RotationOrder": 0,
        }
    keys = []
    for _ in range(size):
        keys.append(
            {
                "time": reader.read_float(),
                "value": reader.read_float(),
                "inSlope": reader.read_float(),
                "outSlope": reader.read_float(),
                "weightedMode": reader.read_int(),
                "inWeight": reader.read_float(),
                "outWeight": reader.read_float(),
            }
        )
    return {
        "nativeSize": size,
        "m_Curve": keys,
        "m_PreInfinity": reader.read_int(),
        "m_PostInfinity": reader.read_int(),
        "m_RotationOrder": reader.read_int(),
    }


def parse_min_max_gradient(node, reader):
    children = {child.m_Name: child for child in node.m_Children}
    state = reader.read_u_short()
    reader.align_stream()

    def gradient(node):
        keys = [
            {
                "r": reader.read_float(),
                "g": reader.read_float(),
                "b": reader.read_float(),
                "a": reader.read_float(),
            }
            for _ in range(8)
        ]
        ctime = [reader.read_u_short() for _ in range(8)]
        atime = [reader.read_u_short() for _ in range(8)]
        return {
            "keys": keys,
            "ctime": ctime,
            "atime": atime,
            "m_Mode": reader.read_int(),
            "m_NumColorKeys": reader.read_u_byte(),
            "m_NumAlphaKeys": reader.read_u_byte(),
        }

    min_color = {
        "r": reader.read_float(),
        "g": reader.read_float(),
        "b": reader.read_float(),
        "a": reader.read_float(),
    }
    max_color = {
        "r": reader.read_float(),
        "g": reader.read_float(),
        "b": reader.read_float(),
        "a": reader.read_float(),
    }
    max_gradient = gradient(children["maxGradient"])
    reader.align_stream()
    min_gradient = gradient(children["minGradient"])
    reader.align_stream()
    return {
        "minMaxState": state,
        "minColor": min_color,
        "maxColor": max_color,
        "maxGradient": max_gradient,
        "minGradient": min_gradient,
    }


def parse_emission_instance(reader):
    time_offset = reader.read_float()
    count = reader.read_int()
    space_mode = reader.read_int()
    flags = reader.read_int()
    duration = reader.read_float()
    instance_data_size = reader.read_int()
    instance_data = reader.read_bytes(instance_data_size)
    reader.align_stream()
    return {
        "timeOffset": time_offset,
        "count": count,
        "spaceMode": space_mode,
        "flags": flags,
        "duration": duration,
        "instanceDataSize": instance_data_size,
        "instanceData": instance_data.hex(),
    }


def parse_emission(children, reader, context, base):
    context["module"] = "EmissionModule"
    node = children["EmissionModule"]
    node_children = {child.m_Name: child for child in node.m_Children}
    start = reader.Position - base
    enabled = reader.read_boolean()
    reader.align_stream()
    values = {
        "enabled": enabled,
        "rateOverTime": parse_min_max_curve(node_children["rateOverTime"], reader),
        "rateOverDistance": parse_min_max_curve(
            node_children["rateOverDistance"], reader
        ),
    }
    burst_count = reader.read_int()
    values["m_BurstCount"] = burst_count
    reader.align_stream()
    burst_array_size = reader.read_int()
    values["m_BurstsArraySize"] = burst_array_size
    values["m_Bursts"] = []
    for _ in range(burst_array_size):
        burst_children = {
            child.m_Name: child
            for child in node_children["m_Bursts"]
            .m_Children[0]
            .m_Children[1]
            .m_Children
        }
        burst = {"time": reader.read_float()}
        burst["countCurve"] = parse_min_max_curve(burst_children["countCurve"], reader)
        burst["cycleCount"] = reader.read_int()
        burst["repeatInterval"] = reader.read_float()
        burst["probability"] = reader.read_float()
        values["m_Bursts"].append(burst)

    instance_count = reader.read_int()
    values["m_EmissionInstanceCount"] = instance_count
    reader.align_stream()
    instance_array_size = reader.read_int()
    values["m_EmissionInstances"] = [
        parse_emission_instance(reader) for _ in range(instance_array_size)
    ]
    values["emissionLevelLow"] = reader.read_float()
    values["emissionLevelMedium"] = reader.read_float()
    values["emissionLevelHigh"] = reader.read_float()
    values["emissionLevelVeryHigh"] = reader.read_float()
    values["emitCallbackThreshold"] = reader.read_int()
    return {
        "name": "EmissionModule",
        "start": start,
        "end": reader.Position - base,
        "values": values,
    }


def parse_clamp_velocity(children, reader, context, base):
    node = children["ClampVelocityModule"]
    node_children = {child.m_Name: child for child in node.m_Children}
    start = reader.Position - base
    enabled = reader.read_boolean()
    reader.align_stream()
    values = {
        "enabled": enabled,
        "x": parse_min_max_curve(node_children["x"], reader),
        "y": parse_min_max_curve(node_children["y"], reader),
        "z": parse_min_max_curve(node_children["z"], reader),
        "magnitude": parse_min_max_curve(node_children["magnitude"], reader),
        "separateAxis": reader.read_boolean(),
        "inWorldSpace": reader.read_boolean(),
        "multiplyDragByParticleSize": reader.read_boolean(),
        "multiplyDragByParticleVelocity": reader.read_boolean(),
    }
    reader.align_stream()
    values["dampen"] = reader.read_float()

    values["drag"] = {"scalar": reader.read_float()}
    values["dragLegacyPayload"] = reader.read_bytes(84).hex()
    return {
        "name": "ClampVelocityModule",
        "start": start,
        "end": reader.Position - base,
        "values": values,
    }


def parse_size(children, reader, context, base):
    context["module"] = "SizeModule"
    node = children["SizeModule"]
    node_children = {child.m_Name: child for child in node.m_Children}
    start = reader.Position - base
    enabled = reader.read_boolean()
    reader.align_stream()
    values = {"enabled": enabled}
    values["curve"] = parse_min_max_curve(node_children["curve"], reader)
    for name in ("y", "z"):
        values[name] = parse_min_max_curve(node_children[name], reader)
    values["separateAxes"] = reader.read_boolean()
    reader.align_stream()
    return {
        "name": "SizeModule",
        "start": start,
        "end": reader.Position - base,
        "values": values,
    }


def parse_text_module(reader, base):
    start = reader.Position - base

    def pptr():
        return {"m_FileID": reader.read_int(), "m_PathID": reader.read_long()}

    values = {"enabled": reader.read_boolean()}
    reader.align_stream()
    for name in ("sceneCamera", "canvas", "font"):
        values[name] = pptr()
    values["fontSize"] = reader.read_int()
    values["fontStyle"] = reader.read_int()
    values["outlineEnable"] = reader.read_boolean()
    reader.align_stream()
    values["outlineDistance"] = [reader.read_float() for _ in range(3)]
    values["emitWithWorldPosition"] = reader.read_boolean()
    reader.align_stream()
    return {
        "name": "TextModule",
        "start": start,
        "end": reader.Position - base,
        "values": values,
    }


def parse_lights(children, reader, base):
    node = children["LightsModule"]
    node_children = {child.m_Name: child for child in node.m_Children}
    start = reader.Position - base
    values = {"enabled": reader.read_boolean()}
    reader.align_stream()
    values["ratio"] = reader.read_float()
    values["light"] = {
        "m_FileID": reader.read_int(),
        "m_PathID": reader.read_long(),
    }
    for name in ("randomDistribution", "color", "range", "intensity"):
        values[name] = reader.read_boolean()
    reader.align_stream()
    values["rangeCurve"] = parse_min_max_curve(node_children["rangeCurve"], reader)
    values["intensityCurve"] = parse_min_max_curve(
        node_children["intensityCurve"], reader
    )
    values["maxLights"] = reader.read_int()
    return {
        "name": "LightsModule",
        "start": start,
        "end": reader.Position - base,
        "values": values,
    }


def parse_external_forces(reader):
    enabled = reader.read_boolean()
    reader.align_stream()
    return {
        "enabled": enabled,
        "multiplier": reader.read_float(),
        "influenceFilter": reader.read_int(),
        "influenceMask": {"m_Bits": reader.read_u_int()},
    }


def read_value(node, reader, context):
    if not isinstance(context, dict) or "base_reader" not in context:
        context = CURRENT_CONTEXT
    module = context["module"]
    if node.m_Type == "MinMaxCurve":
        return parse_min_max_curve(node, reader)
    if node.m_Type == "MinMaxGradient":
        return parse_min_max_gradient(node, reader)
    if module == "ExternalForcesModule" and node.m_Name == "ExternalForcesModule":
        return parse_external_forces(reader)
    return BASE_READ_VALUE(node, reader, context["config"])


def parse_particle_system(asset):
    root = asset._get_typetree_node()
    serialized_version = root.m_Version
    if serialized_version != 6:
        raise RuntimeError(f"unsupported ParticleSystem serializedVersion {serialized_version}")
    reader = asset.reader
    base = asset.byte_start
    config = TypeTreeConfig(True, asset.assets_file)
    context = {
        "base_reader": BASE_READ_VALUE,
        "config": config,
        "module": "",
        "serializedVersion": serialized_version,
    }
    global CURRENT_CONTEXT
    CURRENT_CONTEXT = context
    TypeTreeHelper.read_value = read_value
    reader.Position = base
    children = {child.m_Name: child for child in root.m_Children}
    result = {"pathId": asset.path_id, "byteSize": asset.byte_size, "values": {}}
    values = result["values"]
    start = reader.Position - base
    values["m_GameObject"] = context["base_reader"](children["m_GameObject"], reader, config)

    simple_fields = [
        ("distanceCulling", reader.read_int),
        ("cullingFromDistance", reader.read_int),
        ("postEmissionScalar", reader.read_float),
        ("tickFrequency", reader.read_float),
        ("lengthInSec", reader.read_float),
        ("simulationSpeed", reader.read_float),
        ("frequencyMode", reader.read_int),
        ("stopAction", reader.read_int),
        ("cullingMode", reader.read_int),
        ("ringBufferMode", reader.read_int),
    ]
    for name, read_field in simple_fields:
        values[name] = read_field()
    values["ringBufferLoopRange"] = context["base_reader"](
        children["ringBufferLoopRange"], reader, config
    )
    for name in [
        "looping",
        "prewarm",
        "playOnAwake",
        "useUnscaledTime",
        "autoRandomSeed",
        "useRigidbodyForVelocity",
    ]:
        values[name] = reader.read_boolean()
    reader.align_stream()

    context["module"] = "startDelay"
    values["startDelay"] = read_value(children["startDelay"], reader, context)
    for name in [
        "moveWithTransform",
        "moveWithCustomTransform",
        "scalingMode",
        "randomSeed",
    ]:
        values[name] = context["base_reader"](children[name], reader, config)

    result["modules"] = []
    for name in PARTICLE_MODULES:
        if name == "EmissionModule":
            result["modules"].append(parse_emission(children, reader, context, base))
            values[name] = result["modules"][-1]["values"]
            continue
        if name == "SizeModule":
            result["modules"].append(parse_size(children, reader, context, base))
            values[name] = result["modules"][-1]["values"]
            continue
        if name == "ClampVelocityModule":
            result["modules"].append(
                parse_clamp_velocity(children, reader, context, base)
            )
            values[name] = result["modules"][-1]["values"]
            continue
        if name == "SubModule":
            context["module"] = name
            start = reader.Position - base
            values[name] = read_value(children[name], reader, context)
            values[name]["zzzEnabled"] = reader.read_boolean()
            reader.align_stream()
            result["modules"].append(
                {"name": name, "start": start, "end": reader.Position - base}
            )
            continue
        if name == "LightsModule":
            result["modules"].append(parse_lights(children, reader, base))
            values[name] = result["modules"][-1]["values"]
            continue
        if name == "TextModule":
            result["modules"].append(parse_text_module(reader, base))
            values[name] = result["modules"][-1]["values"]
            continue
        context["module"] = name
        start = reader.Position - base
        values[name] = read_value(children[name], reader, context)
        result["modules"].append(
            {
                "name": name,
                "start": start,
                "end": reader.Position - base,
            }
        )

    result["decodedEnd"] = reader.Position - base
    return result


def parse_particle_system_renderer(asset, environment):
    root = asset._get_typetree_node()
    serialized_version = root.m_Version
    if serialized_version != 6:
        raise RuntimeError(
            f"unsupported ParticleSystemRenderer serializedVersion {serialized_version}"
        )
    reader = asset.reader
    base = asset.byte_start
    config = TypeTreeConfig(True, asset.assets_file)
    context = {
        "base_reader": BASE_READ_VALUE,
        "config": config,
        "module": "ParticleSystemRenderer",
    }
    global CURRENT_CONTEXT
    CURRENT_CONTEXT = context
    TypeTreeHelper.read_value = read_value
    reader.Position = base
    children = root.m_Children
    values = {}
    for child in children:
        if child.m_Name == "m_RenderMode":
            break
        values[child.m_Name] = context["base_reader"](child, reader, config)

    def boolean(name):
        values[name] = reader.read_boolean()
        reader.align_stream()

    def pptr(name):
        values[name] = {
            "m_FileID": reader.read_int(),
            "m_PathID": reader.read_long(),
        }

    def vector3(name):
        values[name] = [reader.read_float() for _ in range(3)]

    boolean("m_NeedHizCulling")
    boolean("m_HighShadingRate")
    values["m_RayTracingLayerMask"] = reader.read_u_short()
    reader.align_stream()
    values["m_CullingDistance"] = reader.read_float()
    values["m_OrderType"] = reader.read_u_short()
    reader.align_stream()
    values["zzzPreRenderMode"] = reader.read_int()
    values["m_RenderMode"] = reader.read_u_short()
    reader.align_stream()
    values["m_SortMode"] = reader.read_u_short()
    reader.align_stream()
    for name in (
        "m_MinParticleSize",
        "m_MaxParticleSize",
        "m_CameraVelocityScale",
        "m_VelocityScale",
        "m_LengthScale",
        "m_SortingFudge",
        "m_NormalDirection",
        "m_ShadowBias",
    ):
        values[name] = reader.read_float()
    values["m_RenderAlignment"] = reader.read_int()
    vector3("m_Pivot")
    vector3("m_Flip")
    for name in (
        "m_UseCustomVertexStreams",
        "m_EnableGPUInstancing",
        "m_ApplyActiveColorSpace",
        "m_AllowRoll",
    ):
        values[name] = reader.read_boolean()
    reader.align_stream()
    vertex_stream_size = reader.read_int()
    values["m_VertexStreams"] = {
        "size": vertex_stream_size,
        "data": reader.read_bytes(vertex_stream_size).hex(),
    }
    reader.align_stream()
    for name in ("m_Mesh", "m_Mesh1", "m_Mesh2", "m_Mesh3", "m_OctagonMesh"):
        pptr(name)
    boolean("m_MaskInteraction")
    boolean("m_UseCustomBoundingBox")
    vector3("m_CustomBoundCenter")
    custom_bound_size_start = reader.Position - base
    unknown_end = asset.byte_size - 12
    values["zzzCustomBoundsSuffix"] = reader.read_bytes(
        unknown_end - custom_bound_size_start
    ).hex()
    vector3("m_CustomBoundSize")
    standard_end = reader.Position - base
    if standard_end != asset.byte_size:
        raise RuntimeError(
            f"unsupported ParticleSystemRenderer closure {standard_end}/{asset.byte_size}"
        )

    dependencies = []
    local_mesh_ids = {
        item.path_id
        for item in environment.objects
        if item.type.name == "Mesh"
    }
    for name in (
        "m_Materials",
        "m_Mesh",
        "m_Mesh1",
        "m_Mesh2",
        "m_Mesh3",
        "m_OctagonMesh",
    ):
        references = values[name] if name == "m_Materials" else [values[name]]
        for reference in references:
            file_id = reference["m_FileID"]
            path_id = reference["m_PathID"]
            if path_id == 0:
                continue
            if file_id == 0:
                scope = "local"
                external = None
                target_in_scope = path_id in local_mesh_ids
                resolution = None
            elif 0 < file_id <= len(environment.file.externals):
                scope = "external"
                external = environment.file.externals[file_id - 1].path
                target_in_scope = None
                resolution = (
                    "builtin"
                    if external == "Library/unity default resources"
                    else "archive"
                )
            else:
                scope = "invalidFileId"
                external = None
                target_in_scope = None
                resolution = None
            dependencies.append(
                {
                    "field": name,
                    "fileId": file_id,
                    "pathId": path_id,
                    "scope": scope,
                    "external": external,
                    "targetInScope": target_in_scope,
                    "resolution": resolution,
                }
            )

    return {
        "objectType": "ParticleSystemRenderer",
        "pathId": asset.path_id,
        "byteSize": asset.byte_size,
        "standardEnd": standard_end,
        "values": values,
        "extensionClosure": True,
        "dependencies": dependencies,
    }


def load_corin_scope(manifest_path, object_type):
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    raw_manifest_path = manifest_path.with_name("raw_module_manifest.json")
    raw_manifest = json.loads(raw_manifest_path.read_text(encoding="utf-8"))
    expected_sizes = {
        (item["cab"], item["pathId"]): item["bytes"] for item in raw_manifest["objects"]
    }
    source_root = Path(manifest["sourceRoot"])
    source_blocks = [
        item
        for item in source_root.iterdir()
        if item.is_dir() and item.name in CORIN_SOURCE_BLOCKS
    ]
    if len(source_blocks) != len(CORIN_SOURCE_BLOCKS):
        missing = sorted(CORIN_SOURCE_BLOCKS - {item.name for item in source_blocks})
        raise RuntimeError(f"missing Corin source blocks: {missing}")

    paths_by_cab = {}
    for block in source_blocks:
        for path in block.glob("*_CAB-*"):
            cab = path.name.split("_", 1)[1]
            paths_by_cab.setdefault(cab, []).append(path)

    expected = {}
    for item in manifest["objects"]:
        if item["type"] != object_type:
            continue
        expected.setdefault(item["cab"], []).append(item["pathId"])

    resolved = {}
    for cab, path_ids in expected.items():
        paths = paths_by_cab.get(cab, [])
        candidates = []
        for path in paths:
            environment = UnityPy.load(str(path))
            assets = {
                item.path_id: item
                for item in environment.objects
                if item.type.name == object_type
                and item.path_id in path_ids
            }
            if set(assets) != set(path_ids):
                continue
            if any(
                asset.byte_size != expected_sizes[(cab, path_id)]
                for path_id, asset in assets.items()
            ):
                continue
            object_signature = tuple(
                (path_id, assets[path_id].get_raw_data()) for path_id in path_ids
            )
            external_signature = tuple(
                external.path for external in environment.file.externals
            )
            candidates.append((path, environment, assets, object_signature, external_signature))
        if not candidates:
            raise RuntimeError(f"no source copy matches raw module sizes for {cab}")
        signatures = {(item[3], item[4]) for item in candidates}
        if len(signatures) != 1:
            raise RuntimeError(f"ambiguous raw module sources for {cab}")
        resolved[cab] = candidates[0][0], path_ids
    return manifest, resolved, paths_by_cab


def load_corin_renderer_scope(manifest_path):
    return load_corin_scope(manifest_path, "ParticleSystemRenderer")


def resolve_external_meshes(records, source_paths):
    references = {
        (dependency["external"].split("/")[1], dependency["pathId"])
        for record in records
        for dependency in record["dependencies"]
        if dependency["scope"] == "external"
        and dependency["resolution"] == "archive"
    }
    resolved = {}
    for cab, path_id in sorted(references):
        candidates = []
        for path in source_paths.get(cab, []):
            environment = UnityPy.load(str(path))
            meshes = {
                item.path_id: item
                for item in environment.objects
                if item.type.name == "Mesh"
            }
            if path_id not in meshes:
                continue
            candidates.append((path, meshes[path_id].get_raw_data()))
        if not candidates:
            resolved[(cab, path_id)] = None
            continue
        signatures = {signature for _, signature in candidates}
        if len(signatures) != 1:
            raise RuntimeError(f"ambiguous external Mesh {cab}/{path_id}")
        resolved[(cab, path_id)] = str(candidates[0][0])
    return resolved


def summarize_corin_renderer_scan(records):
    closures = Counter(record["extensionClosure"] for record in records)
    dependencies = [
        dependency for record in records for dependency in record["dependencies"]
    ]
    dependency_scopes = Counter(dependency["scope"] for dependency in dependencies)
    dependency_fields = Counter(
        dependency["field"] for dependency in dependencies
    )
    dependency_targets = Counter(
        (dependency["field"], dependency["external"], dependency["fileId"])
        for dependency in dependencies
        if dependency["scope"] == "external"
    )
    unresolved_locals = Counter(
        dependency["field"]
        for dependency in dependencies
        if dependency["scope"] == "local" and not dependency["targetInScope"]
    )
    external_dependencies = [
        dependency
        for dependency in dependencies
        if dependency["scope"] == "external"
    ]
    external_targets = {
        (dependency["external"], dependency["pathId"])
        for dependency in external_dependencies
    }
    resolved_external_targets = {
        (dependency["external"], dependency["pathId"])
        for dependency in external_dependencies
        if dependency["externalMeshResolved"] is not None
    }
    external_resolutions = Counter(
        dependency["resolution"] for dependency in external_dependencies
    )
    resolved_external = sum(
        dependency["externalMeshResolved"] is not None
        for dependency in external_dependencies
    )
    invalid_references = Counter(
        (dependency["field"], dependency["fileId"])
        for dependency in dependencies
        if dependency["scope"] == "invalidFileId"
    )
    return {
        "byteSizes": dict(
            sorted(Counter(record["byteSize"] for record in records).items())
        ),
        "standardEnds": dict(
            sorted(Counter(record["standardEnd"] for record in records).items())
        ),
        "extensionClosures": dict(sorted(closures.items())),
        "renderModes": dict(
            sorted(
                Counter(record["values"]["m_RenderMode"] for record in records).items()
            )
        ),
        "sortModes": dict(
            sorted(Counter(record["values"]["m_SortMode"] for record in records).items())
        ),
        "orderTypes": dict(
            sorted(
                Counter(record["values"]["m_OrderType"] for record in records).items()
            )
        ),
        "preRenderModeValues": dict(
            sorted(
                Counter(
                    record["values"]["zzzPreRenderMode"] for record in records
                ).items()
            )
        ),
        "vertexStreamSizes": dict(
            sorted(
                Counter(
                    record["values"]["m_VertexStreams"]["size"] for record in records
                ).items()
            )
        ),
        "dependencyFields": dict(sorted(dependency_fields.items())),
        "dependencyScopes": dict(sorted(dependency_scopes.items())),
        "externalMeshReferences": len(external_dependencies),
        "resolvedExternalMeshReferences": resolved_external,
        "externalMeshResolutions": dict(sorted(external_resolutions.items())),
        "externalMeshTargets": len(external_targets),
        "resolvedExternalMeshTargets": len(resolved_external_targets),
        "unresolvedLocalFields": dict(sorted(unresolved_locals.items())),
        "invalidReferences": [
            {"field": field, "fileId": file_id, "count": count}
            for (field, file_id), count in sorted(invalid_references.items())
        ],
        "dependencyTargets": [
            {
                "field": field,
                "external": external,
                "fileId": file_id,
                "count": count,
            }
            for (field, external, file_id), count in sorted(
                dependency_targets.items(), key=lambda item: (item[0][0], item[0][2])
            )
        ],
    }


def scan_corin_renderers(manifest_path, output_path):
    manifest, scope, source_paths = load_corin_renderer_scope(manifest_path)
    records = []
    for cab in sorted(scope):
        path, expected_path_ids = scope[cab]
        environment = UnityPy.load(str(path))
        assets = [
            item
            for item in environment.objects
            if item.type.name == "ParticleSystemRenderer"
        ]
        actual_path_ids = {item.path_id for item in assets}
        missing = sorted(set(expected_path_ids) - actual_path_ids)
        extra = sorted(actual_path_ids - set(expected_path_ids))
        if missing or extra:
            raise RuntimeError(
                f"renderer identity mismatch in {cab}: missing={missing}, extra={extra}"
            )
        for asset in assets:
            decoded = parse_particle_system_renderer(asset, environment)
            records.append(
                {
                    "cab": cab,
                    "pathId": decoded["pathId"],
                    "byteSize": decoded["byteSize"],
                    "standardEnd": decoded["standardEnd"],
                    "values": decoded["values"],
                    "extensionClosure": decoded["extensionClosure"],
                    "dependencies": decoded["dependencies"],
                }
            )
        print(json.dumps({"cab": cab, "renderers": len(assets), "records": len(records)}))

    external_meshes = resolve_external_meshes(records, source_paths)
    for record in records:
        for dependency in record["dependencies"]:
            if dependency["scope"] != "external":
                continue
            external_cab = dependency["external"].split("/")[1]
            dependency["externalMeshResolved"] = external_meshes[
                (external_cab, dependency["pathId"])
            ]

    result = {
        "schema": "zzz-corin-renderer-dependency-scan/v1",
        "manifest": str(manifest_path.resolve()),
        "sourceRoot": manifest["sourceRoot"],
        "sourceBlocks": sorted(CORIN_SOURCE_BLOCKS),
        "cabCount": len(scope),
        "count": len(records),
        "summary": summarize_corin_renderer_scan(records),
        "records": records,
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


def scan_corin_particle_systems(manifest_path, output_path):
    manifest, scope, _ = load_corin_scope(manifest_path, "ParticleSystem")
    records = []
    closed_count = 0
    for cab in sorted(scope):
        path, expected_path_ids = scope[cab]
        environment = UnityPy.load(str(path))
        assets = [
            item
            for item in environment.objects
            if item.type.name == "ParticleSystem"
        ]
        actual_path_ids = {item.path_id for item in assets}
        missing = sorted(set(expected_path_ids) - actual_path_ids)
        extra = sorted(actual_path_ids - set(expected_path_ids))
        if missing or extra:
            raise RuntimeError(
                f"particle system identity mismatch in {cab}: missing={missing}, extra={extra}"
            )
        for asset in assets:
            decoded = parse_particle_system(asset)
            decoded["objectType"] = "ParticleSystem"
            closed = decoded["decodedEnd"] == decoded["byteSize"]
            closed_count += int(closed)
            records.append({"cab": cab, **decoded})
        print(
            json.dumps(
                {
                    "cab": cab,
                    "particleSystems": len(assets),
                    "records": len(records),
                    "closed": closed_count,
                }
            )
        )

    if closed_count != len(records):
        raise RuntimeError(
            f"particle system closure mismatch: {closed_count}/{len(records)}"
        )
    result = {
        "schema": "zzz-corin-particle-system-scan/v1",
        "manifest": str(manifest_path.resolve()),
        "sourceRoot": manifest["sourceRoot"],
        "sourceBlocks": sorted(CORIN_SOURCE_BLOCKS),
        "cabCount": len(scope),
        "count": len(records),
        "closedCount": closed_count,
        "records": records,
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "manifest": str(manifest_path),
                "output": str(output_path),
                "cabCount": len(scope),
                "count": len(records),
                "closedCount": closed_count,
            }
        )
    )
    print(
        json.dumps(
            {
                "manifest": str(manifest_path),
                "output": str(output_path),
                "cabCount": len(scope),
                "count": len(records),
            }
        )
    )


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path)
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--path-id", type=lambda value: int(value, 0))
    parser.add_argument(
        "--manifest-type",
        choices=("ParticleSystem", "ParticleSystemRenderer"),
        default="ParticleSystemRenderer",
    )
    parser.add_argument(
        "--object-type",
        choices=("all", "ParticleSystem", "ParticleSystemRenderer"),
        default="all",
    )
    args = parser.parse_args()

    if args.manifest is not None:
        if args.input is not None or args.path_id is not None or args.object_type != "all":
            parser.error("--manifest cannot be combined with single-CAB options")
        if args.manifest_type == "ParticleSystem":
            scan_corin_particle_systems(args.manifest, args.output)
        else:
            scan_corin_renderers(args.manifest, args.output)
        return
    if args.input is None:
        parser.error("one of --input or --manifest is required")

    environment = UnityPy.load(str(args.input))
    wanted_types = {"all"} if args.object_type == "all" else {args.object_type}
    systems = (
        [item for item in environment.objects if item.type.name == "ParticleSystem"]
        if "all" in wanted_types or "ParticleSystem" in wanted_types
        else []
    )
    renderers = (
        [
            item
            for item in environment.objects
            if item.type.name == "ParticleSystemRenderer"
        ]
        if "all" in wanted_types or "ParticleSystemRenderer" in wanted_types
        else []
    )
    if args.path_id is not None:
        systems = [item for item in systems if item.path_id == args.path_id]
        renderers = [item for item in renderers if item.path_id == args.path_id]
    decoded = []
    for item in systems:
        result = parse_particle_system(item)
        result["objectType"] = "ParticleSystem"
        decoded.append(result)
    for item in renderers:
        decoded.append(parse_particle_system_renderer(item, environment))
    result = {
        "schema": "zzz-particle-transfer/v2",
        "input": str(args.input.resolve()),
        "count": len(decoded),
        "objects": decoded,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"input": str(args.input), "output": str(args.output), "count": len(decoded)}))


if __name__ == "__main__":
    main()
