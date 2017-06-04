using System;

namespace TwitchCommentReader
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("使い方： TwitchCommentReader.exe [id] [token]");
                Console.WriteLine("id は Twitch の ID");
                Console.WriteLine("token は http://www.twitchapps.com/tmi/ で取得した token: から始まる文字列です。");
                Console.ReadLine();
                Environment.Exit(-1);
                return;
            }

            var una = new Una();
            var twitch = new TwitchClient(args[0], args[1]);
            twitch.MessageReceived += (id, message) => una.Talk(id, message);
            twitch.Start().Wait();
        }
    }
}
