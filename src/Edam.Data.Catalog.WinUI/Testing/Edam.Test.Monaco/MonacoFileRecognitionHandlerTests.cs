using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monaco.MonacoHandler;

namespace Edam.Test.Monaco
{
   /// <summary>
   /// Tests for <see cref="MonacoFileRecognitionHandler.RecognizeLanguageByFileType"/>.
   /// These are pure-logic tests and do not require a UI host.
   /// </summary>
   [TestClass]
   public class MonacoFileRecognitionHandlerTests
   {
      private readonly MonacoFileRecognitionHandler _handler =
         new MonacoFileRecognitionHandler();

      [TestMethod]
      public void RecognizeLanguageByFileType_KnownExtensions_ReturnsExpectedLanguage()
      {
         Assert.AreEqual("csharp", _handler.RecognizeLanguageByFileType(".cs"));
         Assert.AreEqual("javascript", _handler.RecognizeLanguageByFileType(".js"));
         Assert.AreEqual("typescript", _handler.RecognizeLanguageByFileType(".ts"));
         Assert.AreEqual("json", _handler.RecognizeLanguageByFileType(".json"));
         Assert.AreEqual("xml", _handler.RecognizeLanguageByFileType(".xml"));
         Assert.AreEqual("html", _handler.RecognizeLanguageByFileType(".html"));
         Assert.AreEqual("python", _handler.RecognizeLanguageByFileType(".py"));
         Assert.AreEqual("markdown", _handler.RecognizeLanguageByFileType(".md"));
         Assert.AreEqual("sql", _handler.RecognizeLanguageByFileType(".sql"));
         Assert.AreEqual("yaml", _handler.RecognizeLanguageByFileType(".yaml"));
      }

      [TestMethod]
      public void RecognizeLanguageByFileType_UnknownExtension_FallsBackToPlaintext()
      {
         Assert.AreEqual("plaintext", _handler.RecognizeLanguageByFileType(".xyz"));
         Assert.AreEqual("plaintext", _handler.RecognizeLanguageByFileType(".unknown"));
      }

      [TestMethod]
      public void RecognizeLanguageByFileType_EmptyOrNull_FallsBackToPlaintext()
      {
         Assert.AreEqual("plaintext", _handler.RecognizeLanguageByFileType(""));
         Assert.AreEqual("plaintext", _handler.RecognizeLanguageByFileType(null));
      }

      [TestMethod]
      public void RecognizeLanguageByFileType_IsCaseSensitive_UnknownCaseFallsBack()
      {
         // The mapping is case-sensitive; an uppercase extension is not matched.
         Assert.AreEqual("plaintext", _handler.RecognizeLanguageByFileType(".CS"));
      }

      [TestMethod]
      public void Languages_ContainsExpectedMappings()
      {
         Assert.IsTrue(_handler.Languages.ContainsKey(".cs"));
         Assert.IsTrue(_handler.Languages.ContainsKey(".json"));
         Assert.IsTrue(_handler.Languages.ContainsKey(".html"));
         Assert.IsFalse(_handler.Languages.ContainsKey(".unknown"));
      }
   }
}
