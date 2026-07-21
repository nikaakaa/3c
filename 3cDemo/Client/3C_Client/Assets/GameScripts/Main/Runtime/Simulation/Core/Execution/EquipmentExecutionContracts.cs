namespace ThirdPersonSimulation
{
    internal interface IEquipmentActionContextProvider
    {
        EquipmentActionContext Current { get; }
    }
}
