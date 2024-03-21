using B1SA.HanaTranslator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class HanaTranslatorTest
    {
        [TestMethod]
        public void SimpleHanaTranslation()
        {
            var translator = new Translator(new Config() {
                TranslationComments = true,
                FormatOutput = false
            });

            // test sql
            var input = """
                select id, item, name, isnull(qty, 0) from [table] where id = {0}
                """;

            // finally translate
            var output = translator.Translate(input, out var summary, out var statements, out var errors);

            Console.WriteLine($"SUMMARY:\n{summary}");
            Console.WriteLine($"ERRORS: {errors}/{statements}");
            Console.WriteLine($"INPUT: {input}");
            Console.WriteLine($"OUTPUT: {output}");

            Assert.IsTrue(errors == 0);
        }

        [TestMethod]
        public void ComplexHanaTranslation()
        {
            var translator = new Translator(new Config() {
                TranslationComments = true,
                FormatOutput = true
            });

            // test sql
            var input = """
                SELECT
                    t1.DocNum,
                    t0.ItemCode,
                    t2.ItemName,
                    CASE WHEN t0.PlannedQty - t0.IssuedQty < 0 THEN 0.000 ELSE t0.PlannedQty - t0.IssuedQty END AS QtyOpen,
                    t1.PlannedQty,
                    t0.VisOrder,
                    t0.IssueType,
                    ISNULL(t2.InvntryUom, '') AS InvntryUoms
                FROM OWOR t1
                INNER JOIN WOR1 t0 ON t0.DocEntry = t1.DocEntry AND t0.ItemType IN (1,2, 4) AND ISNULL(t0.U_QYCAFO, '') = ''
                INNER JOIN OITM t2 ON t0.ItemCode = t2.ItemCode AND t2.InvntItem = 'Y'
                WHERE CAST(t1.DocNum AS NVARCHAR(50)) = '{obj.DocNum}'
                """;

            // finally translate
            var output = translator.Translate(input, out var summary, out var statements, out var errors);

            Console.WriteLine($"SUMMARY:\n{summary}");
            Console.WriteLine($"ERRORS: {errors}/{statements}");
            Console.WriteLine($"INPUT: {input}");
            Console.WriteLine($"OUTPUT: {output}");

            Assert.IsTrue(errors == 0);
        }
    }
}
