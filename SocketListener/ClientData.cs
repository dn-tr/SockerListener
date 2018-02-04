using System.Net.Sockets;

namespace SocketListener
{
    /// <summary>
    /// Client data
    /// </summary>
    public class ClientData
    {
        public const int BufferSize = 1024;

        public byte[] Buffer = new byte[BufferSize];
        public Socket Socket = null;

        /// <summary>
        /// String data from client
        /// </summary>
        public string StrData = string.Empty;

        /// <summary>
        /// Get summary int data
        /// </summary>
        public int IntData { get; set; }

        /// <summary>
        /// Get client IP address
        /// </summary>
        public string IPAddress
        {
            get
            {
                if (Socket == null)
                    return string.Empty;

                return Socket.RemoteEndPoint.ToString();
            }
        }

        public void AddIntData(int data)
        {
            IntData += data;
        }
    }
}
