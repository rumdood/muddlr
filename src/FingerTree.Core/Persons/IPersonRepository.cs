namespace FingerTree.Persons;

public interface IPersonRepository
{
    Person? GetPerson(PersonFilter filter);
    List<Person> GetAllPersons();
    AddPersonResult AddPerson(Person person);
    UpdatePersonResult UpdatePerson(Person person);
    bool DeletePerson(Person person);
}

public sealed record AddPersonResult(bool Success, string Message, Person? user = null);
public sealed record UpdatePersonResult(bool Success, string Message);

public class PersonFilter
{
    public long Id { get; set; }
    public string? Locator { get; set; }
    public string[] Relationships { get; set; } = Array.Empty<string>();
}