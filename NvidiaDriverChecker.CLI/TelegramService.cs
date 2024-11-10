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
            Task.Run(async () =>
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
            var latestVersion = Program.GetLatestDriverVersion();

            if (latestVersion != cachedVersion)
            {
                cachedVersion = latestVersion;
                Program.dbManager.SetLastVersion(cachedVersion);
                lastCheckedTime = DateTime.Now;

                await NotifyChannelAndUsersAsync();
            }
        }

        private async Task NotifyChannelAndUsersAsync()
        {
            string message = $"🔔 Новая версия драйвера NVIDIA:\n\n      {cachedVersion}\n";
            var keyboarduser = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("🌏 nvidia.com", "https://www.nvidia.com/Download/index.aspx"),
                    InlineKeyboardButton.WithUrl("🔗 В тг канал", $"https://t.me/{GetChannelUsernameAsync(idChannel).Result}")
                }
            });
            var keyboardchannel = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("⬇️ Скачать оригинальный ⬇️", "https://www.nvidia.com/Download/index.aspx"),
                }
            });

            await botClient.SendTextMessageAsync(idChannel, message, replyMarkup: keyboardchannel, parseMode: ParseMode.Markdown);

            var users = Program.dbManager.GetAllNotifiedUser();
            foreach (var user in users)
            {
                await botClient.SendTextMessageAsync(user.TelegramID, message, replyMarkup: keyboarduser, parseMode: ParseMode.Markdown);
            }
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

                case "toggle_notifications":
                    if (Program.dbManager.IsNotified(userId))
                    {
                        Program.dbManager.DeleteUser(userId);
                        await botClient.SendTextMessageAsync(chatId, "❌ Уведомления выключены.");
                    }
                    else
                    {
                        Program.dbManager.AddUser(userId, callbackQuery.From.Username, callbackQuery.From.FirstName);
                        await botClient.SendTextMessageAsync(chatId, "✅ Уведомления включены.");
                    }
                    await SendWelcomeMessageAsync(chatId);
                    break;
            }
        }

        private async Task SendWelcomeMessageAsync(long chatId)
        {
            string message = $"👋 Добро пожаловать! \nЯ уведомлять о обновлениях драйверов NVIDIA.\n";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✨ Получить", "check_version"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Program.dbManager.IsNotified(chatId) ? "🔕 Отключить уведомления" : "🔔 Включить уведомления", "toggle_notifications")
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
                InlineKeyboardButton.WithUrl("🌏 nvidia.com", "https://www.nvidia.com/Download/index.aspx"),
                InlineKeyboardButton.WithUrl("🔗 В тг канал", $"https://t.me/{GetChannelUsernameAsync(idChannel).Result}")
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
