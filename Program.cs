using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Program
{
    class ChatUser
    {
        public long UserId { get; set; } //Айди прользователя
        public long? PartnerId { get; set; } //Айди собеседника
        public bool IsActive { get; set; } //Идет чат с кем-то
        public bool IsLogged { get; set; } //Прошел начальную проверку
        public bool IsMale { get; set; } //Проверка пола
        public bool IsSearch { get; set; } //Проверка идет ли поиск
        public int LogState { get; set; } = 0; //Стадия логирования
        public int MessageLimit { get; set; } = 50; //Количество сообщений
    }
    class Program
    {
        static ITelegramBotClient client = new TelegramBotClient("8475939762:AAHDN0E-dsD2BR_IBcmpIuv1z40ynMOYGc8");
        static async Task Main(string[] args)
        {
            LoadUsers();
            LoadAdminsFromFile();
            await SkipOldUpdates();
            client.StartReceiving(Update, Error);
            await Task.Delay(-1);
        }

        // Список ID администраторов
        static List<long> adminIds = new List<long> { 1991980696 };
        private static Dictionary<long, ChatUser> chatUsers = new();
        public static ChatUser GetChatUser(long userId)
        {
            if (!chatUsers.ContainsKey(userId))
            {
                chatUsers[userId] = new ChatUser { UserId = userId };
            }
            return chatUsers[userId];
        }

        //Булевый метод который возвращает айди админов
        static bool IsAdmin(long userId)
        {
            return adminIds.Contains(userId);
        }
        static string commands = "/help - ℹ️ О боте / Помощь\n" +
                                  "/search - 🔍 Найти собеседника\n" +
                                   "/stop - 🛑 Завершить чат\n" +
                                    "/messages - ✉ Кол-во Сообщений\n" +
                                     "/premium - 🌟 Премиум доступ\n" +
                                      "/buyMessages - 📩 Ещё сообщения\n" +
                                       "/premiumInfo - 💫 Что дает премиум доступ?";

        static ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(new[]
                {
                        new KeyboardButton[] { "ℹ️ О боте / Помощь", "✉ Кол-во Сообщений"},
                        new KeyboardButton[]{ "🔍 Найти собеседника", "🛑 Завершить чат"},
                        new KeyboardButton[]{ "📩 Ещё сообщения", "🌟 Премиум доступ" },
                        new KeyboardButton[]{ "💫 Что дает премиум доступ?" },
                    })
        {
            ResizeKeyboard = true
        };
        static ReplyKeyboardMarkup AdminKeyboard = new ReplyKeyboardMarkup(new[]
                {
                        new KeyboardButton[] { "ℹ️ О боте / Помощь", "✉ Кол-во Сообщений"},
                        new KeyboardButton[]{ "🔍 Найти собеседника", "🛑 Завершить чат"},
                        new KeyboardButton[]{ "📩 Ещё сообщения", "🌟 Премиум доступ" },
                        new KeyboardButton[]{ "💫 Что дает премиум доступ?" },
                    })
        {
            ResizeKeyboard = true
        };

        // Обработка полученных обновлений, которых может быть много :)
        async static Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            var message = update.Message;

            if (message == null)
                return;
            if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
            {
                return;
            }

            long userId;
            if (update.CallbackQuery?.From?.Id != null)
                userId = update.CallbackQuery.From.Id;
            else if (update.Message?.From?.Id != null)
                userId = update.Message.From.Id;
            else
                return; // или логирование ошибки

            var user = GetChatUser(userId);

            // Проверка на команду /start
            if (message.Text == "/start")
            {
                if(user.IsLogged)
                {
                    if (!IsAdmin(message.Chat.Id))
                    {
                        await client.SendMessage(message.Chat.Id,
                        "Анонимный чат бот \n" + $"Выберите, что вас интересует: \n\n{commands}",
                        replyMarkup: keyboard);
                    }
                    else
                    {
                        await client.SendMessage(message.Chat.Id,
                        "Анонимный чат бот \n" + $"✨Выберите, что вас интересует: \n\n{commands}",
                        replyMarkup: AdminKeyboard);
                    }
                }
                else
                {
                    user.LogState = 1;
                    ReplyKeyboardMarkup SelectGender = new ReplyKeyboardMarkup(new[]
                        {
                            new KeyboardButton[] { "👱‍♂️ Мужской", "👱‍♀️ Женский"},
                        })
                    {
                        ResizeKeyboard = true
                    };
                    await client.SendMessage(message.Chat.Id,
                        "Анонимный чат бот \n" + $"Выберите ваш пол 👇",
                        replyMarkup: SelectGender);
                }
            }
            else
            {
                if (!user.IsLogged)
                    return;

                switch (message.Text)
                {
                    case "/help":
                    case "ℹ️ О боте / Помощь":
                        if (!IsAdmin(message.Chat.Id))
                        {
                            await client.SendMessage(message.Chat.Id, $"{commands}", replyMarkup: keyboard);
                        }
                        else
                        {
                            await client.SendMessage(message.Chat.Id, $"{commands}", replyMarkup: AdminKeyboard);
                        }
                        break;
                    case "/search":
                    case "🔍 Найти собеседника":

                        if (user.IsActive)
                        {
                            await client.SendMessage(user.UserId, "Вы уже в чате. Напишите /stop, чтобы выйти.");
                            break;
                        }
                        user.IsSearch = true;
                        var partner = chatUsers.Values.FirstOrDefault(u =>u.UserId != user.UserId &&u.IsSearch &&!u.IsActive &&!u.IsMale);

                        if (partner != null)
                        {
                            // Соединяем
                            user.PartnerId = partner.UserId;
                            partner.PartnerId = user.UserId;

                            user.IsActive = true;
                            partner.IsActive = true;

                            user.IsSearch = false;
                            partner.IsSearch = false;

                            await client.SendMessage(user.UserId, "🎉 Собеседник найден! Напишите сообщение.");
                            await client.SendMessage(partner.UserId, "🎉 Собеседник найден! Напишите сообщение.");
                            SaveUsers();
                        }
                        else
                        {
                            await client.SendMessage(user.UserId, "Поиск... Как только собеседник будет найден, вы получите сообщение.");
                        }
                        break;
                    case "/messages":
                    case "✉ Кол-во Сообщений":
                        await client.SendMessage(message.Chat.Id, $"📨 Столько сообщений у вас осталось: {user.MessageLimit}");
                        break;
                    case "/premium":
                    case "🌟 Премиум доступ":
                        break;
                    case "/buyMessages":
                    case "📩 Ещё сообщения":
                        break;
                    case "/premiumInfo":
                    case "💫 Что дает премиум доступ?":
                        break;
                    case "/stop":
                    case "🛑 Завершить чат":
                        if (user.PartnerId != null)
                        {
                            var currentPartner = GetChatUser(user.PartnerId.Value);
                            await client.SendMessage(currentPartner.UserId, "❌ Собеседник вышел из чата.");

                            currentPartner.IsActive = false;
                            currentPartner.PartnerId = null;
                            currentPartner.IsSearch = false;
                        }

                        user.IsActive = false;
                        user.PartnerId = null;
                        user.IsSearch = false;

                        await client.SendMessage(user.UserId, "Вы вышли из чата.");
                        SaveUsers();
                        break;
                    default:
                        if(user.LogState != 0)
                        {
                            switch(user.LogState)
                            {
                                case 1:
                                    if(message.Text == "👱‍♂️ Мужской")
                                    {
                                        user.IsMale = true;
                                        user.LogState = 0;
                                        user.IsLogged = true;
                                        await client.SendMessage(message.Chat.Id, "💯 Отлично! Вы можете начать поиск собеседника 👌");

                                        if (!IsAdmin(message.Chat.Id))
                                        {
                                            await client.SendMessage(message.Chat.Id, $"{commands}", replyMarkup: keyboard);
                                        }
                                        else
                                        {
                                            await client.SendMessage(message.Chat.Id, $"{commands}", replyMarkup: AdminKeyboard);
                                        }
                                        SaveUsers();
                                    }
                                    else if(message.Text == "👱‍♀️ Женский")
                                    {
                                        user.IsMale = false;
                                        user.LogState = 0;
                                        user.IsLogged = true;
                                        await client.SendMessage(message.Chat.Id, "💯 Отлично! Вы можете начать поиск собеседника 👌");

                                        if (!IsAdmin(message.Chat.Id))
                                        {
                                            await client.SendMessage(message.Chat.Id, $"{commands}", replyMarkup: keyboard);
                                        }
                                        else
                                        {
                                            await client.SendMessage(message.Chat.Id, $"{commands}", replyMarkup: AdminKeyboard);
                                        }
                                        SaveUsers();
                                    }
                                    else
                                    {
                                        await client.SendMessage(message.Chat.Id, "🙈 Нужно выбрать один из вариантов снизу 👇");
                                    }
                                        break;
                            }
                        }
                        else if(user.IsActive)
                        {
                            if (user.PartnerId == null)
                            {
                                user.IsActive = false;
                                return;
                            }    
                            if (message.Text == null)
                            {
                                await client.SendMessage(message.Chat.Id, "Можно отправлять только текст! Фото, Видео, Аудио, Гифки, Стикеры будут проигнорированы 🚫");
                                return;
                            }    

                            if(user.MessageLimit > 0)
                            {
                                await client.SendMessage(user.PartnerId, message.Text);
                                user.MessageLimit--;
                            }
                            else
                            {
                                await client.SendMessage(message.Chat.Id, "😱 У вас закончились сообщения. Вы можете приобрести их вводя команду: /buyMessages");
                            }
                        }
                        else
                            await client.SendMessage(message.Chat.Id, "😔 Извините, я не понимаю эту команду 🤖\nВведите /help, чтобы посмотреть список доступных команд.");
                        break;
                }
            }
        }

        // Обработка ошибок которых не должно быть в проекте :D
        private static Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
        private static async Task SkipOldUpdates()
        {
            try
            {
                var updates = await client.GetUpdates();
                if (updates.Any())
                {
                    // Устанавливаем offset на следующий после последнего UpdateId
                    var lastUpdateId = updates.Last().Id;
                    await client.GetUpdates(offset: lastUpdateId + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке offset: {ex.Message}");
            }
        }
        
        static async void AddAdmin(long chatId, long adminId)
        {
            if (adminIds.Contains(adminId))
            {
                await client.SendMessage(chatId, $"👤 Пользователь с ID {adminId} уже является админом.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                return;
            }

            adminIds.Add(adminId);
            SaveAdminsToFile(); // если сохраняешь в JSON
            await client.SendMessage(chatId, $"✅ Пользователь с ID {adminId} добавлен в список админов.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }
        static async void RemoveAdmin(long chatId, long adminId)
        {
            if (adminIds.Contains(adminId))
            {
                adminIds.Remove(adminId);
                SaveAdminsToFile();
                await client.SendMessage(chatId, $"✅ Пользователь с ID {adminId} удален из списка Админов.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                return;
            }
            else
                await client.SendMessage(chatId, $"✅ Пользователя с ID {adminId} нет в списке Администраторов.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }
        static void SaveAdminsToFile()
        {
            string json = JsonSerializer.Serialize(adminIds);
            System.IO.File.WriteAllText("admins.json", json);
        }
        static void LoadAdminsFromFile()
        {
            string path = "admins.json";

            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<long>>(json);
                if (loaded != null)
                    adminIds = loaded;
            }
        }
        public static void SaveUsers()
        {
            var json = JsonSerializer.Serialize(chatUsers);
            System.IO.File.WriteAllText("chat_users.json", json);
        }
        public static void LoadUsers()
        {
            if (System.IO.File.Exists("chat_users.json"))
            {
                var json = System.IO.File.ReadAllText("chat_users.json");
                chatUsers = JsonSerializer.Deserialize<Dictionary<long, ChatUser>>(json)
                            ?? new Dictionary<long, ChatUser>();
            }
        }
    }
}