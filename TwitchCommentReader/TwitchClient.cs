using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TwitchCommentReader
{
    public class TwitchClient
    {
        readonly TcpClient client;

        readonly string id;
        readonly string token;
        readonly Regex privmsgRegex;

        readonly string host = "irc.chat.twitch.tv";
        readonly int port = 6667;

        public delegate void MessageReceiveHandler(string id, string token);
        public event MessageReceiveHandler MessageReceived;

        public TwitchClient(string id, string token)
        {
            client = new TcpClient()
            {
                NoDelay = true
            };

            this.id = id;
            this.token = token;

            privmsgRegex = new Regex($@":(?<nickname>[^!@:#\s]+)!(?<realname>[^!@:#\s]+)@(?<host>[^!@:#\s]+) PRIVMSG #{id} :(?<message>.+)", RegexOptions.Compiled);
        }

        public async Task Start()
        {
            Console.WriteLine($"{host}:{port} に接続します");
            await client.ConnectAsync(host, port);
            Console.WriteLine("接続しました");

            var networkStream = client.GetStream();
            var reader = new StreamReader(networkStream);
            var writer = TextWriter.Synchronized(new StreamWriter(networkStream) { AutoFlush = true, NewLine = "\r\n" });

            var listeningTask = Task.Run(async () =>
            {
                while (true)
                {
                    var receivedMessage = await reader.ReadLineAsync();
                    Console.WriteLine("> " + receivedMessage);

                    if (receivedMessage.StartsWith("PING"))
                    {
                        Console.WriteLine("PONG");
                        await writer.WriteLineAsync(receivedMessage.Replace("PING", "PONG"));
                    }

                    var privmsgMatch = privmsgRegex.Match(receivedMessage);
                    if (privmsgMatch.Success)
                    {
                        MessageReceived?.Invoke(privmsgMatch.Groups["nickname"].Value, privmsgMatch.Groups["message"].Value);
                    }
                }
            });

            Console.WriteLine("PASS");
            await writer.WriteLineAsync($"PASS {token}");
            Console.WriteLine($"NICK {id}");
            await writer.WriteLineAsync($"NICK {id}");
            Console.WriteLine($"USER {id} 0 * :{id}");
            await writer.WriteLineAsync($"USER {id} 0 * :{id}");
            Console.WriteLine($"JOIN #{id}");
            await writer.WriteLineAsync($"JOIN #{id}");
            Console.WriteLine("ルームに入りました");

            listeningTask.Wait();
        }
    }
}
