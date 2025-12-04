using System;
using System.Collections.Generic;
using FluentAssertions;
using Mystira.App.Application.Parsers;
using Xunit;

namespace Mystira.App.Api.Tests.Parsers;

public class BranchParserTests
{
    [Fact]
    public void Parse_WhenNextSceneIdProvided_SetsNextSceneId()
    {
        // Arrange
        var branchDict = new Dictionary<object, object>
        {
            { "choice", "Test choice" },
            { "nextSceneId", "scene-2" }
        };

        // Act
        var branch = BranchParser.Parse(branchDict);

        // Assert
        branch.Choice.Should().Be("Test choice");
        branch.NextSceneId.Should().Be("scene-2");
    }

    [Fact]
    public void Parse_WhenNextSceneIdMissing_SetsEmptyString()
    {
        // Arrange - branch without nextSceneId (story ending)
        var branchDict = new Dictionary<object, object>
        {
            { "choice", "End the story" }
        };

        // Act
        var branch = BranchParser.Parse(branchDict);

        // Assert
        branch.Choice.Should().Be("End the story");
        branch.NextSceneId.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhenNextSceneIdIsNull_SetsEmptyString()
    {
        // Arrange - branch with null nextSceneId (story ending)
        var branchDict = new Dictionary<object, object>
        {
            { "choice", "End the story" },
            { "nextSceneId", null! }
        };

        // Act
        var branch = BranchParser.Parse(branchDict);

        // Assert
        branch.Choice.Should().Be("End the story");
        branch.NextSceneId.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhenNextSceneIdIsEmpty_SetsEmptyString()
    {
        // Arrange - branch with empty string nextSceneId (story ending)
        var branchDict = new Dictionary<object, object>
        {
            { "choice", "End the story" },
            { "nextSceneId", "" }
        };

        // Act
        var branch = BranchParser.Parse(branchDict);

        // Assert
        branch.Choice.Should().Be("End the story");
        branch.NextSceneId.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhenChoiceMissing_ThrowsArgumentException()
    {
        // Arrange - branch without choice field
        var branchDict = new Dictionary<object, object>
        {
            { "nextSceneId", "scene-2" }
        };

        // Act & Assert
        var act = () => BranchParser.Parse(branchDict);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*choice*");
    }
}
