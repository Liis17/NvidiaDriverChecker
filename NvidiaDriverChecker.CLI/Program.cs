using NvidiaDriverChecker.CLI.DB;

using System.Diagnostics;
using System.IO;

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
        public static string GetLatestDriverVersion()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("choco", "info nvidia-display-driver --limit-output");
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Console.Write(output.Replace("\r\n", ""));

                var temp = output.Replace("\r\n", "").Split('|');
                output = temp[1];
                Console.WriteLine("     Версия " + output);
                return output;
            }
        }
    }
}
