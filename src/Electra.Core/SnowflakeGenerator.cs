namespace Electra.Core;

public static class Snowflake
{
    public static int MachineId { get; private set; } = 0;
    public static void SetMachineId(int machineId) => MachineId = machineId;
    
    public static long NewId()
    {
        SnowflakeGuid.SetMachineID(MachineId);
        var snowflake = SnowflakeGuid.Create();
        return (long)snowflake.Id; // for ef core / db reasons
    }
}