using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompleteMap.Test.Helpers;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Refactorings;

namespace Phil.Test
{
    [TestClass]
    public class FillConstructorBodyTest
    {
        private string search = @"public SubClass(ParentlessClass test, int integer)
        {";
        [TestMethod]
        public void FillFromArgument()
        {
            TestHelper.TestRefactoring(new FillConstrutorBodyRefactoring(),search, "Fill constructor from test", "ConstructorBodyFillFromArgument");
        }

    }
}
