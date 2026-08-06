import fs from "node:fs/promises";
import { Workbook } from "@oai/artifact-tool";

const csvPath = "D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Scenes/Standalone/foot-ik-10affd69dccd445d83cb0a83ced6f583.csv";
const csvText = await fs.readFile(csvPath, "utf8");
const workbook = await Workbook.fromCSV(csvText, { sheetName: "FootIK" });
const sheet = workbook.worksheets.getItem("FootIK");
const used = sheet.getUsedRange(true);
const values = used.values;
const headers = values[0] ?? [];
const rows = values.slice(1).filter(row => row.some(value => value !== null && value !== ""));

const aliases = {
  frame: ["frame"],
  bodyGrounded: ["body_grounded", "bodygrounded"],
  targetGrounded: ["target_grounded", "targetgrounded"],
  groundedBefore: ["grounded_before", "groundedbefore"],
  groundedAfter: ["grounded_after", "groundedafter"],
  solverFailure: ["solver_failure", "solverfailure"],
  leftContact: ["left_plant_contact", "leftplantcontact", "l_plant_contact"],
  leftPlanar: ["left_world_planar_speed", "leftworldplanarspeed", "l_world_planar_speed"],
  leftVertical: ["left_world_vertical_speed", "leftworldverticalspeed", "l_world_vertical_speed"],
  leftDistance: ["left_surface_distance", "leftsurfacedistance", "l_surface_distance"],
  leftAlign: ["left_ground_alignment_weight", "leftgroundalignmentweight", "l_ground_alignment_weight"],
  leftGoal: ["left_goal_position_weight", "leftgoalpositionweight", "l_goal_position_weight"],
  leftResidual: ["left_position_residual", "leftpositionresidual", "l_position_residual"],
  rightContact: ["right_plant_contact", "rightplantcontact", "r_plant_contact"],
  rightPlanar: ["right_world_planar_speed", "rightworldplanarspeed", "r_world_planar_speed"],
  rightVertical: ["right_world_vertical_speed", "rightworldverticalspeed", "r_world_vertical_speed"],
  rightDistance: ["right_surface_distance", "rightsurfacedistance", "r_surface_distance"],
  rightAlign: ["right_ground_alignment_weight", "rightgroundalignmentweight", "r_ground_alignment_weight"],
  rightGoal: ["right_goal_position_weight", "rightgoalpositionweight", "r_goal_position_weight"],
  rightResidual: ["right_position_residual", "rightpositionresidual", "r_position_residual"]
};

const normalized = headers.map(header => String(header ?? "").toLowerCase().replace(/[^a-z0-9]+/g, "_").replace(/^_|_$/g, ""));
const indexes = Object.fromEntries(Object.entries(aliases).map(([key, names]) => {
  const index = normalized.findIndex(header => names.some(name => header === name || header.includes(name)));
  return [key, index];
}));

const numberAt = (row, key) => {
  const index = indexes[key];
  if (index < 0) return null;
  const value = Number(row[index]);
  return Number.isFinite(value) ? value : null;
};
const boolAt = (row, key) => {
  const index = indexes[key];
  if (index < 0) return null;
  const value = String(row[index]).toLowerCase();
  return value === "true" || value === "1";
};
const summarizeNumber = key => {
  const data = rows.map(row => numberAt(row, key)).filter(value => value !== null);
  if (data.length === 0) return null;
  return {
    count: data.length,
    min: Math.min(...data),
    max: Math.max(...data),
    avg: data.reduce((sum, value) => sum + value, 0) / data.length,
    zeroRatio: data.filter(value => Math.abs(value) < 1e-6).length / data.length,
    positiveRatio: data.filter(value => value > 1e-4).length / data.length
  };
};
const summarizeBool = key => {
  const data = rows.map(row => boolAt(row, key)).filter(value => value !== null);
  if (data.length === 0) return null;
  return { count: data.length, trueRatio: data.filter(Boolean).length / data.length };
};

const contactFrames = side => rows.filter(row => boolAt(row, `${side}Contact`));
const contactStats = side => {
  const subset = contactFrames(side);
  const stats = {};
  for (const suffix of ["Planar", "Vertical", "Distance", "Align", "Goal", "Residual"]) {
    const key = `${side}${suffix}`;
    const data = subset.map(row => numberAt(row, key)).filter(value => value !== null);
    stats[suffix.toLowerCase()] = data.length === 0 ? null : {
      min: Math.min(...data),
      max: Math.max(...data),
      avg: data.reduce((sum, value) => sum + value, 0) / data.length,
      zeroRatio: data.filter(value => Math.abs(value) < 1e-6).length / data.length
    };
  }
  return { frames: subset.length, ...stats };
};

const samples = rows.filter(row =>
  boolAt(row, "leftContact") || boolAt(row, "rightContact") ||
  (numberAt(row, "leftAlign") ?? 0) > 0 || (numberAt(row, "rightAlign") ?? 0) > 0
).slice(0, 12).map(row => Object.fromEntries(Object.keys(indexes).map(key => [key, indexes[key] < 0 ? null : row[indexes[key]]])));

console.log(JSON.stringify({
  source: { csvPath, bytes: Buffer.byteLength(csvText), rows: rows.length, columns: headers.length },
  headers,
  normalized,
  indexes,
  grounded: {
    body: summarizeBool("bodyGrounded"),
    target: summarizeBool("targetGrounded"),
    before: summarizeBool("groundedBefore"),
    after: summarizeBool("groundedAfter")
  },
  totals: Object.fromEntries(Object.keys(indexes).filter(key => !key.toLowerCase().includes("grounded") && !key.toLowerCase().includes("contact") && key !== "solverFailure").map(key => [key, summarizeNumber(key)])),
  leftContact: contactStats("left"),
  rightContact: contactStats("right"),
  samples
}, null, 2));
