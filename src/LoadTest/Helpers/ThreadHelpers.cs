namespace LoadTest.Helpers
{
    public class ThreadHelpers
    {
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

            // Thread number is zero-based
            var firstIndex = blockIndex * blockSize;

            // If the last block, then include the last item, else go to the end of this block.
            var lastIndex = blockIndex == blockCount - 1 ?
                totalCount - 1 :
                (blockSize * (blockIndex + 1)) - 1;

            return (firstIndex, lastIndex);
        }
    }
}
