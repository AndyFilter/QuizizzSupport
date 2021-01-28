using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

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
        }

        private async void SerialClicked(object sender, RoutedEventArgs e)
        {
            //Could not be bothered to change the name from StackPanel to something useful
            StackPanel.Children.Clear();

            string url = textBox.Text;

            //finds the index of "quiz/" to then get the next 24 symbols which are used to request data from the quizizz API. They are like a Quiz ID
            int found = url.IndexOf("quiz/");

            string responseString = "";

            try
            {
                responseString = await client.GetStringAsync("https://quizizz.com/api/main/" + url.Substring(found, 29));
            }
            catch (HttpRequestException)
            {
                MessageBox.Show(this, "Error: " + e, "An Error Has Occured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show(this, "Error: " + e, "An Error Has Occured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                MessageBox.Show(this, string.Format("Not a valid quizizz url adress \n\nError: " + e), "Wrong Url Adress", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //deserialize the response
            try
            {
                CurrentQuiz = JsonSerializer.Deserialize<Utils.QuizData>(responseString.ToString());
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show(this, "Error: " + e, "Error Has Occured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (JsonException)
            {
                MessageBox.Show(this, string.Format("Invalid data was received \n\nError: " + e), "An Error Has Occured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //Debug.Content = CurrentQuiz.data.quiz.info.questions[0].structure.answer;

            //Loop through all the questions
            foreach (Utils.Questions question in CurrentQuiz.data.quiz.info.questions)
            {
                Label label = new Label();
                string answer = "";
                JsonElement p = question.structure.answer;
                switch (question.type)
                {
                    case "quiz":
                    case "BLANK":
                        answer += Utils.RemoveHtml(question.structure.options[0].text);
                        break;

                    case "MSQ":
                    case "MCQ":
                    default:
                        List<int> listansw = Regex.Matches(p.GetRawText(), "(-?[0-9]+)").OfType<Match>().Select(m => int.Parse(m.Value)).ToList();
                        var strings =
                        listansw.Select(i => i.ToString(CultureInfo.InvariantCulture)).Aggregate((s1, s2) => s1 + ", " + s2);
                        if (listansw.Count <= 0)
                        {
                            answer += Utils.RemoveHtml(question.structure.options[0].text);
                        }
                        answer += Utils.RemoveHtml(question.structure.options[0].text);
                        // listansw - List of Answers. Loops through all the correct answers and adds them to the string
                        foreach (int i in listansw)
                        {
                            answer += Utils.RemoveHtml(question.structure.options[i].text);

                            //checks if there is more than One correct question and if so and the question is not the last one adds " & " at the end
                            if (listansw.Count - listansw.IndexOf(i) > 1)
                            {
                                answer += " & ";
                            }
                        }
                        break;
                }
                var _question = Utils.RemoveHtml(question.structure.query.text);

                label.Content = _question + " - " + answer;

                StackPanel.Children.Add(label);
            }

        }

        //Copy the data from the Panel to the Clipboard
        private void CopyClicked(object sender, RoutedEventArgs e)
        {
            string AllData = "";
            foreach (Label child in StackPanel.Children.OfType<Label>())
            {
                AllData += string.Format(" {0} \n", child.Content);
            }
            Clipboard.SetText(AllData);
        }
    }
}
