using System.Text.Json.Serialization;

namespace RPPP_WebApp.ViewModels
{
  public class MessageType
  {
    public const string Success = "success";
    public const string Info = "success";
    public const string Warning = "warning";
    public const string Error = "error";
  } 
  public record ActionResponseMessage([property:JsonPropertyName("messageType")] string MessageType, [property: JsonPropertyName("message")]  string Message);
}
