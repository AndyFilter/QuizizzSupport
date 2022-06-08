namespace QuizizzSupport
{
    public class Structs
    {
        public class GetQuizResp
        {
            public bool Success { get; set; }
            public bool Wait { get; set; }
            public Utils.Quiz Quiz { get; set; }
        }

        public class UserData // Very safe way of storing user's credentials, I know.
        {
            public string username { get; set; }
            public string password { get; set; }
        }
    }
}
