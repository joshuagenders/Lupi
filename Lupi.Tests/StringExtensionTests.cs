using FluentAssertions;
using Lupi.Listeners;
using Xunit;

namespace Lupi.Tests
{
    public class StringExtensionTests
    {
        [Theory]
        [InlineData("{Value1}", "42")]
        [InlineData("{String1}", "Hello")]
        [InlineData("{String1,10}", "     Hello")]
        [InlineData("{String1,10} String1 {Value1,3}", "     Hello String1  42")]
        public void StringFormatsCorrectly(string format, string result)
        {
            var obj = new
            {
                Value1 = "42",
                String1 = "Hello"
            };
            format.FormatWith(obj).Should().Be(result);
        }
    }
}
