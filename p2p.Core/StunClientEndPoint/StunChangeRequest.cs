namespace p2p.Core.StunClientEndPoint;

/// <summary>
/// This class implements STUN CHANGE-REQUEST attribute. Defined in RFC 3489 11.2.4.
/// </summary>
/// <remarks>
/// Default constructor.
/// </remarks>
/// <param name="changeIP">Specifies if STUN server must
/// send response to different IP than request was received.</param>
/// <param name="changePort">Specifies if STUN server must
/// send response to different port than request was received.</param>
public class StunChangeRequest(bool changeIP, bool changePort)
{
    /// <summary>
    /// Gets or sets if STUN server must send response
    /// to different IP than request was received.
    /// </summary>
    public bool ChangeIP { get; set; } = changeIP;

    /// <summary>
    /// Gets or sets if STUN server must send response
    /// to different port than request was received.
    /// </summary>
    public bool ChangePort { get; set; } = changePort;
}