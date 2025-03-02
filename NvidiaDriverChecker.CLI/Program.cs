using HtmlAgilityPack;

using NvidiaDriverChecker.CLI.DB;

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace NvidiaDriverChecker.CLI
{
    public class Program
    {
        public static DatabaseManager dbManager;
        public static TelegramService telegramService;

        public static int test1;
        static void Main()
        {

            string dbPath = Path.Combine(Environment.CurrentDirectory, "Database.db");
            dbManager = new DatabaseManager(dbPath);

            if (!dbManager.Exist())
            {
                Console.WriteLine("Отсутствуют настройки для бота (токен бота и id админа и канала)");
                Console.WriteLine("Введи токен бота: ");
                string _newtoken = Console.ReadLine();
                Console.WriteLine("Введи id канала: ");
                long _newcahnnelid = Int64.Parse(Console.ReadLine());
                Console.WriteLine("Введи id админа: ");
                long _newadminid = Int64.Parse(Console.ReadLine());
                dbManager.CleareConf();
                Console.WriteLine("Сохранение новых настроек...");
                dbManager.CreateConf(_newtoken, _newcahnnelid, _newadminid);
                Console.Clear();
            }

            string token = dbManager.GetBotToken();
            long channelid = dbManager.GetChannel();
            long adminid = dbManager.GetAdmin();
            telegramService = new TelegramService(token, channelid, adminid);
            telegramService.StartAsync();

            while (true)
            {
                Console.ReadKey();
                Console.WriteLine(" - Bruh, для выхода Ctrl+C");
            }
        }
        public async static Task<string> GetLatestDriverVersion()
        {
            try
            {
                string url = "https://www.techpowerup.com/download/nvidia-geforce-graphics-drivers/";

                using HttpClient client = new HttpClient();
                string pageContent = await client.GetStringAsync(url);

                Match match = Regex.Match(pageContent, @"<title>.*?NVIDIA GeForce Graphics Drivers (\d+\.\d+) WHQL.*?</title>");

                if (match.Success)
                {
                    Console.WriteLine("Последняя версия драйвера: " + match.Groups[1].Value);
                    return match.Groups[1].Value;
                }
                else
                {
                    Console.WriteLine("Не удалось найти версию драйвера.");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
