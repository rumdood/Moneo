namespace Moneo.Chat.BotResponses;

internal enum BotResponseType
{
    UnsureHowToProceed,
    RequestYesOrNo
}

internal static class BotResponseHelper
{
    private static readonly Random Random = new Random();

    private static readonly string[] UnsureHowToProceed =
    [
        "I'm sorry, I don't understand what you're trying to say. Please respond with a yes or no.",
        "I apologize, but I'm unsure how to proceed.",
        "I'm sorry, I'm not entirely sure how to handle this situation.",
        "I'm afraid I don't have a clear solution at the moment.",
        "Regrettably, I'm uncertain about the way forward.",
        "I apologize, but I'm unsure how to move forward from here.",
        "I'm sorry, but I'm not entirely sure what to do in this scenario.",
        "I apologize for any confusion, but I'm uncertain about the way forward.",
        "Unfortunately, I'm unsure how to address this issue.",
        "I'm sorry, but I'm not certain how to proceed further.",
        "Unfortunately, I'm not entirely sure what to do next.",
        "My apologies, but I'm unsure of the correct course of action.",
        "Unfortunately, I'm uncertain about the best way to proceed.",
        "I'm sorry, but I'm unsure how to address this matter.",
        "Regrettably, I'm not entirely sure what to do at this juncture.",
        "I apologize, but I'm at a loss for the next steps.",
        "Please excuse me, but I'm uncertain about the way forward.",
        "My apologies, but I'm lacking clarity on how to proceed.",
        "Unfortunately, I'm not certain about the best approach here.",
        "I'm sorry, but I'm not entirely sure what to do next.",
        "Please accept my apologies, but I'm unsure of the next steps.",
        "I apologize, but I'm uncertain about the appropriate action to take.",
        "My apologies, but I'm struggling to determine the best course of action.",
        "Unfortunately, I'm unsure how to proceed from here.",
        "I'm sorry, but I'm not certain about the way forward.",
        "Please forgive me, but I'm unsure of the best approach in this situation.",
        "I apologize, but I'm uncertain about the appropriate steps to take.",
        "My apologies, but I'm unsure what to do in this scenario.",
        "Unfortunately, I'm not entirely sure how to handle this particular circumstance.",
        "I'm sorry, but I'm uncertain about the appropriate response.",
        "Please excuse me, but I'm not confident in my ability to offer guidance.",
        "My apologies, but I'm unsure of the correct course of action.",
        "Unfortunately, I'm uncertain about the best way to proceed.",
        "I'm sorry, but I'm unsure how to address this matter.",
        "Regrettably, I'm not entirely sure what to do at this juncture.",
        "I apologize, but I'm at a loss for the next steps.",
        "Please excuse me, but I'm uncertain about the way forward."
    ];

    private static readonly string[] RequestYesOrNo =
    [
        "I'm sorry, I don't understand what you're trying to say. Please respond with a yes or no.",
        "I'm sorry, but could you please respond with a simple yes or no?",
        "Apologies, I'm looking for a straightforward yes or no answer.",
        "My apologies, I need a clear yes or no to proceed.",
        "I'm sorry, I'm specifically seeking a yes or no response.",
        "Sorry, I'm asking for a direct yes or no.",
        "I apologize, but I need a simple yes or no answer to move forward.",
        "Apologies, I'm seeking a clear yes or no reply.",
        "I'm sorry, could you please provide a yes or no answer?",
        "Sorry, I'm requesting a straightforward yes or no.",
        "I apologize, but I'm asking for a clear yes or no response.",
        "Apologies, I'm specifically asking for a yes or no.",
        "I'm sorry, but I need a yes or no answer to continue.",
        "Apologies, I'm waiting for a direct yes or no response.",
        "I'm sorry, but I'm looking for a simple yes or no.",
        "Apologies, could you please respond with either yes or no?",
        "I apologize, but I require a yes or no to proceed.",
        "I'm sorry, but I need a yes or no to move forward.",
        "Apologies, I'm seeking a yes or no reply.",
        "I apologize, but I'm asking for a yes or no to continue.",
        "I'm sorry, but I'm specifically seeking a yes or no answer."
    ];
    
    public static string GetBotResponse(BotResponseType type)
    {
        return type switch
        {
            BotResponseType.UnsureHowToProceed => UnsureHowToProceed[Random.Next(UnsureHowToProceed.Length)],
            BotResponseType.RequestYesOrNo => RequestYesOrNo[Random.Next(RequestYesOrNo.Length)],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}