using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

namespace QuizizzSupport
{
    // Most the structs are in the Utils.cs file, cuz I didn't bother moving them to structs.cs
    public class Utils
    {
        public static string version = "7.1";
        //Response Json structure to deserialize to. There are many more interesting informations in the response, they are just not useful for this aplication; ex. 
        /*
        "stats": {
                "played": 18,
                "totalPlayers": 121,
                "totalCorrect": 752,
                "totalQuestions": 1622
            },
        */

        public class QuizData
        {
            public Data data { get; set; }
            public bool success { get; set; }
        }
        public class Data
        {
            public Quiz quiz { get; set; }
            public List<Questions> questions { get; set; }
        }
        public class Quiz
        {
            public Info info { get; set; }
        }
        public class Info
        {
            public List<Questions> questions { get; set; }
            public string lang { get; set; }
        }
        public class Questions
        {
            public Structure structure { get; set; }
            public string type { get; set; }
            public string _id { get; set; }
        }
        public class Structure
        {
            public Settings settings { get; set; }
            public Query query { get; set; }
            public List<Options> options { get; set; }
            public dynamic answer { get; set; }
        }
        public class Settings
        {
            public bool hasCorrectAnswer { get; set; }
        }
        public class Query
        {
            public string text { get; set; }
            public List<Media> media { get; set; }
            public bool hasMath { get; set; }
            public Math math { get; set; }
        }
        public class Math
        {
            public List<string> latex { get; set; }
            public string template { get; set; }
        }
        public class Media
        {
            public string type { get; set; }
            public string url { get; set; }
        }
        public class Options
        {
            public List<Answers> options { get; set; }
            public string text { get; set; }
            public List<Media> media { get; set; }
            public bool hasMath { get; set; }
            public Math math { get; set; }
        }
        public class Answers
        {
            public string text { get; set; }
        }

        public class RoomData
        {
            public Room room { get; set; }
        }
        public class Room
        {
            public string hash { get; set; }
            public string quizId { get; set; }
            public string versionId { get; set; }
            public List<string> questions { get; set; }
            public string type { get; set; }
            public string hostId { get; set; }
        }
        public class Version
        {
            public int version { get; set; }
            public string type { get; set; }
        }

        public class GameSummary
        {
            public SummaryData data { get; set; }
        }
        public class SummaryData
        {
            public List<SummaryQuiz> quizzes { get; set; }
        }
        public class SummaryQuiz
        {
            public string _id { get; set; }
            public SummaryInfo info { get; set; }
        }
        public class SummaryInfo
        {
            public string name { get; set; }
        }

        public class QuestionResponse
        {
            public Dictionary<string, Questions> questions { get; set; }
        }

        public class Answer
        {
            public string answer { get; set; }
            public string _id { get; set; }
        }

        //Json Data comes with HTML tags ex.{ "text": "<p>Elton John sang Crocodile Rock.</p>" }. This function gets rid of them
        public static string RemoveHtml(string htmlstring)
        {
            var tmp = HttpUtility.HtmlDecode(htmlstring);
            string clean = Regex.Replace(tmp, "<.*?>", String.Empty);
            return Regex.Replace(clean, "&nbsp;", String.Empty);
        }

        public class InfoData
        {
            public string name { get; set; }
            public List<string> subjects { get; set; }
            public List<string> grade { get; set; }
            public List<string> grades { get; set; }
        }

        public partial class SearchArgs
        {
            [JsonPropertyName("grade_type.aggs")]
            public List<object> GradeTypeAggs { get; set; }
            public List<string> occupation { get; set; }
            public List<bool> cloned { get; set; }
            [JsonPropertyName("subjects.aggs")]
            public List<string> SubjectsAggs { get; set; }
            [JsonPropertyName("lang.aggs")]
            public List<string> LangAggs { get; set; }
            public List<string> grade { get; set; }
            public List<string> isProfane { get; set; }
        }

        public class MultiQuizz
        {
            public MultiData data { get; set; }
        }
        public class MultiData
        {
            public List<Quiz> hits { get; set; }
        }

        public class QuestionsRange
        {
            public List<int> numberOfQuestions { get; set; }
        }

        public class GitHubRelease
        {
            public string tag_name { get; set; }
        }

        public class StudentGameInfo
        {
            public StudentGameData data { get; set; }
        }
        public class StudentGameData
        {
            public Dictionary<string, InfoData> quizzes { get; set; }
        }


        public class LoginLocal
        {
            public string username { get; set; }
        }
        public class LoginUser
        {
            public LoginLocal local { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
        }
        public class LoginData
        {
            public LoginUser user { get; set; }
        }
        public class LoginInfo
        {
            public bool success { get; set; }
            public LoginData data { get; set; }

            public CookieCollection cookies;

            public bool rememberCreds;

            public string username;
            public string password;
        }

        //Function below was meant to remove the "<>, []" brackets from string but it was useles after implementing the function above
        /*
        public static int MakeSureItsInt(string suspect)
        {
            if (int.TryParse((string)suspect, out _))
            {
                return int.Parse(suspect);
            }
            else
            {
                if (int.TryParse(suspect.Substring(1, suspect.Length - 2), out _))
                {
                    return int.Parse(suspect.Substring(1, suspect.Length - 2));
                }
                else
                {
                    return MakeSureItsInt(suspect.Substring(1, suspect.Length - 2));
                }
            }
        }
        */

        public static String CleanLatex(string Latex)
        {
            var LatexArray = Latex.ToList();
            String clean = "";
            //for (int i = 0; i < LatexArray.Count; i++)
            //{
            //    if (Latex[i] == '\\' && Latex[i + 1] == '\\')
            //    {
            //        clean = Latex.Remove(i, 2).Insert(i, "\\");
            //    }
            //    else if (Latex[i] == '\\' && Latex[i + 1] == ' ' && Latex[i + 2] == '\\')
            //    {
            //        clean = Latex.Remove(i, 3).Insert(i, "\\");
            //    }
            //    else if (Latex[i] == '\\' && Latex[i + 1] == '\\')
            //    {
            //        clean = Latex.Remove(i, 2).Insert(i, "v");
            //    }
            //}
            //clean = Regex.Replace(Latex, @"\\ \\", @"\");

            for (int i = 0; i < LatexArray.Count; i++)
            {
                if (Latex[i] == '\\' && Latex[i + 1] != '\\')
                {
                    Latex = Latex.Remove(i, 2).Insert(i, string.Format(@"\{0}", Latex[i + 1]));
                }
            }

            clean = Latex.Replace(@"\\", @"\").Replace(@"\ ", "%^&").Replace("%^& %^&", @" \ ").Replace("%^&", "").Replace(@"\ ", @"\,");
            return clean;
        }

        public static Utils.GitHubRelease CheckVersion(string username, string repoName)
        {
            try
            {
                const string GITHUB_API = "https://api.github.com/repos/{0}/{1}/releases/latest";
                WebClient webClient = new WebClient();
                // Added user agent
                webClient.Headers.Add("User-Agent", "Unity web player");
                Uri uri = new Uri(string.Format(GITHUB_API, username, repoName));
                string releases = webClient.DownloadString(uri);
                return JsonSerializer.Deserialize<Utils.GitHubRelease>(releases);
            }
            catch
            {
                return null;
            }
        }

        public static string GetLatextFromHtml(string html)
        {
            var i1 = html.IndexOf("latex=") + 7;
            var i2 = (html.IndexOf("katex>")) - 4;
            var temp =  html.Substring(i1, i2 - i1);

            return temp;
        }

        public static int GetQuestionsHash(List<Questions> questions)
        {
            var cleanString = "";
            questions.ForEach(hit => cleanString += hit._id);
            return cleanString.GetHashCode();
        }

        public static int GetQuestionsHash(List<string> questions)
        {
            var cleanString = "";
            questions.ForEach(hit => cleanString += hit);
            return cleanString.GetHashCode();
        }

        // Who knows?
        //#nullable enable
        //        public static async Task<string?> AuthorizeUser(string username, string pass)
        //        {
        //            var url = "https://quizizz.com/_api/main/auth/local";

        //            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
        //            httpRequest.Method = "POST";

        //            httpRequest.Headers["Host"] = "quizizz.com";
        //            httpRequest.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:96.0) Gecko/20100101 Firefox/96.0";
        //            httpRequest.Accept = "*/*";
        //            httpRequest.Headers["Accept-Language"] = "pl,en-US;q=0.7,en;q=0.3";
        //            httpRequest.Headers["Accept-Encoding"] = "gzip, deflate, br";
        //            httpRequest.Headers["Referer"] = "https://quizizz.com/login?q=%2F_api%2Fmain%2Fsearch%2Fv1%2Fprivate%3Ffrom%3D0%26size%3D10%26sortKey%3DcreatedAt%26filterList%3D%7B%2522createdBy%2522%3A%5B%25225fb63aa98a9db1001cfc7058%2522%5D%7D%26forProfile%3Dtrue%26_%3D1643660863179";
        //            httpRequest.ContentType = "application/json";
        //            httpRequest.Headers["x-csrf-token"] = "X0rkmPTO-uMS0Hd-AEFSoSSDELi-lSbC2Peg";
        //            httpRequest.Headers["X-Requested-With"] = "XMLHttpRequest";
        //            httpRequest.Headers["Content-Length"] = "83";
        //            httpRequest.Headers["Origin"] = "https://quizizz.com";
        //            httpRequest.Headers["DNT"] = "1";
        //            httpRequest.Headers["Connection"] = "keep-alive";
        //            httpRequest.Headers["Cookie"] = "_ga_N10L950FVL=GS1.1.1643893397.2.1.1643893676.0; quizizz_uid=68a361d6-7d50-4c17-b432-c5005a51b4a3; QUIZIZZ_EXP_SLOT=5; QUIZIZZ_EXP_NAME=curriculum_exp; QUIZIZZ_EXP_LEVEL=live; country=US; QUIZIZZ_SPONS_SLOT2=3; _ga=GA1.1.2003452377.1643882928; __zlcmid=18Mkg9C0YhoEqIG; __stripe_mid=e48fb8eb-ff7f-40ae-b5d1-1a6758e371edb390a7; moe_uuid=ad6a88d3-7aeb-46e1-a2ee-b16a7176659e; __stripe_sid=4f338f91-a3f6-49bd-845b-167775ad0b55422042; _csrf=g8Nyp54OFMj4iHlQI9rhGCFI; x-csrf-token=X0rkmPTO-uMS0Hd-AEFSoSSDELi-lSbC2Peg; locale=pl";
        //            httpRequest.Headers["Sec-Fetch-Dest"] = "empty";
        //            httpRequest.Headers["Sec-Fetch-Mode"] = "no-cors";
        //            httpRequest.Headers["Sec-Fetch-Site"] = "same-origin";
        //            httpRequest.Headers["TE"] = "trailers";
        //            httpRequest.Headers["Pragma"] = "no-cache";
        //            httpRequest.Headers["Cache-Control"] = "no-cache";
        //            httpRequest.Headers["Authorization"] = "Bearer mt0dgHmLJMVQhvjpNXDyA83vA_PxH23Y";
        //            httpRequest.AutomaticDecompression = DecompressionMethods.GZip;

        //            var data = @"{
        //              ""username"": ""gmaciejg@poczta.fm"",
        //              ""password"": ""Gambol12"",
        //              ""requestId"": """"
        //            }";
        //            try
        //            {
        //                httpRequest.Headers["Content-Length"] = data.Length.ToString();
        //                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
        //                {
        //                    streamWriter.Write(data);
        //                }

        //                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
        //                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        //                {
        //                    var result = streamReader.ReadToEnd();
        //                    return result;
        //                }

        //                Console.WriteLine(httpResponse.StatusCode);
        //            }
        //            catch (Exception ex)
        //            {
        //                return null;
        //            }

        //            return null;
        //        }
        //#nullable disable

        public static async Task<(string, string, CookieCollection)?> GetCsrfTokens()
        {
            var myClientHandler = new HttpClientHandler();
            myClientHandler.CookieContainer = new CookieContainer();

            (string, string, CookieCollection) tokens = ("", "", null);

            var client = new HttpClient(myClientHandler);
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Host", "quizizz.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:96.0) Gecko/20100101 Firefox/96.0");
            client.DefaultRequestHeaders.Add("Accept-Language", "pl,en-US;q=0.7,en;q=0.3");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("ContentType", "application/json");
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            var data = await client.GetAsync("https://quizizz.com/login");

            var cookieCollection = myClientHandler.CookieContainer.GetCookies(new Uri("https://quizizz.com/login"));

            tokens.Item3 = cookieCollection;

            foreach (var cookie in cookieCollection.Cast<Cookie>())
            {
                if (cookie.Name.Contains("x-csrf-token", StringComparison.CurrentCultureIgnoreCase))
                {
                    tokens.Item1 = cookie.Value;
                }
                else if (cookie.Name.Contains("_csrf", StringComparison.CurrentCultureIgnoreCase))
                {
                    tokens.Item2 = cookie.Value;
                }
            }
            return tokens;
        }

#nullable enable
        public static async Task<LoginInfo?> AuthorizeUserClient(string username, string pass)
        {
            var url = "https://quizizz.com/_api/main/auth/local";

            var clientCookies = new CookieContainer();
            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = clientCookies
            });

            var tokens = await GetCsrfTokens();
            if (tokens == null) return null;

            clientCookies.Add(tokens.Value.Item3);

            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Host", "quizizz.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:96.0) Gecko/20100101 Firefox/96.0");
            client.DefaultRequestHeaders.Add("Accept-Language", "pl,en-US;q=0.7,en;q=0.3");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            //client.DefaultRequestHeaders.Add("Referer", "https://quizizz.com");
            client.DefaultRequestHeaders.Add("ContentType", "application/json");
            client.DefaultRequestHeaders.Add("x-csrf-token", tokens.Value.Item1);
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add("Origin", "https://quizizz.com");
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            //client.DefaultRequestHeaders.Add("Cookie", $"_ga_N10L950FVL=GS1.1.1643893397.2.1.1643893676.0; QUIZIZZ_EXP_SLOT=5; QUIZIZZ_EXP_NAME=curriculum_exp; QUIZIZZ_EXP_LEVEL=live; country=US; QUIZIZZ_SPONS_SLOT2=3; _ga=GA1.1.2003452377.1643882928; __zlcmid=18Mkg9C0YhoEqIG; __stripe_mid=e48fb8eb-ff7f-40ae-b5d1-1a6758e371edb390a7; moe_uuid=ad6a88d3-7aeb-46e1-a2ee-b16a7176659e; __stripe_sid=4f338f91-a3f6-49bd-845b-167775ad0b55422042; _csrf={tokens.Value.Item2}; locale=pl");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("TE", "trailers");
            client.DefaultRequestHeaders.Add("Pragma", "no-cache");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer mt0dgHmLJMVQhvjpNXDyA83vA_PxH23Y");

            var data = new Dictionary<string, string>
            {
                {"username", username },
                {"password", pass },
                {"requestId", "" },
            };
            var content = new FormUrlEncodedContent(data);
            try
            {
                var test = await content.ReadAsStringAsync();
                var test1 = JsonSerializer.Serialize(data);
                //Clipboard.SetText(test1);
                //client.DefaultRequestHeaders.Add("Content-Length", test.Length.ToString());
                var authData = await client.PostAsync(url, content);
                if (authData.IsSuccessStatusCode)
                {
                    var returnData = JsonSerializer.Deserialize<LoginInfo>(await authData.Content.ReadAsStringAsync());
                    var currentCookies = clientCookies.GetCookies(new Uri(url));
                    returnData.cookies = currentCookies;
                    //var info = await client.GetStringAsync(HttpUtility.UrlDecode("https%3A%2F%2Fquizizz.com%2F_api%2Fmain%2Fsearch%2Fv1%2Fprivate%3Ffrom%3D0%26size%3D20%26sortKey%3DcreatedAt%26filterList%3D%7B%22createdBy%22%3A%5B%2261c2f4c6114864001d20d0d3%22%5D%2C%22subjects.aggs%22%3A%5B%22Mathematics%22%5D%7D%26forProfile%3Dtrue%26_%3D1643660863179")); Debug
                    return returnData;
                }
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
#nullable disable

        public static (string, string) GetCsrfTokens(CookieCollection cookieCollection)
        {
            (string, string) tokens = ("", "");
            foreach (var cookie in cookieCollection.Cast<Cookie>())
            {
                if (cookie.Name.Contains("x-csrf-token", StringComparison.CurrentCultureIgnoreCase))
                {
                    tokens.Item1 = cookie.Value;
                }
                else if (cookie.Name.Contains("_csrf", StringComparison.CurrentCultureIgnoreCase))
                {
                    tokens.Item2 = cookie.Value;
                }
            }
            return tokens;
        }
    }

    public static class StaticExtensionAwaitClick
    {
        public static Task WhenClicked(this Button btn)
        {
            var tcs = new TaskCompletionSource<object>();
            RoutedEventHandler lambda = null;

            lambda = (s, e) =>
            {
                btn.Click -= lambda;
                tcs.SetResult(null);
            };

            btn.Click += lambda;
            return tcs.Task;
        }
    }
}
