using GroupProject.Models;
using System;
using Xunit;

namespace GroupProject.Tests.Models
{
	public class PuzzleModelTests
	{
		[Fact]
		public void Puzzle_Should_Initialize_Correctly()
		{
			// Arrange & Act
			var puzzle = new Puzzle
			{
				Id = 1,
				UserId = 42,
				PuzzleName = "Test Puzzle",
				PuzzleData = "Some serialized data",
				CreatedAt = DateTime.UtcNow,
				Views = 10
			};

			// Assert
			Assert.Equal(1, puzzle.Id);
			Assert.Equal(42, puzzle.UserId);
			Assert.Equal("Test Puzzle", puzzle.PuzzleName);
			Assert.Equal("Some serialized data", puzzle.PuzzleData);
			Assert.Equal(10, puzzle.Views);
			Assert.True((DateTime.UtcNow - puzzle.CreatedAt).TotalSeconds < 5);
		}

		[Fact]
		public void Puzzle_Should_Have_Default_Values()
		{
			// Arrange & Act
			var puzzle = new Puzzle();

			// Assert
			Assert.Equal(0, puzzle.Id);
			Assert.Equal(0, puzzle.UserId);
			Assert.Null(puzzle.PuzzleName);
			Assert.Null(puzzle.PuzzleData);
			Assert.Equal(default(DateTime), puzzle.CreatedAt);
			Assert.Equal(0, puzzle.Views);
		}
	}
}
