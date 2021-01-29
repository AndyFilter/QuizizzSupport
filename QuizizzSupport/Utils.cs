using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuizizzSupport
{
    class Utils
    {
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
        }
        public class Questions
        {
            public Structure structure { get; set; }
            public string type { get; set; }
        }
        public class Structure
        {
            public Settings setting { get; set; }
            public Query query { get; set; }
            public List<Options> options { get; set; }
            public dynamic answer { get; set; }
        }
        public class Settings
        {
            public string hasCorrectAnswer { get; set; }
        }
        public class Query
        {
            public string text { get; set; }
        }
        public class Options
        {
            public List<Answers> options { get; set; }
            public string text { get; set; }
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
        }

        //Json Data comes with HTML tags ex.{ "text": "<p>Elton John sang Crocodile Rock.</p>" }. This function gets rid of them
        public static string RemoveHtml(string htmlstring)
        {
            string clean = Regex.Replace(htmlstring, "<.*?>", String.Empty);
            return Regex.Replace(clean, "&nbsp;", String.Empty);
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
    }
}
