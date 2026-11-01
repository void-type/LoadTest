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
        Assert.Equal((expectedFirst, expectedLast), ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount));
    }

    /// <summary>
    /// More blocks than items, one item each, then (-1, -1)
    /// </summary>
    [Theory]
    [InlineData(0, 11, 10, 0, 0)]
    [InlineData(9, 11, 10, 9, 9)]
    [InlineData(10, 11, 10, -1, -1)]
    public void ExcessBlocksGivesSingleItemBlocks(int blockIndex, int blockCount, int totalCount, int expectedFirst, int expectedLast)
    {
        Assert.Equal((expectedFirst, expectedLast), ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount));
    }

    /// <summary>
    /// Block index out of range gives (-1, -1)
    /// </summary>
    [Theory]
    [InlineData(12, 10, 10)]
    [InlineData(12, 10, 11)]
    [InlineData(10, 10, 10)]
    [InlineData(-1, 10, 10)]
    public void OutOfRangeBlockGivesNegativeValues(int blockIndex, int blockCount, int totalCount)
    {
        Assert.Equal((-1, -1), ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount));
    }

    /// <summary>
    /// Nothing to divide, or nothing to divide it among
    /// </summary>
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 4, 0)]
    [InlineData(0, 0, 10)]
    [InlineData(0, -1, 10)]
    public void NoItemsOrNoBlocksGivesNegativeValues(int blockIndex, int blockCount, int totalCount)
    {
        Assert.Equal((-1, -1), ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount));
    }

    /// <summary>
    /// A single block gets every item
    /// </summary>
    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(2, 0, 1)]
    [InlineData(50, 0, 49)]
    public void SingleBlockGetsAllItems(int totalCount, int expectedFirst, int expectedLast)
    {
        Assert.Equal((expectedFirst, expectedLast), ThreadHelpers.GetBlockStartAndEnd(0, 1, totalCount));
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
        Assert.Equal((expectedFirst, expectedLast), ThreadHelpers.GetBlockStartAndEnd(blockIndex, 4, 99));
    }

    /// <summary>
    /// With 8 blocks, 27 items leave the last full block short and the final block empty.
    /// </summary>
    [Theory]
    [InlineData(0, 27, 0, 3)]
    [InlineData(5, 27, 20, 23)]
    [InlineData(6, 27, 24, 26)]
    [InlineData(7, 27, -1, -1)]
    [InlineData(6, 28, 24, 27)]
    [InlineData(7, 28, -1, -1)]
    [InlineData(6, 29, 24, 27)]
    [InlineData(7, 29, 28, 28)]
    [InlineData(8, 29, -1, -1)]
    public void LastBlocksHandleRemainders(int blockIndex, int totalCount, int expectedFirst, int expectedLast)
    {
        Assert.Equal((expectedFirst, expectedLast), ThreadHelpers.GetBlockStartAndEnd(blockIndex, 8, totalCount));
    }

    /// <summary>
    /// Every item belongs to exactly one block: blocks are in order, don't overlap, don't skip items, and any empty blocks come last.
    /// </summary>
    [Fact]
    public void BlocksCoverEveryItemExactlyOnce()
    {
        for (var totalCount = 1; totalCount <= 60; totalCount++)
        {
            for (var blockCount = 1; blockCount <= 70; blockCount++)
            {
                var covered = new List<int>();
                var sawEmptyBlock = false;

                for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    var (first, last) = ThreadHelpers.GetBlockStartAndEnd(blockIndex, blockCount, totalCount);
                    var context = $"block {blockIndex} of {blockCount}, total {totalCount}";

                    if (first == -1)
                    {
                        Assert.True(last == -1, context);
                        sawEmptyBlock = true;
                        continue;
                    }

                    Assert.False(sawEmptyBlock, $"Non-empty block after an empty block: {context}");
                    Assert.True(first <= last, context);
                    covered.AddRange(Enumerable.Range(first, last - first + 1));
                }

                Assert.True(
                    Enumerable.Range(0, totalCount).SequenceEqual(covered),
                    $"Items not covered exactly once for {blockCount} blocks, total {totalCount}: [{string.Join(",", covered)}]");
            }
        }
    }
}
