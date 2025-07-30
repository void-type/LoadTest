namespace LoadTest.Test;
using LoadTest.Helpers;

public class ThreadHelpersTests
{
    /// <summary>
    /// Block division
    /// </summary>
    [Theory]
    [InlineData(0, 10, 100, 0, 9)]
    [InlineData(9, 10, 100, 90, 99)]
    [InlineData(9, 10, 101, 99, 100)]
    public void BlocksAreDividedAmongThreadCount(int blockIndex, int blockCount, int totalCount, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }

    /// <summary>
    /// More blocks than items, one item each, then (-1, -1)
    /// </summary>
    [Theory]
    [InlineData(9, 11, 10, 9, 9)]
    [InlineData(10, 11, 10, -1, -1)]
    public void ExcessBlocksGivesSingleItemBlocks(int blockIndex, int blockCount, int totalCount, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }

    /// <summary>
    /// Block index out of range gives (-1, -1)
    /// </summary>
    [Theory]
    [InlineData(12, 10, 10, -1, -1)]
    [InlineData(12, 10, 11, -1, -1)]
    [InlineData(-1, 10, 10, -1, -1)]
    public void OutOfRangeBlockGivesNegativeValues(int blockIndex, int blockCount, int totalCount, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }

    /// <summary>
    /// 99 items divided amongst 4 blocks nets 25 items per block, last block gets 24 items: block 0 = (0, 24), 1 = (25, 49), 2 = (50, 74), 3 = (75, 98)
    /// Any block out of bounds will return (-1, -1).
    /// </summary>
    [Theory]
    [InlineData(0, 0, 24)]
    [InlineData(1, 25, 49)]
    [InlineData(2, 50, 74)]
    [InlineData(3, 75, 98)]
    [InlineData(4, -1, -1)]
    public void BlocksAreDividedAsDescribedInDocs(int blockIndex, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, 4, 99);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }

    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(1, 4, 7)]
    [InlineData(2, 8, 11)]
    [InlineData(3, 12, 15)]
    [InlineData(4, 16, 19)]
    [InlineData(5, 20, 23)]
    [InlineData(6, 24, 26)]
    [InlineData(7, -1, -1)]
    [InlineData(8, -1, -1)]
    public void BlocksAreDividedAsDescribedInDocs2(int blockIndex, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, 8, 27);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }

    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(1, 4, 7)]
    [InlineData(2, 8, 11)]
    [InlineData(3, 12, 15)]
    [InlineData(4, 16, 19)]
    [InlineData(5, 20, 23)]
    [InlineData(6, 24, 27)]
    [InlineData(7, -1, -1)]
    [InlineData(8, -1, -1)]
    public void BlocksAreDividedAsDescribedInDocs3(int blockIndex, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, 8, 28);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }

    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(1, 4, 7)]
    [InlineData(2, 8, 11)]
    [InlineData(3, 12, 15)]
    [InlineData(4, 16, 19)]
    [InlineData(5, 20, 23)]
    [InlineData(6, 24, 27)]
    [InlineData(7, 28, 28)]
    [InlineData(8, -1, -1)]
    public void BlocksAreDividedAsDescribedInDocs4(int blockIndex, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, 8, 29);

        Assert.Equal(expectedFirst, firstIndex);
        Assert.Equal(expectedLast, lastIndex);
    }
}
