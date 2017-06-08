using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Http;

namespace AeonGrinder
{
    public static class Utils
    {
        public static void InvokeOn(Control ctl, Action action)
        {
            if (ctl != null)
            {
                if (ctl.InvokeRequired)
                {
                    ctl.Invoke(new Action(() => InvokeOn(ctl, action)));
                }
                else action();
            }
        }

        public static void Delay(int ms, CancellationToken token)
        {
            Task.Delay(ms, token).Wait();
        }

        public static void Delay(int min, int max, CancellationToken token)
        {
            Task.Delay(Rand(min, max), token).Wait();
        }

        public static void Sleep(int ms)
        {
            Thread.Sleep(ms);
        }

        public static void Sleep(int minms, int maxms)
        {
            Random rand = new Random();
            int ms = rand.Next(minms, maxms);

            Thread.Sleep(ms);
        }

        public static int Rand(int min, int max)
        {
            Random rand = new Random();
            return rand.Next(min, max);
        }

        public static void Delay(int[] t1, int[] t2, CancellationToken token)
        {
            switch (Rand(0, 2))
            {
                case 0: Delay(t1[0], t1[1], token); break;
                case 1: Delay(t2[0], t2[1], token); break;
            }
        }

        public static void Delay(int[] t1, int[] t2, int[] t3, CancellationToken token)
        {
            switch (Rand(0, 3))
            {
                case 0: Delay(t1[0], t1[1], token); break;
                case 1: Delay(t2[0], t2[1], token); break;
                case 2: Delay(t3[0], t3[1], token); break;
            }
        }

        public static double Rand(double min, double max)
        {
            Random rand = new Random();
            return rand.NextDouble() * (min - max) + max;
        }

        public static T[] RandomPermutation<T>(T[] array)
        {
            T[] retArray = new T[array.Length];
            array.CopyTo(retArray, 0);

            Random random = new Random();
            for (int i = 0; i < array.Length; i += 1)
            {
                int swapIndex = random.Next(i, array.Length);
                if (swapIndex != i)
                {
                    T temp = retArray[i];
                    retArray[i] = retArray[swapIndex];
                    retArray[swapIndex] = temp;
                }
            }

            return retArray;
        }

        public static string GenerateMD5Hash(string value)
        {
            MD5 md5 = MD5.Create();
            byte[] input = Encoding.UTF8.GetBytes(value);
            byte[] hash = md5.ComputeHash(input);

            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }


        public static async Task<T> GetRequest<T>(string url)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetAsync(url);
                    var contentString = await response.Content.ReadAsStringAsync();

                    return Serializer.ToJsonObject<T>(contentString);
                }
            }
            catch
            {
                return default(T);
            }
        }

        public static async Task<T> PostRequest<T>(string url, HttpContent content)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var response = await http.PostAsync(url, content);
                    var contentString = await response.Content.ReadAsStringAsync();

                    return Serializer.ToJsonObject<T>(contentString);
                }
            }
            catch
            {
                return default(T);
            }
        }
    }
}
