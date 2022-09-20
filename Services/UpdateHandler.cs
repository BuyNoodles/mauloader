using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
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

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, ITikTokRepository tikTokRepository)
    {
        _botClient = botClient;
        _logger = logger;
        _tikTokRepository = tikTokRepository;
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

        var action = messageText.Split(' ')[0] switch
        {
            "/photo" => SendFile(_botClient, message, cancellationToken),
            //"/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
            "/chuck" => Chuck(_botClient, message, cancellationToken),
            "/tiktok" => Tiktok(_botClient, message, cancellationToken),
            //"/throw" => FailingHandler(_botClient, message, cancellationToken),
            _ => Usage(_botClient, message, cancellationToken)
        };
        Message sentMessage = await action;
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
            const string usage = "Usage:\n" +
                                 "/tiktok `[url]`   `TikTok video without watermark`\n" +
                                 "/photo            `Sends a photo`\n" +
                                 "/chuck            `Chuck Norris joke`\n";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                parseMode: ParseMode.MarkdownV2,
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
            string[] url = message.Text.Split(" ");

            if (url.Length < 2 || !url[1].StartsWith("https://vm.tiktok.com/"))
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Usage: /tiktok `[url]`",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            TikTok content = await _tikTokRepository.FetchVideo(url[1]);

            // Uncomment to save video to server, might be unnecessary
            // Only useful if you want to hoard and archive them

            //bool download = Task.Run(async () => await TikTokRepository.DownloadVideo(content.DownloadLink, $"{content.Id}.mp4")).Result;

            //Console.WriteLine("---------------------\n\n" + content.DownloadLink + $"\n\n**********************");

            return await botClient.SendVideoAsync(
                    chatId: message.Chat.Id,
                    video: content.DownloadLink,
                    caption: $"{content.AuthorName} - {content.CreatedTime}\n" +
                             $"❤️: {content.LikeCount.FormatNumber()}\n" +
                             $"💬: {content.CommentCount.FormatNumber()}\n" +
                             $"🔗: {content.ShareCount.FormatNumber()}",
                    supportsStreaming: true,
                    cancellationToken: cancellationToken);
        }
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
