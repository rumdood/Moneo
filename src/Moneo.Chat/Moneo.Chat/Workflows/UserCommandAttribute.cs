namespace Moneo.Chat;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class UserCommandAttribute : Attribute
{
    public string CommandKey { get; init; }
    public string HelpDescription { get; init; } = "";

    public UserCommandAttribute()
    {
    }

    public UserCommandAttribute(string commandKey)
    {
        CommandKey = commandKey;
    }

    public UserCommandAttribute(string commandKey, string helpDescription)
    {
        CommandKey = commandKey;
        HelpDescription = helpDescription;
    }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public class UserCommandArgument : Attribute
{
    public bool IsRequired { get; init; } = false;
    public string HelpText { get; init; } = "";
    public bool IsHidden { get; init; } = false;
    public string ShortName { get; init; } = "";
    public string LongName { get; init; }

    public UserCommandArgument()
    {
    }

    public UserCommandArgument(string shortName, string longName, string helpText, bool isRequired = false, bool isHidden = false)
    {
        LongName = longName;
        ShortName = shortName;
        HelpText = helpText;
        IsRequired = isRequired;
        IsHidden = isHidden;
    }

    public UserCommandArgument(string longName, bool isRequired = false, bool isHidden = false) : 
        this(string.Empty, longName, string.Empty, isRequired, isHidden)
    {
    }

    public UserCommandArgument(char shortName, bool isRequired = false, bool isHidden = false) : 
        this(shortName.ToString(), string.Empty, string.Empty, isRequired, isHidden)
    {
    }

    public UserCommandArgument(string longName, string helpText, bool isRequired = false, bool isHidden = false) :
        this(string.Empty, longName, helpText, isRequired, isHidden)
    {
    }
}

public enum UserCommandType
{
    Internal,
    External,
}
