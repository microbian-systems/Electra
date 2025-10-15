using System.Security.Cryptography;

namespace Electra.Core;

public static class Snowflake
{
    public static int MachineId { get; private set; } = RandomNumberGenerator.GetInt32(1, 1024);
    public static void SetMachineId(int machineId) => MachineId = machineId;

    static Snowflake()
    {
        SnowflakeGuid.SetMachineID(MachineId);
    }
    public static long NewId()
    {
        var snowflake = SnowflakeGuid.Create();
        return (long)snowflake.Id; // for ef core / db reasons
    }
}