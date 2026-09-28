using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraProjectionCompilationContext
    {
        readonly Dictionary<int, CameraCurveAsset> m_Curves = new Dictionary<int, CameraCurveAsset>();

        public CameraProjectionCompilationContext(CharacterCameraProfile profile)
        {
            for (int i = 0; i < profile.Curves.Count; i++)
            {
                CameraCurveAsset curve = profile.Curves[i];
                if (!m_Curves.TryAdd(curve.GetInstanceID(), curve))
                    throw new InvalidOperationException($"Camera Profile '{profile.name}' contains a duplicated Curve reference.");
            }
        }

        public CameraCurvePayload CompileCurve(CameraCurveAsset curve)
        {
            RequireCurve(curve).RequireValid();
            return curve.Compile();
        }

        public CameraCurvePayload CompileCurve(string curveId)
        {
            foreach (CameraCurveAsset curve in m_Curves.Values)
                if (string.Equals(curve.CurveId, curveId, StringComparison.Ordinal))
                    return CompileCurve(curve);
            throw new InvalidOperationException($"Camera Curve '{curveId}' is not registered by its Profile.");
        }

        public CameraCurveAsset RequireCurve(CameraCurveAsset curve)
        {
            if (!curve || !m_Curves.ContainsKey(curve.GetInstanceID()))
                throw new InvalidOperationException($"Camera Curve '{curve?.name ?? "Missing"}' is not registered by its Profile.");
            return curve;
        }

        public CameraCurvePayload[] CompileCurves(IReadOnlyList<CameraCurveAsset> curves)
        {
            var result = new CameraCurvePayload[curves.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = CompileCurve(curves[i]);
            return result;
        }
    }
}
