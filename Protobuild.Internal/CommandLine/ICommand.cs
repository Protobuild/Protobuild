using System;

namespace Protobuild
{
    internal interface ICommand
    {
        void Encounter(Execution pendingExecution, string[] args);

        int Execute(Execution execution);

        string GetShortCategory();

        string GetShortDescription();

        string GetDescription();

        int GetArgCount();

        string[] GetShortArgNames();

        string[] GetArgNames();

        bool IsInternal();

        bool IsRecognised();

        bool IsIgnored();
    }
}

