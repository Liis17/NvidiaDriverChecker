using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace NvidiaDriverChecker.CLI
{
    public class TelegramService
    {
        private readonly string botToken;
        private readonly long idChannel;
        private readonly long adminId;
        private TelegramBotClient botClient;
        private string cachedVersion = string.Empty;
        private static DateTime lastCheckedTime = DateTime.MinValue;

        public TelegramService(string token, long channelId, long adminId)
        {
            botToken = token;
            idChannel = channelId;
            this.adminId = adminId;
            botClient = new TelegramBotClient(botToken);
            cachedVersion = Program.dbManager.GetLastVersion();
        }

        public async Task StartAsync()
        {
            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };

            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cts.Token
            );

            Console.WriteLine($"Бот запущен.");
            await UpdateCachedVersionAsync();

            // Запуск периодической проверки версии драйвера
            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    await UpdateCachedVersionAsync();
                }
            });
        }

        private async Task UpdateCachedVersionAsync()
        {
            var latestVersion = await Program.GetLatestDriverVersion();

            Version newVersion = new Version(latestVersion);
            Version currentVersion = new Version(cachedVersion);

            if (newVersion > currentVersion)
            {
                cachedVersion = latestVersion;
                Program.dbManager.SetLastVersion(cachedVersion);
                lastCheckedTime = DateTime.Now;

                await NotifyChannelAsync();
            }
        }

        private async Task NotifyChannelAsync()
        {
            string message = $"🔔 Новая версия драйвера\n```Driver\r\nGeForce Game Ready\r\n{cachedVersion} | WHQL\r\nWindows 10 64-bit, Windows 11\r\n{DateTime.Now.ToString("dd.MM.yyyy")}```";

            await botClient.SendTextMessageAsync(idChannel, message, parseMode: ParseMode.Markdown);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                if (message.Text == "/start")
                {
                    await SendWelcomeMessageAsync(chatId);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery);
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;

            switch (callbackQuery.Data)
            {
                case "check_version":
                    await botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                    await SendDriverVersionAsync(chatId);
                    break;
            }
        }

        private async Task SendWelcomeMessageAsync(long chatId)
        {
            string message = $"👋 Добро пожаловать! \nЯ могу проверять обновление драйверов NVIDIA и отправлять уведомление об этом в канал @NvidiaNVCleanstallDrivers.\n";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✨ Получить", "check_version"),
            }
        });

            await botClient.SendTextMessageAsync(chatId, message, replyMarkup: keyboard, parseMode: ParseMode.Markdown);
        }

        private async Task SendDriverVersionAsync(long chatId)
        {
            string message = $"🔎 Текущая версия драйвера: \n\n      {cachedVersion} \n";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithUrl("🌏 Сайт Nvidia", "https://www.nvidia.com/Download/index.aspx")

            },
            new[]
            {
                InlineKeyboardButton.WithUrl("🔗 Открыть ТГК", $"https://t.me/{GetChannelUsernameAsync(idChannel).Result}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔄️ Обновить", "check_version"),
            }
        });

            await botClient.SendTextMessageAsync(chatId, message, replyMarkup: keyboard);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
        private async Task<string> GetChannelUsernameAsync(long channelId)
        {
            Chat chat = await botClient.GetChatAsync(channelId);

            return chat.Username != null ? $"{chat.Username}" : "durov";
        }
    }
}
