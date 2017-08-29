using System;
using System.IO;
using CommandLine;

namespace Geeks.Profiler
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (!Parser.Default.ParseArguments(args, options))
            {
                return;
            }

            if (!options.Validate())
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            var cloner = new Cloner();
            var inputDirectory = Path.GetDirectoryName(options.InputFile);
            cloner.Clone(inputDirectory, options.OutputDirectory);

            var solutionFileName = Path.GetFileName(options.InputFile);
            var solutionFilePath = Path.Combine(options.OutputDirectory, solutionFileName);
            var transformer = new Transformer(solutionFilePath, new Uri(options.WebApi), options.Preprocessors);
            transformer.Transform();
        }
    }
}