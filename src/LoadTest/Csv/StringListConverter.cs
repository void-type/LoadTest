using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace LoadTest.Csv;

public class StringListConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is List<string> list)
        {
            return string.Join("; ", list);
        }
        return string.Empty;
    }
}
