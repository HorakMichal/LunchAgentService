using Google.Apis.Auth.OAuth2;
using Google.Apis.HangoutsChat.v1;
using Google.Apis.HangoutsChat.v1.Data;
using Google.Apis.Services;

namespace LunchAgent.Core.Google;

public sealed class GoogleChatService : IGoogleChatService, IDisposable
{
    private readonly HangoutsChatService _chatService;

    public GoogleChatService(string credentials)
    {
        var scopes = new[] { "https://www.googleapis.com/auth/chat.bot" };

        var credential = CredentialFactory.FromJson<GoogleCredential>(credentials).CreateScoped(scopes);
        _chatService = new HangoutsChatService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MyApplication"
        });
    }
    
    public async Task<IReadOnlyCollection<Space>> GetSpaces()
    {
        return (await _chatService.Spaces.List().ExecuteAsync()).Spaces.ToList();
    }

    public async Task<Message> CreateMessage(Message message, string spaceName)
    {
        return await _chatService.Spaces.Messages.Create(message, spaceName).ExecuteAsync();
    }

    public async Task<Message> UpdateMessage(Message message, string spaceName)
    {
        var request = _chatService.Spaces.Messages.Update(message, spaceName);
        request.UpdateMask = "text";
        
        return await request.ExecuteAsync();
    }

    public void Dispose()
    {
        _chatService.Dispose();
    }
}