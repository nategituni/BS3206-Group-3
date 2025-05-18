using Xunit;
using GroupProject.Model.LogicModel;
using System.Globalization;

namespace GroupProject.Tests.Model.LogicModel
{
    public class BoolToBinaryConverterTests
    {
        [Theory]
        [InlineData(true, "1")]
        [InlineData(false, "0")]
        [InlineData("true", "1")]
        [InlineData("false", "0")]
        public void Convert_ValidInputs_ReturnsExpectedString(object input, string expected)
        {
            var converter = new BoolToBinaryConverter();
            var result = converter.Convert(input, null, null, CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Convert_InvalidInput_ReturnsZero()
        {
            var converter = new BoolToBinaryConverter();
            var result = converter.Convert(123, null, null, CultureInfo.InvariantCulture);
            Assert.Equal("0", result);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void ConvertBack_ValidString_ReturnsBool(string input, bool expected)
        {
            var converter = new BoolToBinaryConverter();
            var result = converter.ConvertBack(input, null, null, CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }
    }
}
