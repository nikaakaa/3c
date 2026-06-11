using NUnit.Framework;

public sealed class CorinAnimationSplitterTests
{
    [TestCase("Bip001")]
    [TestCase("Bip001/Bip001 Pelvis")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 L Thigh/Bip001 L Calf/Bip001 L Foot")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Spine2/Bip001 L Clavicle/Bip001 L UpperArm")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Spine2/Bip001 Neck/Bip001 Head")]
    public void KeepsHumanoidSkeletonPath(string path)
    {
        Assert.IsTrue(CorinAnimationSplitter.IsHumanoidBindingForTest(path));
    }

    [TestCase("Bip001/Bip001 Prop1/Weapon_Lever_01")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Spine2/Bip001 Neck/Bip001 Head/Hair_F_01")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Skirt_01")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/S_ChainB_01")]
    [TestCase("Bip001/Bip001 Pelvis/Spring_L01")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Kuma_Main")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Spine2/Chest_L")]
    [TestCase("Corin_face")]
    public void RemovesNonHumanoidAttachmentPath(string path)
    {
        Assert.IsFalse(CorinAnimationSplitter.IsHumanoidBindingForTest(path));
    }

    [TestCase("Bip001/Bip001 Prop1/Weapon_Lever_01")]
    [TestCase("Bip001/Bip001 Prop1/Weapon_saw")]
    [TestCase("Corin_Weapon")]
    public void DetectsWeaponPath(string path)
    {
        Assert.IsTrue(CorinAnimationSplitter.IsWeaponBindingForTest(path));
        Assert.IsFalse(CorinAnimationSplitter.IsGenericWithoutWeaponBindingForTest(path));
    }

    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Skirt_01")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Spine2/Bip001 Neck/Bip001 Head/Hair_F_01")]
    [TestCase("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Spine2/Bip001 L Clavicle/Bip001 L UpperArm")]
    public void GenericWithoutWeaponKeepsNonWeaponPath(string path)
    {
        Assert.IsFalse(CorinAnimationSplitter.IsWeaponBindingForTest(path));
        Assert.IsTrue(CorinAnimationSplitter.IsGenericWithoutWeaponBindingForTest(path));
    }
}
