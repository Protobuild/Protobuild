using Xunit;
using Protobuild.Services;
using System.Collections.Generic;

namespace Protobuild.Tests
{
    public class ServiceResolutionTests
    {
        [Fact]
        public void MultiPassResolutionDoesNotIntroduceConflictingRequirements()
        {
            var a = new Service
            {
                ProjectName = "Test",
                ServiceName = "A",
                DesiredLevel = ServiceDesiredLevel.Recommended,
                Requires = new List<string> { "Test/B" },
            };
            var b = new Service
            {
                ProjectName = "Test",
                ServiceName = "B",
            };
            var c = new Service
            {
                ProjectName = "Test",
                ServiceName = "C",
                Conflicts = new List<string> { "Test/A", "Test/B" },
            };

            var services = new List<Service> { a, b, c };

            var manager = new ServiceManager("Windows");
            manager.EnableService("Test/C");

            manager.EnableDefaultAndExplicitServices(services);

            // This should not throw an exception.
            var enabled = manager.ResolveServices(services);
            Assert.DoesNotContain(a, enabled);
            Assert.DoesNotContain(b, enabled);
            Assert.Contains(c, enabled);
        }

        [Fact]
        public void EnabledServiceWithConflictingDependencyDisablesDependentRecommendedService()
        {
            var a = new Service
            {
                ProjectName = "Test",
                ServiceName = "A",
                DesiredLevel = ServiceDesiredLevel.Recommended,
                Requires = new List<string> { "Test/B" },
            };
            var b = new Service
            {
                ProjectName = "Test",
                ServiceName = "B",
            };
            var c = new Service
            {
                ProjectName = "Test",
                ServiceName = "C",
                Conflicts = new List<string> { "Test/B" },
            };

            var services = new List<Service> { a, b, c };

            var manager = new ServiceManager("Windows");
            manager.EnableService("Test/C");

            manager.EnableDefaultAndExplicitServices(services);

            // This should not throw an exception.
            var enabled = manager.ResolveServices(services);
            Assert.DoesNotContain(a, enabled);
            Assert.DoesNotContain(b, enabled);
            Assert.Contains(c, enabled);
        }
    }
}

