// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Scriban.Functions;
using Scriban.Runtime;

namespace Scriban.Tests
{
    public class TestFunctions
    {
        public class Arrays
        {
            [Test]
            public void TestOffset()
            {
                Assert.Null(ArrayFunctions.Offset(null, 0));
            }

            [Test]
            public void TestLimit()
            {
                Assert.Null(ArrayFunctions.Limit(null, 0));
            }

            [Test]
            public void TestSortNoError()
            {
                TestParser.AssertTemplate("true", "{{ [1,2] || array.sort }}");
            }

            [Test]
            public void TestContains()
            {
                var mixed = new object[] { "hi", 1, TestEnum.First };
                Assert.True(ArrayFunctions.Contains(mixed, "hi"));
                Assert.True(ArrayFunctions.Contains(mixed, 1));
                Assert.True(ArrayFunctions.Contains(mixed, TestEnum.First));
                Assert.True(ArrayFunctions.Contains(mixed, "First"));
                Assert.True(ArrayFunctions.Contains(mixed, 100));
                TestParser.AssertTemplate("true", "{{ value | array.contains 'First' }}", model: new ObjectModel { Value = mixed });
                TestParser.AssertTemplate("true", "{{ value | array.contains 100 }}", model: new ObjectModel { Value = mixed });
                TestParser.AssertTemplate("false", "{{ value | array.contains 'Second' }}", model: new ObjectModel { Value = mixed });
                TestParser.AssertTemplate("false", "{{ value | array.contains 101 }}", model: new ObjectModel { Value = mixed });
                TestParser.AssertTemplate("false", "{{ value | array.contains 'Third' }}", model: new ObjectModel { Value = mixed });
            }
            class ObjectModel
            {
                public object[] Value { get; set; }
            }

            enum TestEnum : int
            {
                First = 100,
                Second = 101
            }
        }

        public class Strings
        {
            [Test]
            public void TestSliceError()
            {
                TestParser.AssertTemplate("text(1,16) : error : Invalid number of arguments `0` passed to `string.slice` while expecting `2` arguments", "{{ string.slice }}");
            }
            [Test]
            public void TestSliceAtError()
            {
                TestParser.AssertTemplate("text(1,17) : error : Invalid number of arguments `0` passed to `string.slice1` while expecting `2` arguments", "{{ string.slice1 }}");
            }

            public record IndexOfTestCase(
                string Text,
                string Search,
                int Expected,
                int? StartIndex = null,
                int? Count = null,
                StringComparison? StringComparison = null
            );

            public static readonly IReadOnlyList<IndexOfTestCase> IndexOfTestCases = new IndexOfTestCase[]
            {
                new ("The quick brown fox", "quick", 4),
                new ("The the the the", "the", 0, null, null, StringComparison.OrdinalIgnoreCase),
                new ("The quick brown fox", "quick", -1, null, 2),
                new ("The the the the", "the", 8, 6),
            };

            [Test]
            [TestCaseSource(nameof(IndexOfTestCases))]
            public void TestIndexOf(IndexOfTestCase testCase)
            {
                testCase = testCase ?? throw new ArgumentNullException(nameof(testCase));
                var args = new[]
                {
                    (Name: "text", Value: MakeString(testCase.Text)),
                    (Name: "search", Value: MakeString(testCase.Search)),
                    (Name: "start_index", Value: MakeInt(testCase.StartIndex)),
                    (Name: "count", Value: MakeInt(testCase.Count)),
                    (Name: "string_comparison", Value: MakeString(testCase.StringComparison?.ToString())),
                }.Select(x => $"{x.Name}: {x.Value}");
                var script = $@"{{{{ string.index_of {string.Join(" ", args)} }}}}";
                Template template = null;
                Assert.DoesNotThrow(() => template = Template.Parse(script));
                Assert.That(template, Is.Not.Null);
                Assert.That(template.HasErrors, Is.False);
                Assert.That(template.Messages, Is.Not.Null);
                Assert.That(template.Messages.Count, Is.EqualTo(0));
                var result = template.Render();
                Assert.That(result, Is.EqualTo(testCase.Expected.ToString()));

                static string MakeString(string value) => value is null ? "null" : $"'{value}'";
                static string MakeInt(int? value) => value is null ? "null" : value.ToString();
            }

            [Test]
            [TestCaseSource(nameof(TestReplaceFirstArguments))]
            public void TestReplaceFirst(string source, string match, string replace, bool fromend, string expected)
            {
                var script = @"{{source | string.replace_first  match replace fromend}}";
                var template = Template.Parse(script);
                var result = template.Render(new
                {
                    Source = source,
                    Match = match,
                    Replace = replace,
                    Fromend = fromend,
                });
                Assert.AreEqual(result, expected);
            }

            static readonly object[] TestReplaceFirstArguments =
            {
                // Replace from start
                new object [] {
                    "Hello, world. Goodbye, world.",    // source
                    "world",                            // match
                    "buddy",                            // replace
                    false,                              // fromEnd
                    "Hello, buddy. Goodbye, world.",    // expected
                },
                // Replace from end
                new object [] {
                    "Hello, world. Goodbye, world.",    // source
                    "world",                            // match
                    "buddy",                            // replace
                    true,                               // fromEnd
                    "Hello, world. Goodbye, buddy.",    // expected
                },
                // nothing to replace
                new object [] {
                    "Hello, world. Goodbye, world.",    // source
                    "xxxx",                             // match
                    "buddy",                            // replace
                    false,                              // fromEnd
                    "Hello, world. Goodbye, world.",    // expected
                },
            };
        }

        public class Math
        {
            [Test]
            public void TestMathUuid()
            {
                var script = @"{{math.uuid}}";
                var template = Template.Parse(script);
                var result = template.Render();
                Assert.IsTrue(Guid.TryParse(result, out var _));
            }

            [Test]
            public void TestMathRandom()
            {
                var script = @"{{math.random 1 10}}";
                var template = Template.Parse(script);
                var result = template.Render();
                Assert.IsTrue(int.TryParse(result, out var number));
                Assert.IsTrue(number < 10 && number >= 1);
            }

            [Test]
            public void TestMathRandomError()
            {
                TestParser.AssertTemplate("text(1,4) : error : minValue must be greater than maxValue", "{{ math.random 11 10 }}");
            }
        }

        public class Custom
        {
            [Test]
            public void TestInstanceCall()
            {
                var script = "{{\n" +
                             " obj.append_to_sb(sb);\n" +
                             " obj.get_test_string(sb.to_string())\n" +
                             "}}";
                var template = Template.Parse(script);
                var testInstance = new TestClass();

                var ctx = new TemplateContext();
                var so = new ScriptObject
                {
                    ["obj"] = testInstance,
                    ["sb"] = new StringBuilder()
                };
                ctx.PushGlobal(so);

                var res = template.Render(ctx);
                Assert.AreEqual("nice TestString nice", res);
            }

            [Test]
            public void TestInstanceCallRenamed()
            {
                var script = "{{obj.GetTestString(\"nice\")}}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();
                ctx.MemberRenamer = m => m.Name;
                var res = template.Render(ctx);
                Assert.AreEqual("nice TestString nice", res);
            }

            [Test]
            public void TestAsynchronousInstanceCall()
            {
                var script = "{{obj.call_async()}}";
                var template = Template.Parse(script);

                TemplateContext ctx = GetContext();

                var res = template.Render(ctx);
                Assert.AreEqual("nice", res);
            }

            [Test]
            public void TestInstanceFieldAccess()
            {
                var script = "{{obj.test_int}}";
                var template = Template.Parse(script);

                TemplateContext ctx = GetContext();

                var res = template.Render(ctx);
                Assert.AreEqual("42", res);
            }

            [Test]
            public void TestInstancePropertyAccess()
            {
                var script = "{{obj.test_float}}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();

                var res = template.Render(ctx);
                Assert.AreEqual("1337", res);
            }

            [Test]
            public void TestAsyncEnumerableTask()
            {
                var script = "{{ for i in obj.enumerable_async()}}{{i}}{{end}}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();

                var res = template.Render(ctx);
                Assert.AreEqual("testtest2", res);
            }

            [Test]
            public void TestTaskNonGeneric()
            {
                var script = "{{obj.call_task()}}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();

                var res = template.Render(ctx);
                Assert.Pass();
            }

            [Test]
            public void TestTaskCallWithException()
            {
                var script = "{{obj.call_async_fail()}}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();

                Assert.Throws<ScriptRuntimeException>(() => template.Render(ctx));
            }

            [Test]
            public void Property_Setter()
            {
                var script = "{{obj.test_float = 42.0f}}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();
                template.Render(ctx);

                Assert.That(ctx.CurrentGlobal.TryGetValue("obj", out dynamic obj));
                Assert.AreEqual(42.0f, obj.TestFloat);
            }

            [Test]
            public void Property_Setter_Array()
            {
                var script = "{{ obj.test_list = obj.test_list | array.add \"password\" }}";
                var template = Template.Parse(script);
                TemplateContext ctx = GetContext();
                template.Render(ctx);

                Assert.That(ctx.CurrentGlobal.TryGetValue("obj", out dynamic obj));
                CollectionAssert.AreEquivalent(new List<string> {"password"}, obj.TestList);
            }
            private static TemplateContext GetContext()
            {
                var ctx = new TemplateContext();
                var so = new ScriptObject();
                so["obj"] = new TestClass();
                ctx.PushGlobal(so);
                return ctx;
            }
        }

        public class TestClass
        {
            private string _test = "nice";
            public int TestInt = 42;
            public float TestFloat { get; } = 1337.0f;

            public string GetTestString(string input)
            {
                return _test + " TestString " + input;
            }

            public void AppendToSb(StringBuilder sb)
            {
                sb.Append(_test);
            }

            public static string Geil()
            {
                return "hier";
            }

            public Task<string> CallAsync()
            {
                return Task.Run(() => _test);
            }

            public Task CallTask()
            {
                return Task.CompletedTask;
            }

            public async Task<IEnumerable<string>> EnumerableAsync()
            {
                await Task.Run(() => 1 + 2);
                return new List<string> { "test", "test2" };
            }
        }
    }
}