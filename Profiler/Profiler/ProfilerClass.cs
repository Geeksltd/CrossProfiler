using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace Geeks.Profiler
{
    internal class ProfilerClass
    {
        private const string GetReportFileContentMethodBodyPlaceHolder = "// _GetReportFileContentMethodBodyPlaceHolder_";
        private const string ClearMethodBodyPlaceHolder = "// _ClearMethodBodyPlaceHolder_";
        private const string FieldsPlaceHolder = "// _FieldsPlaceHolder_";

        public Solution CreateClass(Solution solution, Uri webApi, ICollection<ProjectMethodsInfo> methodsInfo)
        {
            foreach (var projectMethodsInfo in methodsInfo)
            {
                var source = GetProfilerClassContent(webApi, projectMethodsInfo);
                var project = solution.GetProject(projectMethodsInfo.ProjectId);

                var doc = project.AddDocument("Profiler.cs", source);
                var docRoot = doc.GetSyntaxRootAsync().Result;
                docRoot = Formatter.Format(docRoot, solution.Workspace, solution.Workspace.Options);
                doc = doc.WithSyntaxRoot(docRoot);
                project = doc.Project;

                project = AddMetadataReferences(project);
                solution = project.Solution;
            }

            return solution;
        }

        private string GetProfilerClassContent(Uri webApi, ProjectMethodsInfo projectMethodsInfo)
        {
            var source = GetBaseClassBody(webApi);
            var fieldsBuilder = new StringBuilder();
            var getReportFileContent = new StringBuilder();
            var clearBody = new StringBuilder();

            foreach (var documentMethodInfo in projectMethodsInfo.DocumentMethodsInfo)
            {
                foreach (var method in documentMethodInfo.Methods)
                {
                    var variableName = MethodFullNameToVariableName(method.FullName);
                    fieldsBuilder.AppendLine($"public static long {variableName}_time;");
                    fieldsBuilder.AppendLine($"public static long {variableName}_count;");

                    getReportFileContent.AppendLine($"yield return GetReportFileLine(\"{method.FullName}\", {variableName}_time, {variableName}_count);");

                    clearBody.AppendLine($"{variableName}_time = 0;");
                    clearBody.AppendLine($"{variableName}_count = 0;");
                }
            }

            var getReportFileContentMethodBody = getReportFileContent.ToString();
            if (string.IsNullOrEmpty(getReportFileContentMethodBody))
            {
                getReportFileContentMethodBody = "yield break;";
            }

            return source.Replace(FieldsPlaceHolder, fieldsBuilder.ToString())
                .Replace(GetReportFileContentMethodBodyPlaceHolder, getReportFileContentMethodBody)
                .Replace(ClearMethodBodyPlaceHolder, clearBody.ToString());
        }

        private Project AddMetadataReferences(Project project)
        {
            var hasTheReference = false;
            foreach (var reference in project.MetadataReferences)
            {
                var fileInfo = new FileInfo(reference.Display);
                if (string.Equals(fileInfo.Name, "System.Net.Http.dll", StringComparison.OrdinalIgnoreCase))
                {
                    hasTheReference = true;
                    break;
                }
            }
            if (!hasTheReference)
            {
                var metadataReference = MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location);
                project = project.AddMetadataReferences(new List<MetadataReference> { metadataReference });
            }

            return project;
        }

        public string GetProfilerCallStatements(MethodInfo methodInfo, string originalMethodCallStatement)
        {
            var variableName = MethodFullNameToVariableName(methodInfo.FullName);

            if (!(methodInfo.IsAsync || methodInfo.IsEnumerable))
            {
                return $"Geeks.Profiler.Profiler.TotalMethodCallCount.Value++;{Environment.NewLine}" +
                         $"var totalMethodCallCount = Geeks.Profiler.Profiler.TotalMethodCallCount.Value;{Environment.NewLine}" +
                         $"var stopwatchTimer = new System.Diagnostics.Stopwatch();{Environment.NewLine}" +
                         $"stopwatchTimer.Start();{Environment.NewLine}" +
                         $"{originalMethodCallStatement}{Environment.NewLine}" +
                         $"stopwatchTimer.Stop();{Environment.NewLine}" +
                         $"var subMethodCallCount = Geeks.Profiler.Profiler.TotalMethodCallCount.Value - totalMethodCallCount;{Environment.NewLine}" +
                         $"Geeks.Profiler.Profiler.ReportMethodRun(ref Geeks.Profiler.Profiler.{variableName}_time, ref Geeks.Profiler.Profiler.{variableName}_count, stopwatchTimer.ElapsedTicks, subMethodCallCount);{Environment.NewLine}";
            }

            return $"var stopwatchTimer = new System.Diagnostics.Stopwatch();{Environment.NewLine}" +
                         $"stopwatchTimer.Start();{Environment.NewLine}" +
                         $"{originalMethodCallStatement}{Environment.NewLine}" +
                         $"stopwatchTimer.Stop();{Environment.NewLine}" +
                         $"Geeks.Profiler.Profiler.ReportMethodRun(ref Geeks.Profiler.Profiler.{variableName}_time, ref Geeks.Profiler.Profiler.{variableName}_count, stopwatchTimer.ElapsedTicks, 0);{Environment.NewLine}";
        }

        private string MethodFullNameToVariableName(string methodFullName)
        {
            var replacePatterns = new string[][] {
                new[] { "()", "" },
                new[] { ".", "_" },
                new[] { "(", "_" },
                new[] { ",", "_" },
                new[] { "<", "_" },
                new[] { ")", "" },
                new[] { "[", "_array" },
                new[] { "]", "" },
                new[] { ">", "" },
                new[] { "?", "_nullable" },
                new[] { " ", "" },
            };

            foreach (var replacePattern in replacePatterns)
            {
                methodFullName = methodFullName.Replace(replacePattern[0], replacePattern[1]);
            }

            return methodFullName;
        }

        private string GetBaseClassBody(Uri webApi)
        {
            var commandApiUrl = new Uri(webApi, "api/command");
            var reportApiUrl = new Uri(webApi, "api/report");
            const int profilerReportTime = 20000; // every 20 sec

            return
$@"using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Geeks.Profiler
{{
    internal static class Profiler
    {{
        private const string CommandApiUrl = ""{commandApiUrl}"";
        private const string ReportApiUrl = ""{reportApiUrl}"";
        private const int ProfilerReportTime = {profilerReportTime};
        private static readonly HttpClient Client = new HttpClient();
        private static bool _isBackgroundRunning;
        private static readonly double ConstantNoise;

        public static ThreadLocal<long> TotalMethodCallCount = new ThreadLocal<long>();

        static Profiler()
        {{
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            for (var i = 0; i < 1000000; i++)
            {{
                TotalMethodCallCount.Value++;
                var totalMethodCallCount = TotalMethodCallCount.Value;
                var stopwatchTimer = new System.Diagnostics.Stopwatch();
                stopwatchTimer.Start();
                         
                stopwatchTimer.Stop();
                var subMethodCallCount = TotalMethodCallCount.Value--;
                ReportMethodRun(ref subMethodCallCount, ref subMethodCallCount, stopwatchTimer.ElapsedTicks, 0);
            }}
            timer.Stop();
            ConstantNoise = (double)timer.ElapsedTicks / 1000000;
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReportMethodRun(ref long time, ref long count, long elapsedTicks, long subMethodCallCount)
        {{
            if (!_isBackgroundRunning)
            {{
                _isBackgroundRunning = true;
                ProfilerReportTask();
            }}
            
            elapsedTicks = (long)(elapsedTicks - (subMethodCallCount * ConstantNoise));

            time += elapsedTicks;
            count++;
        }}

        private static async void ProfilerReportTask()
        {{
            while (true)
            {{
                try
                {{
                    var response = await Client.GetAsync(CommandApiUrl);
                    if (response.IsSuccessStatusCode)
                    {{
                        var content = await response.Content.ReadAsStringAsync();
                        var command = new Command(content.Replace(""\"""", """"));
                        RunCommand(command);
                    }}
                }}
                catch
                {{
                }}
                await Task.Delay(ProfilerReportTime);
            }}
        }}

        private static void RunCommand(Command command)
        {{
            if (command.Equals(Command.Discard))
            {{
                Clear();
            }}
            else if (command.Equals(Command.Start))
            {{
                Clear();
            }}
            else if (command.Equals(Command.GetResults))
            {{
                PostValues();
            }}
        }}

        private static async void PostValues()
        {{
            var file = GenerateReportFile();
            try
            {{
                using (var formData = new MultipartFormDataContent())
                {{
                    formData.Add(new StringContent(file), ""file"", ""fileName"");
                    var response = Client.PostAsync(ReportApiUrl, formData).Result;
                }}
            }}
            catch
            {{
            }}
        }}

        private static string GenerateReportFile()
        {{
            var csvExport = new StringBuilder();

            foreach (var line in GetReportFileContent())
            {{
                csvExport.AppendLine(line);    
            }}

            return csvExport.ToString();
        }}

        private static string GetReportFileLine(string key, long time, long count)
        {{
            return string.Format(""\""{{0}}\"",\""{{1}}\"",\""{{2}}\"""", key.Replace("","", """"), count, time);
        }}

        private static IEnumerable<string> GetReportFileContent()
        {{
            {GetReportFileContentMethodBodyPlaceHolder}
        }}

        private static void Clear()
        {{
            {ClearMethodBodyPlaceHolder}
        }}

        private sealed class Command
        {{
            public static readonly Command Discard = new Command(""Discard"");
            public static readonly Command Start = new Command(""Start"");
            public static readonly Command GetResults = new Command(""GetResults"");
            private readonly string _command;

            public Command(string command)
            {{
                _command = command;
            }}

            public override string ToString()
            {{
                return _command;
            }}

            public override bool Equals(object obj)
            {{
                var command = obj as Command;
                if (command != null)
                {{
                    return Equals(command);
                }}

                return base.Equals(obj);
            }}

            public bool Equals(Command command)
            {{
                return command.ToString().Equals(_command);
            }}
        }}

        {FieldsPlaceHolder}
    }}
}}";
        }
    }
}
