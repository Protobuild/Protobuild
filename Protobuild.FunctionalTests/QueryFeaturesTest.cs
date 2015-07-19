namespace Protobuild.Tests
{
    using Xunit;

    public class QueryFeaturesTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("QueryFeatures");

            var featureList = this.OtherMode("query-features", capture: true).Item1.Split('\n');
            var expectedFeatureList = new[] {
                "query-features",
            };

            Assert.Equal(expectedFeatureList, featureList);
        }
    }
}