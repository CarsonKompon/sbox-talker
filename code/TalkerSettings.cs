using Sandbox;

namespace Talker;

public static class TalkerSettings
{

    public static ChatSettings Chat
    {
        get
        {
            if (_chatSettings is null)
            {
                var file = "/settings/chat.json";
                _chatSettings = FileSystem.Data.ReadJson(file, new ChatSettings());
            }
            return _chatSettings;
        }
    }
    static ChatSettings _chatSettings;

}

public class ChatSettings
{
    public bool ShowAvatars { get; set; } = true;
    public int FontSize { get; set; } = 16;
    public bool ChatSounds { get; set; } = true;
}