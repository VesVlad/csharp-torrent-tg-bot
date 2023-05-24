// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace torrent_tg_bot;

// List<string> cat = new List<string> { "Console", "Movies", "Audio", "PC", "TV", "XXX", "Books", "Other" };
class Program {
    static readonly ITelegramBotClient bot = new TelegramBotClient("5838918727:AAG-XEa4JfuxQev-4KkUkTel60kqTvPZXks");
    static readonly HttpClient client = new HttpClient();
    private static string token;
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Некоторые действия
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if(update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            var message = update.Message;
            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать на борт, пират!", cancellationToken: cancellationToken);
                await botClient.SendTextMessageAsync(message.Chat, "/q ВашЗапрос или /q ВашЗапрос [-cat КатегорияФайла] \n" +
                                                                   "-cat {Console, Movies, Audio, PC, TV, XXX, Books, Other}");
            }
            else if (message.Text.ToLower() == "/help")
            {
                await botClient.SendTextMessageAsync(message.Chat, "/q ВашЗапрос или /q ВашЗапрос [-cat КатегорияФайла] \n" +
                                                                   "-cat {Console, Movies, Audio, PC, TV, XXX, Books, Other}");
            }
            else if (message.Text.StartsWith("/q"))
            {
                if (message.Text.Replace("/q", "").Trim().Length > 0)
                {
                    if (message.Text.Contains("-cat"))
                    {
                        int indexer = message.Text.LastIndexOf("-cat");
                        // message.Text.Substring(indexer, message.Text.Length)
                        // Console.WriteLine();
                    }
                    var response = await client.GetAsync(token
                    .Replace("query", message.Text.Replace("/q", "").Trim()), cancellationToken);
                    HttpResponseMessage status = response.EnsureSuccessStatusCode();
                
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Ваш запрос обрабатывается, пожалуйста, подождите...");
                    
                    if (status.StatusCode != HttpStatusCode.OK)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Что-то пошло не так, status code:" + status.StatusCode, cancellationToken: cancellationToken);
                        throw new Exception("Что-то пошло не так, status code:" + status.StatusCode);
                    }
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    Console.WriteLine(responseBody);
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.LoadXml(responseBody);
                    
                    if (xDoc.DocumentElement.SelectNodes("//item").Count == 0)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"В Текущем indexer: {"0magnet"} не удалось ничего найти по вашему запросу: " +
                                                                           $" {message.Text.Replace("/q", "").Trim()}");
                    }
                    
                    var xmlNodesTitle = xDoc.DocumentElement.SelectNodes("//item/title");
                    var xmlNodesId = xDoc.DocumentElement.SelectNodes("//item/guid");
                    var xmlNodesDate = xDoc.DocumentElement.SelectNodes("//item/pubDate");
                    var xmlNodesSize = xDoc.DocumentElement.SelectNodes("//item/size");
                    int max_rows = xmlNodesTitle.Count > 15 ? 15 : xmlNodesTitle.Count;
                    
                    if (xmlNodesTitle != null)
                        for (int i = 0; i < max_rows; i++)
                        {
                            var title = ((XmlElement)xmlNodesTitle[i]).InnerText;
                            title = title != null && title.Length > 100 ? title.Substring(0, 100) : title;
                            
                            var guid = ((XmlElement)xmlNodesId[i]).InnerText;
                            guid = guid != null && guid.Length > 100 ? guid.Substring(0, 100) : guid;
                            
                            var date = ((XmlElement)xmlNodesDate[i]).InnerText;
                            date = date != null && date.Length > 100 ? date.Substring(0, 100) : date;
                            
                            var size = ((XmlElement)xmlNodesSize[i]).InnerText;
                            float fileSize = float.Parse(size)/ (1024 * 1024 * 1024);
                            
                            await botClient.SendTextMessageAsync(message.Chat,
                                $"{i+1}) Title: «{title}»\n     Url: {guid}\n     PubDate: {date}\n     Size: {fileSize} GB");
                        }
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Аргумент запроса пуст.\n Для отправки запроса введите /q ВашЗапрос или /q ВашЗапрос [-cat КатегорияФайла] \n" +
                    "-cat {Console, Movies, Audio, PC, TV, XXX, Books, Other}");
                }
                
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, 
                    "Неопознанное поведение. Для получения полного списка команд введите /help", cancellationToken: cancellationToken);
            }
        }
    }
    
    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        // Некоторые действия
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        token =
            "http://localhost:9118/api/v2.0/indexers/0magnet/results/torznab/api?apikey=95pqrsj9c8te7w75baz4gnxqdpf8to4e&t=search&q=query";
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }, // receive all update types
        };
        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        Console.ReadLine();
    }
}