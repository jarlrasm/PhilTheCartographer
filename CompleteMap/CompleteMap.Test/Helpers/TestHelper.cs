﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Refactorings;

namespace CompleteMap.Test.Helpers
{
    public class TestHelper
    {
        public static string SomeCode { get; } = File.ReadAllText("TestCode/input.cs");
        

        public static string Result(string file) => File.ReadAllText($"TestCode/Output/{file}.cs");


        public static void TestRefactoring( CodeRefactoringProvider codeRefactoringProvider, string interestingText, string expectedTitle, string resultFilename)
        {
            var document = new AdhocWorkspace()
               .AddProject("Test", "C#")
               .AddDocument("Test", SomeCode);
            var refactorings = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, new TextSpan(SomeCode.IndexOf(interestingText, StringComparison.InvariantCulture) + interestingText.Length, 0),
                                                     refactorings.Add, CancellationToken.None);
      
            codeRefactoringProvider.ComputeRefactoringsAsync(context).Wait();
            Assert.AreEqual(1, refactorings.Count(x => x.Title==expectedTitle));
            var refaktoring = refactorings.First(x => x.Title == expectedTitle);
            var operations = refaktoring.GetOperationsAsync(CancellationToken.None).Result;
            Assert.AreEqual(1, operations.Length);
            var workspace = document.Project.Solution.Workspace;
            operations.First().Apply(workspace, CancellationToken.None);
            document = workspace.CurrentSolution.GetDocument(document.Id);
            var text = document.GetTextAsync(CancellationToken.None).Result.ToString();
            Assert.AreEqual(text, Result(resultFilename));
        }
    }
}