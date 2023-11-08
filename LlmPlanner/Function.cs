using System;

namespace PlanningTest;

public abstract class Function
{
    public string Description { get; protected set; }
        = string.Empty;

    public Type ResultType { get; private set; }

    public List<object> Results { get; private set; }
        = new();

    public bool IsResultList { get; private set; } 
        = false;

    public Dictionary<string, string> Inputs { get; private set; }
        = new();

    protected static Dictionary<Type, Func<object>> _dependencies { get; private set; }
        = new();
    protected static Dictionary<Type, object> _cache { get; private set; }
        = new();

    public Function(
        string description,
        Type resultType,
        bool isResultList = false)
    {
        this.Description = description;
        this.ResultType = resultType;
        this.IsResultList = isResultList;
    }

    public async Task Run()
    {
        string response = await this.RunPrompt();
        List<object> results = this.ExtractResult(response);
        this.Results.Clear();
        this.Results.AddRange(results);
    }

    public static void InjectDependency<T>(Func<T> func)
    {
        if (func == null)
        {
            throw new NullReferenceException("func");
        }
        _dependencies[typeof(T)] = () => func.Invoke();
    }

    // only support singleton dependencies
    protected static T GetDependency<T>()
    {
        if (!_cache.ContainsKey(typeof(T)))
        {
            _cache[typeof(T)] = _dependencies[typeof(T)].Invoke();
        }

        return (T)_cache[typeof(T)];
    }

    protected abstract List<object> ExtractResult(string input);

    protected abstract Task<string> RunPrompt();
}
