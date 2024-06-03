using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace p2p.Core.StunClientEndPoint;

/// <summary>
/// This class implements STUN client. Defined in RFC 3489.
/// </summary>
/// <example>
/// <code>
/// // Create new socket for STUN client.
/// Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
/// socket.Bind(new IPEndPoint(IPAddress.Any,0));
/// 
/// // Query STUN server
/// STUN_Result result = STUN_Client.Query("stunserver.org",3478,socket);
/// if(result.NetType != STUN_NetType.UdpBlocked){
///     // UDP blocked or !!!! bad STUN server
/// }
/// else{
///     IPEndPoint publicEP = result.PublicEndPoint;
///     // Do your stuff
/// }
/// </code>
/// </example>
public class StunClient
{
    #region static method Query

    /// <summary>
    /// Gets NAT info from STUN server.
    /// </summary>
    /// <param name="host">STUN server name or IP.</param>
    /// <param name="port">STUN server port. Default port is 3478.</param>
    /// <param name="socket">UDP socket to use.</param>
    /// <returns>Returns UDP netwrok info.</returns>
    /// <exception cref="Exception">Throws exception if unexpected error happens.</exception>
    public static StunResult Query(string host, int port, Socket socket)
    {
        ArgumentNullException.ThrowIfNull(nameof(host));
        ArgumentNullException.ThrowIfNull(nameof(socket));

        if (port < 1)
        {
            throw new ArgumentException("Port value must be >= 1 !");
        }

        if (socket.ProtocolType != ProtocolType.Udp)
        {
            throw new ArgumentException("Socket must be UDP socket !");
        }

        var remoteEndPoint = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);

        socket.ReceiveTimeout = 3000;
        socket.SendTimeout = 3000;

        /*
            In test I, the client sends a STUN Binding Request to a server, without any flags set in the
            CHANGE-REQUEST attribute, and without the RESPONSE-ADDRESS attribute. This causes the server 
            to send the response back to the address and port that the request came from.
        
            In test II, the client sends a Binding Request with both the "change IP" and "change port" flags
            from the CHANGE-REQUEST attribute set.  
          
            In test III, the client sends a Binding Request with only the "change port" flag set.
                      
                                +--------+
                                |  Test  |
                                |   I    |
                                +--------+
                                     |
                                     |
                                     V
                                    /\              /\
                                 N /  \ Y          /  \ Y             +--------+
                  UDP     <-------/Resp\--------->/ IP \------------->|  Test  |
                  Blocked         \ ?  /          \Same/              |   II   |
                                   \  /            \? /               +--------+
                                    \/              \/                    |
                                                     | N                  |
                                                     |                    V
                                                     V                    /\
                                                 +--------+  Sym.      N /  \
                                                 |  Test  |  UDP    <---/Resp\
                                                 |   II   |  Firewall   \ ?  /
                                                 +--------+              \  /
                                                     |                    \/
                                                     V                     |Y
                          /\                         /\                    |
           Symmetric  N  /  \       +--------+   N  /  \                   V
              NAT  <--- / IP \<-----|  Test  |<--- /Resp\               Open
                        \Same/      |   I    |     \ ?  /               Internet
                         \? /       +--------+      \  /
                          \/                         \/
                          |                           |Y
                          |                           |
                          |                           V
                          |                           Full
                          |                           Cone
                          V              /\
                      +--------+        /  \ Y
                      |  Test  |------>/Resp\---->Restricted
                      |   III  |       \ ?  /
                      +--------+        \  /
                                         \/
                                          |N
                                          |       Port
                                          +------>Restricted

        */

        // Test I
        var test1 = new StunMessage
        {
            Type = StunMessageType.BindingRequest
        };

        var test1response = DoTransaction(test1, socket, remoteEndPoint);

        // UDP blocked.
        if (test1response is null)
        {
            return new StunResult(StunNetType.UdpBlocked, null);
        }
        else
        {
            // Test II
            var test2 = new StunMessage
            {
                Type = StunMessageType.BindingRequest,
                ChangeRequest = new StunChangeRequest(true, true)
            };

            // No NAT.
            if (socket.LocalEndPoint?.Equals(test1response.MappedAddress) ?? false)
            {
                var test2Response = DoTransaction(test2, socket, remoteEndPoint);
                // Open Internet.
                if (test2Response != null)
                {
                    return new StunResult(StunNetType.OpenInternet, test1response.MappedAddress);
                }
                // Symmetric UDP firewall.
                else
                {
                    return new StunResult(StunNetType.SymmetricUdpFirewall, test1response.MappedAddress);
                }
            }
            // NAT
            else
            {
                var test2Response = DoTransaction(test2, socket, remoteEndPoint);
                // Full cone NAT.
                if (test2Response != null)
                {
                    return new StunResult(StunNetType.FullCone, test1response.MappedAddress);
                }
                else
                {
                    /*
                        If no response is received, it performs test I again, but this time, does so to 
                        the address and port from the CHANGED-ADDRESS attribute from the response to test I.
                    */

                    // Test I(II)
                    var test12 = new StunMessage
                    {
                        Type = StunMessageType.BindingRequest
                    };

                    var test12Response = DoTransaction(test12, socket, test1response.ChangedAddress);
                    if (test12Response == null)
                    {
                        throw new Exception("STUN Test I(II) dind't get resonse !");
                    }
                    else
                    {
                        // Symmetric NAT
                        if (!test12Response.MappedAddress?.Equals(test1response.MappedAddress) ?? false)
                        {
                            return new StunResult(StunNetType.Symmetric, test1response.MappedAddress);
                        }
                        else
                        {
                            // Test III
                            var test3 = new StunMessage
                            {
                                Type = StunMessageType.BindingRequest,
                                ChangeRequest = new StunChangeRequest(false, true)
                            };

                            var test3Response = DoTransaction(test3, socket, test1response.ChangedAddress);
                            // Restricted
                            if (test3Response != null)
                            {
                                return new StunResult(StunNetType.RestrictedCone, test1response.MappedAddress);
                            }
                            // Port restricted
                            else
                            {
                                return new StunResult(StunNetType.PortRestrictedCone, test1response.MappedAddress);
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion
    #region method DoTransaction

    /// <summary>
    /// Does STUN transaction. Returns transaction response or null if transaction failed.
    /// </summary>
    /// <param name="request">STUN message.</param>
    /// <param name="socket">Socket to use for send/receive.</param>
    /// <param name="remoteEndPoint">Remote end point.</param>
    /// <returns>Returns transaction response or null if transaction failed.</returns>
    static StunMessage? DoTransaction(StunMessage request, Socket socket, IPEndPoint? remoteEndPoint)
    {
        if (remoteEndPoint is null)
        {
            return null;
        }

        var requestBytes = request.ToByteData();
        var startTime = DateTime.UtcNow;

        // We do it only 2 sec and retransmit with 100 ms.
        while (startTime.AddSeconds(2) > DateTime.UtcNow)
        {
            try
            {
                socket.SendTo(requestBytes, remoteEndPoint);

                // We got response.
                if (socket.Poll(100, SelectMode.SelectRead))
                {
                    byte[] receiveBuffer = new byte[512];
                    socket.Receive(receiveBuffer);

                    // Parse message
                    var response = new StunMessage();
                    response.Parse(receiveBuffer);

                    // Check that transaction ID matches or not response what we want.
                    if (request.TransactionId.Equals(response.TransactionId))
                    {
                        return response;
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    #endregion

}