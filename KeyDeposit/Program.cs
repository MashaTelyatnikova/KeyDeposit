using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using Parameters;

namespace KeyDeposit
{
    public class Program
    {
        public static void DistribureKeys(string id, int[] t, int[] s, int[] ports)
        {
            for (var i = 0; i < ports.Length; ++i)
            {
                var bytes = new byte[1024];
                var ipHost = Dns.GetHostEntry("localhost");
                var ipAddr = ipHost.AddressList[0];
                var ipEndPoint = new IPEndPoint(ipAddr, ports[i]);

                var sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(ipEndPoint);

                var m = $"set id={id};t={t[i]};s={s[i]}";
                var msg = Center.Encrypt(m);
                Console.WriteLine($"Send Encrypt({m}) = {Encoding.UTF8.GetString(msg)} to localhost:{ports[i]}");
                sender.Send(msg);
                var bytesRec = sender.Receive(bytes);

                Console.WriteLine("Center reply: {0}\n", Encoding.UTF8.GetString(bytes, 0, bytesRec));
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
        }

        public static void SignOpenKey(string id, int t, int bankPort)
        {
            var bytes = new byte[1024];
            var ipHost = Dns.GetHostEntry("localhost");
            var ipAddr = ipHost.AddressList[0];
            var ipEndPoint = new IPEndPoint(ipAddr, bankPort);
            var sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(ipEndPoint);
            byte[] msg = Encoding.UTF8.GetBytes($"sign id={id};t={t}");
            sender.Send(msg);
            var bytesRec = sender.Receive(bytes);

            Console.WriteLine("Bank reply: {0}\n", Encoding.UTF8.GetString(bytes, 0, bytesRec));
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You have to enter config file");
                Environment.Exit(0);
            }

            var config = args[0];
            if (!File.Exists(config))
            {
                Console.WriteLine("Config file doesn't exist");
                Environment.Exit(0);
            }

            var reader = new StreamReader(config);

            var id = Guid.NewGuid().ToString();
            Console.WriteLine($"id = {id}");
            var bankPort = int.Parse(reader.ReadLine());
            var n = int.Parse(reader.ReadLine());
            var ports = new int[n];
            for (var i = 0; i < n; ++i)
            {
                ports[i] = int.Parse(reader.ReadLine());
            }

            var p = Consts.P;
            var g = Consts.G;

            var random = new Random(Guid.NewGuid().GetHashCode());
            var s = random.Next(1, p);

            var sParts = new int[n];
            var sum = 0;
            for (var i = 0; i < n - 1; ++i)
            {
                sParts[i] = random.Next(1, p);
                sum += sParts[i];
            }

            var result = (s - sum)%(p - 1);
            if (result < 0)
            {
                result += p - 1;
            }

            sParts[n - 1] = result;
            var tParts = new int[n];
            var t = 1;
            for (var i = 0; i < n; ++i)
            {
                tParts[i] = (int) BigInteger.ModPow(g, sParts[i], p);
                t *= tParts[i];
            }

            DistribureKeys(id, tParts, sParts, ports);
            Thread.Sleep(500);
            SignOpenKey(id, t, bankPort);
        }
    }
}