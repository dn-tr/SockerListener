using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketListener
{
    class Program
    {
        // Connected client list
        static List<ClientData> ClientList = new List<ClientData>();

        static void Main(string[] args)
        {
            int port = 0;

            if (args.Length == 0 || !int.TryParse(args[0], out port))
                port = 11000; // default port for test

            StartListening(port);
        }

        // Thread signal
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static void StartListening(int port)
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Create a socket
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);

            // Bind the socket to the local endpoint and listen for incoming connections
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                Console.WriteLine($"Waiting for a connection on port [{port}]...");

                while (true)
                {
                    allDone.Reset();

                    // Start listen for connections
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Client connection
        /// </summary>
        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue 
            allDone.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ClientData clientData = new ClientData();
            clientData.Socket = handler;
            handler.BeginReceive(clientData.Buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReadCallback), clientData);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"[{clientData.IPAddress}] connected");
            Console.ResetColor();

            Send(clientData, GetHello());

            ClientList.Add(clientData);
        }

        /// <summary>
        /// Read and parse received data
        /// </summary>
        public static void ReadCallback(IAsyncResult ar)
        {
            ClientData clientData = (ClientData)ar.AsyncState;
            Socket handler = clientData.Socket;

            // Read data from the client socket
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                clientData.StrData += Encoding.UTF8.GetString(clientData.Buffer, 0, bytesRead).Replace("\r\n", "\n");

                if (clientData.StrData.IndexOf("\n") > -1)
                {
                    string strData = clientData.StrData.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();

                    Console.WriteLine($"[{clientData.IPAddress}]: received '{strData}'");

                    int intData = 0;

                    if (int.TryParse(strData, out intData))
                    {
                        clientData.AddIntData(intData);
                        Send(clientData, $"SUM: {clientData.IntData}\r\n");
                    }
                    else
                    {
                        strData = strData.ToLower();
                        switch (strData)
                        {
                            case "h":
                            case "help":
                                Send(clientData, GetHelp());
                                break;

                            case "l":
                            case "list":
                                Send(clientData, GetClients());
                                break;

                            case "c":
                            case "close":
                                CloseConnection(clientData);
                                return;

                            default:
                                Send(clientData, "Unknown command. Enter h/help for help.\r\n");
                                break;
                        }
                    }

                    clientData.StrData = string.Empty;
                }

                // Get next data  
                handler.BeginReceive(clientData.Buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReadCallback), clientData);
            }
        }

        /// <summary>
        /// Send data to client
        /// </summary>
        private static void Send(ClientData clientData, string data)
        {
            Socket handler = clientData.Socket;
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), clientData);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                ClientData clientData = (ClientData)ar.AsyncState;
                Socket handler = clientData.Socket;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine($"[{clientData.IPAddress}]: sent {bytesSent} bytes to client.");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Close client connection
        /// </summary>
        private static void CloseConnection(ClientData clientData)
        {
            try
            {
                Socket handler = clientData.Socket;

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[{clientData.IPAddress}] disconnected");
                Console.ResetColor();

                ClientList.Remove(clientData);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Get welcome string
        /// </summary>
        public static string GetHello()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Connection OK...");
            sb.AppendLine("Enter any int value for get summary int value. Enter h/help for help.");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Get help string
        /// </summary>
        public static string GetHelp()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[any int value]          print summary int value");
            sb.AppendLine("l        - list          print IP addresses all of clints");
            sb.AppendLine("c        - close         close connection");
            sb.AppendLine("h        - help          print help information");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Get client list
        /// </summary>
        public static string GetClients()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var client in ClientList)
                sb.AppendLine($"[{client.IPAddress}] SUM: {client.IntData}");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
