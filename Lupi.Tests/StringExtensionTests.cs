using Lupi.Listeners;

namespace Lupi.Tests
{
    public class StringExtensionTests
    {
        [Theory]
        [InlineData("{Value1}", "42")]
        [InlineData("{String1}", "Hello")]
        [InlineData("{String1,10}", "     Hello")]
        [InlineData("{String1,10} String1 {Value1,3}", "     Hello String1  42")]
        [InlineData("{Double1,10:N}", "     42.46")]
        public void StringFormatsCorrectly(string format, string result)
        {
            var obj = new
            {
                Value1 = "42",
                String1 = "Hello",
                Double1 = 42.4567d
            };
            format.FormatWith(obj).Should().Be(result);
        }
    }
}
