using System;

namespace Moneo.Functions.Bot;

public enum States
{
    Main,
    Completing,
    Skipping,
    Canceling,
    Responding
}

public record StateAction(BotResponse Response = default, string Activity = default);

public static class Commands
{
    public const string Cancel = "/cancel";
    public const string CompleteTask = "/completeTask";
    public const string SkipTask = "/skipTask";
    public const string CancelTask = "/endTask";
}

internal class BotCommand
{
    public long ChatId { get; private set; }
    public string Command { get; private set; }
    public string Argument { get; private set;}

    public static BotCommand ParseMessage(long forChatId, string message)
    {
        var parts = message.Trim().Split(" ");

        if (parts.Length < 2)
        {
            throw new InvalidOperationException("You must supply an argument");
        }

        return new BotCommand
        {
            Command = parts[0],
            Argument = parts[1]
        };
    }
}

public abstract class BotState
{
    public abstract States Type { get; }

    protected BotContext _context;

    public void SetContext(BotContext context)
    {
        _context = context;
    }

    public abstract StateAction GetAction(string messageText);
}

public class CreatingTaskState : BotState
{
    public override States Type => States.Responding;

    public override StateAction GetAction(string messageText)
    {
        var message = messageText.Trim();

        if (message.Equals(Commands.Cancel, StringComparison.OrdinalIgnoreCase))
        {
            _context.TransitionTo(new MainState());
            return new StateAction(new BotResponse("Cancelled"));
        }

        return new StateAction();
    }
}

public class CompletingTaskState : BotState
{
    public override States Type => States.Completing;

    public override StateAction GetAction(string messageText)
    {
        _context.TransitionTo(new MainState());
        return new StateAction(Activity: nameof(ActivityFunctions.ActivityCompleteMoneoTask));
    }
}

public class MainState : BotState
{
    public override States Type => States.Main;

    public override StateAction GetAction(string messageText)
    {
        return messageText.Trim() switch
        {
            string cmd when cmd.Equals(Commands.CompleteTask, StringComparison.OrdinalIgnoreCase) => 
                new StateAction(Response: new BotResponse("This should complete a given task")),
            string cmd when cmd.Equals(Commands.SkipTask, StringComparison.OrdinalIgnoreCase) => 
                new StateAction(Response: new BotResponse("This should skip a given task")),
            string cmd when cmd.Equals(Commands.CancelTask, StringComparison.OrdinalIgnoreCase) =>
                new StateAction(Response: new BotResponse("This should cancel/disable an existing task")),
            _ => throw new InvalidOperationException("Unknown command")
        };
    }
}
