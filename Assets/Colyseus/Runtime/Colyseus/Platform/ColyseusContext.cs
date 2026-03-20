namespace Colyseus
{
    public static class ColyseusContext
    {
        public static ILogger Logger { get; set; }
        public static IHttpClient HttpClient { get; set; }
        public static ITokenStorage TokenStorage { get; set; }

        static ColyseusContext()
        {
            SetDefaults();
        }

        public static void SetDefaults()
        {
            Logger = new ConsoleLogger();
            HttpClient = new DefaultHttpClient();
            TokenStorage = new InMemoryTokenStorage();
        }
    }
}
