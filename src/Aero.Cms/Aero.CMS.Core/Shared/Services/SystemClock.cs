using Aero.CMS.Core.Shared.Interfaces;

namespace Aero.CMS.Core.Shared.Services;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
