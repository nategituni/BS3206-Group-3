using Xunit;
using GroupProject.Model.LogicModel;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace GroupProject.Tests.Model.LogicModel
{
    public class DifficultyToColourConverterTests
    {
        [Theory]
        [InlineData("easy", "Green")]
        [InlineData("medium", "Orange")]
        [InlineData("hard", "Red")]
        [InlineData("unknown", "Gray")]
        public void Convert_DifficultyToColor_ReturnsExpectedColor(string input, string expectedColorName)
        {
            var converter = new DifficultyToColourConverter();
            var expected = Color.FromArgb(expectedColorName);
            var result = converter.Convert(input, null, null, CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new DifficultyToColourConverter();
            Assert.Throws<NotImplementedException>(() => converter.ConvertBack(null, null, null, CultureInfo.InvariantCulture));
        }
    }
}
