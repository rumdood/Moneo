using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneo.Functions.Bot;

public class ContextFactory
{
    public BotContext RestoreContext(States state)
    {
        var currentState = RestoreState(state) ?? RestoreState(States.Main);
        var context = new BotContext(currentState);
        return context;
    }

    private static BotState RestoreState(States state)
        => state switch
        {
            States.Main => new MainState(),
            States.Responding => new CreatingTaskState(),
            _ => null
        };
}
