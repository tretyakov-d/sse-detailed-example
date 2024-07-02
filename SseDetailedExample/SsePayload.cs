using System.Text;

namespace SseDetailedExample;

public class SsePayload
{
    public string? Id { get; init; }
    public string? EventName { get; init; }
    public string? Data { get; init; }
    public int? RetryInterval { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (RetryInterval != null)
            sb.Append("retry: ").Append(RetryInterval).Append('\n');

        if (EventName != null)
            sb.Append("event: ").Append(EventName).Append('\n');

        if (Data != null)
            foreach (var line in Data.Split('\n'))
                sb.Append("data: ").Append(line).Append('\n');

        if (Id != null)
            sb.Append("id: ").Append(Id).Append('\n');

        sb.Append('\n'); // finalize event

        return sb.ToString();
    }
}