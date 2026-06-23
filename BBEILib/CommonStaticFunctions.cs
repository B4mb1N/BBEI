using BBEIDataAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using System.Text;
using ExcelDataReader;
using BBEIDataAccess.Models;
using Serilog;
using HtmlAgilityPack;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Net.Sockets;

namespace BBEILib
{
    public static class CommonStaticFunctions
    {
        private static readonly string apiUrlLoginFantaLeghe = $"https://www.fantacalcio.it/api/v1/User/login";
        private static readonly string apiUrlPricesExcelFantaLeghe = $"https://www.fantacalcio.it/api/v1/Excel/prices/20/1";
        private static readonly string urlCalendarFantaLeghe = $"https://www.fantacalcio.it/serie-a/calendario/";
        private static readonly string apiUrlVotesExcelFantaLeghe = $"https://www.fantacalcio.it/api/v1/Excel/votes/20/";
        private static readonly string excelPath = @"./tmp/SRAexcel.xlsx";
        private static readonly string votesPath = @"./tmp/SRAvotes_";
        private static readonly string htmlPath = @"./tmp/round_{ind}.html";


        public static bool LoginFantaLeghe(string username, string password, out IEnumerable<string> cookies)
        {
            cookies = null;
            bool ret = false;
            try
            {
                //username = "B41N88";
                //password = "T3stFAS10?";

                string jsonInString = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";

                var handler = new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    EnableMultipleHttp2Connections = true,
                    UseProxy = false,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12
                    },

                    // 👇 LA CALLBACK CHE FORZA IPv4
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        // Risolve gli IP dell'host
                        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host);

                        // Prende SOLO il primo IPv4
                        var ipv4 = addresses.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                        // Apre un socket IPv4
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        await socket.ConnectAsync(new IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);

                        // Restituiamo lo stream della connessione
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/123.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Accept-Language", "it-IT,it;q=0.9");
                    client.DefaultRequestHeaders.Add("Origin", "https://www.fantacalcio.it");
                    client.DefaultRequestHeaders.Add("Referer", "https://www.fantacalcio.it");

                    HttpResponseMessage response = client.PostAsync(apiUrlLoginFantaLeghe, new StringContent(jsonInString, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Leggere il cookie dalla risposta
                        if (response.Headers.TryGetValues("Set-Cookie", out cookies))
                        {
                            Log.Information("Cookies salvati.");
                        }
                        else
                        {
                            Log.Error("Nessun cookie trovato nella risposta.");
                        }

                        ret = true;

                        Log.Information("Login Response: " + ret);
                    }
                    else
                    {
                        Log.Error("Errore nella richiesta API.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("LoginFantaLeghe Errore: " + ex.Message);
            }

            return ret;
        }

        public static bool GetSRAVotesInfo(IEnumerable<string> cookies, int round)
        {
            bool ret = false;
            try
            {
                var handler = new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    EnableMultipleHttp2Connections = true,
                    UseProxy = false,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12
                    },

                    // 👇 LA CALLBACK CHE FORZA IPv4
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        // Risolve gli IP dell'host
                        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host);

                        // Prende SOLO il primo IPv4
                        var ipv4 = addresses.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                        // Apre un socket IPv4
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        await socket.ConnectAsync(new IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);

                        // Restituiamo lo stream della connessione
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/123.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Accept-Language", "it-IT,it;q=0.9");
                    client.DefaultRequestHeaders.Add("Origin", "https://www.fantacalcio.it");
                    client.DefaultRequestHeaders.Add("Referer", "https://www.fantacalcio.it");

                    var request = new HttpRequestMessage(HttpMethod.Get, apiUrlVotesExcelFantaLeghe + round);
                    foreach (string cookie in cookies)
                    {
                        request.Headers.Add("Cookie", cookie);
                    }

                    // Eseguire la richiesta successiva
                    HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        File.WriteAllBytes(votesPath + round.ToString("D2") + ".xlsx", fileBytes); // Usa WriteAllBytes invece di WriteAllBytesAsync
                        Log.Information("File salvato con successo a: " + votesPath + round.ToString("D2") + ".xlsx");
                        ret = true;
                    }
                    else
                    {
                        Log.Error("Errore nella richiesta API.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("GetSRAVotesInfo Errore: " + ex.Message);
            }

            return ret;
        }

        public static bool GetSRAPlayersInfo(IEnumerable<string> cookies)
        {
            bool ret = false;
            try
            {
                var handler = new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    EnableMultipleHttp2Connections = true,
                    UseProxy = false,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12
                    },

                    // 👇 LA CALLBACK CHE FORZA IPv4
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        // Risolve gli IP dell'host
                        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host);

                        // Prende SOLO il primo IPv4
                        var ipv4 = addresses.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                        // Apre un socket IPv4
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        await socket.ConnectAsync(new IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);

                        // Restituiamo lo stream della connessione
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/123.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Accept-Language", "it-IT,it;q=0.9");
                    client.DefaultRequestHeaders.Add("Origin", "https://www.fantacalcio.it");
                    client.DefaultRequestHeaders.Add("Referer", "https://www.fantacalcio.it");

                    var request = new HttpRequestMessage(HttpMethod.Get, apiUrlPricesExcelFantaLeghe);
                    foreach (string cookie in cookies)
                    {
                        request.Headers.Add("Cookie", cookie);
                    }

                    // Eseguire la richiesta successiva
                    HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        File.WriteAllBytes(excelPath, fileBytes); // Usa WriteAllBytes invece di WriteAllBytesAsync
                        Log.Information("File salvato con successo a: " + excelPath);
                        ret = true;
                    }
                    else
                    {
                        Log.Error("Errore nella richiesta API.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("GetSRAPlayersInfo Errore: " + ex.Message);
            }

            return ret;
        }

        
        public static string NormalizzaStr(string s)
        {
            return s.ToUpper()
                .Replace("\t", "")
                .Replace("-", "")
                .Replace("À", "A")
                .Replace("È", "E")
                .Replace("Ò", "O")
                .Replace("Ì", "I")
                .Replace("Ù", "U")
                .Replace("Ä", "A")
                .Replace("Ö", "O")
                .Replace("Ü", "U")
                .Replace(".", "")
                .Replace("(", " ")
                .Replace(")", " ");
        }

        public static bool ConfrontaNormalizzate(string s1, string s2)
        {
            string n1 = NormalizzaStr(s1);
            string n2 = NormalizzaStr(s2);

            if (string.IsNullOrEmpty(n1))
                return false;

            if (string.IsNullOrEmpty(n2))
                return false;

            n1 = n1.Trim();
            n2 = n2.Trim();
            n1 = (n1.StartsWith("P ") || n1.StartsWith("D ") || n1.StartsWith("C ") || n1.StartsWith("A ")) ? n1.Substring(2) : n1;
            n2 = (n2.StartsWith("P ") || n2.StartsWith("D ") || n2.StartsWith("C ") || n2.StartsWith("A ")) ? n2.Substring(2) : n2;
            n1 = (n1.EndsWith(" P") || n1.EndsWith(" D") || n1.EndsWith(" C") || n1.EndsWith(" A")) ? n1.Substring(0, n1.Length - 2) : n1;
            n2 = (n2.EndsWith(" P") || n2.EndsWith(" D") || n2.EndsWith(" C") || n2.EndsWith(" A")) ? n2.Substring(0, n2.Length - 2) : n2;

            string[] a1 = n1.Split(' ');
            string[] a2 = n2.Split(' ');
            if (a1.Length > 1)
            {
                if (a1.Any(x => x.Length > 3))
                {
                    n1 = "";
                    for (int i = 0; i < a1.Length; i++)
                    {
                        if (a1[i].Length <= 3)
                            a1[i] = "";
                        n1 += a1[i];
                    }
                }
            }
            if (a2.Length > 1)
            {
                if (a2.Any(x => x.Length > 3))
                {
                    n2 = "";
                    for (int i = 0; i < a2.Length; i++)
                    {
                        if (a2[i].Length <= 3)
                            a2[i] = "";
                        n2 += a2[i];
                    }
                }
            }

            if (n1.Contains(n2 + " ") || n2.Contains(n1 + " "))
                return true;

            if (ComputeDistance(n1, n2) <= 2)
                return true;
            
            return false;
        }

        public static int ComputeDistance(
            string first,
            string second
        )
        {
            if (first.Length == 0)
            {
                return second.Length;
            }

            if (second.Length == 0)
            {
                return first.Length;
            }

            var current = 1;
            var previous = 0;
            var r = new int[2, second.Length + 1];
            for (var i = 0; i <= second.Length; i++)
            {
                r[previous, i] = i;
            }

            for (var i = 0; i < first.Length; i++)
            {
                r[current, 0] = i + 1;

                for (var j = 1; j <= second.Length; j++)
                {
                    var cost = (second[j - 1] == first[i]) ? 0 : 1;
                    r[current, j] = Min(
                        r[previous, j] + 1,
                        r[current, j - 1] + 1,
                        r[previous, j - 1] + cost);
                }
                previous = (previous + 1) % 2;
                current = (current + 1) % 2;
            }
            return r[previous, second.Length];
        }

        private static int Min(int e1, int e2, int e3) =>
            Math.Min(Math.Min(e1, e2), e3);
    }
}
