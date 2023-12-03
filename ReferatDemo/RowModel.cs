using CommunityToolkit.Mvvm.ComponentModel;

namespace ReferatDemo;

public enum Gender
{
    Male,
    Female,
    Other,
}

public sealed partial class RowModel : ObservableObject
{
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _lastName;
    [ObservableProperty] private int _age;
    [ObservableProperty] private Gender _gender;
}

public static class DataGenerationHelper
{
    private static readonly string[] _Names =
    {
        "Alex",
        "John",
        "Mary",
        "Jane",
        "Bob",
        "Alice",
        "Eve",
        "Carol",
    };

    private static readonly string[] _LastNames =
    {
        "Smith",
        "Johnson",
        "Williams",
        "Jones",
        "Brown",
        "Davis",
        "Miller",
        "Wilson",
    };

    public static IEnumerable<RowModel> GenerateRandomData(Random random, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var firstName = _Names[random.Next(_Names.Length)];
            var lastName = _LastNames[random.Next(_LastNames.Length)];
            var gender = (Gender) random.Next(3);
            var age = random.Next(20, 60);
            yield return new RowModel
            {
                FirstName = firstName,
                LastName = lastName,
                Gender = gender,
                Age = age,
            };
        }
    }
}
