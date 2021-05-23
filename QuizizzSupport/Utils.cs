using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace QuizizzSupport
{
    class Utils
    {
        public static string version = "6.0";
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
            string clean = Regex.Replace(htmlstring, "<.*?>", String.Empty);
            return Regex.Replace(clean, "&nbsp;", String.Empty);
        }

        public class InfoData
        {
            public string name { get; set; }
            public List<string> subjects { get; set; }
            public List<string> grade { get; set; }
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

    }
}
