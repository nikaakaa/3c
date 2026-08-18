using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootLandingStep1Report
    {
        internal CharacterFootLandingStep1Report(
            bool passed,
            string csvPath,
            int uniqueFrames,
            int dualWeightFrames,
            int singleSwingFrames,
            int visibleLiftFrames,
            float maxPelvisWeight,
            float maxNegativeCorrection,
            string summary)
        {
            Passed = passed;
            CsvPath = csvPath;
            UniqueFrames = uniqueFrames;
            DualWeightFrames = dualWeightFrames;
            SingleSwingFrames = singleSwingFrames;
            VisibleLiftFrames = visibleLiftFrames;
            MaxPelvisWeight = maxPelvisWeight;
            MaxNegativeCorrection = maxNegativeCorrection;
            Summary = summary;
        }

        internal bool Passed { get; }
        internal string CsvPath { get; }
        internal int UniqueFrames { get; }
        internal int DualWeightFrames { get; }
        internal int SingleSwingFrames { get; }
        internal int VisibleLiftFrames { get; }
        internal float MaxPelvisWeight { get; }
        internal float MaxNegativeCorrection { get; }
        internal string Summary { get; }
    }

    internal static class CharacterFootLandingStep1Evaluator
    {
        internal static CharacterFootLandingStep1Report Evaluate(string csvPath)
        {
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                return new CharacterFootLandingStep1Report(
                    false, csvPath ?? string.Empty, 0, 0, 0, 0, 0f, 0f,
                    "CSV 不存在。");
            }

            Dictionary<(int frame, string root), FramePair> frames =
                new Dictionary<(int, string), FramePair>();
            using (StreamReader reader = new StreamReader(csvPath))
            {
                string header = reader.ReadLine();
                if (string.IsNullOrEmpty(header))
                    return Fail(csvPath, "CSV 为空。");
                string[] names = header.Split(',');
                int iFrame = Index(names, "FrameSequence");
                int iRoot = Index(names, "RootInstanceId");
                int iSide = Index(names, "Side");
                int iW = Index(names, "FinalGoalPositionWeight");
                int iPel = Index(names, "PelvisPositionWeight");
                int iVc = Index(names, "FootMotionVerticalCorrection");
                int iResid = Index(names, "FinalIkPositionResidual");
                if (iFrame < 0 || iRoot < 0 || iSide < 0 || iW < 0 || iPel < 0 || iVc < 0)
                    return Fail(csvPath, "CSV 缺第 1 步所需列。");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] cells = line.Split(',');
                    if (cells.Length <= Math.Max(iFrame, Math.Max(iSide, iW)))
                        continue;
                    int frame = ParseInt(cells[iFrame]);
                    string root = cells[iRoot];
                    string side = cells[iSide];
                    (int frame, string root) key = (frame, root);
                    if (!frames.TryGetValue(key, out FramePair pair))
                        pair = new FramePair();
                    FootRow row = new FootRow(
                        ParseFloat(cells[iW]),
                        ParseFloat(cells[iVc]),
                        iResid >= 0 ? ParseFloat(cells[iResid]) : 0f,
                        ParseFloat(cells[iPel]));
                    if (side == "Left")
                        pair.Left = row;
                    else if (side == "Right")
                        pair.Right = row;
                    frames[key] = pair;
                }
            }

            int unique = 0;
            int dual = 0;
            int single = 0;
            int visible = 0;
            float maxPelvis = 0f;
            float maxNeg = 0f;
            foreach (FramePair pair in frames.Values)
            {
                if (!pair.Left.HasValue || !pair.Right.HasValue)
                    continue;
                unique++;
                FootRow left = pair.Left.Value;
                FootRow right = pair.Right.Value;
                maxPelvis = Math.Max(maxPelvis, Math.Max(left.PelvisWeight, right.PelvisWeight));
                maxNeg = Math.Min(maxNeg, Math.Min(left.Vertical, right.Vertical));
                bool leftOn = left.Weight > 0.01f;
                bool rightOn = right.Weight > 0.01f;
                if (leftOn && rightOn)
                    dual++;
                else if (leftOn || rightOn)
                    single++;
                if (leftOn && left.Vertical > 0.03f && left.Residual < 0.01f)
                    visible++;
                if (rightOn && right.Vertical > 0.03f && right.Residual < 0.01f)
                    visible++;
            }

            bool passed = unique > 0 &&
                          maxPelvis <= 0.01f &&
                          dual == 0 &&
                          single > 0 &&
                          visible >= 8 &&
                          maxNeg > -0.02f;
            string summary =
                $"frames={unique} dual={dual} singleSwing={single} visibleLift={visible} " +
                $"maxPelvisW={maxPelvis:0.###} maxNegCorr={maxNeg:0.###} => {(passed ? "PASS" : "FAIL")}";
            return new CharacterFootLandingStep1Report(
                passed, csvPath, unique, dual, single, visible, maxPelvis, maxNeg, summary);
        }

        static CharacterFootLandingStep1Report Fail(string path, string reason) =>
            new CharacterFootLandingStep1Report(false, path, 0, 0, 0, 0, 0f, 0f, reason);

        static int Index(string[] names, string name)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                    return i;
            }
            return -1;
        }

        static int ParseInt(string value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : 0;

        static float ParseFloat(string value) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : 0f;

        struct FramePair
        {
            internal FootRow? Left;
            internal FootRow? Right;
        }

        readonly struct FootRow
        {
            internal FootRow(float weight, float vertical, float residual, float pelvisWeight)
            {
                Weight = weight;
                Vertical = vertical;
                Residual = residual;
                PelvisWeight = pelvisWeight;
            }

            internal float Weight { get; }
            internal float Vertical { get; }
            internal float Residual { get; }
            internal float PelvisWeight { get; }
        }
    }
}
