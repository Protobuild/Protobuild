namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class TreatWarningsAsErrorsPropertyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public TreatWarningsAsErrorsPropertyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("TreatWarningsAsErrorsProperty");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            _assert.Contains(@"<TreatWarningsAsErrors>TREAT_WARNINGS_AS_ERRORS_TEST</TreatWarningsAsErrors>", projectContents);
        }
    }
}