using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfMath;

//Before you read some of that code, make sure you are able to sustain major brain damage from useless and dumb code. Good Luck!

namespace QuizizzSupport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        private Utils.QuizData CurrentQuiz { get; set; }
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();

            versionLabel.Content = Utils.version;
            Label VerTip = new Label();
            VerTip.Padding = new Thickness(2, 2, 2, 2);
            VerTip.Foreground = Brushes.DarkSlateGray;
            var Version = Utils.CheckVersion("AndyFilter", "QuizizzSupport");
            if (Utils.version == Version.tag_name && Version != null)
            {
                VerTip.Content = "You are running the latest version of the program!";
                versionLabel.Foreground = Brushes.LimeGreen;
            }
            else
            {
                VerTip.Content = "You need to update the program to have access to the latest features";
                versionLabel.Foreground = Brushes.Red;
                versionLabel.Cursor = System.Windows.Input.Cursors.Hand;
                versionLabel.MouseLeftButtonDown += VersionLabel_MouseLeftButtonDown;
            }
            ToolTipService.SetToolTip(versionLabel, VerTip);
        }

        private void VersionLabel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/AndyFilter/QuizizzSupport/releases/latest") { UseShellExecute = true });
        }

        private Utils.GitHubRelease CheckVersion(string username, string repoName)
        {
            const string GITHUB_API = "https://api.github.com/repos/{0}/{1}/releases/latest";
            WebClient webClient = new WebClient();
            // Added user agent
            webClient.Headers.Add("User-Agent", "Unity web player");
            Uri uri = new Uri(string.Format(GITHUB_API, username, repoName));
            string releases = webClient.DownloadString(uri);
            return JsonSerializer.Deserialize<Utils.GitHubRelease>(releases);
        }

        private async void GetAnswersClicked(object sender, RoutedEventArgs e)
        {
            GetAsnwersBtn.IsEnabled = false;

            string code = QuizIdBox.Text;

            if (int.TryParse((string)QuizIdBox.Text, out _))
            {
                try
                {
                    CurrentQuiz = await GetQuizInfoInfo(code);
                    setQuestionData(CurrentQuiz.data.questions);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    PremiumScan(code);
                }
                catch
                {
                    PremiumScan(code);
                }

                //setQuestionData(CurrentQuiz.data.questions);
            }
            else if (QuizIdBox.Text.Contains("/join?gc="))
            {
                string url = QuizIdBox.Text;

                if (url.Contains("&"))
                {
                    int start = url.IndexOf("/join?gc=") + 9;
                    int end = url.IndexOf("&");
                    code = url.Substring(start, end - start);
                }
                else
                {
                    int found = url.IndexOf("/join?gc=") + 9;
                    code = url.Substring(found, url.Length - found);
                }
                try
                {
                    CurrentQuiz = await GetQuizInfoInfo(code);
                    setQuestionData(CurrentQuiz.data.questions);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    PremiumScan(code);
                }
                catch (System.ArgumentException)
                {
                    PremiumScan(code);
                }

                //setQuestionData(CurrentQuiz.data.questions);
            }
            else if (QuizIdBox.Text.Contains("quiz/") && !QuizIdBox.Text.Contains("admin") && QuizIdBox.Text.Length >= 28)
            {
                string url = QuizIdBox.Text;

                //finds the index of "quiz/" to then get the next 24 symbols which are used to request data from the quizizz API. They are like a Quiz ID
                int found = url.IndexOf("quiz/");

                CurrentQuiz = await getQuizInfo("https://quizizz.com/api/main/" + url.Substring(found, 29) + "?bypassProfanity=true&returnPrivileges=true&source=join");

                try
                {
                    if (CurrentQuiz.data != null)
                        setQuestionData(CurrentQuiz.data.quiz.info.questions);
                    else
                    {
                        MessageBox.Show(this, "Not a valid quizizz url adress", "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                        GetAsnwersBtn.IsEnabled = true;
                        return;
                    }

                }
                catch (System.NullReferenceException)
                {
                    MessageBox.Show(this, "Not a valid quizizz url adress", "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                    GetAsnwersBtn.IsEnabled = true;
                    return;
                }
            }
            else if (QuizIdBox.Text.Contains("?gameType=async"))
            {
                //I dont think quizizz puts "?gameType=async" in the link anymore so...
            }
            else
            {
                MessageBox.Show(this, "Not a valid url. PLEASE USE THE CODE", "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                GetAsnwersBtn.IsEnabled = true;
            }
        }

        private async void PremiumScan(string code)
        {
            var VerRes = await FromVer(code);
            if (!VerRes.Success)
            {
                var SearchRes = await searchFor(code);
                if (!SearchRes.Success)
                {
                    var Reco1res = await FindRecommended1(code);
                    if (!Reco1res.Success)
                    {
                        var RecoRes = await FindRecommended(code);
                        if (!RecoRes.Success)
                        {
                            userResponse.Foreground = Brushes.PaleVioletRed;
                            userResponse.Content = "Quiz not found";
                            GetAsnwersBtn.IsEnabled = true;
                        }
                        else
                            setQuestionData(RecoRes.Quiz.info.questions);
                    }
                    else
                        setQuestionData(Reco1res.Quiz.info.questions);
                }
                else
                    setQuestionData(SearchRes.Quiz.info.questions);
            }
            else
                setQuestionData(VerRes.Quiz.info.questions);
        }

        private async Task<Structs.GetQuizResp> FromVer(string code)
        {
            //So... When you create a quizz a VersionId is generared alongside the QuizzId the same goes to the UpdateVersion which is updated when the quizz is updated.
            //Now using this knowledge we can get the quizz id just from the VersionId id the Quizz was not updated (around 50-70% of all quizzes have not been updated EVER).
            //Also there is a small delay between creating a VersionId and a QuizzId thats why we have to subtract one from VersionId. This method works for both private and public quizzes.
            userResponse.Foreground = Brushes.WhiteSmoke;
            userResponse.Content = "Please Wait...";

            List<string> quizTag = new List<string>();

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code }
                };

            try
            {
                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync("https://game.quizizz.com/play-api/v4/checkRoom", content);

                var codeString = await response.Content.ReadAsStringAsync();

                var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

                string quizCode = "";
                if (roomObject.room != null)
                {
                    try
                    {
                        quizCode = (int.Parse(roomObject.room.versionId.Substring(roomObject.room.versionId.Length - 2), System.Globalization.NumberStyles.HexNumber) - 1).ToString("X").ToLower();
                    }
                    catch
                    {
                        return new Structs.GetQuizResp() { Success = false };
                    }
                }
                else
                    return new Structs.GetQuizResp() { Success = false };

                string quizId = roomObject.room.versionId[0..^2] + quizCode;
                //Clipboard.SetText(quizId);

                var currentQuiz = await client.GetStringAsync("https://quizizz.com/api/main/quiz/" + quizId + "?bypassProfanity=true&returnPrivileges=true&source=join");

                var currentQuizObject = JsonSerializer.Deserialize<Utils.QuizData>(currentQuiz);

                //setQuestionData(currentQuizObject.data.quiz.info.questions);
                return new Structs.GetQuizResp() { Success = true, Quiz = currentQuizObject.data.quiz };
            }
            catch
            {
                return new Structs.GetQuizResp() { Success = false };
            }
        }

        private async Task<Structs.GetQuizResp> FindRecommended(string code)
        {
            userResponse.Foreground = Brushes.WhiteSmoke;
            userResponse.Content = "Please Wait...";
            List<string> ScannedQuizes = new List<string>();

            //bool wasFound = false;

            List<string> quizTag = new List<string>();

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code }
                };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v4/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

            if (roomObject.room != null)
            {
                quizTag = roomObject.room.questions;
            }
            else
            {
                //MessageBox.Show(this, "Not a valid quizizz code", "Expired Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                userResponse.Foreground = Brushes.PaleVioletRed;
                userResponse.Content = "Quiz not found";
                return new Structs.GetQuizResp() { Success = false };
            }

            var quizLongId = roomObject.room.quizId;

            string quizString = await client.GetStringAsync("http://quizizz.com/api/main/gameSummaryRec?quizId=" + quizLongId);

            var SummaryObject = JsonSerializer.Deserialize<Utils.GameSummary>(quizString);

            foreach (Utils.SummaryQuiz RecomendedQuiz in SummaryObject.data.quizzes)
            {
                if (!ScannedQuizes.Contains(RecomendedQuiz._id))
                {
                    Debug.Content = RecomendedQuiz.info.name;

                    string RecomendedQuizString;
                    try
                    {
                        RecomendedQuizString = await client.GetStringAsync("https://quizizz.com/api/main/gameSummaryRec?quizId=" + RecomendedQuiz._id);
                    }
                    catch
                    {
                        continue;
                    }

                    var RecomendedQuizObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString);

                    ScannedQuizes.Add(RecomendedQuiz._id);

                    foreach (Utils.SummaryQuiz quiz in RecomendedQuizObject.data.quizzes)
                    {
                        Debug.Content = quiz.info.name;

                        string RecomendedQuizString2;
                        try
                        {
                            RecomendedQuizString2 = await client.GetStringAsync("https://quizizz.com/api/main/gameSummaryRec?quizId=" + quiz._id);
                        }
                        catch
                        {
                            continue;
                        }

                        var RecomendedQuizObject2 = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString2);

                        foreach (Utils.SummaryQuiz quiz2 in RecomendedQuizObject2.data.quizzes)
                        {

                            if (!ScannedQuizes.Contains(quiz2._id))
                            {
                                Debug.Content = quiz2.info.name;
                                string currentQuiz;

                                try
                                {
                                    currentQuiz = await client.GetStringAsync("https://quizizz.com/api/main/quiz/" + quiz2._id + "?bypassProfanity=true&returnPrivileges=true&source=join");
                                }
                                catch
                                {
                                    continue;
                                }

                                var currentQuizObject = JsonSerializer.Deserialize<Utils.QuizData>(currentQuiz);

                                ScannedQuizes.Add(quiz2._id);

                                //Debug.Content = ScannedQuizes.Count.ToString();

                                if (quizTag[0] == currentQuizObject.data.quiz.info.questions[0]._id && quizTag[1] == currentQuizObject.data.quiz.info.questions[1]._id)
                                {
                                    //Clipboard.SetText(quiz2._id);
                                    //setQuestionData(currentQuizObject.data.quiz.info.questions);
                                    //continue;
                                    return new Structs.GetQuizResp() { Success = true, Quiz = currentQuizObject.data.quiz };
                                }
                            }
                        }
                    }
                }
            }
            userResponse.Foreground = Brushes.PaleVioletRed;
            userResponse.Content = "Quiz not found";
            return new Structs.GetQuizResp() { Success = false };
        }

        private async Task<Structs.GetQuizResp> FindRecommended1(string code)
        {
            userResponse.Foreground = Brushes.WhiteSmoke;
            userResponse.Content = "Please Wait...";
            List<string> ScannedQuizes = new List<string>();

            //bool wasFound = false;

            List<string> quizTag = new List<string>();

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code }
                };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v4/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

            if (roomObject.room != null)
            {
                quizTag = roomObject.room.questions;
            }
            else
            {
                //MessageBox.Show(this, "Not a valid quizizz code", "Expired Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                userResponse.Foreground = Brushes.PaleVioletRed;
                userResponse.Content = "Quiz not found";
                return new Structs.GetQuizResp() { Success = false };
            }

            var quizLongId = roomObject.room.quizId;

            string quizString = await client.GetStringAsync("https://quizizz.com/api/main/getSimilarQuizzes?quizId=" + quizLongId + "&page=1&pageSize=20");

            var SummaryObject = JsonSerializer.Deserialize<Utils.GameSummary>(quizString);

            foreach (Utils.SummaryQuiz RecomendedQuiz in SummaryObject.data.quizzes)
            {
                if (!ScannedQuizes.Contains(RecomendedQuiz._id))
                {
                    Debug.Content = RecomendedQuiz.info.name;

                    string RecomendedQuizString;
                    try
                    {
                        RecomendedQuizString = await client.GetStringAsync("https://quizizz.com/api/main/getSimilarQuizzes?quizId=" + RecomendedQuiz._id + "&page=1&pageSize=20");
                    }
                    catch
                    {
                        continue;
                    }

                    var RecomendedQuizObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString);

                    ScannedQuizes.Add(RecomendedQuiz._id);

                    foreach (Utils.SummaryQuiz quiz in RecomendedQuizObject.data.quizzes)
                    {
                        Debug.Content = quiz.info.name;

                        string RecomendedQuizString2;
                        try
                        {
                            RecomendedQuizString2 = await client.GetStringAsync("https://quizizz.com/api/main/getSimilarQuizzes?quizId=" + quiz._id + "&page=1&pageSize=20");
                        }
                        catch
                        {
                            continue;
                        }

                        var RecomendedQuizObject2 = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString2);

                        foreach (Utils.SummaryQuiz quiz2 in RecomendedQuizObject2.data.quizzes)
                        {

                            if (!ScannedQuizes.Contains(quiz2._id))
                            {
                                Debug.Content = quiz2.info.name;
                                string currentQuiz;

                                try
                                {
                                    currentQuiz = await client.GetStringAsync("https://quizizz.com/api/main/quiz/" + quiz2._id + "?bypassProfanity=true&returnPrivileges=true&source=join");
                                }
                                catch
                                {
                                    continue;
                                }

                                var currentQuizObject = JsonSerializer.Deserialize<Utils.QuizData>(currentQuiz);

                                ScannedQuizes.Add(quiz2._id);

                                //Debug.Content = ScannedQuizes.Count.ToString();

                                if (quizTag[0] == currentQuizObject.data.quiz.info.questions[0]._id && quizTag[1] == currentQuizObject.data.quiz.info.questions[1]._id)
                                {
                                    //Clipboard.SetText(quiz2._id);
                                    //setQuestionData(currentQuizObject.data.quiz.info.questions);
                                    //continue;
                                    return new Structs.GetQuizResp() { Success = true, Quiz = currentQuizObject.data.quiz };
                                }
                            }
                        }
                    }
                }
            }
            userResponse.Foreground = Brushes.PaleVioletRed;
            userResponse.Content = "Quiz not found";
            return new Structs.GetQuizResp() { Success = false };
        }

        private async Task<Structs.GetQuizResp> searchFor(string code)
        {
            userResponse.Foreground = Brushes.WhiteSmoke;
            userResponse.Content = "Please Wait...";

            //bool wasFound = false;

            string quizTag = "";
            int numberOfQuestions;

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code }
                };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v4/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

            if (roomObject.room != null)
            {
                quizTag = roomObject.room.questions[1];
                numberOfQuestions = roomObject.room.questions.Count;
            }
            else
            {
                //MessageBox.Show(this, "Not a valid quizizz code", "Expired Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                userResponse.Foreground = Brushes.PaleVioletRed;
                userResponse.Content = "Quiz not found";
                return new Structs.GetQuizResp() { Success = false };
            }

            var quizLongId = roomObject.room.quizId;

            string quizString = await client.GetStringAsync("https://quizizz.com/api/main/quiz/" + quizLongId + "/info");

            var SummaryObject = JsonSerializer.Deserialize<Utils.InfoData>(quizString);

            var searchArgs = new Utils.SearchArgs();
            var RangeList = new Utils.QuestionsRange();

            searchArgs.occupation = new List<string>() { "teacher", "teacher_school", "teacher_university", "other", "student" };
            searchArgs.SubjectsAggs = SummaryObject.subjects;
            searchArgs.isProfane = new List<string>() { "false", "true" };
            searchArgs.grade = SummaryObject.grade;
            //searchArgs.LangAggs = new List<string> { "English" };

            RangeList.numberOfQuestions = new List<int>() { numberOfQuestions, numberOfQuestions };

            Regex rgx = new Regex(@"[^\p{L}\p{N}]+");

            string searchName = HttpUtility.UrlEncode(SummaryObject.name = rgx.Replace(SummaryObject.name, " "));

            //SEARCH    CAN BE DONE BETTER WHEN USING LEVEVLS (EX. [1,10], [11,20])

            string searchResponse = await client.GetStringAsync("https://quizizz.com/api/main/search?from=0&sortKey=_score&filterList=" + JsonSerializer.Serialize<Utils.SearchArgs>(searchArgs) + "&source=MainHeader&page=SearchPage&size=20&query=" + searchName + "&_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

            var searchObject = JsonSerializer.Deserialize<Utils.MultiQuizz>(searchResponse);

            foreach (Utils.Quiz quiz in searchObject.data.hits)
            {
                if (quiz.info.questions[1]._id == quizTag)
                {
                    //Clipboard.SetText(JsonSerializer.Serialize<Utils.Quiz>(quiz));
                    //setQuestionData(quiz.info.questions);
                    return new Structs.GetQuizResp() { Success = true, Quiz = quiz };
                }
            }
            var RecommendedQuizzesString = await client.GetStringAsync("https://quizizz.com/api/main/getSimilarQuizzes?quizId=" + quizLongId + "&page=1&pageSize=20");
            var RecommendedQuizzesObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecommendedQuizzesString);

            var RecommendedQuizString = await client.GetStringAsync("https://quizizz.com/api/main/quiz/" + RecommendedQuizzesObject.data.quizzes[0]._id + "?bypassProfanity=true&returnPrivileges=true&source=join");
            var RecommendedQuiz = JsonSerializer.Deserialize<Utils.QuizData>(RecommendedQuizString);

            searchArgs.LangAggs = new List<string> { RecommendedQuiz.data.quiz.info.lang };

            //searchResponse = await client.GetStringAsync("https://quizizz.com/api/main/search?from=0&sortKey=_score&filterList=" + JsonSerializer.Serialize<Utils.SearchArgs>(searchArgs) + "&rangeList=" + JsonSerializer.Serialize<Utils.QuestionsRange>(RangeList) + "&source=MainHeader&page=SearchPage&query=" + searchName + "&size=20" + "&_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());
            searchResponse = await client.GetStringAsync("https://quizizz.com/api/main/search?from=0&sortKey=_score&filterList=" + JsonSerializer.Serialize<Utils.SearchArgs>(searchArgs) + "&source=MainHeader&page=SearchPage&size=20&query=" + searchName + "&_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

            searchObject = JsonSerializer.Deserialize<Utils.MultiQuizz>(searchResponse);

            foreach (Utils.Quiz quiz in searchObject.data.hits)
            {
                if (quiz.info.questions[0]._id == quizTag)
                {
                    //Clipboard.SetText(JsonSerializer.Serialize<Utils.Quiz>(quiz));
                    //setQuestionData(quiz.info.questions);

                    return new Structs.GetQuizResp() { Success = true, Quiz = quiz };
                }
            }
            return new Structs.GetQuizResp() { Success = false };
        }

        private async System.Threading.Tasks.Task<Utils.QuizData> getQuizInfo(string url)
        {
            string quizinfo;
            try
            {
                quizinfo = await client.GetStringAsync(url);
            }
            catch (HttpRequestException)
            {
                //MessageBox.Show(this, "Error", "An Error Has Occured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new Utils.QuizData();
            }
            catch (ArgumentNullException)
            {
                //MessageBox.Show(this, "Error", "An Error Has Occured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new Utils.QuizData();
            }
            catch (System.ArgumentOutOfRangeException)
            {
                //MessageBox.Show(this, string.Format("Not a valid quizizz url adress \n\nError"), "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new Utils.QuizData();
            }
            Utils.QuizData quizData = JsonSerializer.Deserialize<Utils.QuizData>(quizinfo);
            return quizData;
        }


        private async System.Threading.Tasks.Task<Utils.QuizData> GetQuizInfoInfo(string code)
        {
            var values = new Dictionary<string, string>
                {
                    { "roomCode", code.ToString() }
                };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v4/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            //Clipboard.SetText(codeString);

            string quizString = "";

            if (JsonSerializer.Deserialize<Utils.RoomData>(codeString).room != null)
            {
                quizString = await client.GetStringAsync("https://quizizz.com/api/main/game/" + JsonSerializer.Deserialize<Utils.RoomData>(codeString).room.hash);
            }
            //else
            //throw new ArgumentException("Parameter cannot be null", nameof(getQuizInfo));

            Utils.QuizData quizData = JsonSerializer.Deserialize<Utils.QuizData>(quizString);

            return quizData;
        }


        private async void setQuestionData(List<Utils.Questions> questions)
        {
            userResponse.Foreground = Brushes.LimeGreen;
            userResponse.Content = "Quiz Found!";
            GetAsnwersBtn.IsEnabled = true;

            //Could not be bothered to change the name from StackPanel to something useful
            StackPanel.Children.Clear();

            foreach (Utils.Questions question in questions)
            {
                if (!question.structure.settings.hasCorrectAnswer)
                    continue;
                Label Question = new Label();
                Label Answer = new Label();
                StackPanel QuestData = new StackPanel();
                QuestData.Orientation = Orientation.Horizontal;
                QuestData.Children.Add(Question);
                QuestData.Children.Add(Answer);
                Answer.Foreground = (Brush)FindResource("AnswerColor");
                string answer = "";
                JsonElement p = new JsonElement();
                var parser = new TexFormulaParser();
                switch (question.type)
                {
                    //case "OPEN":
                    case "SLIDE":
                        continue;
                    case "quiz":
                    case "BLANK":
                        p = question.structure.answer;
                        answer += Utils.RemoveHtml(question.structure.options[0].text);
                        if (question.structure.options[0].media.Count > 0)
                        {
                            Image td = new Image();
                            td.MaxHeight = 400;
                            td.Source = new BitmapImage(new Uri(question.structure.options[0].media[0].url));
                            ToolTipService.SetToolTip(Answer, td);
                            if (answer.Length < 1)
                                answer = "(Image)";
                        }
                        else if (question.structure.options[0].hasMath)
                        {
                            foreach (string latex in question.structure.options[0].math.latex)
                            {
                                try
                                {
                                    var formula = parser.Parse(latex);
                                    var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                    Label image = new Label();
                                    Image td = new Image();
                                    image.Foreground = (Brush)FindResource("AnswerColor");
                                    td.MaxHeight = 400;
                                    td.MaxWidth = 400;
                                    td.Source = LoadImage(pngBytes);
                                    ToolTipService.SetToolTip(image, td);
                                    QuestData.Children.Add(image);
                                    image.Content = "(Math)";
                                    if (question.structure.options[0].math.latex.Count - question.structure.options[0].math.latex.IndexOf(latex) > 1)
                                    {
                                        image.Content += "  &  ";
                                    }
                                }
                                catch
                                {
                                    try
                                    {
                                        var formula = parser.Parse(Utils.CleanLatex(latex));
                                        var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                        Label image = new Label();
                                        Image td = new Image();
                                        image.Foreground = (Brush)FindResource("AnswerColor");
                                        td.MaxHeight = 400;
                                        td.MaxWidth = 400;
                                        td.Source = LoadImage(pngBytes);
                                        ToolTipService.SetToolTip(image, td);
                                        QuestData.Children.Add(image);
                                        image.Content = "(Math)";
                                        if (question.structure.options[0].math.latex.Count - question.structure.options[0].math.latex.IndexOf(latex) > 1)
                                        {
                                            image.Content += "  &  ";
                                        }
                                    }
                                    catch
                                    {
                                        Label label = new Label();
                                        label.Foreground = (Brush)FindResource("AnswerColor");
                                        label.Content = latex;
                                        QuestData.Children.Add(label);
                                        label.Content = "(Math)";
                                        if (question.structure.options[0].math.latex.Count - question.structure.options[0].math.latex.IndexOf(latex) > 1)
                                        {
                                            label.Content += "  &  ";
                                        }
                                    }
                                }
                            }
                            if (question.structure.options[0].math.latex.Count <= 0)
                            {
                                answer += Utils.RemoveHtml(question.structure.options[0].text);
                            }
                        }
                        break;
                    case "MSQ":
                    case "MCQ":
                    default:
                        p = question.structure.answer;
                        List<int> listansw = Regex.Matches(p.GetRawText(), "(-?[0-9]+)").OfType<Match>().Select(m => int.Parse(m.Value)).ToList();
                        var strings = listansw.Select(i => i.ToString(CultureInfo.InvariantCulture)).Aggregate((s1, s2) => s1 + ", " + s2);
                        if (listansw.Count <= 0)
                        {
                            answer += Utils.RemoveHtml(question.structure.options[0].text);
                            if (question.structure.options[0].media.Count > 0)
                            {
                                Image td = new Image();
                                td.MaxHeight = 400;
                                td.MaxWidth = 400;
                                td.Source = new BitmapImage(new Uri(question.structure.options[0].media[0].url));
                                ToolTipService.SetToolTip(Answer, td);
                                if (answer.Length < 1)
                                    answer = "(Image)";
                            }
                            else if (question.structure.options[0].hasMath)
                            {
                                foreach (string latex in question.structure.options[0].math.latex)
                                {
                                    try
                                    {
                                        var formula = parser.Parse(latex);
                                        var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                        Label image = new Label();
                                        image.Foreground = (Brush)FindResource("AnswerColor");
                                        Image td = new Image();
                                        td.MaxHeight = 400;
                                        td.MaxWidth = 400;
                                        td.Source = LoadImage(pngBytes);
                                        ToolTipService.SetToolTip(image, td);
                                        QuestData.Children.Add(image);
                                        image.Content = "(Math)";
                                        if (question.structure.options[0].math.latex.Count - question.structure.options[0].math.latex.IndexOf(latex) > 1)
                                        {
                                            image.Content += "  &  ";
                                        }
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            var formula = parser.Parse(Utils.CleanLatex(latex));
                                            var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                            Label image = new Label();
                                            Image td = new Image();
                                            image.Foreground = (Brush)FindResource("AnswerColor");
                                            td.MaxHeight = 400;
                                            td.MaxWidth = 400;
                                            td.Source = LoadImage(pngBytes);
                                            ToolTipService.SetToolTip(image, td);
                                            QuestData.Children.Add(image);
                                            image.Content = "(Math)";
                                            if (question.structure.options[0].math.latex.Count - question.structure.options[0].math.latex.IndexOf(latex) > 1)
                                            {
                                                image.Content += "  &  ";
                                            }
                                        }
                                        catch
                                        {
                                            Label label = new Label();
                                            label.Foreground = (Brush)FindResource("AnswerColor");
                                            label.Content = latex;
                                            QuestData.Children.Add(label);
                                            label.Content = "(Math)";
                                            if (question.structure.options[0].math.latex.Count - question.structure.options[0].math.latex.IndexOf(latex) > 1)
                                            {
                                                label.Content += "  &  ";
                                            }
                                        }
                                    }
                                    if (question.structure.options[0].math.latex.Count <= 0)
                                    {
                                        answer += Utils.RemoveHtml(question.structure.options[0].text);
                                    }
                                }
                            }
                        }
                        foreach (int i in listansw)
                        {
                            if (question.structure.options[i].media.Count > 0)
                            {
                                foreach (Utils.Media media in question.structure.options[i].media)
                                {
                                    Label image = new Label();
                                    Image td = new Image();
                                    image.Foreground = (Brush)FindResource("AnswerColor");
                                    td.MaxHeight = 400;
                                    td.MaxWidth = 400;
                                    td.Source = new BitmapImage(new Uri(media.url));
                                    ToolTipService.SetToolTip(image, td);
                                    QuestData.Children.Add(image);
                                    image.Content = "(Image)";
                                    //image.Padding = new Thickness(0, 5, 0, 5);
                                    if (listansw.Count - listansw.IndexOf(i) > 1)
                                    {
                                        image.Content += "  &  ";
                                    }
                                }
                            }
                            else if (question.structure.options[i].hasMath)
                            {
                                foreach (string latex in question.structure.options[i].math.latex)
                                {
                                    try
                                    {
                                        var formula = parser.Parse(latex);
                                        var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                        Label image = new Label();
                                        Image td = new Image();
                                        //Border brd = new Border();
                                        //brd.CornerRadius = new CornerRadius(5, 5, 5, 5);
                                        //brd.Padding = new Thickness(5, 5, 5, 5);
                                        //brd.Child = td;
                                        image.Foreground = (Brush)FindResource("AnswerColor");
                                        td.MaxHeight = 400;
                                        td.MaxWidth = 400;
                                        td.Source = LoadImage(pngBytes);
                                        ToolTipService.SetToolTip(image, td);
                                        QuestData.Children.Add(image);
                                        image.Content = "(Math)";
                                        if (listansw.Count - listansw.IndexOf(i) > 1)
                                        {
                                            image.Content += "  &  ";
                                        }
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            var clean = Utils.CleanLatex(latex);
                                            var formula = parser.Parse(Utils.CleanLatex(latex));
                                            var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                            Label image = new Label();
                                            Image td = new Image();
                                            image.Foreground = (Brush)FindResource("AnswerColor");
                                            td.MaxHeight = 400;
                                            td.MaxWidth = 400;
                                            td.Source = LoadImage(pngBytes);
                                            ToolTipService.SetToolTip(image, td);
                                            QuestData.Children.Add(image);
                                            image.Content = "(Math)";
                                            if (listansw.Count - listansw.IndexOf(i) > 1)
                                            {
                                                image.Content += "  &  ";
                                            }
                                        }
                                        catch
                                        {
                                            Label label = new Label();
                                            label.Foreground = (Brush)FindResource("AnswerColor");
                                            label.Content = latex;
                                        }
                                    }
                                }
                                if (question.structure.options[i].math.latex.Count <= 0)
                                {
                                    answer += Utils.RemoveHtml(question.structure.options[i].text);
                                }
                            }
                            else
                            {
                                if (i >= 0)
                                    answer += Utils.RemoveHtml(question.structure.options[i].text);

                                //checks if there is more than One correct question and if so and the question is not the last one adds " & " at the end
                                if (listansw.Count - listansw.IndexOf(i) > 1)
                                {
                                    answer += " & ";
                                }
                            }
                        }
                        break;
                }
                var _question = Utils.RemoveHtml(question.structure.query.text);

                if (question.structure.query.media.Count > 0)
                {
                    Image td = new Image();
                    td.MaxHeight = 400;
                    td.MaxWidth = 400;
                    Question.Foreground = (Brush)FindResource("SpecialQuestion");
                    td.Source = new BitmapImage(new Uri(question.structure.query.media[0].url));
                    ToolTipService.SetToolTip(Question, td);
                    if (_question.Length < 2)
                        _question = "(Image)";
                }
                else if (question.structure.query.hasMath)
                {
                    //in case of multiple math equations in one question what yo should do is Loop for each latex split by {0} / {1}... and add a new label to the question stack panel with the correct Math image, But I couldn't be bothered, no one saw that. So idgaf tbh.
                    foreach (string latex in question.structure.query.math.latex)
                    {
                        try
                        {
                            var formula = parser.Parse(latex);
                            var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                            var QuestPart = new Label();
                            Image td = new Image();
                            Question.Foreground = (Brush)FindResource("SpecialQuestion");
                            td.MaxHeight = 400;
                            td.MaxWidth = 400;
                            td.Source = LoadImage(pngBytes);
                            ToolTipService.SetToolTip(Question, td);
                            if (question.structure.query.math.latex.Count - question.structure.query.math.latex.IndexOf(latex) > 1)
                            {
                                QuestPart.Content += " & ";
                            }
                        }
                        catch
                        {
                            try
                            {
                                var formula = parser.Parse(Utils.CleanLatex(latex));
                                var pngBytes = formula.RenderToPng(200.0, 0.0, 0.0, "Arial");
                                Image td = new Image();
                                Question.Foreground = (Brush)FindResource("SpecialQuestion");
                                td.MaxHeight = 400;
                                td.MaxWidth = 400;
                                td.Source = LoadImage(pngBytes);
                                ToolTipService.SetToolTip(Question, td);
                                continue;
                            }
                            catch
                            {
                                Question.Content = latex;
                            }
                        }
                    }
                    var cleanQuestion = Utils.RemoveHtml(question.structure.query.math.template);
                    //_question = cleanQuestion.Substring(0, cleanQuestion.Length - 4);
                    try
                    {
                        if (cleanQuestion.Contains("{$0}"))
                        {
                            _question = cleanQuestion.Replace(" {$0} ", " (Math) ");
                            _question = cleanQuestion.Replace(" {$0}", " (Math) ");
                            _question = cleanQuestion.Replace("{$0} ", " (Math) ");
                            _question = cleanQuestion.Replace("{$0}", " (Math) ");
                        }
                        //if (_question.Contains("{$"))
                        //    _question = _question.Remove(_question.IndexOf("{$"), "{$".Length + 2);
                    }
                    catch
                    {

                    }
                    if (_question.Length < 2)
                    {
                        _question = "(Math)";
                    }
                }

                Question.Content = _question + "   -  ";
                Answer.Content = answer;
                Answer.HorizontalAlignment = HorizontalAlignment.Left;
                Question.HorizontalAlignment = HorizontalAlignment.Left;

                StackPanel.Children.Add(QuestData);
            }
        }
        //The Latex Math *Works*...

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private async void DebugButtonClicked(object sender, RoutedEventArgs e)
        {
            string code = "";

            if (int.TryParse((string)QuizIdBox.Text, out _) && QuizIdBox.Text.Length == 6)
            {
                code = QuizIdBox.Text;

                try
                {
                    CurrentQuiz = await GetQuizInfoInfo(code);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    goto BruteForce;
                }
                catch (System.NullReferenceException)
                {
                    goto BruteForce;
                }

                setQuestionData(CurrentQuiz.data.questions);
            }
            else if (QuizIdBox.Text.Contains("/join?gc="))
            {
                string url = QuizIdBox.Text;

                int found = url.IndexOf("/join?gc=");

                code = url.Substring(found + 9, 6);

                try
                {
                    CurrentQuiz = await GetQuizInfoInfo(code);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    goto BruteForce;
                }
                catch (System.NullReferenceException)
                {
                    goto BruteForce;
                }

                setQuestionData(CurrentQuiz.data.questions);
            }
            else if (QuizIdBox.Text.Contains("quiz/"))
            {
                string url = QuizIdBox.Text;

                //finds the index of "quiz/" to then get the next 24 symbols which are used to request data from the quizizz API. They are like a Quiz ID
                int found = url.IndexOf("quiz/");

                CurrentQuiz = await getQuizInfo("https://quizizz.com/api/main/quiz/" + url.Substring(found, 29));

                try
                {
                    setQuestionData(CurrentQuiz.data.quiz.info.questions);
                }
                catch (System.NullReferenceException)
                {
                    MessageBox.Show(this, "Not a valid quizizz url adress nor ID", "Wrong Url Adress/ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show(this, "Not a valid quizizz url adress nor ID", "Wrong Url Adress/ID", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        BruteForce:
            List<string> ScannedQuizes = new List<string>();

            string quizTag;

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code.ToString() }
                };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v4/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

            quizTag = roomObject.room.questions[0];

            var quizLongId = roomObject.room.quizId;

            string quizString = await client.GetStringAsync("http://quizizz.com/api/main/gameSummaryRec?quizId=" + quizLongId);

            var SummaryObject = JsonSerializer.Deserialize<Utils.GameSummary>(quizString);

            foreach (Utils.SummaryQuiz RecomendedQuiz in SummaryObject.data.quizzes)
            {
                if (!ScannedQuizes.Contains(RecomendedQuiz._id))
                {
                    //Debug.Content = RecomendedQuiz.info.name;

                    var RecomendedQuizString = await client.GetStringAsync("https://quizizz.com/api/main/gameSummaryRec?quizId=" + RecomendedQuiz._id);

                    var RecomendedQuizObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString);

                    ScannedQuizes.Add(RecomendedQuiz._id);

                    foreach (Utils.SummaryQuiz quiz in RecomendedQuizObject.data.quizzes)
                    {
                        if (!ScannedQuizes.Contains(quiz._id))
                        {
                            //Debug.Content = quiz.info.name;

                            var currentQuiz = await client.GetStringAsync("https://quizizz.com/api/main/quiz/" + quiz._id + "?bypassProfanity=true&returnPrivileges=true&source=join");

                            var currentQuizObject = JsonSerializer.Deserialize<Utils.QuizData>(currentQuiz);

                            ScannedQuizes.Add(quiz._id);

                            if (quizTag == currentQuizObject.data.quiz.info.questions[0]._id)
                            {
                                setQuestionData(currentQuizObject.data.quiz.info.questions);
                                return;
                            }
                        }
                    }
                }
            }
        }

        //Copy the data from the Panel to the Clipboard
        private void CopyClicked(object sender, RoutedEventArgs e)
        {
            string AllData = "";
            foreach (StackPanel child in StackPanel.Children.OfType<StackPanel>())
            {
                foreach (Label label in child.Children.OfType<Label>())
                {
                    if (child.Children.IndexOf(label) == 0)
                    {
                        AllData += string.Format(" {0}    {1}\n", label.Content, (child.Children[child.Children.IndexOf(label) + 1] as Label).Content);
                    }
                }
            }
            Clipboard.SetText(AllData);
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            string SearchText = SearchBox.Text;
            if (SearchText.Length > 0)
            {

                foreach (StackPanel child in StackPanel.Children.OfType<StackPanel>())
                {
                    foreach (Label label in child.Children.OfType<Label>())
                    {
                        string Combo = string.Empty;
                        List<Label> CurrentLabels = new List<Label>();
                        foreach (Label sublabel in (label.Parent as StackPanel).Children.OfType<Label>())
                        {
                            Combo += sublabel.Content;
                            CurrentLabels.Add(sublabel);
                        }
                        if (Combo.ToLower().Contains(SearchText.ToLower()))
                        {
                            foreach (Label current in CurrentLabels)
                            {
                                if (current.Foreground == (Brush)FindResource("SpecialQuestion"))
                                {
                                    current.Foreground = (Brush)FindResource("SpecialQuestionSearch");
                                    current.BringIntoView();
                                }
                                else if (current.Foreground != (Brush)FindResource("AnswerColor") && current.Foreground != (Brush)FindResource("SpecialQuestionSearch"))
                                {
                                    current.Foreground = (Brush)FindResource("QuestionSearch");
                                    current.BringIntoView();
                                }
                            }
                        }
                        else
                        {
                            foreach (Label current in CurrentLabels)
                            {
                                if (current.Foreground == (Brush)FindResource("SpecialQuestionSearch"))
                                {
                                    current.Foreground = (Brush)FindResource("SpecialQuestion");
                                }
                                else if (current.Foreground != (Brush)FindResource("AnswerColor") && current.Foreground != (Brush)FindResource("SpecialQuestion"))
                                {
                                    current.Foreground = (Brush)FindResource("FontColor");
                                }
                            }
                        }
                    }
                }

            }
            else
            {
                foreach (StackPanel child in StackPanel.Children.OfType<StackPanel>())
                {
                    foreach (Label label in child.Children.OfType<Label>())
                    {
                        if (label.Content.ToString().ToLower().Contains(SearchText.ToLower()))
                        {
                            if (label.Foreground == (Brush)FindResource("SpecialQuestionSearch"))
                            {
                                label.Foreground = (Brush)FindResource("SpecialQuestion");
                            }
                            else if (label.Foreground != (Brush)FindResource("AnswerColor") && label.Foreground != (Brush)FindResource("SpecialQuestion"))
                            {
                                label.Foreground = (Brush)FindResource("FontColor");
                            }
                        }
                    }
                }
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void MinimalizeClicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            switch (WindowState)
            {
                case WindowState.Maximized:
                    this.WindowState = WindowState.Normal;
                    button.Content = "◻";
                    break;
                case WindowState.Normal:
                    this.WindowState = WindowState.Maximized;
                    button.Content = "❏";
                    break;
            }
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MouseTabDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!DiscordLink.IsMouseOver && !versionLabel.IsMouseOver)
                this.DragMove();
        }

        private void DiscordClicked(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://discord.gg/xjecDUHYay") { UseShellExecute = true });
        }

        private void PinClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (Topmost)
            {
                Topmost = false;
                button.Style = (Style)FindResource("TopTabButton");
            }
            else
            {
                Topmost = true;
                button.Style = (Style)FindResource("TogglePressed");
            }
        }

        private void EnterPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                GetAnswersClicked(sender, e);
            }
        }
    }
}