using System;

namespace Protobuild
{
    internal interface ICommand
    {
        void Encounter(Execution pendingExecution, string[] args);

        int Execute(Execution execution);

        string GetDescription();

        int GetArgCount();

        string[] GetArgNames();

        bool IsInternal();

        bool IsRecognised();

        bool IsIgnored();
    }
}

