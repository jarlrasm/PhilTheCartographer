using CompleteMap.Test.Helpers;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Refactorings;

namespace Phil.Test
{
    [TestClass]
    public class FillInitializerTest
    {
        string search = @"var t = new ParentlessClass() {";
        [TestMethod]
        public void ImplementFromField()
        {
            TestHelper.TestRefactoring(new FillInitializerRefactoring(), search, "Fill from field", "InitializerFixerfromField");
        }


        [TestMethod]
        public void ImplementFromLocal()
        {
            TestHelper.TestRefactoring(new FillInitializerRefactoring(), search, "Fill from t2", "InitializerFixerfromLocal");
        }
        

        [TestMethod]
        public void ImplementFromProperty()
        {
            TestHelper.TestRefactoring(new FillInitializerRefactoring(), search, "Fill from Getter", "InitializerFixerfromPropertyWithInheritance");
        }
        [TestMethod]
        public void FillInBlanks()
        {
            TestHelper.TestRefactoring(new FillInitializerRefactoring(), search, "Fill in blanks", "InitializerFixerFillBlanks");
        }

        
        [TestMethod]
        public void DontCrashDictionaries()
        {
            var code = @"{var foobar=new Dictionary<string, string> { { ""foo"", ""bar"" } };}";
            var document = TestHelper.GetDocument(code);
            var refactorings = TestHelper.FindRefactorings(new FillInitializerRefactoring(), document, @"bar"" } ");
        }
    }
}
