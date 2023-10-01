using LoadTest.Helpers;

for (int j = 99; j < 110; j++)
{
    for (int i = -1; i < 5; i++)
    {
        (int start, int end) = ThreadHelpers.GetBlockStartAndEnd(i, 4, j);
        Console.WriteLine($"({start}, {end}) {end - start + 1}");
    }

    Console.WriteLine();
}

