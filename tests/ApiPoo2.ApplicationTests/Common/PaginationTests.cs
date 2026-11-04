using ApiPoo2.Application.Common;
using FluentAssertions;

namespace ApiPoo2.Application.Tests.Common;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(3, 20, 40)]
    public void PagedFilter_SkipCalculatesCorrectly(int page, int pageSize, int expectedSkip)
        => new PagedFilter(page, pageSize).Skip.Should().Be(expectedSkip);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 0)]
    [InlineData(10, 5)]
    [InlineData(10, 3)]
    public void PagedResult_TotalPagesCalculatesCorrectly(int total, int pageSize)
    {
        var expected = pageSize <= 0 || total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        new PagedResult<string>([], total, 1, pageSize).TotalPages.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void PagedResult_HasPreviousCalculatesCorrectly(int page, bool expected)
        => new PagedResult<string>([], 100, page, 10).HasPrevious.Should().Be(expected);

    [Theory]
    [InlineData(10, 100, 10, false)]
    [InlineData(1, 100, 10, true)]
    [InlineData(5, 100, 10, true)]
    [InlineData(1, 10, 10, false)]
    public void PagedResult_HasNextCalculatesCorrectly(int page, int total, int pageSize, bool expected)
        => new PagedResult<string>([], total, page, pageSize).HasNext.Should().Be(expected);

    [Fact]
    public void PagedResult_EmptyList_HasNoPages()
    {
        var result = new PagedResult<string>([], 0, 1, 10);

        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }
}
