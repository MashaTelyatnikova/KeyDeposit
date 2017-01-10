using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using KeyDeposit;
using Parameters;

namespace TrustedCenter
{
    public class Program
    {
        private static string SetTemplate = @"set id=([a-zA-Z0-9-]{36});t=([0-9]+);s=([0-9]+)";

        public static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Enter bankPort and port");
                    Environment.Exit(0);
                }

                int bankPort;
                if (!int.TryParse(args[0], out bankPort))
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

                var dict = new Dictionary<string, Tuple<int, int>>();
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
                        var bytes = new byte[1024];
                        var bytesRec = handler.Receive(bytes);

                        var encryptedData = bytes.Take(bytesRec).ToArray();

                        Console.Write("Received encrypted data: " + Encoding.UTF8.GetString(encryptedData) + "\n\n");

                        var data = Center.Decrypt(encryptedData);
                        Console.WriteLine($"Real data = {data}");
                        if (Regex.IsMatch(data, SetTemplate))
                        {
                            Console.WriteLine("Set template");
                            var groups = Regex.Match(data, SetTemplate).Groups;
                            var id = groups[1].Value;
                            var t = int.Parse(groups[2].Value);
                            var s = int.Parse(groups[3].Value);

                            var reply = "";
                            if (t == BigInteger.ModPow(Consts.G, s, Consts.P))
                            {
                                reply = "Ok";
                                dict[id] = Tuple.Create(t, s);
                                SendMessageTo(bankPort, $"set id={id};t={t}");
                            }
                            else
                            {
                                reply = "Not ok";
                            }

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
                finally
                {
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }

        static void SendMessageTo(int port, string s)
        {
            var ipHost = Dns.GetHostEntry("localhost");
            var ipAddr = ipHost.AddressList[0];
            var ipEndPoint = new IPEndPoint(ipAddr, port);
            var sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(ipEndPoint);
            var msg = Encoding.UTF8.GetBytes(s);

            sender.Send(msg);
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
    }
}