using Xunit;
using GroupProject.Model.LogicModel;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace GroupProject.Tests.Model.LogicModel
{
    public class BoolToColourConverterTests
    {
        [Theory]
        [InlineData(true, "Green")]
        [InlineData(false, "Red")]
        public void Convert_BoolToColor_ReturnsExpectedColor(bool input, string expectedColorName)
        {
            var converter = new BoolToColourConverter();
            var expected = Color.FromArgb(expectedColorName);
            var result = converter.Convert(input, null, null, CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new BoolToColourConverter();
            Assert.Throws<NotImplementedException>(() => converter.ConvertBack(null, null, null, CultureInfo.InvariantCulture));
        }
    }
}
