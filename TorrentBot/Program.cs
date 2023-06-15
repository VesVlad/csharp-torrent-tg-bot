// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace torrent_tg_bot;

class Program {
    static readonly ITelegramBotClient bot = new TelegramBotClient("5838918727:AAG-XEa4JfuxQev-4KkUkTel60kqTvPZXks");
    static readonly HttpClient client = new HttpClient();
    private static string token;
    private static Dictionary<string, int>? categories;
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if(update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            var message = update.Message;
            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать на борт, пират!", cancellationToken: cancellationToken);
                await botClient.SendTextMessageAsync(message.Chat, "/q ВашЗапрос или /q ВашЗапрос [-cat КатегорияФайла] \n" +
                                                                   "-cat {Audio, PC, TV, XXX, Books, Other}", cancellationToken: cancellationToken);
            }
            else if (message.Text.ToLower() == "/help")
            {
                await botClient.SendTextMessageAsync(message.Chat, "/q ВашЗапрос или /q ВашЗапрос [-cat КатегорияФайла] \n" +
                                                                   "-cat {Audio, PC, TV, XXX, Books, Other}", cancellationToken: cancellationToken);
            }
            else if (message.Text.StartsWith("/q"))
            {
                if (message.Text.Replace("/q", "").Trim().Length > 0)
                {
                    string q = message.Text;
                    string catSubstring = "";
                    if (message.Text.Contains("-cat"))
                    {
                        int catStartIndex = q.LastIndexOf("-cat") + 4;
                        catSubstring = q.Substring(catStartIndex, q.Length - catStartIndex).Trim();
                        
                        if (catSubstring.Trim().Length != 0 && categories.ContainsKey(catSubstring))
                        {
                            q = q.Substring(0, catStartIndex - 4);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Пустая или несуществующая категория -сat: " + q, cancellationToken: cancellationToken);
                        }
                        
                    }
                    
                    var response = await client.GetAsync(token
                    .Replace("query", q.Replace("/q", "").Trim())  + categories[catSubstring].ToString(), cancellationToken: cancellationToken);
                    HttpStatusCode status = response.StatusCode;
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Ваш запрос обрабатывается, пожалуйста, подождите...", cancellationToken: cancellationToken);
                    
                    if (status != HttpStatusCode.OK)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Что-то пошло не так, status code:" + status, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(responseBody);
                    
                        if (xDoc.DocumentElement.SelectNodes("//item").Count == 0)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"В Текущем indexer: {"anidex"} не удалось ничего найти по вашему запросу: " +
                                                                               $" {message.Text.Replace("/q", "").Trim()}", cancellationToken: cancellationToken);
                        }
                        var xmlNodesTitle = xDoc.DocumentElement.SelectNodes("//item/title");
                        var xmlNodesId = xDoc.DocumentElement.SelectNodes("//item/guid");
                        var xmlNodesDate = xDoc.DocumentElement.SelectNodes("//item/pubDate");
                        var xmlNodesSize = xDoc.DocumentElement.SelectNodes("//item/size");
                        int maxRows = xmlNodesTitle.Count > 15 ? 15 : xmlNodesTitle.Count;

                        if (xmlNodesTitle is not null)
                        {
                            for (int i = 0; i < maxRows; i++)
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
                                    $"({i+1}) Title: «{title}»\n     Url: {guid}\n     PubDate: {date}\n     Size: {fileSize} GB", cancellationToken: cancellationToken);
                            } 
                        }
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Пустой запрос.\n Для отправки запроса введите /q ВашЗапрос или /q ВашЗапрос [-cat КатегорияФайла] \n" +
                    "-cat {Audio, PC, TV, XXX, Books, Other}", cancellationToken: cancellationToken);
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
        categories = new Dictionary<string, int>()
        {
            {"Audio", 3000},
            {"PC", 4000},
            {"TV", 5000},
            {"XXX", 6000},
            {"Books", 7000},
            {"Other", 8000}
        };
        token =
            "http://localhost:9118/api/v2.0/indexers/anidex/results/torznab/api?apikey=95pqrsj9c8te7w75baz4gnxqdpf8to4e&t=search&q=query&cat=";
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