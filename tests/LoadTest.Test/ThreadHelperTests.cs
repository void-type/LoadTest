namespace LoadTest.Test;
using LoadTest.Helpers;

public class ThreadHelperTests
{
    [Theory]
    // Block division
    [InlineData(0, 10, 100, 0, 9)]
    [InlineData(9, 10, 100, 90, 99)]
    [InlineData(9, 10, 101, 90, 100)]
    // More blocks than items
    [InlineData(9, 11, 10, 9, 9)]
    [InlineData(10, 11, 10, -1, -1)]
    // Block index out of range
    [InlineData(12, 10, 10, -1, -1)]
    [InlineData(12, 10, 11, -1, -1)]
    public void BlocksAreDividedAmongThreadCount(int blockIndex, int blockCount, int blockTotalCount, int expectedFirst, int expectedLast)
    {
        (var firstIndex, var lastIndex) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, blockTotalCount);

        Assert.Equal(firstIndex, expectedFirst);
        Assert.Equal(lastIndex, expectedLast);
    }
}