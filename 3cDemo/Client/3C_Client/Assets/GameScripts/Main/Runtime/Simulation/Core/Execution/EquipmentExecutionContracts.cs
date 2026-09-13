namespace ThirdPersonSimulation
{
    internal interface IEquipmentActionContextProvider
    {
        bool TryReadActionContext(EquipmentActionRouteId routeId, out EquipmentActionContext context);
        bool IsAbilityBinding(EquipmentActionContext context, CharacterSkillId abilityId);
        bool IsCurrentActionContext(EquipmentActionContext context);
    }
}
