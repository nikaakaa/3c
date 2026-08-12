import fs from "node:fs/promises";
import { Workbook } from "@oai/artifact-tool";

const csvPath = "D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Scenes/Standalone/foot-ik-ac3bfc2ad2944ea68ee48c269cdd3664.csv";
const csvText = await fs.readFile(csvPath, "utf8");
const workbook = await Workbook.fromCSV(csvText, { sheetName: "FootIK" });
const sheet = workbook.worksheets.getItem("FootIK");
const used = sheet.getUsedRange(true);
const values = used.values;
const headers = values[0].map(value => String(value ?? ""));
const counts = new Map();
for (const header of headers)
  counts.set(header, (counts.get(header) ?? 0) + 1);
const duplicateHeaders = [...counts.entries()].filter(([, count]) => count > 1);
const rowWidths = values.map(row => row.length);
console.log(JSON.stringify({
  rowsIncludingHeader: values.length,
  dataRows: Math.max(0, values.length - 1),
  columns: headers.length,
  minimumRowWidth: Math.min(...rowWidths),
  maximumRowWidth: Math.max(...rowWidths),
  duplicateHeaders,
  firstHeaders: headers.slice(0, 12),
  lastHeaders: headers.slice(-12),
}));
