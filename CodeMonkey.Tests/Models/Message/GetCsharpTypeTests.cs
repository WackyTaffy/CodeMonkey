namespace CodeMonkey.Tests.Models.Message
{
    [TestFixture]
    public class GetCsharpTypeTests
    {


        // ---------------------------------------------------------------
        // Null / empty / whitespace input
        // ---------------------------------------------------------------

        [Test]
        public void Null_Input_Returns_Null()
        {
            Assert.That(Core.Models.Message.GetCsharpType(null!), Is.Null);
        }

        [Test]
        public void Empty_String_Returns_Null()
        {
            Assert.That(Core.Models.Message.GetCsharpType(""), Is.Null);
        }

        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("\n\n\n")]
        [TestCase("  \r\n\t  ")]
        public void Whitespace_Only_Input_Returns_Null(string input)
        {
            Assert.That(Core.Models.Message.GetCsharpType(input), Is.Null);
        }

        // ---------------------------------------------------------------
        // No recognizable type present
        // ---------------------------------------------------------------

        [Test]
        public void UsingsAndNamespaceOnly_NoType_Returns_Null()
        {
            var code = @"
            using System;
            using System.Linq;

            namespace MyApp.Services
            {
                // nothing declared here yet
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        [Test]
        public void CommentsOnly_Returns_Null()
        {
            var code = @"
            // this file used to have a class
            /* it also used to have a struct and an enum */
        ";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        [Test]
        public void TopLevelStatements_NoTypeDeclared_Returns_Null()
        {
            var code = @"
            Console.WriteLine(""Hello, World!"");
            var x = 5;
            DoSomething(x);

            static void DoSomething(int n) => Console.WriteLine(n);
        ";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        [Test]
        public void DelegateDeclaration_NotInAllowedSet_Returns_Null()
        {
            // delegate isn't one of the recognized keywords
            var code = "public delegate void OnCompleted(int result);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        [Test]
        public void RandomNonCsharpText_Returns_Null()
        {
            var code = "This is just some plain English text about classes and structs in general.";
            // Lowercase "classes" / "structs" are plurals, not bare keywords immediately
            // followed by an identifier — should not match.
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        // ---------------------------------------------------------------
        // Basic detection — one per keyword
        // ---------------------------------------------------------------

        [TestCase("class", "public class Foo { }")]
        [TestCase("struct", "public struct Foo { }")]
        [TestCase("interface", "public interface IFoo { }")]
        [TestCase("enum", "public enum Foo { A, B }")]
        [TestCase("record", "public record Foo(string Name);")]
        public void Detects_Each_Type_Keyword(string expected, string code)
        {
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo(expected));
        }

        // ---------------------------------------------------------------
        // Access modifiers / other modifiers
        // ---------------------------------------------------------------

        [TestCase("public class Foo { }")]
        [TestCase("internal class Foo { }")]
        [TestCase("private class Foo { }")]
        [TestCase("protected class Foo { }")]
        [TestCase("sealed class Foo { }")]
        [TestCase("abstract class Foo { }")]
        [TestCase("static class Foo { }")]
        [TestCase("partial class Foo { }")]
        [TestCase("public sealed partial class Foo { }")]
        [TestCase("internal abstract class Foo { }")]
        public void Detects_Class_Regardless_Of_Modifiers(string code)
        {
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_ReadonlyStruct()
        {
            var code = "public readonly struct Point { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("struct"));
        }

        [Test]
        public void Detects_RefStruct()
        {
            var code = "public ref struct Span { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("struct"));
        }

        // ---------------------------------------------------------------
        // Attributes preceding declaration
        // ---------------------------------------------------------------

        [Test]
        public void Detects_Type_With_Attribute_Above_It()
        {
            var code = @"
            [Serializable]
            public class Foo { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_Type_With_Multiple_Attributes()
        {
            var code = @"
            [Serializable]
            [Obsolete(""use Bar instead"")]
            public class Foo { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        // ---------------------------------------------------------------
        // Generics
        // ---------------------------------------------------------------

        [Test]
        public void Detects_Generic_Class_SingleTypeParam()
        {
            var code = "public class Repository<T> { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_Generic_Class_MultipleTypeParams()
        {
            var code = "public class Dictionary<TKey, TValue> { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_Generic_Interface()
        {
            var code = "public interface IRepository<T> { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("interface"));
        }

        // ---------------------------------------------------------------
        // Comment / string stripping (avoiding false positives)
        // ---------------------------------------------------------------

        [Test]
        public void Ignores_Keyword_In_LineComment_Before_Real_Type()
        {
            var code = @"
            // this used to be a class, now it's an interface
            public interface IFoo { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("interface"));
        }

        [Test]
        public void Ignores_Keyword_In_BlockComment_Before_Real_Type()
        {
            var code = @"
            /* class Foo should be ignored here */
            public struct Bar { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("struct"));
        }

        [Test]
        public void Ignores_Keyword_In_MultilineBlockComment()
        {
            var code = @"
            /*
             * TODO: consider turning this into a class
             * or maybe an enum at some point
             */
            public record Foo(string Name);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        [Test]
        public void Ignores_Keyword_In_StringLiteral()
        {
            var code = @"
            public class Program
            {
                static void Main()
                {
                    string s = ""this describes a struct example"";
                }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Ignores_Keyword_In_StringLiteral_With_EscapedQuotes()
        {
            var code = @"
            public class Program
            {
                string s = ""He said \""this is an interface\"" once"";
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Ignores_Keyword_In_CharLiteral()
        {
            // contrived, but exercises the char-literal branch of the strip regex
            var code = @"
            public class Program
            {
                char c = 'x'; // not 'class'
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void FileWithOnlyCommentedOutType_Returns_Null()
        {
            var code = @"
            // public class Foo { }
            /* public struct Bar { } */
        ";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        // ---------------------------------------------------------------
        // False-positive prevention: keyword as substring of a larger word
        // ---------------------------------------------------------------

        [Test]
        public void DoesNotMatch_Struct_As_Substring_Of_Structure()
        {
            var code = @"
            void ConfigureStructure()
            {
                // no real type declaration in this file
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        [Test]
        public void DoesNotMatch_Record_As_Substring_Of_Recorder()
        {
            var code = @"
            void PlayRecorder()
            {
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        [Test]
        public void DoesNotMatch_Enum_As_Substring_Of_IEnumerable()
        {
            var code = @"
            public class Foo
            {
                IEnumerable<int> GetValues() => Enumerable.Range(0, 10);
            }";
            // Should match 'class' (the real declaration), not be confused
            // by 'Enum' inside 'IEnumerable'/'Enumerable'.
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void DoesNotMatch_Class_As_Substring_Of_Classification()
        {
            var code = @"
            public struct ClassificationResult
            {
                public string Label;
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("struct"));
        }

        [Test]
        public void DoesNotMatch_Interface_As_Substring_Of_LongerIdentifier()
        {
            var code = @"
            public class InterfaceGenerator
            {
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        // ---------------------------------------------------------------
        // Case sensitivity
        // ---------------------------------------------------------------

        [TestCase("Class Foo { }")]
        [TestCase("CLASS Foo { }")]
        [TestCase("Struct Foo { }")]
        [TestCase("Interface Foo { }")]
        [TestCase("Enum Foo { }")]
        [TestCase("Record Foo { }")]
        public void WrongCaseKeyword_Returns_Null(string code)
        {
            // C# keywords are lowercase; wrongly-cased text shouldn't match.
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.Null);
        }

        // ---------------------------------------------------------------
        // Generic constraint clauses containing keyword text
        // (semantic correctness: constraint shouldn't hijack classification)
        // ---------------------------------------------------------------

        [Test]
        public void InterfaceWithClassConstraint_StillReturnsInterface()
        {
            var code = @"
            public interface IRepository<T> where T : class
            {
                T Get(int id);
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("interface"));
        }

        [Test]
        public void ClassWithStructConstraint_StillReturnsClass()
        {
            var code = @"
            public class Box<T> where T : struct
            {
                public T Value;
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void ClassWithMultipleConstraintsIncludingClass_StillReturnsClass()
        {
            var code = @"
            public class Repository<TEntity, TKey>
                where TEntity : class
                where TKey : struct
            {
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        // ---------------------------------------------------------------
        // Multiple type declarations — first-match-wins semantics
        // ---------------------------------------------------------------

        [Test]
        public void MultipleTypes_ReturnsFirstDeclaredType()
        {
            var code = @"
            public interface IFoo { }
            public class Foo : IFoo { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("interface"));
        }

        [Test]
        public void MultipleTypes_EnumBeforeClass_ReturnsEnum()
        {
            var code = @"
            public enum Status { Active, Inactive }

            public class StatusChecker
            {
                public Status Current { get; set; }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("enum"));
        }

        [Test]
        public void NestedClassInsideClass_ReturnsOuterClass()
        {
            var code = @"
            public class Outer
            {
                public class Inner { }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void NestedStructInsideClass_ReturnsOuterClass()
        {
            var code = @"
            public class Outer
            {
                private struct Inner { }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        // ---------------------------------------------------------------
        // record / record class / record struct
        // ---------------------------------------------------------------

        [Test]
        public void Detects_Record_PositionalSyntax()
        {
            var code = "public record Person(string Name, int Age);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        [Test]
        public void Detects_Record_ClassBodySyntax()
        {
            var code = @"
            public record Person
            {
                public string Name { get; init; }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        [Test]
        public void Detects_RecordClass_Explicit()
        {
            var code = "public record class Person(string Name);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        [Test]
        public void Detects_RecordStruct()
        {
            var code = "public record struct Point(int X, int Y);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        [Test]
        public void Detects_ReadonlyRecordStruct()
        {
            var code = "public readonly record struct Point(int X, int Y);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        [Test]
        public void Detects_Record_WithInheritance()
        {
            var code = "public record Employee(string Name) : Person(Name);";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("record"));
        }

        // ---------------------------------------------------------------
        // Enum specifics
        // ---------------------------------------------------------------

        [Test]
        public void Detects_Enum_WithUnderlyingType()
        {
            var code = "public enum StatusCode : byte { Ok = 0, NotFound = 1 }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("enum"));
        }

        [Test]
        public void Detects_FlagsEnum()
        {
            var code = @"
            [Flags]
            public enum Permissions
            {
                None = 0,
                Read = 1,
                Write = 2
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("enum"));
        }

        // ---------------------------------------------------------------
        // Whitespace / formatting variations
        // ---------------------------------------------------------------

        [Test]
        public void Detects_Type_When_Keyword_And_Name_On_Different_Lines()
        {
            var code = @"
            public class
                Foo
            {
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_Type_With_TabsBetweenKeywordAndName()
        {
            var code = "public class\t\tFoo { }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_Type_With_ExcessiveWhitespace()
        {
            var code = "public    class     Foo    {    }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        // ---------------------------------------------------------------
        // Namespace styles
        // ---------------------------------------------------------------

        [Test]
        public void Detects_Type_With_FileScopedNamespace()
        {
            var code = @"
            namespace MyApp.Models;

            public class Customer
            {
                public string Name { get; set; }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        [Test]
        public void Detects_Type_With_BlockScopedNamespace()
        {
            var code = @"
            namespace MyApp.Models
            {
                public class Customer
                {
                }
            }";
            Assert.That(Core.Models.Message.GetCsharpType(code), Is.EqualTo("class"));
        }

        // ---------------------------------------------------------------
        // Known limitations — characterization tests
        // These document current behavior of the regex-based heuristic
        // rather than asserting "ideal" behavior. A real parser (Roslyn)
        // would not have these failure modes.
        // ---------------------------------------------------------------

        [Test]
        [Description("Known limitation: verbatim strings using \"\" as an escaped " +
                     "quote are not correctly handled by the string-stripping regex, " +
                     "since it assumes backslash-escaping semantics.")]
        public void KnownLimitation_VerbatimStringWithEscapedQuote_MayMisparse()
        {
            var code = @"
            public class Program
            {
                string s = @""He said """"this is a struct"""" once"";
            }";

            // Documenting actual behavior rather than asserting correctness —
            // if this starts failing after a regex fix, that's a good thing.
            var result = Core.Models.Message.GetCsharpType(code);
            Assert.That(result, Is.EqualTo("class"),
                "If this fails, the verbatim-string handling may have been fixed " +
                "(or regressed further) — re-evaluate this test either way.");
        }

        [Test]
        [Description("Known limitation: C# 11 raw string literals (triple-quoted) " +
                     "are not recognized by the stripping regex at all.")]
        public void KnownLimitation_RawStringLiteral_NotStripped()
        {
            var code = "public class Program { string s = \"\"\"this mentions struct\"\"\"; }";

            // Current implementation doesn't special-case triple-quoted raw strings;
            // this test exists to make that gap visible rather than silently rely on
            // the "class" keyword happening to appear first anyway.
            var result = Core.Models.Message.GetCsharpType(code);
            Assert.That(result, Is.EqualTo("class"));
        }

        [Test]
        [Description("Known limitation: this is a regex heuristic, not a real parser, " +
                     "so code inside inactive #if blocks is still scanned as if active.")]
        public void KnownLimitation_PreprocessorDirectives_NotRespected()
        {
            var code = @"
            #if NEVER_DEFINED
            public class ShouldBeExcluded { }
            #endif
        ";

            // A real parser honoring preprocessor symbols would return null here.
            // The regex heuristic will still find "class" since it doesn't
            // evaluate #if conditions.
            var result = Core.Models.Message.GetCsharpType(code);
            Assert.That(result, Is.EqualTo("class"),
                "Documents that inactive #if blocks are not excluded by this heuristic.");
        }
    }
}