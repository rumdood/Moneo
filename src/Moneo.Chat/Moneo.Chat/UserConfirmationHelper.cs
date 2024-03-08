namespace Moneo.Chat;

public enum UserConfirmation
{
    Affirmative,
    Negative,
    Unknown,
}

public static class UserConfirmationHelper
{
    private static readonly HashSet<string> AffirmativeAnswers =
    [
        "yes", "y", "yes.", "yep", "yep.", "yeah", "yeah.", "sure", "sure.", "ok", "ok.", "okay", "okay.", "alright",
        "alright.", "fine", "fine.", "go ahead", "go ahead.", "go for it", "go for it.", "do it", "do it.", "do", "do.",
        "do that", "do that.", "do it!", "do it!", "do that!", "do that!", "do it now", "do it now.", "do that now",
        "do that now.", "do it now!", "do it now!", "do that now!", "do that now!", "yes please", "yes please.",
        "yes, please", "yes, please.", "please", "please.", "please do", "please do.", "please do that",
        "please do that.",
    ];

    private static readonly HashSet<string> NegativeAnswers =
    [
        "no", "n", "no.", "nope", "nope.", "nah", "nah.", "no thanks", "no thanks.", "no, thanks", "no, thanks.",
        "no thank you", "no thank you.", "no, thank you", "no, thank you.", "no thank you.", "no thank you.",
    ];
    
    public static UserConfirmation GetConfirmation(string userInput)
    {
        if (AffirmativeAnswers.Contains(userInput.ToLowerInvariant()))
        {
            return UserConfirmation.Affirmative;
        }

        if (NegativeAnswers.Contains(userInput.ToLowerInvariant()))
        {
            return UserConfirmation.Negative;
        }

        return UserConfirmation.Unknown;
    }
}