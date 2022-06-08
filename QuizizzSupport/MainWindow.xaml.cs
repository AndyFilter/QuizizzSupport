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

/* 
 * -------------------------------------------------Notes-------------------------------------------------

    Before you read some of that code, make sure you are able to sustain major brain damage from useless and dumb code. Good Luck!

    Latex math doesn't work that well, but at least it should not crash. It might not show the correct answer, but it's still better than nothing.
    
    I don't know much about the latest authentication methods used by Quizizz. Thus the code might need some refining after the release. But It works for now.

    Versions of the api calls will change sooner or later which will inevitably break the program. I should probably use smth like string.Format() to have the verions of the APIs as global vars, but I'll leave that to you. <3

    Naming of the variables / functions is so bad it hurts me when I look at it, but It was my first project on this scale, and I actually wanted to use this on one single occasion. As you can see, the plans have changed a little.

    One last thing. The code should compile (obviously), but if it doesn't, then I wish you the best of luck, and may God be with you.
*/

namespace QuizizzSupport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        private Utils.QuizData CurrentQuiz { get; set; }
        private static readonly HttpClient client = new HttpClient(); 
        private bool isBusy = false, isProfileScanOpen = false, isLoginWindowOpen = false;
        private Controls.ProfileScanControl profileScan;
        public Controls.LoginWindow loginWindow;
        public Utils.LoginInfo? lastLoginInfo;
        private static string fileName = "userData.json", folderName = "QuizizzSupport", folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public Structs.UserData currentUserData;

        public MainWindow()
        {
            InitializeComponent();

            // Version checking with Github. I know there is a better way of setting the version, but who cares?
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
                versionLabel.Foreground = Resources["ValidationErrorBrush"] as Brush;
                versionLabel.Cursor = System.Windows.Input.Cursors.Hand;
                versionLabel.MouseLeftButtonDown += VersionLabel_MouseLeftButtonDown;
            }
            ToolTipService.SetToolTip(versionLabel, VerTip);

            currentUserData = ReadCreds();
        }

        private void VersionLabel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/AndyFilter/QuizizzSupport/releases/latest") { UseShellExecute = true });
        }

        private Utils.GitHubRelease CheckVersion(string username, string repoName)
        {
            const string GITHUB_API = "https://_api.github.com/repos/{0}/{1}/releases/latest";
            WebClient webClient = new WebClient();
            // Added user agent
            webClient.Headers.Add("User-Agent", "Unity web player");
            Uri uri = new Uri(string.Format(GITHUB_API, username, repoName));
            string releases = webClient.DownloadString(uri);
            return JsonSerializer.Deserialize<Utils.GitHubRelease>(releases);
        }

        private async void GetAnswersClicked(object sender, RoutedEventArgs e)
        {
            if (isBusy) return;
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

                CurrentQuiz = await getQuizInfo("https://quizizz.com/_api/main/" + url.Substring(found, 29) + "?bypassProfanity=true&returnPrivileges=true&source=join");

                try
                {
                    if (CurrentQuiz.data != null)
                        setQuestionData(CurrentQuiz.data.quiz.info.questions);
                    else
                    {
                        MessageBox.Show(this, "Not a valid quizizz url address", "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                        GetAsnwersBtn.IsEnabled = true;
                        return;
                    }

                }
                catch (System.NullReferenceException)
                {
                    MessageBox.Show(this, "Not a valid quizizz url address", "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                    GetAsnwersBtn.IsEnabled = true;
                    return;
                }
            }
            else if (QuizIdBox.Text.Contains("?gameType=async"))
            {
                //I dont think quizizz puts "?gameType=async" in the link anymore so...
            }
            else if (QuizIdBox.Text.Contains("\"success\":true"))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<Utils.MultiQuizz>(QuizIdBox.Text);
                    setQuestionData(data.data.hits[0].info.questions);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(this, "Not a valid url. PLEASE USE THE CODE", "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                GetAsnwersBtn.IsEnabled = true;
            }
        }

        // Imagine just using guard clauses technique. 
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
                            var profileRes = await FromProfile(code);
                            if (profileRes.Wait)
                            {
                                userResponse.Foreground = Brushes.Honeydew;
                                userResponse.Content = "Complete the actions first";
                                GetAsnwersBtn.IsEnabled = false;
                            }
                            else if (!profileRes.Success)
                            {
                                userResponse.Foreground = Brushes.PaleVioletRed;
                                userResponse.Content = "Quiz not found";
                                GetAsnwersBtn.IsEnabled = true;
                                isProfileScanOpen = isBusy = false;
                            }
                            else
                                setQuestionData(profileRes.Quiz.info.questions);
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

        private async Task<Structs.GetQuizResp> FromProfile(string code)
        {
            isProfileScanOpen = isBusy = true;

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code }
                };
            try
            {
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);
                if (response.StatusCode != HttpStatusCode.OK) return new Structs.GetQuizResp() { Success = false };
                var codeString = await response.Content.ReadAsStringAsync();
                var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);
                int questionsHash = Utils.GetQuestionsHash(roomObject.room.questions);

                Utils.InfoData quizInfo;
                List<string> subjects = new List<string>();

                if (roomObject.room.quizId != null)
                {
                    var quizInfoStr = await client.GetStringAsync($"https://quizizz.com/_api/main/quiz/{roomObject.room.quizId}/info");
                    if (quizInfoStr.Length < 2) return new Structs.GetQuizResp() { Success = false };
                    quizInfo = JsonSerializer.Deserialize<Utils.InfoData>(quizInfoStr);
                    subjects = quizInfo.subjects;
                }
                else
                {
                    var studentInfo = await client.GetStringAsync($"https://quizizz.com/_api/main/v4/students/game/{roomObject.room.hash}");
                    if (studentInfo.Length < 2) return new Structs.GetQuizResp() { Success = false };
                    var studentInfoObject = JsonSerializer.Deserialize<Utils.StudentGameInfo>(studentInfo);
                    subjects = studentInfoObject.data.quizzes.ElementAt(0).Value.subjects;
                }

                int startFrom = 0;

            LoginCheck:
                var profileGetRequest = HttpUtility.UrlDecode($"https%3A%2F%2Fquizizz.com%2F_api%2FlandingPg%2Fsearch%2Fv1%2Fprivate%3Ffrom%3D{startFrom}%26size%3D20%26sortKey%3DcreatedAt%26filterList%3D%7B%22createdBy%22%3A%5B%22{roomObject.room.hostId}%22%5D%2C%22subjects.aggs%22%3A{JsonSerializer.Serialize(subjects)}%7D%26forProfile%3Dtrue%26_%3D1644934119306"); //imagine using /" .Cringe
                if (lastLoginInfo == null || lastLoginInfo.success == false) //NOT LOGGED-IN
                {
                    var profileScanControl = new Controls.ProfileScanControl(profileGetRequest);
                    profileScan = profileScanControl;
                    profileScanControl.Show();
                    profileScanControl.OnContinueClicked += async (string text) =>
                    {
                        if (text == "LOGIN")
                        {
                            lastLoginInfo = loginWindow.lastLoginInfo;
                            await FromProfile(code);
                            return;
                        }
                        Topmost = true;
                        Topmost = false;
                        Focus();
                        var quizData = JsonSerializer.Deserialize<Utils.MultiQuizz>(text);
                        if (quizData.data.hits.Any(q => Utils.GetQuestionsHash(q.info.questions) == questionsHash))
                        {
                            setQuestionData(quizData.data.hits.First(q => Utils.GetQuestionsHash(q.info.questions) == questionsHash).info.questions);
                            profileScanControl.Close();
                            profileScanControl = null;
                            isProfileScanOpen = isBusy = false;
                        }
                        else
                        {
                            profileScanControl.Close();
                            profileScanControl = null;
                            isProfileScanOpen = isBusy = false;
                            QuizNotFound();
                            //Owner has too many quizzes. Can automate this, but not with user
                        }

                    };
                }
                else //LOGGED-IN
                {
                    var clientCookies = new CookieContainer();
                    var client = new HttpClient(new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip,
                        CookieContainer = clientCookies
                    });

                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Host", "quizizz.com");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:96.0) Gecko/20100101 Firefox/96.0");
                    client.DefaultRequestHeaders.Add("Accept-Language", "pl,en-US;q=0.7,en;q=0.3");
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    //client.DefaultRequestHeaders.Add("Referer", "https://quizizz.com");
                    client.DefaultRequestHeaders.Add("ContentType", "application/json");
                    client.DefaultRequestHeaders.Add("x-csrf-token", Utils.GetCsrfTokens(lastLoginInfo.cookies).Item1);
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

                    clientCookies.Add(lastLoginInfo.cookies);

                    var profileInfoStr = await client.GetAsync(profileGetRequest);
                    //var cookies = clientCookies.GetCookies(new Uri("https://quizizz.com/_api/main/search/v1/private"));
                    if (!profileInfoStr.IsSuccessStatusCode) return new Structs.GetQuizResp() { Success = false };
                    var profileInfo = JsonSerializer.Deserialize<Utils.MultiQuizz>(await profileInfoStr.Content.ReadAsStringAsync());
                    if (profileInfo.data.hits.Any(q => Utils.GetQuestionsHash(q.info.questions) == questionsHash))
                    {
                        var foundQuiz = profileInfo.data.hits.First(q => Utils.GetQuestionsHash(q.info.questions) == questionsHash);
                        setQuestionData(foundQuiz.info.questions);
                        isProfileScanOpen = isBusy = false;
                        return new Structs.GetQuizResp() { Quiz = foundQuiz, Success = true };
                    }
                    else if (profileInfo.data.hits != null && profileInfo.data.hits.Count > 0)
                    {
                        if (startFrom >= 60)
                            return new Structs.GetQuizResp() { Success = false };
                        startFrom += 20;
                        goto LoginCheck;
                    }
                    else
                    {
                        return new Structs.GetQuizResp() { Success = false };
                    }
                    //Clipboard.SetText(await profileInfo.Content.ReadAsStringAsync());
                }
            }
            catch (Exception)
            {
                isProfileScanOpen = isBusy = false;
                return new Structs.GetQuizResp() { Success = false };
            }

            isProfileScanOpen = isBusy = false;
            return new Structs.GetQuizResp() { Success = false, Wait = true };
        }

        public void QuizNotFound()
        {
            userResponse.Foreground = Brushes.PaleVioletRed;
            userResponse.Content = "Quiz not found";
            GetAsnwersBtn.IsEnabled = true;
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

                var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);

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

                var currentQuiz = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quizId + "?bypassProfanity=true&returnPrivileges=true&source=join");

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
            try
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

                var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);
                if (response.StatusCode != HttpStatusCode.OK) return new Structs.GetQuizResp() { Success = false };

                var codeString = await response.Content.ReadAsStringAsync();

                var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

                if (roomObject.room != null)
                {
                    quizTag = roomObject.room.questions;
                }
                else
                {
                    //MessageBox.Show(this, "Not a valid quizizz code", "Expired Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //userResponse.Foreground = Brushes.PaleVioletRed;
                    //userResponse.Content = "Quiz not found";
                    return new Structs.GetQuizResp() { Success = false };
                }

                var quizLongId = roomObject.room.quizId;

                string quizString;
                try
                {
                    quizString = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quizLongId + "/info");
                }
                catch (Exception)
                {
                    return new Structs.GetQuizResp() { Success = false };
                }

                if (quizString.Length < 2)
                    return new Structs.GetQuizResp() { Success = false };

                var SummaryObject = JsonSerializer.Deserialize<Utils.GameSummary>(quizString);
                if (SummaryObject == null || SummaryObject.data == null) return new Structs.GetQuizResp() { Success = false };

                foreach (Utils.SummaryQuiz RecomendedQuiz in SummaryObject.data.quizzes)
                {
                    if (!ScannedQuizes.Contains(RecomendedQuiz._id))
                    {
                        Debug.Content = RecomendedQuiz.info.name;

                        string RecomendedQuizString;
                        try
                        {
                            RecomendedQuizString = await client.GetStringAsync("https://quizizz.com/_api/main/gameSummaryRec?quizId=" + RecomendedQuiz._id);
                        }
                        catch
                        {
                            return new Structs.GetQuizResp() { Success = false };
                        }

                        var RecomendedQuizObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString);

                        ScannedQuizes.Add(RecomendedQuiz._id);

                        foreach (Utils.SummaryQuiz quiz in RecomendedQuizObject.data.quizzes)
                        {
                            Debug.Content = quiz.info.name;

                            string RecomendedQuizString2;
                            try
                            {
                                RecomendedQuizString2 = await client.GetStringAsync("https://quizizz.com/_api/main/gameSummaryRec?quizId=" + quiz._id);
                            }
                            catch
                            {
                                return new Structs.GetQuizResp() { Success = false };
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
                                        currentQuiz = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quiz2._id + "?bypassProfanity=true&returnPrivileges=true&source=join");
                                    }
                                    catch
                                    {
                                        return new Structs.GetQuizResp() { Success = false };
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
            catch (Exception)
            {
                return new Structs.GetQuizResp() { Success = false };
            }
        }

        private async Task<Structs.GetQuizResp> FindRecommended1(string code)
        {
            try
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

                var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);
                if (response.StatusCode != HttpStatusCode.OK) return new Structs.GetQuizResp() { Success = false };

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

                string quizString;
                try
                {
                    quizString = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quizLongId + "/info");
                }
                catch (Exception)
                {
                    return new Structs.GetQuizResp() { Success = false };
                }

                if (quizString.Length < 2)
                    return new Structs.GetQuizResp() { Success = false };

                var SummaryObject = JsonSerializer.Deserialize<Utils.GameSummary>(quizString);
                if (SummaryObject == null || SummaryObject.data == null) return new Structs.GetQuizResp() { Success = false };

                foreach (Utils.SummaryQuiz RecomendedQuiz in SummaryObject.data.quizzes)
                {
                    if (!ScannedQuizes.Contains(RecomendedQuiz._id))
                    {
                        Debug.Content = RecomendedQuiz.info.name;

                        string RecomendedQuizString;
                        try
                        {
                            RecomendedQuizString = await client.GetStringAsync("https://quizizz.com/_api/main/getSimilarQuizzes?quizId=" + RecomendedQuiz._id + "&page=1&pageSize=20");
                        }
                        catch
                        {
                            return new Structs.GetQuizResp() { Success = false };
                        }

                        var RecomendedQuizObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString);

                        ScannedQuizes.Add(RecomendedQuiz._id);

                        foreach (Utils.SummaryQuiz quiz in RecomendedQuizObject.data.quizzes)
                        {
                            Debug.Content = quiz.info.name;

                            string RecomendedQuizString2;
                            try
                            {
                                RecomendedQuizString2 = await client.GetStringAsync("https://quizizz.com/_api/main/getSimilarQuizzes?quizId=" + quiz._id + "&page=1&pageSize=20");
                            }
                            catch
                            {
                                return new Structs.GetQuizResp() { Success = false };
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
                                        currentQuiz = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quiz2._id + "?bypassProfanity=true&returnPrivileges=true&source=join");
                                    }
                                    catch
                                    {
                                        return new Structs.GetQuizResp() { Success = false };
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
            catch (Exception)
            {
                return new Structs.GetQuizResp() { Success = false };
            }
        }

        private async Task<Structs.GetQuizResp> searchFor(string code)
        {
            try
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

                var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);
                if (response.StatusCode != HttpStatusCode.OK) return new Structs.GetQuizResp() { Success = false };

                var codeString = await response.Content.ReadAsStringAsync();

                var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

                if (roomObject.room != null)
                {
                    numberOfQuestions = roomObject.room.questions.Count;
                    if (numberOfQuestions > 1)
                        quizTag = roomObject.room.questions[1];
                    else
                        quizTag = roomObject.room.questions[0];
                }
                else
                {
                    //MessageBox.Show(this, "Not a valid quizizz code", "Expired Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                    userResponse.Foreground = Brushes.PaleVioletRed;
                    userResponse.Content = "Quiz not found";
                    return new Structs.GetQuizResp() { Success = false };
                }

                var quizLongId = roomObject.room.quizId;

                string quizString;
                try
                {
                    quizString = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quizLongId + "/info");
                }
                catch (Exception)
                {
                    return new Structs.GetQuizResp() { Success = false };
                }

                if (quizString == "")
                    return new Structs.GetQuizResp() { Success = false };

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

                string searchResponse = await client.GetStringAsync("https://quizizz.com/_api/main/search?from=0&sortKey=_score&filterList=" + JsonSerializer.Serialize<Utils.SearchArgs>(searchArgs) + "&source=MainHeader&page=SearchPage&size=20&query=" + searchName + "&_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

                var searchObject = JsonSerializer.Deserialize<Utils.MultiQuizz>(searchResponse);

                foreach (Utils.Quiz quiz in searchObject.data.hits)
                {
                    if (quiz.info.questions.Count > 1)
                    {
                        if (quiz.info.questions[1]._id == quizTag)
                        {
                            //Clipboard.SetText(JsonSerializer.Serialize<Utils.Quiz>(quiz));
                            //setQuestionData(quiz.info.questions);
                            return new Structs.GetQuizResp() { Success = true, Quiz = quiz };
                        }
                    }
                    else
                    {
                        if (quiz.info.questions[0]._id == quizTag)
                        {
                            //Clipboard.SetText(JsonSerializer.Serialize<Utils.Quiz>(quiz));
                            //setQuestionData(quiz.info.questions);
                            return new Structs.GetQuizResp() { Success = true, Quiz = quiz };
                        }
                    }
                }
                var RecommendedQuizzesString = await client.GetStringAsync("https://quizizz.com/_api/main/getSimilarQuizzes?quizId=" + quizLongId + "&page=1&pageSize=20");
                var RecommendedQuizzesObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecommendedQuizzesString);

                var RecommendedQuizString = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + RecommendedQuizzesObject.data.quizzes[0]._id + "?bypassProfanity=true&returnPrivileges=true&source=join");
                var RecommendedQuiz = JsonSerializer.Deserialize<Utils.QuizData>(RecommendedQuizString);

                searchArgs.LangAggs = new List<string> { RecommendedQuiz.data.quiz.info.lang };

                //searchResponse = await client.GetStringAsync("https://quizizz.com/_api/main/search?from=0&sortKey=_score&filterList=" + JsonSerializer.Serialize<Utils.SearchArgs>(searchArgs) + "&rangeList=" + JsonSerializer.Serialize<Utils.QuestionsRange>(RangeList) + "&source=MainHeader&page=SearchPage&query=" + searchName + "&size=20" + "&_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());
                searchResponse = await client.GetStringAsync("https://quizizz.com/_api/main/search?from=0&sortKey=_score&filterList=" + JsonSerializer.Serialize<Utils.SearchArgs>(searchArgs) + "&source=MainHeader&page=SearchPage&size=20&query=" + searchName + "&_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

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
            catch (Exception)
            {
                return new Structs.GetQuizResp() { Success = false };
            }
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
                //MessageBox.Show(this, string.Format("Not a valid quizizz url address \n\nError"), "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            //Clipboard.SetText(codeString);

            string quizString = "";

            if (JsonSerializer.Deserialize<Utils.RoomData>(codeString).room != null)
            {
                quizString = await client.GetStringAsync("https://quizizz.com/_api/main/game/" + JsonSerializer.Deserialize<Utils.RoomData>(codeString).room.hash);
            }
            //else
            //throw new ArgumentException("Parameter cannot be null", nameof(getQuizInfo));

            Utils.QuizData quizData = JsonSerializer.Deserialize<Utils.QuizData>(quizString);

            return quizData;
        }


        private void setQuestionData(List<Utils.Questions> questions)
        {
            userResponse.Foreground = Brushes.LimeGreen;
            userResponse.Content = "Quiz Found!";
            GetAsnwersBtn.IsEnabled = true;

            //Could not be bothered to change the name from StackPanel to something useful
            StackPanel.Children.Clear();

            foreach (Utils.Questions question in questions)
            {
                Label Question = new Label();
                Label Answer = new Label();
                StackPanel QuestData = new StackPanel();
                if (!question.structure.settings.hasCorrectAnswer)
                {//Open Question. can also check with {case "OPEN":}
                    Button searchButton = new Button
                    {
                        Width = 20,
                        Height = 20,
                        BorderThickness = new Thickness(0),
                        Content = new Image
                        {
                            Source = new BitmapImage(new Uri("GoogleLogo.png", UriKind.Relative)), // I love the quality of this image, it's just different
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                        Background = new SolidColorBrush(Colors.Transparent),
                        Style = FindResource("Normal") as Style,
                        Padding = new Thickness(0),
                        Cursor = Cursors.Hand,
                    };
                    var searchTip = new ToolTip();
                    searchTip.Content = "Google It!";
                    searchButton.ToolTip = searchTip;
                    searchButton.Click += SearchButton_Click;
                    searchButton.DataContext = question;
                    RenderOptions.SetBitmapScalingMode(searchButton.Content as Image, BitmapScalingMode.HighQuality);
                    QuestData.Orientation = Orientation.Horizontal;

                    QuestData.Children.Add(Question);
                    QuestData.Children.Add(Answer);
                    QuestData.Children.Add(searchButton);
                    Question.Content = Utils.RemoveHtml(question.structure.query.text) + "   -  ";
                    StackPanel.Children.Add(QuestData);
                    Answer.Foreground = (Brush)FindResource("AnswerColor");
                    continue;
                }
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
                            var latexText = Utils.GetLatextFromHtml(question.structure.query.text);
                            latexText = latex;
                            var formula = parser.Parse(latexText);
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
                                var latexText = Utils.GetLatextFromHtml(question.structure.query.text);
                                var cleanLatex = Utils.CleanLatex(latexText);
                                var formula = parser.Parse(cleanLatex);
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

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var button = sender as Button;
            Uri uri = new Uri($"https://www.google.com/search?q={Utils.RemoveHtml((button.DataContext as Utils.Questions).structure.query.text)}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
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
        // Funny hidden button good luck finding it tho...
        private async void DebugButtonClicked(object sender, RoutedEventArgs e)
        {
            //if (auth != null)
            //{
            //    //Clipboard.SetText(auth.ToString());
            //    lastLoginInfo = auth;
            //    SetLoginState(auth);
            //}
            //return;

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

                CurrentQuiz = await getQuizInfo("https://quizizz.com/_api/main/quiz/" + url.Substring(found, 29));

                try
                {
                    setQuestionData(CurrentQuiz.data.quiz.info.questions);
                }
                catch (System.NullReferenceException)
                {
                    MessageBox.Show(this, "Not a valid quizizz url address nor ID", "Wrong Url Adress/ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show(this, "Not a valid quizizz url address nor ID", "Wrong Url Adress/ID", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        BruteForce:
            List<string> ScannedQuizes = new List<string>();

            string quizTag;

            var values = new Dictionary<string, string>
                {
                    { "roomCode", code.ToString() }
                };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://game.quizizz.com/play-api/v5/checkRoom", content);

            var codeString = await response.Content.ReadAsStringAsync();

            var roomObject = JsonSerializer.Deserialize<Utils.RoomData>(codeString);

            quizTag = roomObject.room.questions[0];

            var quizLongId = roomObject.room.quizId;

            string quizString = await client.GetStringAsync("http://quizizz.com/_api/main/gameSummaryRec?quizId=" + quizLongId);

            var SummaryObject = JsonSerializer.Deserialize<Utils.GameSummary>(quizString);

            foreach (Utils.SummaryQuiz RecomendedQuiz in SummaryObject.data.quizzes)
            {
                if (!ScannedQuizes.Contains(RecomendedQuiz._id))
                {
                    //Debug.Content = RecomendedQuiz.info.name;

                    var RecomendedQuizString = await client.GetStringAsync("https://quizizz.com/_api/main/gameSummaryRec?quizId=" + RecomendedQuiz._id);

                    var RecomendedQuizObject = JsonSerializer.Deserialize<Utils.GameSummary>(RecomendedQuizString);

                    ScannedQuizes.Add(RecomendedQuiz._id);

                    foreach (Utils.SummaryQuiz quiz in RecomendedQuizObject.data.quizzes)
                    {
                        if (!ScannedQuizes.Contains(quiz._id))
                        {
                            //Debug.Content = quiz.info.name;

                            var currentQuiz = await client.GetStringAsync("https://quizizz.com/_api/main/quiz/" + quiz._id + "?bypassProfanity=true&returnPrivileges=true&source=join");

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
            if (StackPanel.Children.Count < 1)
            {
                MessageBox.Show("Please find a quiz first", "What do you want to copy?", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
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
            loginWindow?.Close();
            profileScan?.Close();
            System.Windows.Application.Current.Shutdown();
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

        private async void LogInClicked(object sender, RoutedEventArgs e)
        {
            if (lastLoginInfo == null || !lastLoginInfo.success)
            {
                if (loginWindow != null && loginWindow.IsVisible)
                {
                    loginWindow.Hide();
                    isLoginWindowOpen = false;
                }
                else
                {
                    isLoginWindowOpen = true;
                    if (loginWindow == null)
                    {
                        loginWindow = new Controls.LoginWindow();
                        loginWindow.OnLoginButtonClicked += LoginWindow_OnLoginButtonClicked;
                    }
                    loginWindow.Show();
                    await loginWindow.AuthorizationSuccessful.Task.ContinueWith(k =>
                    {
                        if (loginWindow.lastLoginInfo != null && loginWindow.lastLoginInfo.success)
                        {
                            lastLoginInfo = loginWindow.lastLoginInfo;
                            SetLoginState(lastLoginInfo);
                        }
                    });
                }
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to log out?", "Achtung!", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    LogOut();
                }
            }
        }

        // Should move these 2 functions to UTILS or smth
        private void SaveCreds(Structs.UserData userData)
        {
            if (!Directory.Exists(Path.Combine(folderPath, folderName)))
            {
                Directory.CreateDirectory(Path.Combine(folderPath, folderName));
            }
            // Imagine encryption of user credentials. Pffff...
            File.WriteAllText($"{Path.Combine(Path.Combine(folderPath, folderName), fileName)}", JsonSerializer.Serialize(userData));
        }

        private Structs.UserData ReadCreds()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(folderPath, folderName)))
                {
                    Directory.CreateDirectory(Path.Combine(folderPath, folderName));
                }
                if (File.Exists(Path.Combine(Path.Combine(folderPath, folderName), fileName)))
                {
                    var userData = JsonSerializer.Deserialize<Structs.UserData>(File.ReadAllText($"{Path.Combine(Path.Combine(folderPath, folderName), fileName)}"));
                    if (userData.password.Length > 3)
                        return userData;
                    else
                        return null;
                }
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void LogOut()
        {
            if (loginWindow != null)
            {
                loginWindow.LogOut();

                loginButton.Content = "Log in";
                loggedUserLabel.Content = "Not logged in";
                loggedUserLabel.Foreground = Resources["ValidationErrorBrush"] as Brush;

                lastLoginInfo = null;
            }
        }

        public void LoginWindow_OnLoginButtonClicked(object sender, EventArgs e)
        {
            lastLoginInfo = loginWindow.lastLoginInfo;
            if (loginWindow.lastLoginInfo == null || loginWindow.lastLoginInfo.success)
            {
                SetLoginState(lastLoginInfo);
                if (lastLoginInfo != null && lastLoginInfo.password != null && lastLoginInfo.password.Length > 1 && lastLoginInfo.rememberCreds)
                    SaveCreds(new Structs.UserData() { password = lastLoginInfo.password, username = lastLoginInfo.username });
                else if (lastLoginInfo != null)
                    SaveCreds(new Structs.UserData() { password = "", username = "" });
            }
        }

        private void SetLoginState(Utils.LoginInfo login)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (login == null || !login.success)
                    {
                        loginButton.Content = "Log in";
                        loggedUserLabel.Content = "Not logged in";
                        loggedUserLabel.Foreground = Resources["ValidationErrorBrush"] as Brush;
                    }
                    else
                    {
                        var useUsername = (login.data.user.firstName == null || login.data.user.firstName.Length < 1) ? login.data.user.local.username : login.data.user.firstName;
                        loginButton.Content = "Log out";
                        loggedUserLabel.Content = $"Welcome, {useUsername}";
                        loggedUserLabel.Foreground = Brushes.LimeGreen;
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }));
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