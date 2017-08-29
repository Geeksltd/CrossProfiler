using System;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace Geeks.Profiler
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true,
            HelpText = "Input .sln file to be processed, e.g.: C:\\path\\to\\test.sln")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true,
            HelpText = "Output directory to export new solution, e.g.: C:\\path\\to\\output")]
        public string OutputDirectory { get; set; }

        [Option('w', "webapi", Required = true,
            HelpText = "Web API address to report the results, e.g.: http://localhost:9200",
            MetaValue = "URI")]
        public string WebApi { get; set; }

        [OptionArray('p', "preprocessor", HelpText = "List of preprocessors, e.g.: DEBUG TEST WIN")]
        public string[] Preprocessors { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public bool Validate()
        {
            if (!File.Exists(InputFile))
            {
                return false;
            }

            try
            {
                var uri = new Uri(WebApi);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}