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

    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    class Program
    {

        // The port number for the remote device.
        //private const int port = 11000;
        //Char IAC = Convert.ToChar(255);
        //Char DO = Convert.ToChar(253);
        //Char DONT = Convert.ToChar(254);
        //Char WILL = Convert.ToChar(251);
        //Char WONT = Convert.ToChar(252);
        //Char SB = Convert.ToChar(250);
        //Char SE = Convert.ToChar(240);
        //const Char IS = '0';
        //const Char SEND = '1';
        //const Char INFO = '2';
        //const Char VAR = '0';
        //const Char VALUE = '1';
        //const Char ESC = '2';
        //const Char USERVAR = '3';

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.
        private static String response = String.Empty;

        private static bool cond_code = false;
        private static List<byte> cond_token = new List<byte>();
        private static List<byte> byte_str = new List<byte>();
        private static List<byte> chinese_chr = new List<byte>();
        private static bool has_SquareBracket = false;
        private static int wordcount = 0;


        static void Main(string[] args)
        {
            // Connect to a remote device.
            try
            {


                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".                
                IPHostEntry ipHostInfo = Dns.GetHostEntry("ptt.cc");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 23);

                Console.WindowWidth = 90;
                Console.WindowHeight = 0x18 + 1; // add one more row
                Console.Title = "Sample BBS - " + ipAddress;

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
                    Send(client, ReturnKeyPress());
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

                    //// There might be more data, so store the data received so far.
                    //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\cxxxxn.txt", true))
                    //{
                    //    file.Write(result);

                    //}
                    if (state.buffer.Contains((byte)255))
                    {
                        ASCII_sequenceParser(ref client, ref state.buffer);
                    }
                    else
                    {
                        ASCII_Parser(ref state.buffer, ref  cond_code, ref  has_SquareBracket, ref chinese_chr, ref  byte_str, ref  cond_token, ref wordcount, ref bytesRead);
                    }
                  
                    

                    state.sb.Append(bytesRead);
                    //state.sb.Append(result);
                    //Console.WriteLine(result);
                    // Get the rest of the data.
                    //clear buff
                        state.buffer = new byte[256];
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
                // var headerbuffer = new Buffer([0xFF, 0xFF, 0xAA, 0x55, 0xAA, 0x55, 0x37, 0xBA]);
                // Create a new dictionary of strings, with string keys. 
                //
                //Dictionary<int, byte[]> map =new Dictionary<int, byte[]>();
                //map.Add(ConsoleKey.UpArrow,{ 0x1b, 0x4f, 0x44 });

                if (key.Key == ConsoleKey.UpArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x41 };
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x42 };
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x44 };
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    byteData = new byte[3] { 0x1b, 0x4f, 0x43 };
                }
                else if (key.Key == ConsoleKey.Delete)
                {
                    byteData = new byte[1] { 127 };
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    byteData = new byte[4] { 0x1b, (byte)'[', (byte)'5', (byte)'1' };
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    byteData = new byte[3] { 0x1b, (byte)'[', (byte)'2' };
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    byteData = new byte[3] { 0x1b, (byte)'[', (byte)'~' };
                }
                else if (key.Key == ConsoleKey.End)
                {
                    byteData = new byte[3] { 0x1b, (byte)'[', (byte)'4' };
                }
                else if (key.Key == ConsoleKey.Insert)
                {
                    byteData = new byte[3] { 0x1b, (byte)'[', (byte)'2' };
                }
                //else if (key.KeyChar != 224)
                //{
                //    // chinese char start with  0xe0 or double-char key that probably miss
                //    byteData = new byte[2] { 0xe0, Convert.ToByte(key.KeyChar) };
                //}
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

        private static void ASCII_Parser(ref byte[] ASCII, ref bool cond_code, ref bool has_SquareBracket, ref List<byte> chinese_chr, ref  List<byte> byte_str, ref List<byte> cond_token, ref int wordcount, ref int bytesRead)
        {
            // There might be more data, so store the data received so far.
            //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\cxxxxn.txt", true))
            //{
            //    file.Write(result);

            //}
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\cxxxxn.txt", true))
            {

                // There might be more data, so store the data received so far.
                //file.Write(result);

                string test = Encoding.GetEncoding(950).GetString(ASCII);
 
                foreach (byte c in ASCII)
                {                     
                    //escape sequence start flag
                    if (c == 0x1b) cond_code = true;

                    //file.Write(BitConverter.ToString(new byte[] { c })+ "_"); 
                    //text content direct output to console
                    if (cond_code == false)
                    {
                        if (wordcount == 80)
                        {
                            //Console.CursorTop++;
                           //file.Write(Environment.NewLine);
                            wordcount = 0;
                        }
                        if (c == 0x0a) // need check
                        {
                            wordcount = 0;
                            if (Console.CursorTop == 39)
                            { Console.Clear(); }
                            // Console.CursorTop++;
                            
                            Console.Write(Convert.ToChar(c));
                        }
                        if (c == 0x0d) Console.CursorLeft = 0;
                        if (c == 0x08) Console.CursorLeft--;

                        if (c != 0x08 && c != 0x0a && c != 0x0d)
                        {
                            if (chinese_chr.Count == 0)
                            {                                
                                // ASCII only encodes 128 characters 0x7f
                                // characters start with char greater then 128 0x80
                                if (c < 128) Console.Write(Convert.ToChar(c));
                                else  chinese_chr.Add(c);
                                
                            }
                            else
                            {
                               
                                    chinese_chr.Add(c);
                                    wordcount++;
                                    Console.Write(Encoding.GetEncoding(950).GetString(chinese_chr.ToArray()));
                                    chinese_chr.Clear();

                                //else {
                                //    chinese_chr.Add(c);
                                //    wordcount++;
                                //    Console.Write("[" + BitConverter.ToString(new byte[] { chinese_chr[0], chinese_chr[1] }) +"]"  );
                                //    chinese_chr.Clear();
                                //}
                            }
                        }
                    }
                    if (cond_code == true && c != 0x1b)
                    {

                        cond_token.Add(c);
                        if (c == (byte)'[') has_SquareBracket = true;

                        //finish the escape sequence and parser the display output
                        if (c == (byte)'m')
                        {
                            string token = Encoding.GetEncoding(950).GetString(cond_token.ToArray());
                            if (token == "[;m" || token == "[m")
                            {
                                Console.ResetColor();
                            }
                            List<string> asii_tokens = new List<string>();
                            asii_tokens = token.Split(new char[] { ';' }).ToList();
                            //token = token.Replace("[", "").Replace("m", "");

                            //escape sequence Color not finished
                            foreach (string asii_t in asii_tokens)
                            {
                                string ascii = asii_t.Replace("[", "").Replace("m", "");
                                switch (ascii)
                                {
                                    case "0":
                                        Console.ResetColor();
                                        break;
                                    case "30":
                                        Console.ForegroundColor = ConsoleColor.Black; //DarkGray ; //DarkGray  ;

                                        break;
                                    case "31":
                                        Console.ForegroundColor = ConsoleColor.DarkRed;

                                        break;
                                    case "32":
                                        Console.ForegroundColor = ConsoleColor.DarkGreen; // DarkGreen;

                                        break;
                                    case "33":
                                        Console.ForegroundColor = ConsoleColor.DarkYellow; //Yellow;

                                        break;
                                    case "34":
                                        Console.ForegroundColor = ConsoleColor.DarkBlue; //DarkBlue; //Blue;

                                        break;
                                    case "35":
                                        Console.ForegroundColor = ConsoleColor.DarkMagenta; //Magenta;

                                        break;
                                    case "36":
                                        Console.ForegroundColor = ConsoleColor.DarkCyan; //Cyan;

                                        break;
                                    case "37":
                                        Console.ForegroundColor = ConsoleColor.White; //白色以淺灰代表

                                        break;
                                    //背景比前景暗一度
                                    case "40":
                                        Console.BackgroundColor = ConsoleColor.Black;

                                        break;
                                    case "41":
                                        Console.BackgroundColor = ConsoleColor.DarkRed;//ok

                                        break;
                                    case "42":
                                        Console.BackgroundColor = ConsoleColor.DarkGreen; //ok

                                        break;
                                    case "43":
                                        Console.BackgroundColor = ConsoleColor.DarkYellow; //ok

                                        break;
                                    case "44":
                                        Console.BackgroundColor = ConsoleColor.DarkBlue; //ok

                                        break;
                                    case "45":
                                        Console.BackgroundColor = ConsoleColor.DarkMagenta; //ok

                                        break;
                                    case "46":
                                        Console.BackgroundColor = ConsoleColor.DarkCyan;//Cyan;

                                        break;
                                    case "47":
                                        Console.BackgroundColor = ConsoleColor.Gray; //??

                                        break;
                                }
                            }
                            //finish parser
                            cond_code = false;
                            has_SquareBracket = false;

                        }
                        if (c == (byte)'H' || c == (byte)'f')
                        {
                            string token = Encoding.GetEncoding(950).GetString(cond_token.ToArray());
                            if (token == "[H" || token == "[;H" || token == "[f" || token == "[;f")
                                Console.SetCursorPosition(Console.WindowLeft, Console.WindowTop);
                            else
                            {
                                try
                                {
                                    List<string> c_TopRight = new List<string>();
                                    token = token.Replace("[", "").Replace("H", "");
                                    c_TopRight = token.Split(new char[] { ';' }).ToList();
                                    if (int.Parse(c_TopRight[1]) - 1 < 0) c_TopRight[1] = "1";
                                    if (int.Parse(c_TopRight[0]) - 1 < 0) c_TopRight[0] = "1";

                                    Console.CursorLeft = Console.WindowLeft + int.Parse(c_TopRight[1]) - 1;
                                    Console.CursorTop = Console.WindowTop + int.Parse(c_TopRight[0]) - 1;
                                }
                                catch (Exception e)
                                {
                                }
                            }
                            cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                        if (c == (byte)'J')
                        {
                            string token = Encoding.GetEncoding(950).GetString(cond_token.ToArray());
                            if (token == "[2J")
                                Console.Clear();
                            else
                                //"escape sequence J ");//haven't seen before
                                cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                        if (c == (byte)'K')
                        {
                            string token = Encoding.GetEncoding(950).GetString(cond_token.ToArray());
                            if (token == "[K" || token == "[0K")
                            {
                                int org = Console.CursorLeft;
                                for (int th = Console.CursorLeft; th < Console.WindowWidth - 1; th++) //probably correct
                                    Console.Write(" ");
                                Console.CursorLeft = org;
                            }
                            else
                                //"escape sequence K ,never meet
                                cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                        if (c == (byte)'r') //ColaBBS 發表編輯或讀取文章會用到的控制屬性  set scroll region ?
                        {
                            string token = Encoding.GetEncoding(950).GetString(cond_token.ToArray());
                            token = token.Replace("[", "").Replace("r", "");
                            List<string> c_TopRight = token.Split(new char[] { ';' }).ToList();

                            //if (token == "[;r")
                            //("unfinish [;r");
                            //else
                            // show(token);

                            cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                        if (c == (byte)'D') //ColaBBS attribute index when edit??
                        {
                            //&& has_SquareBracket == false
                            //MessageBox.Show("unfinish cond D");
                            cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                        //Inser line (<n> lines)  ; Esc  [ <n> L
                        if (c == (byte)'L') //fix 2013.04.04
                        {
                            Console.WindowTop--;
                            Console.CursorTop--;
                            int org = Console.CursorLeft;
                            for (int th = Console.CursorLeft; th < Console.WindowWidth - 1; th++)
                                Console.Write(" ");
                            Console.CursorLeft = org;
                            cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                        if (c == (byte)'M' && has_SquareBracket == false)
                        {
                            if (Console.WindowTop - 1 > 0) Console.WindowTop--;
                            if (Console.CursorTop - 1 > 0) Console.CursorTop--;
                            cond_code = false;
                            has_SquareBracket = false;
                            cond_token.Clear();
                        }
                    }
                }
                //here 
                if (cond_code == true)
                {                 
                }
                //file.Write(Environment.NewLine);
            }
        }

        private static void ASCII_sequenceParser(ref Socket client, ref byte[] sequence)
        {
            bool IAC_code = false;
            byte res = 0;
            //WILL = 251, 0xfb
            //WONT = 252, 0xfc
            //DO = 253, 0xfd
            //DONT = 254, 0xfe
            //IAC = 255 0xff
            for (int i = 0; i < sequence.Length - 1; i++)
            {
                byte c = sequence[i];
                if (IAC_code == true)
                {
                    switch (c)
                    {
                        case (int)Verbs.IAC:
                            break;
                        case (int)Verbs.DO:
                        case (int)Verbs.DONT:
                        case (int)Verbs.WILL:
                        case 0xfa:
                        case (int)Verbs.WONT:
                            int inputoption = sequence[i + 1];
                            if (c == 0xfd) res = 0xfb;
                            if (c == 0xfb) res = 0xfd;
                            if (c == 0xfa) res = 0xfa;

                            if (inputoption == 0)
                            {
                                if (c == 0xfb) res = 0xfe;
                                if (c == 0xfd) res = 0xfc;
                            }

                            break;

                    }
                    IAC_code = false;
                }

                if (c == 255)
                {
                    IAC_code = true;
                }

            }
        }
    }


}
