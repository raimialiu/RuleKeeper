// Sample file with intentional rule violations for testing RuleKeeper

namespace TestProject;

// Violation: CS-NAME-001 - Class should use PascalCase
class badClassName
{
    // Violation: CS-NAME-004 - Private field should use _camelCase
    private string BadFieldName = "test";

    // Violation: CS-NAME-006 - Property should use PascalCase
    public string badPropertyName { get; set; } = "";

    // Violation: CS-SEC-002 - Hardcoded secret
    private const string ApiKey = "sk-1234567890abcdef";

    // Violation: CS-DESIGN-002 - Too many parameters
    public void MethodWithTooManyParameters(
        string param1,
        string param2,
        string param3,
        int param4,
        int param5,
        bool param6,
        DateTime param7)
    {
        Console.WriteLine("Too many parameters!");
    }

    // Violation: CS-ASYNC-001 - Async void should return Task
    public async void ProcessAsync()
    {
        await Task.Delay(100);
    }

    // Violation: CS-ASYNC-002 - Blocking on async
    public void BlockingMethod()
    {
        var task = Task.Run(() => "Hello");
        var result = task.Result; // Should use await
        Console.WriteLine(result);
    }

    // Violation: CS-EXC-001 - Empty catch block
    public void EmptyCatch()
    {
        try
        {
            throw new Exception("Test");
        }
        catch (Exception)
        {
            // Empty catch block - violation!
        }
    }

    // Violation: CS-NAME-003 - Async method should end with Async
    public async Task<string> GetData()
    {
        await Task.Delay(10);
        return "data";
    }

    // Violation: CS-SEC-001 - Potential SQL injection
    public void ExecuteQuery(string userInput)
    {
        var query = "SELECT * FROM Users WHERE Name = '" + userInput + "'";
        Console.WriteLine(query);
    }

    // Violation: CS-DI-001 - Injecting concrete type
    public badClassName(UserService userService)
    {
        // Should inject IUserService instead
    }
}

// Violation: CS-NAME-007 - Interface should start with 'I'
interface UserRepository
{
    void Save();
}

// Good class for contrast
public class GoodClass : IDisposable
{
    private readonly string _privateField;

    public string PublicProperty { get; set; } = "";

    public GoodClass(string value)
    {
        _privateField = value;
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(10);
        return _privateField;
    }

    public void HandleError()
    {
        try
        {
            throw new Exception("Test");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Handled: {ex.Message}");
        }
    }

    public void Dispose()
    {
        // Cleanup
    }
}

// Stub service for testing DI rule
public class UserService
{
    public void Process() { }
}
