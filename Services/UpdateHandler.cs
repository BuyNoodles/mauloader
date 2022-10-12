using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using mauloader_bot.Entities;
using mauloader_bot.Interfaces;

namespace Telegram.Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ITikTokRepository _tikTokRepository;
    private readonly IRedditRepository _redditRepository;
    private readonly IConfiguration _config;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger,
        ITikTokRepository tikTokRepository, IRedditRepository redditRepository, IConfiguration config)
    {
        _botClient = botClient;
        _logger = logger;
        _tikTokRepository = tikTokRepository;
        _redditRepository = redditRepository;
        _config = config;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        Message action;

        if (messageText.StartsWith("https://vm.tiktok.com/"))
        {
            action = await Tiktok(_botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("https://www.reddit.com/r/"))
        {
            action = await Reddit(_botClient, message, cancellationToken);
        }
        else
        {
            action = messageText.Split(' ')[0] switch
            {
                "/mau" => await SendFile(_botClient, message, cancellationToken),
                // "/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
                "/chuck" => await Chuck(_botClient, message, cancellationToken),
                "/tiktok" => await Tiktok(_botClient, message, cancellationToken),
                "/reddit" => await Reddit(_botClient, message, cancellationToken),
                // "/throw" => FailingHandler(_botClient, message, cancellationToken),
                _ => await Usage(_botClient, message, cancellationToken)
            };
        }
        Message sentMessage = action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendChatActionAsync(
                message.Chat.Id,
                ChatAction.UploadPhoto,
                cancellationToken: cancellationToken);

            const string filePath = @"Files/mau.jpg";
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

            return await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputOnlineFile(fileStream, fileName),
                caption: "MAUU",
                cancellationToken: cancellationToken);
        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            const string usage = "<b>Usage</b>:\n\n" +
                                 "/tiktok - TikTok video without watermark\n" +
                                 "/reddit - Reddit videos\n" +
                                 "/mau    - Mau\n" +
                                 "/chuck  - Chuck Norris joke\n\n" +
                                 "Or send me Tiktok or Reddit links without commands";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                parseMode: ParseMode.Html,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> Chuck(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            HttpClient req = new HttpClient();
            string content = await req.GetStringAsync("https://api.chucknorris.io/jokes/random");
            var joke = JsonSerializer.Deserialize<Joke>(content);

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"{joke?.Content}",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        async Task<Message> Tiktok(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            /* *
             * Fetches api and sends TikTok video to user/group
             * Can be used with and without command
             * ie.
             * 
             * https://vm.tiktok.com...
             * or /tiktok https://tiktok.com....
             * 
             */

            string errorMessage = "*Usage*: /tiktok `[url]`";

            string url = CheckUrl("https://vm.tiktok.com", message);

            if (url == "invalid")
            {
                return await SendMessage(botClient, message, cancellationToken, errorMessage);
            }

            Message initialMessage = await SendMessage(botClient, message,
                cancellationToken, "Searching");

            TikTok content = await _tikTokRepository.FetchVideo(url);

            // Uncomment to save video to server, might be unnecessary
            // Only useful if you want to hoard and archive them

            //bool download = Task.Run(async () => await TikTokRepository.DownloadVideo(content.DownloadLink, $"{content.Id}.mp4")).Result;

            message = await botClient.SendVideoAsync(
                    chatId: message.Chat.Id,
                    video: content.DownloadLink,
                    caption: $"{content.AuthorName} - {content.CreatedTime}\n" +
                             $"❤️: {content.LikeCount.FormatNumber()}\n" +
                             $"💬: {content.CommentCount.FormatNumber()}\n" +
                             $"🔗: {content.ShareCount.FormatNumber()}",
                    supportsStreaming: true,
                    cancellationToken: cancellationToken);

            await DeleteMessage(botClient, initialMessage, cancellationToken);

            return message;
        }
    }

    async Task<Message> Reddit(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        /* *
         * Same as Tiktok
         */

        string errorMessage = "*Usage*: /reddit `[url]`";

        string url = CheckUrl("https://www.reddit.com/r/", message);

        if (url == "invalid")
        {
            return await SendMessage(botClient, message, cancellationToken, errorMessage);
        }

        Message initialMessage = await SendMessage(botClient, message,
            cancellationToken, "Downloading Reddit video");

        bool downloadVideo = _redditRepository.FetchVideo(url);

        var compress = _config["BotConfiguration:CompressVideo"];

        if (Convert.ToBoolean(compress))
        {
            await EditMessage(botClient, initialMessage, cancellationToken, "Compressing video");
            bool compressedVideo = _redditRepository.CompressVideo();
        }

        await EditMessage(botClient, initialMessage, cancellationToken, "Uploading video");

        await using (Stream stream = System.IO.File.OpenRead(@"Files\Reddit\reddit.mp4"))
        {
            Message m = await botClient.SendVideoAsync(
                             chatId: message.Chat.Id,
                             video: new InputMedia(stream, "reddit.mp4"),
                             supportsStreaming: true,
                             cancellationToken: cancellationToken);
        }

        await DeleteMessage(botClient, initialMessage, cancellationToken);

        return message;
    }

    private static async Task<Message> SendMessage(ITelegramBotClient botClient,
        Message message, CancellationToken cancellationToken, string messageContent)
    {
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: messageContent,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    private static async Task<Message> EditMessage(ITelegramBotClient botClient, Message initialMessage, CancellationToken cancellationToken, string messageContent)
    {
        return await botClient.EditMessageTextAsync(
                chatId: initialMessage.Chat.Id,
                messageId: initialMessage.MessageId,
                text: messageContent,
                cancellationToken: cancellationToken);
    }

    private static async Task DeleteMessage(ITelegramBotClient botClient, Message initialMessage, CancellationToken cancellationToken)
    {
        await botClient.DeleteMessageAsync(
                chatId: initialMessage.Chat.Id,
                messageId: initialMessage.MessageId,
                cancellationToken: cancellationToken
            );
    }

    private static string CheckUrl(string targetUrl, Message message)
    {
        string url = message.Text;
        string[] command = message.Text.Split(" ");

        // check if message sent is a command, if it is check if it has arguments
        if (url.StartsWith("/") && command.Length > 1)
        {
            url = url.Split(" ")[1];
        }

        if (!url.StartsWith(targetUrl))
        {
            return "invalid";
        }

        return url;
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
