namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class FeatureWarningIsShownWhenPreviousFeatureIsMissingTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public FeatureWarningIsShownWhenPreviousFeatureIsMissingTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("FeatureWarningIsShownWhenPreviousFeatureIsMissing");

            var output = this.Generate(capture: true);

            _assert.Contains("WARNING: The active feature set is missing previous features!", output.Item2);
        }
    }
}