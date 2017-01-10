using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Bank
{
    public class Program
    {
        private static string SetTemplate = @"set id=([a-zA-Z0-9-]{36});t=([0-9]+)";
        private static string SignTemplate = @"sign id=([a-zA-Z0-9-]{36});t=([0-9]+)";
        private static Bank bank = new Bank();

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Enter amount of centers and port");
                Environment.Exit(0);
            }

            int n;
            if (!int.TryParse(args[0], out n))
            {
                Console.WriteLine("Enter valid number");
                Environment.Exit(0);
            }

            int port;
            if (!int.TryParse(args[1], out port))
            {
                Console.WriteLine("Enter valid number");
                Environment.Exit(0);
            }

            var dict = new Dictionary<string, List<int>>();
            var ipHost = Dns.GetHostEntry("localhost");
            var ipAddr = ipHost.AddressList[0];
            var ipEndPoint = new IPEndPoint(ipAddr, port);
            var sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);

                while (true)
                {
                    var handler = sListener.Accept();
                    string data = null;

                    var bytes = new byte[1024];
                    var bytesRec = handler.Receive(bytes);

                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    Console.Write("Received: " + data + "\n\n");

                    string reply = "";
                    if (Regex.IsMatch(data, SetTemplate))
                    {
                        var groups = Regex.Match(data, SetTemplate).Groups;
                        var id = groups[1].Value;
                        var t = int.Parse(groups[2].Value);
                        if (!dict.ContainsKey(id))
                        {
                            dict[id] = new List<int>();
                        }
                        dict[id].Add(t);
                    }
                    else if (Regex.IsMatch(data, SignTemplate))
                    {
                        var groups = Regex.Match(data, SignTemplate).Groups;
                        var id = groups[1].Value;
                        var t = int.Parse(groups[2].Value);
                        if (dict.ContainsKey(id))
                        {
                            var ts = dict[id];
                            if (ts.Count == n)
                            {
                                if (ts.Aggregate(1, (x, y) => x*y) == t)
                                {
                                    reply = $"Ok. Signature = {bank.Sign(id, t)}";
                                }
                                else
                                {
                                    reply = "Not ok";
                                }
                            }
                            else
                            {
                                reply = "I have not all keys";
                            }
                        }
                        else
                        {
                            reply = "Id is not found";
                        }
                    }

                    if (reply.Length > 0)
                    {
                        byte[] msg = Encoding.UTF8.GetBytes(reply);
                        handler.Send(msg);
                    }
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}