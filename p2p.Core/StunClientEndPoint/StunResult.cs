using System.Net;

namespace p2p.Core.StunClientEndPoint;

/// <summary>
/// Default constructor.
/// </summary>
/// <param name="netType">Specifies UDP network type.</param>
/// <param name="publicEndPoint">Public IP end point.</param>
public class StunResult(StunNetType netType, IPEndPoint? publicEndPoint)
{
    /// <summary>
    /// Gets UDP network type.
    /// </summary>
    public StunNetType NetType { get; } = netType;

    /// <summary>
    /// Gets internal IP end point. This value is null if failed to get network type.
    /// </summary>
    public IPEndPoint? PublicEndPoint { get; } = publicEndPoint;
}
