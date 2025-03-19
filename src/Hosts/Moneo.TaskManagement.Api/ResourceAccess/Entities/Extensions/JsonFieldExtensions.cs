using System.Text.Json;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

/*
public class JsonField<TValue> where TValue : class
{
    private readonly Func<string?> _getJson;
    private readonly Action<string?> _setJson;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public JsonField(Func<string?> getJson, Action<string?> setJson)
    {
        _getJson = getJson;
        _setJson = setJson;
    }

    public TValue? Value
    {
        get
        {
            var json = _getJson();
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<TValue>(json, _jsonSerializerOptions);
        }
        set
        {
            var json = value is null ? null : JsonSerializer.Serialize(value, _jsonSerializerOptions);
            _setJson(json);
        }
    }
}
*/

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

    public static void SetJsonFieldFromList(
        this IEntity entity, 
        Func<(string? jsonField, List<string>? list)> selectors, 
        Action<string?> updateJsonField)
    {
        var (json, internalList) = selectors();
        var newJsonValue = internalList is null || internalList.Count == 0 ? null : JsonSerializer.Serialize(internalList, JsonSerializerOptions);

        if (!string.Equals(newJsonValue, json, StringComparison.OrdinalIgnoreCase))
        {
            updateJsonField(newJsonValue);
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
    
    public static void SetJsonFieldFromValue<TValue>(
        this IEntity entity, 
        Func<(string?, TValue?)> selectors,
        Action<string?> updateInternalValue)
    {
        var (json, internalValue) = selectors();
        var newJsonValue = internalValue is null ? null : JsonSerializer.Serialize(internalValue, JsonSerializerOptions);

        if (!string.Equals(newJsonValue, json, StringComparison.OrdinalIgnoreCase))
        {
            updateInternalValue(newJsonValue);
        }
    }
}