using SqlExpressions.OrderBy;
using Xunit;
using Xunit.Abstractions;

namespace SqlExpressions.Tests.OrderBy
{

    class Order
    {
        public string OrderNo { get; set; } = string.Empty;
        public decimal Charge { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SqlCompilingTests
    {
        private readonly ITestOutputHelper output;

        public SqlCompilingTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("OrderNo", "\"Orders\".\"OrderNo\"")]
        [InlineData("OrderNo asc", "\"Orders\".\"OrderNo\"")]
        [InlineData("OrderNo desc", "\"Orders\".\"OrderNo\" desc")]
        [InlineData("OrderNo asc, OrderDate desc", "\"Orders\".\"OrderNo\", \"Orders\".\"OrderDate\" desc")]
        public void FinalTests(string test, string expected)
        {
            var expression = test.ParseOrderBy();
            output.WriteLine(expression.ToString());
            var where = expression.Compile(PropertyMapper);
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