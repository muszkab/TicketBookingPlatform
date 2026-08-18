using Application.Common.Paging;

namespace Application.Tests.Common;

public class PagingHelpersTests
{
    [Theory]
    [InlineData(0, 10, 20, 1, 10)]
    [InlineData(-5, 10, 20, 1, 10)]
    [InlineData(2, 10, 20, 2, 10)]
    [InlineData(1, 0, 20, 1, 20)]
    [InlineData(1, -3, 20, 1, 20)]
    [InlineData(1, 999, 20, 1, PagingHelpers.MaxPageSize)]
    [InlineData(1, PagingHelpers.MaxPageSize, 20, 1, PagingHelpers.MaxPageSize)]
    public void Normalize_Should_ClampInputs(int page, int pageSize, int defaultPageSize, int expectedPage, int expectedPageSize)
    {
        var (p, ps) = PagingHelpers.Normalize(page, pageSize, defaultPageSize);
        p.Should().Be(expectedPage);
        ps.Should().Be(expectedPageSize);
    }
}
