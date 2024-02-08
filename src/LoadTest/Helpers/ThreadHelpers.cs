namespace LoadTest.Helpers;

/// <summary>
/// Helpers to divide work among threads.
/// </summary>
public static class ThreadHelpers
{
    /// <summary>
    /// Get the starting and ending index of a block from a collection of items.
    /// 99 items divided amongst 4 blocks nets 25 items per block, last block gets 24 items: block 0 = (0, 24), 1 = (25, 49), 2 = (50, 74), 3 = (75, 98)
    /// Any block out of bounds will return (-1, -1).
    /// </summary>
    /// <param name="blockIndex">Index of the block of items from the collection. Zero based.</param>
    /// <param name="blockCount">How many blocks to divide the collection of items into.</param>
    /// <param name="totalCount">The total count of items in the collection.</param>
    public static (int firstIndex, int lastIndex) GetBlockStartAndEnd(int blockIndex, int blockCount, int totalCount)
    {
        if (blockIndex >= blockCount || blockIndex < 0)
        {
            return (-1, -1);
        }

        if (blockCount > totalCount)
        {
            return blockIndex < totalCount ? (blockIndex, blockIndex) : (-1, -1);
        }

        var blockSize = Convert.ToInt32(Math.Ceiling((double)totalCount / blockCount));

        var firstIndex = blockIndex * blockSize;

        // If the firstIndex is on the edge, check to see if the last item included it.
        if (firstIndex >= totalCount - 1)
        {
            var (_, lastLastIndex) = GetBlockStartAndEnd(blockIndex - 1, blockCount, totalCount);

            // If the last item included it, then this block is out of bounds.
            if (lastLastIndex >= totalCount - 1 || lastLastIndex == -1)
            {
                return (-1, -1);
            }

            firstIndex = Math.Min(firstIndex, totalCount - 1);
        }

        // If the last block, then include the last item, else go to the end of this block.
        var lastIndex = blockIndex == blockCount - 1
            ? totalCount - 1
            : Math.Min((blockSize * (blockIndex + 1)) - 1, totalCount - 1);

        return (firstIndex, lastIndex);
    }
}
