using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prototest.Library.Version11;

namespace Protobuild.FunctionalTests.Regression
{
    public class RegressionTestSetProvider : ITestSetProvider
    {
        public List<TestSet> GetTestSets(List<TestInputEntry> classes, Dictionary<Type, object> assertTypes)
        {
            var sets = new List<TestSet>();

            sets.Add(new TestSet
            {
                Name = "regression-Protobuild-9999999999-latest",
                Entries = classes.SelectMany(x => x.TestMethods.Select(
                    y => new { x.Type, x.Constructor, TestMethod = y }))
                    .Where(x => x.TestMethod.GetParameters().Length == 2 && x.TestMethod.GetParameters().All(y => y.ParameterType == typeof(string)))
                    .Select(x => new TestSetEntry
                    {
                        TestClass = x.Type,
                        TestConstructor = x.Constructor,
                        TestMethod = x.TestMethod,
                        RunTestMethod =
                            obj =>
                                ((Action<string, string>)
                                    Delegate.CreateDelegate(typeof(Action<string, string>), obj,
                                        x.TestMethod))(null, null),
                        AllowFail = false,
                    }).ToList()
            });

            var directoryInfo = new FileInfo(typeof(RegressionTestSetProvider).Assembly.Location).Directory?.Parent?.Parent?.Parent?.Parent;

            if (directoryInfo != null)
            {
                var previousVersions = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "PreviousVersions"));
                if (previousVersions.Exists)
                {
                    foreach (var exe in previousVersions.GetFiles("*.exe"))
                    {
                        var components = exe.Name.Split('-');
                        var allowFail = int.Parse(components[1]) <= 1443619291;

                        sets.Add(new TestSet
                        {
                            Name = "regression-" + exe.Name + "-parent",
                            Entries = classes.SelectMany(x => x.TestMethods.Select(
                                y => new {x.Type, x.Constructor, TestMethod = y}))
                                .Where(x => x.TestMethod.GetParameters().Length == 2 && x.TestMethod.GetParameters().All(y => y.ParameterType == typeof(string)))
                                .Select(x => new TestSetEntry
                                {
                                    TestClass = x.Type,
                                    TestConstructor = x.Constructor,
                                    TestMethod = x.TestMethod,
                                    RunTestMethod =
                                        obj =>
                                            ((Action<string, string>)
                                                Delegate.CreateDelegate(typeof (Action<string, string>), obj,
                                                    x.TestMethod))(exe.FullName, null),
                                    AllowFail = allowFail,
                                }).ToList()
                        });
                        sets.Add(new TestSet
                        {
                            Name = "regression-" + exe.Name + "-child",
                            Entries = classes.SelectMany(x => x.TestMethods.Select(
                                y => new {x.Type, x.Constructor, TestMethod = y}))
                                .Where(x => x.TestMethod.GetParameters().Length == 2 && x.TestMethod.GetParameters().All(y => y.ParameterType == typeof(string)))
                                .Select(x => new TestSetEntry
                                {
                                    TestClass = x.Type,
                                    TestConstructor = x.Constructor,
                                    TestMethod = x.TestMethod,
                                    RunTestMethod =
                                        obj =>
                                            ((Action<string, string>)
                                                Delegate.CreateDelegate(typeof (Action<string, string>), obj,
                                                    x.TestMethod))(null, exe.FullName),
                                    AllowFail = false,
                                }).ToList()
                        });
                    }
                }
            }

            return sets;
        }
    }
}
