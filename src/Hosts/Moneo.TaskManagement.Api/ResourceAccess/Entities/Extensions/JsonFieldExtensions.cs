using System.Text.Json;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

internal static class JsonFieldExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    
    public static List<string> GetListFromJsonField(this IEntity entity, Func<(string?, List<string>?)> selectors)
    {
        var (json, internalList) = selectors();
        if (internalList is not null || json is null)
        {
            return internalList ??= [];
        }

        internalList = JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptions) ?? [];
        return internalList;
    }

    public static void SetJsonFieldFromList(this IEntity entity, Func<(string?, List<string>?)> selectors)
    {
        var (json, internalList) = selectors();
        var newJsonValue = internalList is null || internalList.Count == 0 ? null : JsonSerializer.Serialize(internalList, JsonSerializerOptions);

        if (!string.Equals(newJsonValue, json, StringComparison.OrdinalIgnoreCase))
        {
            json = newJsonValue;
        }
    }

    public static TValue? GetValueFromJson<TValue>(this IEntity entity, Func<(string?, TValue?)> selectors)
    {
        var (json, internalValue) = selectors();

        if (internalValue is not null)
        {
            return internalValue;
        }

        internalValue = string.IsNullOrEmpty(json)
            ? default
            : JsonSerializer.Deserialize<TValue>(json, JsonSerializerOptions) ?? default;

        return internalValue;
    }
    
    public static void SetJsonFieldFromValue<TValue>(this IEntity entity, Func<(string?, TValue?)> selectors)
    {
        var (json, internalValue) = selectors();
        var newJsonValue = internalValue is null ? null : JsonSerializer.Serialize(internalValue, JsonSerializerOptions);

        if (!string.Equals(newJsonValue, json, StringComparison.OrdinalIgnoreCase))
        {
            json = newJsonValue;
        }
    }
}