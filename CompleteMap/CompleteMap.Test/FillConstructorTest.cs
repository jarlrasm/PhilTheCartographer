using System;
using CompleteMap.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Refactorings;

namespace Phil.Test
{
    [TestClass]
    public class FillConstructorTest
    {
        [TestMethod]
        public void ConstructorFillFromArguments()
        {
            TestHelper.TestRefactoring(new FillConstrutorRefactoring(),
                "var t2 = new SubClass(",
                "Fill constructor from eh", 
                "ConstructorFillFromArgument");
        }
    }
}
