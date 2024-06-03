namespace p2p.Core.StunClientEndPoint;

/// <summary>
/// This class implements STUN ERROR-CODE. Defined in RFC 3489 11.2.9.
/// </summary>
/// <remarks>
/// Default constructor.
/// </remarks>
/// <param name="code">Error code.</param>
/// <param name="reasonText">Reason text.</param>
public class StunErrorCode(int code, string reasonText)
{
    /// <summary>
    /// Gets or sets error code.
    /// </summary>
    public int Code { get; set; } = code;

    /// <summary>
    /// Gets reason text.
    /// </summary>
    public string ReasonText { get; set; } = reasonText;
}