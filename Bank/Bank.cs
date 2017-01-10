using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Bank
{
    public class Bank
    {
        private static readonly BigInteger n;
        private static readonly BigInteger e;
        private static readonly BigInteger d;

        static Bank()
        {
            //todo возможно читать из конфига
            n = 527;
            e = 7;
            d = 343;
        }
        public string Sign(string s, int t)
        {
            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes($"{s}${t}");
            var data = new BigInteger(md5.ComputeHash(inputBytes));
            data = data%n;
            if (data < 0)
            {
                data += n;
            }

            return BigInteger.ModPow(data, d, n).ToString();
        }
    }
}