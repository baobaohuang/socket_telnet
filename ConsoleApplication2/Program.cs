using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;

// State object for receiving data from remote device.
public class StateObject
{
    // Client socket.
    public Socket workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 256;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    public StringBuilder sb = new StringBuilder();
}

namespace ConsoleApplication2
{
    class Program
    {
        // The port number for the remote device.
        private const int port = 11000;
        Char IAC = Convert.ToChar(255);
        Char DO = Convert.ToChar(253);
        Char DONT = Convert.ToChar(254);
        Char WILL = Convert.ToChar(251);
        Char WONT = Convert.ToChar(252);
        Char SB = Convert.ToChar(250);
        Char SE = Convert.ToChar(240);
        const Char IS = '0';
        const Char SEND = '1';
        const Char INFO = '2';
        const Char VAR = '0';
        const Char VALUE = '1';
        const Char ESC = '2';
        const Char USERVAR = '3';

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.
        private static String response = String.Empty;

        static void Main(string[] args)
        {
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".                
                IPHostEntry ipHostInfo = Dns.GetHostEntry("kk.muds.idv.tw");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 4000);

                // Create a TCP/IP socket.
                Socket client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the remote device.
                while (true)
                {
                     
                    Send(client, ReturnKeyPress ());
                    sendDone.WaitOne();
                }
                // Receive the response from the remote device.
                //Receive(client);
                //receiveDone.WaitOne();

                // Write the response to the console.
                //Console.WriteLine("Response received : {0}", response);

                // Release the socket.
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                if (client.Connected)
                {
                    Receive(client);
                }
                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());
                // Signal that the connection has been made.
                connectDone.Set();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    string pattern = @"([\x00-\x1f]\[0?(\d+)m)|([\x00-\x1f]\[(\d+);(\d+)m)|([\x00-\x1f]\[(\d+)m)|([\x00-\x1f]\[m)|([\x00-\x1f]\[(\d*);(\d+);(\d+)m)|([\x00-\x1f]\[(\d*)H)|([\x00-\x1f]\[(\d*)J)|(\x3f\[(\d*)m)|(\x3f\[(\d*);(\d+)m)|(\x3f\[(\d*);(\d*);(\d+)m)";
                    Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                    string result = rgx.Replace(Encoding.GetEncoding(950).GetString(state.buffer, 0, bytesRead), "");
                    // There might be more data, so store the data received so far.
                    state.sb.Append(result);
                    Console.WriteLine(result);
                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, byte[] byteData)
        {

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                if (client.Connected)
                {
                    Receive(client);
                }

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static byte[] ReturnKeyPress()
        {
            try
            {
                byte[] byteData;
                ConsoleKeyInfo key;
                key = Console.ReadKey(true);
                // Convert the string data to byte data using ASCII encoding.
                //var headerbuffer = new Buffer([0xFF, 0xFF, 0xAA, 0x55, 0xAA, 0x55, 0x37, 0xBA]);
                // Create a new dictionary of strings, with string keys. 
                //
                //Dictionary<int, byte[]> map =new Dictionary<int, byte[]>();
                //map.Add(ConsoleKey.UpArrow,{ 0x1b, 0x4f, 0x44 });

                if (key.Key == ConsoleKey.UpArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x44 };
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x44 };
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x44 };
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x44 };
                }
                else
                {
                    byteData = new byte[1] { Convert.ToByte(key.KeyChar) };

                }

                return byteData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Byte[] empty = new Byte[0];
                return empty;
            }
            
        }

    }


}
