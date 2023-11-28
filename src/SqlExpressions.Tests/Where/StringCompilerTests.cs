using SqlExpressions.Where;
using Xunit;
using Xunit.Abstractions;

namespace SqlExpressions.Tests.Where
{

    class Order
    {
        public string OrderNo { get; set; } = string.Empty;
        public decimal Charge { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class StringCompilerTests
    {
        private readonly ITestOutputHelper output;

        public StringCompilerTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("OrderNo = '12345'", "\"Orders\".\"OrderNo\" = '12345'")]
        [InlineData("OrderNo <> '12345'", "\"Orders\".\"OrderNo\" <> '12345'")]
        [InlineData("OrderNo > '12345'", "\"Orders\".\"OrderNo\" > '12345'")]
        [InlineData("OrderNo >= '12345'", "\"Orders\".\"OrderNo\" >= '12345'")]
        [InlineData("OrderNo < '12345'", "\"Orders\".\"OrderNo\" < '12345'")]
        [InlineData("OrderNo <= '12345'", "\"Orders\".\"OrderNo\" <= '12345'")]
        [InlineData("OrderDate <= '2022-10-10'", "\"Orders\".\"OrderDate\" <= '2022-10-10'")]
        [InlineData("Charge = 4533.10", "\"Orders\".\"Charge\" = 4533.10")]
        [InlineData("4533.10 = Charge", "4533.10 = \"Orders\".\"Charge\"")]
        [InlineData("OrderNo is null", "\"Orders\".\"OrderNo\" is null")]
        [InlineData("OrderNo is not null", "\"Orders\".\"OrderNo\" is not null")]
        [InlineData("OrderNo like '2342%'", "\"Orders\".\"OrderNo\" like '2342%'")]
        [InlineData("OrderNo not like '2342%'", "\"Orders\".\"OrderNo\" not like '2342%'")]
        [InlineData("OrderDate >= '2021-01-01' and OrderDate <= '2022-01-01'",
            "\"Orders\".\"OrderDate\" >= '2021-01-01' and \"Orders\".\"OrderDate\" <= '2022-01-01'")]
        [InlineData("OrderDate is null and (OrderDate >= '2021-01-01' and OrderDate <= '2022-01-01')",
            "\"Orders\".\"OrderDate\" is null and (\"Orders\".\"OrderDate\" >= '2021-01-01' and \"Orders\".\"OrderDate\" <= '2022-01-01')")]
        [InlineData("OrderDate is null or (OrderDate >= '2021-01-01' and OrderDate <= '2022-01-01')",
            "\"Orders\".\"OrderDate\" is null or (\"Orders\".\"OrderDate\" >= '2021-01-01' and \"Orders\".\"OrderDate\" <= '2022-01-01')")]
        [InlineData("OrderDate is null or OrderDate >= '2021-01-01' and OrderDate <= '2022-01-01'",
            "(\"Orders\".\"OrderDate\" is null or \"Orders\".\"OrderDate\" >= '2021-01-01') and \"Orders\".\"OrderDate\" <= '2022-01-01'")]
        [InlineData("OrderDate >= '2021-01-01' and OrderDate <= '2022-01-01' or OrderDate is null",
            "(\"Orders\".\"OrderDate\" >= '2021-01-01' and \"Orders\".\"OrderDate\" <= '2022-01-01') or \"Orders\".\"OrderDate\" is null")]
        [InlineData("Status in ['C', 'P']",
            "\"Orders\".\"Status\" in ('C','P')")]
        [InlineData("Status not in ['C', 'P']",
            "\"Orders\".\"Status\" not in ('C','P')")]
        public void FinalTests(string test, string expected)
        {
            var expression = test.ParseWhere();
            output.WriteLine(expression.ToString());
            var where = expression.CompileToString(PropertyMapper);
            Assert.Equal(expected, @where);
        }
        
        private static readonly Type OrderType = typeof(Order);

        private static readonly Func<string, string> PropertyMapper = property =>
        {
            return OrderType.GetProperties().Any(p =>
                string.Equals(p.Name, property, StringComparison.InvariantCultureIgnoreCase))
                ? $"\"Orders\".\"{property}\""
                : throw new Exception($"Unknown property of {property}");
        };
    }
}