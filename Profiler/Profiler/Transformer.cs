﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Geeks.Profiler
{
    internal class Transformer
    {
        private readonly string _solutionFile;
        private readonly Uri _webApi;
        private readonly string[] _preprocessors;

        public Transformer(string solutionFile, Uri webApi, string[] preprocessors)
        {
            _solutionFile = solutionFile;
            _webApi = webApi;
            _preprocessors = preprocessors;
        }

        public void Transform()
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(_solutionFile).Result;

            var projectParseOptions = GetProjectParseOptions(solution);
            solution = SetProjectParseOptions(solution, _preprocessors);

            var methodsInfo = GatherMethods(solution);

            var regionRemover = new RegionRemover();
            solution = regionRemover.RemoveRegions(solution);

            var methodsTransformer = new MethodsTransformer();
            var profilerClass = new ProfilerClass();
            solution = methodsTransformer.TransformMethods(solution, profilerClass);

            solution = profilerClass.CreateClass(solution, _webApi, methodsInfo);

            solution = SetProjectParseOptions(solution, projectParseOptions);

            Save(solution);
        }

        private Dictionary<ProjectId, CSharpParseOptions> GetProjectParseOptions(Solution solution)
        {
            var result = new Dictionary<ProjectId, CSharpParseOptions>();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);

                var parseOptions = (CSharpParseOptions)project.ParseOptions;
                result.Add(projectId, parseOptions);
            }

            return result;
        }

        private Solution SetProjectParseOptions(Solution solution, string[] preprocessors)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);

                var parseOptions = (CSharpParseOptions)project.ParseOptions;
                parseOptions = parseOptions.WithPreprocessorSymbols(preprocessors);
                project = project.WithParseOptions(parseOptions);

                solution = project.Solution;
            }

            return solution;
        }

        private Solution SetProjectParseOptions(Solution solution, Dictionary<ProjectId, CSharpParseOptions> projectsParseOptions)
        {
            foreach (var projectParseOptions in projectsParseOptions)
            {
                var project = solution.GetProject(projectParseOptions.Key);

                project = project.WithParseOptions(projectParseOptions.Value);

                solution = project.Solution;
            }

            return solution;
        }

        private List<ProjectMethodsInfo> GatherMethods(Solution solution)
        {
            var projectMethodsInfo = new List<ProjectMethodsInfo>();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var documentMethodsInfo = new List<DocumentMethodsInfo>();
                foreach (var documentId in project.DocumentIds)
                {
                    var doc = project.GetDocument(documentId);
                    var docRoot = doc.GetSyntaxRootAsync().Result;
                    var semanticModel = doc.GetSemanticModelAsync().Result;

                    // Gather methods
                    var collector = new MethodCollector(semanticModel);
                    collector.Visit(docRoot);

                    documentMethodsInfo.Add(new DocumentMethodsInfo(documentId, collector.Methods));
                }
                projectMethodsInfo.Add(new ProjectMethodsInfo(projectId, documentMethodsInfo));
            }

            return projectMethodsInfo;
        }

        private Solution Save(Solution solution)
        {
            var result = solution.Workspace.TryApplyChanges(solution);
            if (!result)
            {
                throw new Exception("Failed to apply changes to solution.");
            }

            return solution;
        }
    }
}
