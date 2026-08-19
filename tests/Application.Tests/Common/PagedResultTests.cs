using Application.Common.Paging;
using System.Collections.Generic;

namespace Application.Tests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void TotalPages_Should_BeCeilingOfTotalDividedByPageSize(int totalCount, int pageSize, int expected)
    {
        var result = new PagedResult<int>(new List<int>(), 1, pageSize, totalCount);
        result.TotalPages.Should().Be(expected);
    }

    [Fact]
    public void TotalPages_Should_BeZero_When_PageSizeIsZero()
    {
        var result = new PagedResult<int>(new List<int>(), 1, 0, 42);
        result.TotalPages.Should().Be(0);
    }
}
