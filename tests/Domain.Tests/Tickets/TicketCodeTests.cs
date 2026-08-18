using Domain.Tickets;
using System.Collections.Generic;

namespace Domain.Tests.Tickets;

public class TicketCodeTests
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    [Fact]
    public void New_Should_ReturnCodeOfExpectedLength()
    {
        TicketCode.New().Length.Should().Be(10);
    }

    [Fact]
    public void New_Should_ContainOnlyAllowedCharacters()
    {
        var code = TicketCode.New();
        code.ToCharArray().Should().OnlyContain(c => Alphabet.Contains(c));
    }

    [Fact]
    public void New_Should_ProduceUniqueCodes_InSmallBatch()
    {
        var codes = new HashSet<string>();
        for (int i = 0; i < 200; i++)
        {
            codes.Add(TicketCode.New());
        }
        codes.Count.Should().Be(200);
    }
}
