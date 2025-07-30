using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace LoadTest.Csv;

public class UriListConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is List<Uri> list)
        {
            return string.Join("; ", list.Select(uri => uri.ToString()));
        }
        return string.Empty;
    }
}
