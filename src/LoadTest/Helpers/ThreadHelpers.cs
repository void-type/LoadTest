namespace LoadTest.Helpers;

/// <summary>
/// Helpers to divide work among threads.
/// </summary>
public static class ThreadHelpers
{
    /// <summary>
    /// Get the starting and ending index of a block from a collection of items.
    /// For example of 4 blocks out of 15 items: block 0 = (0, 3), 1 = (4, 7), 2 = (8, 11), 3 = (12, 15)
    /// </summary>
    /// <param name="blockIndex">Index of the block of items from the collection. Zero based.</param>
    /// <param name="blockCount">How many blocks to divide the collection of items into.</param>
    /// <param name="totalCount">The total count of items in the collection.</param>
    public static (int firstIndex, int lastIndex) GetBlockStartAndEnd(int blockIndex, int blockCount, int totalCount)
    {
        if (blockIndex >= blockCount)
        {
            return (-1, -1);
        }

        if (blockCount > totalCount)
        {
            return blockIndex < totalCount ?
                (blockIndex, blockIndex) :
                (-1, -1);
        }

        var blockSize = Convert.ToInt32(Math.Floor((double)totalCount / blockCount));

        var firstIndex = blockIndex * blockSize;

        // If the last block, then include the last item, else go to the end of this block.
        var lastIndex = blockIndex == blockCount - 1 ?
            totalCount - 1 :
            (blockSize * (blockIndex + 1)) - 1;

        return (firstIndex, lastIndex);
    }
}
