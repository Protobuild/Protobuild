using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Prototest.Library.Version11;

namespace Protobuild.FunctionalTests.Regression
{
    public class RegressionHtmlSummaryConsumer : ITestSummaryConsumer
    {
        public void HandleResults(List<TestResult> results)
        {
            var resultsBySet = results.GroupBy(x => x.Set).Where(g => g.Key.Name.StartsWith("regression-")).ToList();

            if (resultsBySet.Count == 0)
            {
                return;
            }

            var columns = resultsBySet.First().Select(x => x.Entry.TestClass).ToArray();

            Func<string, string> nameTranslate = x =>
            {
                switch (x)
                {
                    case "ExampleMonoGameTest":
                        return "MG";
                    case "ExampleCSToolsTest":
                        return "CS";
                    case "ExampleCocos2DXNATest":
                        return "CO";
                    default:
                        return string.Empty;
                }
            };

            var content = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
<link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css"" integrity=""sha384-1q8mTJOASx8j1Au+a5WDVnPi2lkFfwwEAa8hDDdjZlpLegxhjVME1fgjWPGmkzs7"" crossorigin=""anonymous"">
<link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/font-awesome/4.5.0/css/font-awesome.min.css"">
<style type=""text/css"">
#container { max-width: 800px; margin: auto; }
.mcol { text-align: center; width:0px; width: 47px; padding-left: 0px; padding-right: 0px; }
.timestamp { width: 160px; }
.commit { width: 100px; }
</style>
</head>
<body>
<div id=""container"">
<h1>Protobuild Regression Test Suite</h1>
<p>
    Legend:
</p>
<ul>
    <li><strong>MG</strong>: Test a structure similar to the <a href=""https://github.com/mono/MonoGame"">MonoGame</a> repository</li>
    <li><strong>CS</strong>: Test a structure similar to the <a href=""https://github.com/hach-que/cstools"">C# Tools</a> repository</li>
    <li><strong>CO</strong>: Test a structure similar to the <a href=""https://github.com/Cocos2DXNA/cocos2d-xna"">Cocos2D XNA</a>  repository</li>
    <li><i class='fa fa-check-circle' style='color: #090;'></i>: Test Passed</li>
    <li><i class='fa fa-dot-circle-o' style='color: #F90;'></i>: Expected Failure (not compatible in this scenario)</li>
    <li><i class='fa fa-times-circle' style='color: #F00;'></i>: Test Failed</li>
</ul>
<table class=""table""><tr><th colspan=""3"">Regression Set</th>";
            content += "<th colspan=\"" + columns.Length + @""" width=""0"" style=""text-align: center;padding-left:0px; padding-right:0px;"">Latest as Child</th>";
            content += "<th colspan=\"" + columns.Length + @""" width=""0"" style=""text-align: center;padding-left:0px; padding-right:0px;"">Latest as Parent</th>";
            content += "</tr><tr>";
            content += "<th colspan=\"3\">&nbsp;</th>";
            foreach (var column in columns)
            {
                content += "<th class=\"mcol\">" + nameTranslate(column.Name) + "</th>";
            }
            foreach (var column in columns)
            {
                content += "<th class=\"mcol\">" + nameTranslate(column.Name) + "</th>";
            }
            content += "</tr>";

            var resultsByCommitHash = resultsBySet.GroupBy(x => x.Key.Name.Split('-')[3]).ToList();

            var directoryInfo = new FileInfo(typeof(RegressionTestSetProvider).Assembly.Location).Directory?.Parent?.Parent?.Parent?.Parent;

            foreach (var set in resultsByCommitHash.OrderByDescending(x => x.First().Key.Name))
            {
                content += "<tr>";
                var sp = set.First().Key.Name.Split('-');
                if (sp[3] == "latest")
                {
                    content += "<td class=\"timestamp\"></td>";
                }
                else
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
                    var c = epoch.AddSeconds(int.Parse(sp[2]));
                    content += "<td class=\"timestamp\">" + c.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + "</td>";
                }

                if (set.Key == "latest")
                {
                    content += "<td class=\"commit\">" + set.Key + "</td>";
                    content += "<td class=\"message\"></td>";
                }
                else
                {
                    content += "<td class=\"commit\"><a href=\"\">" + set.Key.Substring(0, 9) + "</td>";
                    var message = string.Empty;

                    if (directoryInfo != null)
                    {
                        var messageFile =
                            new FileInfo(Path.Combine(directoryInfo.FullName, "PreviousVersions", set.Key.Split('.').First() + ".txt"));
                        if (messageFile.Exists)
                        {
                            using (var reader = new StreamReader(messageFile.FullName))
                            {
                                message = reader.ReadToEnd().Trim();
                            }
                        }
                    }

                    content += "<td class=\"message\">" + SecurityElement.Escape(message) + "</td>";
                }

                var parent = set.FirstOrDefault(x => x.Key.Name.Contains("parent") || x.Key.Name.Contains("latest"));

                if (parent != null)
                {
                    var passSet = parent.ToDictionary(k => k.Entry.TestClass, v => v);
                    foreach (var column in columns)
                    {
                        if (passSet[column].Passed)
                        {
                            content += "<td class=\"mcol\"><i class='fa fa-check-circle' style='color: #090;'></i></td>";
                        }
                        else if (passSet[column].Entry.AllowFail)
                        {
                            content += "<td class=\"mcol\"><i class='fa fa-dot-circle-o' style='color: #F90;'></i></td>";
                        }
                        else
                        {
                            content += "<td class=\"mcol\"><i class='fa fa-times-circle' style='color: #F00;'></i></td>";
                        }
                    }
                }

                var child = set.FirstOrDefault(x => x.Key.Name.Contains("child") || x.Key.Name.Contains("latest"));

                if (child != null)
                {
                    var passSet = child.ToDictionary(k => k.Entry.TestClass, v => v);
                    foreach (var column in columns)
                    {
                        if (passSet[column].Passed)
                        {
                            content += "<td class=\"mcol\"><i class='fa fa-check-circle' style='color: #090;'></i></td>";
                        }
                        else if (passSet[column].Entry.AllowFail)
                        {
                            content += "<td class=\"mcol\"><i class='fa fa-dot-circle-o' style='color: #F90;'></i></td>";
                        }
                        else
                        {
                            content += "<td class=\"mcol\"><i class='fa fa-times-circle' style='color: #F00;'></i></td>";
                        }
                    }
                }

                content += "</tr>";
            }

            content += "</table></div></body></html>";

            using (var writer = new StreamWriter("RegressionResults.html"))
            {
                writer.Write(content);
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(content);
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
