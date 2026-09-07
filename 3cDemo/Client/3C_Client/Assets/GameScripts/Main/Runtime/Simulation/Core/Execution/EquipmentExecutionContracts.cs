namespace ThirdPersonSimulation
{
    internal interface IEquipmentActionContextProvider
    {
        bool TryReadActionContext(EquipmentActionRouteId routeId, out EquipmentActionContext context);
        bool IsSkillBinding(EquipmentActionContext context, CharacterSkillId skillId);
        bool IsCurrentActionContext(EquipmentActionContext context);
    }
}
