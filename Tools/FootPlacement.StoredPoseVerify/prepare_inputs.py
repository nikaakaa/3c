from pathlib import Path
import csv
import hashlib
import json
import subprocess
import argparse

ROOT = Path(__file__).resolve().parents[2]
FOOT = ROOT / '3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260930-172333-3cb49fba52604669bfae7fe6eda3aabc'
PRESENTATION = ROOT / '3cDemo/Client/3C_Client/Diagnostics/GeneratedPresentationSampling/20260930-172333-12e81b2dcf6b446380069951b3a9895d'
OUT = ROOT / 'docs/diagnostics/foot-placement/ik-tests'
OUT.mkdir(parents=True, exist_ok=True)
PREFIX = 'character-foot-ik/main/'

def rows(path):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        yield from csv.DictReader(stream)

main = list(rows(FOOT / 'character-foot-ik%2Ffull.csv'))
sources = {}
source_path = PRESENTATION / 'character-presentation-replication%2Ffull.sources.csv'
source_columns = [k for k in next(rows(source_path)) if k.startswith('character-presentation-replication/sources/animation/')]
for row in rows(source_path):
    sources.setdefault((row['sample.lineage.high'], row['sample.lineage.low']), []).append([row[k] for k in source_columns])
source_names = [k.removeprefix('character-presentation-replication/sources/animation/') for k in source_columns]
kind_index = source_names.index('kind')
weight_index = source_names.index('weight')
frame_key = PREFIX + 'foot/resolved/core/frame-sequence'

def is_stored(row):
    return any(s[kind_index] == '2' and float(s[weight_index]) > 0 for s in sources[(row['sample.lineage.high'], row['sample.lineage.low'])])

previous = {}
contact_captures = []
for row in main:
    side = row['sample.dimension'].rsplit('/', 1)[1]
    frame = int(row[frame_key])
    if side in previous:
        prev = previous[side]
        if is_stored(row) and not is_stored(prev) and float(prev[PREFIX + 'formal-input/contact']) > .9 and float(prev[PREFIX + 'formal-input/lock-weight']) > .5 and float(prev[PREFIX + 'input/foot-step-observation/source-weight']) > .5:
            contact_captures.append({'frame': frame, 'side': side, 'previousContact': prev[PREFIX + 'formal-input/contact'], 'previousLockWeight': prev[PREFIX + 'formal-input/lock-weight'], 'previousSource': prev[PREFIX + 'input/foot-step-observation/source-identity'], 'sourceRows': [dict(zip(source_names, s)) for s in sources[(row['sample.lineage.high'], row['sample.lineage.low'])]]})
    previous[side] = row

parser = argparse.ArgumentParser()
parser.add_argument('--scene', choices=['stored', 'stored-rotation', 'live-switch', 'live-continued', 'releasing', 'releasing-continued', 'step-edge'], default='stored')
scene = parser.parse_args().scene
paired_scene = scene in ['live-continued', 'releasing', 'releasing-continued']
scenarios = {
    'stored': {'stored-no-contact-left': ('left', 1174, 1186), 'stored-contact-right': ('right', 889, 958)},
    'stored-rotation': {'stored-contact-rotation-right': ('right', 874, 958)},
    'live-switch': {'live-switch-right': ('right', 2192, 2214)},
    'live-continued': {'live-switch-continued-right': ('right', 2192, 2249)},
    'releasing': {'releasing-right': ('right', 2035, 2046)},
    'releasing-continued': {'releasing-business-right': ('right', 2024, 2056)},
    'step-edge': {'step-edge-left': ('left', 2537, 2550)}
}[scene]
columns = {'main': [k.removeprefix(PREFIX) for k in main[0] if k.startswith(PREFIX)], 'sources': source_names}
main_keys = [PREFIX + key for key in columns['main']]
by_frame_side = {(int(row[frame_key]), row['sample.dimension'].rsplit('/', 1)[1]): row for row in main}
source_ids = {}
for row in main:
    observed_weight = float(row[PREFIX + 'input/foot-step-observation/source-weight'])
    observed_time = float(row[PREFIX + 'input/foot-step-observation/normalized-time'])
    candidates = sources[(row['sample.lineage.high'], row['sample.lineage.low'])]
    live = [s for s in candidates if s[kind_index] == '1']
    selected_source = max(live, key=lambda s: float(s[weight_index]))
    if abs(float(selected_source[weight_index]) - observed_weight) < 1e-6 and abs(float(selected_source[source_names.index('normalized-time')]) - observed_time) < 1e-5:
        key = '|'.join(selected_source[source_names.index(k)] for k in ['node-id', 'selection-generation', 'action-instance-id'])
        source_ids[key] = row[PREFIX + 'input/foot-step-observation/source-id']
selected = {}
seeds = {}
for name, (side, first, last) in scenarios.items():
    values = []
    for row in main:
        if row['sample.dimension'].endswith('/' + side) and first <= int(row[frame_key]) <= last:
            other = by_frame_side[(int(row[frame_key]), 'right' if side == 'left' else 'left')]
            values.append({'frame': int(row[frame_key]), 'sampleSequence': row['sample.sequence'], 'sampleDimension': row['sample.dimension'], 'lineageHigh': row['sample.lineage.high'], 'lineageLow': row['sample.lineage.low'], 'main': [row[k] for k in main_keys], 'pairedMain': [other[k] for k in main_keys], 'sources': sources[(row['sample.lineage.high'], row['sample.lineage.low'])]})
            if paired_scene:
                values[-1]['paired'] = {'frame': int(other[frame_key]), 'sampleSequence': other['sample.sequence'], 'sampleDimension': other['sample.dimension'], 'lineageHigh': other['sample.lineage.high'], 'lineageLow': other['sample.lineage.low'], 'main': [other[k] for k in main_keys], 'sources': sources[(other['sample.lineage.high'], other['sample.lineage.low'])]}
    assert len(values) == last - first + 1, name
    selected[name] = values
    if scene in ['releasing', 'releasing-continued']:
        row = by_frame_side[(first - 1, side)]
        other = by_frame_side[(first - 1, 'left')]
        def seed_frame(value):
            return {'frame': int(value[frame_key]), 'sampleSequence': value['sample.sequence'], 'sampleDimension': value['sample.dimension'],
                'lineageHigh': value['sample.lineage.high'], 'lineageLow': value['sample.lineage.low'], 'main': [value[k] for k in main_keys],
                'sources': sources[(value['sample.lineage.high'], value['sample.lineage.low'])]}
        seeds[name] = seed_frame(row)
        seeds[name]['paired'] = seed_frame(other)

lookup = {}
for name, selected_values in selected.items():
    values = selected_values + ([seeds[name]] if name in seeds else [])
    for frame in values:
        lookup.setdefault((frame['sampleSequence'], frame['sampleDimension']), []).append(frame)
        if paired_scene:
            paired = frame['paired']
            lookup.setdefault((paired['sampleSequence'], paired['sampleDimension']), []).append(paired)

tables = {'probes': ('current-support-probes', 'foot'), 'outputProbes': ('output-support-probes', 'foot'), 'targetProbes': ('state-target-support-probes', 'foot'), 'contacts': ('ground-contacts', 'foot'), 'envelope': ('ground-envelope', 'foot'), 'surfaces': ('ground-surfaces', 'foot'), 'responseLineage': ('pre-state-response-lineage', 'foot'), 'bodyTrajectory': ('future-body-trajectory', 'input')}
hashes = {'main': hashlib.sha256((FOOT / 'character-foot-ik%2Ffull.csv').read_bytes()).hexdigest(), 'sources': hashlib.sha256(source_path.read_bytes()).hexdigest()}
for table, (suffix, group) in tables.items():
    path = FOOT / ('character-foot-ik%2Ffull.' + suffix + '.csv')
    prefix = 'character-foot-ik/' + suffix + '/' + group + '/'
    keys = [k for k in next(rows(path)) if k.startswith(prefix)]
    columns[table] = [k.removeprefix(prefix) for k in keys]
    for targets in lookup.values():
        for frame in targets:
            frame[table] = []
    for row in rows(path):
        key = (row['sample.sequence'], row['sample.dimension'])
        if key in lookup:
            values = [row[k] for k in keys]
            for frame in lookup[key]: frame[table].append(values)
    hashes[table] = hashlib.sha256(path.read_bytes()).hexdigest()

for name, (side, first, last) in scenarios.items():
    fixture = {'capture': FOOT.name, 'presentationCapture': PRESENTATION.name, 'side': side, 'firstFrame': first, 'lastFrame': last, 'baselineCommit': '2c757a422', 'columns': columns, 'sourceIds': source_ids, 'sourceSha256': hashes, 'frames': selected[name], 'executionScope': 'Recorded mixed animated foot and formal inputs to real foot functions; no whole pre-solver pose or FBBIK bend history in this capture'}
    if name in seeds: fixture['seed'] = seeds[name]
    for frame in fixture['frames']:
        assert len(frame['probes']) == 23, (name, frame['frame'])
    OUT.joinpath(name + '-input.json').write_text(json.dumps(fixture, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')

contracts_path = ROOT / '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement'
baseline = ROOT / '3cDemo/Client/3C_Client/Temp/FootStoredPoseFunctions/baseline-2c757a422'
baseline.mkdir(parents=True, exist_ok=True)
for file in ['CharacterFootLifecycle.cs', 'CharacterFootInterpolationRuntime.cs', 'CharacterFootHardConstraintResolver.cs', 'CharacterFootLandingRuntime.cs']:
    relative = (contracts_path / file).relative_to(ROOT).as_posix()
    content = subprocess.check_output(['git', '-C', str(ROOT), 'show', '2c757a422:' + relative])
    baseline.joinpath(file).write_bytes(content)

print(json.dumps({'fixtures': {name: {'frames': len(values), 'first': values[0]['frame'], 'last': values[-1]['frame'], 'sourceWeight': values[0]['main'][columns['main'].index('input/foot-step-observation/source-weight')]} for name, values in selected.items()}, 'contactCaptureCandidateCount': len(contact_captures)}, ensure_ascii=True))
baseline.joinpath('contact-capture-candidates.json').write_text(json.dumps(contact_captures, ensure_ascii=False, indent=2), encoding='utf-8')
