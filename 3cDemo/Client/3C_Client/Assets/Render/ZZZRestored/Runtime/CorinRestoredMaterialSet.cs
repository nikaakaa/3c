using UnityEngine;

namespace ZZZ.Rendering.Restored
{
    [CreateAssetMenu(menuName = "ZZZ/Restored/Corin Material Set")]
    public sealed class CorinRestoredMaterialSet : ScriptableObject
    {
        [SerializeField] Material body;
        [SerializeField] Material face;
        [SerializeField] Material eye;
        [SerializeField] Material hair;
        [SerializeField] Material hairShadow;
        [SerializeField] Material weapon;
        [SerializeField] Texture2DArray matCap;

        public Material Body => body;
        public Material Face => face;
        public Material Eye => eye;
        public Material Hair => hair;
        public Material HairShadow => hairShadow;
        public Material Weapon => weapon;
        public Texture2DArray MatCap => matCap;

        public void Initialize(Material bodyMaterial, Material faceMaterial, Material eyeMaterial,
            Material hairMaterial, Material hairShadowMaterial, Material weaponMaterial, Texture2DArray matCapTexture)
        {
            body = bodyMaterial;
            face = faceMaterial;
            eye = eyeMaterial;
            hair = hairMaterial;
            hairShadow = hairShadowMaterial;
            weapon = weaponMaterial;
            matCap = matCapTexture;
            ApplyRuntimeArrays();
        }

        public void ApplyRuntimeArrays()
        {
            if (body == null || matCap == null)
                return;
            var refract = new Vector4[5];
            var tint = new Vector4[5];
            var textureAndBurst = new Vector4[5];
            var speedAndRefract = new Vector4[5];
            for (var index = 0; index < 5; index++)
            {
                refract[index] = body.GetVector(Indexed("_RefractParam", index));
                tint[index] = body.GetVector(Indexed("_MatCapColorTint", index));
                textureAndBurst[index] = new Vector4(
                    0f,
                    body.GetFloat(Indexed("_MatCapColorBurst", index)),
                    body.GetFloat(Indexed("_MatCapAlphaBurst", index)),
                    body.GetFloat(Indexed("_MatCapUSpeed", index)));
                speedAndRefract[index] = new Vector4(
                    body.GetFloat(Indexed("_MatCapVSpeed", index)),
                    body.GetFloat(Indexed("_MatCapBlendMode", index)),
                    body.GetFloat(Indexed("_MatCapRefract", index)),
                    body.GetFloat(Indexed("_RefractDepth", index)));
            }
            body.SetTexture("_MatCap2DArray", matCap);
            body.SetVectorArray("_RefractParamArray", refract);
            body.SetVectorArray("_MatCapColorTintArray", tint);
            body.SetVectorArray("_MatCapTexID_MatCapColorBurst_MatCapAlphaBurst_MatCapUSpeed", textureAndBurst);
            body.SetVectorArray("_MatCapVSpeed_MatCapBlendMode_MatCapRefract_RefractDepth", speedAndRefract);
        }

        static string Indexed(string name, int index) => index == 0 ? name : name + (index + 1);
    }
}
