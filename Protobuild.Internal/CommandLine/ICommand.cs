using System;

namespace Protobuild
{
    public interface ICommand
    {
        void Encounter(Execution pendingExecution, string[] args);

        int Execute(Execution execution);

        string GetDescription();

        int GetArgCount();

        string[] GetArgNames();
    }
}

