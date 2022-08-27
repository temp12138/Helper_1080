namespace DataLibrary
{
    public class ForumApi
    {
        public HttpClient Client;
        public string Cookie;

        public ForumApi(string cookie = "", bool useCookie = true)
        {
            Cookie = cookie;

            var handler = new HttpClientHandler { UseCookies = false };
            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.61 Safari/537.36 115Browser/25.0.1.0");
            if (useCookie)
            {
                //cookie不为空且可用
                if (!string.IsNullOrEmpty(Cookie))
                {
                    Client.DefaultRequestHeaders.Add("Cookie", Cookie);
                }
            }
        }

    }
}