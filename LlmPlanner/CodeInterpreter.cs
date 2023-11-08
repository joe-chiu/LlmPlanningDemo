using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PlanningTest;

public class CodeInterpreter
{
    protected Dictionary<string, Function> functionMap = new();
    // need typed heap to store object or list of objects
    protected Dictionary<string, object> heap = new();

    public async Task<string> Run(string code, IEnumerable<Function> functions)
    {
        Regex printExpression = new Regex(@"print\(([^)]+)\)");
        Regex assignmentExpression = new Regex(@"([^\s=]+)\s*=\s*(.+)");
        StringBuilder output = new();
        this.functionMap = new();

        foreach (Function func in functions)
        {
            functionMap[func.GetType().Name] = func;
        }

        foreach (string lineOfCode in code.Split("\n", StringSplitOptions.RemoveEmptyEntries))
        {
            string line = lineOfCode.Trim();
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (assignmentExpression.IsMatch(line))
            {
                Match match = assignmentExpression.Match(line);
                string lhsVariable = match.Groups[1].Value;
                // for assignment to local variable, keep the object form
                object rhsValue = await Eval(match.Groups[2].Value);
                this.heap[lhsVariable] = rhsValue;
            }
            else if (printExpression.IsMatch(line))
            {
                Match match = printExpression.Match(line);
                // for print() input expression, should always convert to string
                string rhsValue = (await Eval(match.Groups[1].Value)).ToString() ?? string.Empty;
                output.Append(rhsValue + "\n");
            }
        }

        return output.ToString();
    }

    protected async Task<object> Eval(string expression)
    {
        expression = expression.Trim();
        object rhsValue = string.Empty;
        Regex functionCall = new Regex(@"([^\(]+)\s*\(([^\)]+)\)");
        Regex functionParam = new Regex(@"([^:]+)\s*:\s*(.+)");
        Regex numberLiteral = new Regex(@"^(\d+)$");
        Regex stringLiteral = new Regex(@"""([^""]+)""");
        Regex arrayItemField = new Regex(@"([^[]+)\[(\d+)\]\.(.+)");
        Regex objectField = new Regex(@"([^.]+)\.(.+)");
        Regex arrayItem = new Regex(@"([^[]+)\[(\d+)\]");

        if (functionCall.IsMatch(expression))
        {
            // this 
            Dictionary<string, string> paramValues = new();
            Match match = functionCall.Match(expression);
            string functionName = match.Groups[1].Value;
            string paramsString = match.Groups[2].Value;
            string[] paramParts = paramsString.Split(",", StringSplitOptions.TrimEntries);

            foreach (string paramPart in paramParts)
            {
                Match paramMatch = functionParam.Match(paramPart);
                string paramName = paramMatch.Groups[1].Value;
                string paramValue = (await Eval(paramMatch.Groups[2].Value)).ToString() ?? string.Empty;
                paramValues[paramName] = paramValue;
            }

            // function only support string parameters, but return value can be number, string, object or list
            Function func = this.InvokeFunction(functionName, paramValues);
            await func.Run();
            // string result
            if (func.ResultType == typeof(string) && func.Results.Count == 1)
            {
                rhsValue = (string)func.Results[0];
            }
            // number result
            else if (func.ResultType == typeof(int) && func.Results.Count == 1)
            {
                rhsValue = (int)func.Results[0];
            }
            // a single object
            else if (func.ResultType != typeof(string) && func.Results.Count == 1)
            {
                rhsValue = func.Results[0];
            }
            // list of objects
            else if (func.ResultType != typeof(string) && func.Results.Count > 1)
            {
                rhsValue = func.Results;
            }
            else
            {
                throw new InvalidCastException("unexpected function result");
            }
        }
        else if (arrayItemField.IsMatch(expression))
        {
            Match match = arrayItemField.Match(expression);
            string variableName = match.Groups[1].Value;
            int index = int.Parse(match.Groups[2].Value);
            string fieldName = match.Groups[3].Value;
            IList list = (IList)this.heap[variableName];
            rhsValue = this.GetValueHelper(list[index], fieldName);
        }
        else if (objectField.IsMatch(expression))
        {
            Match match = objectField.Match(expression);
            string variableName = match.Groups[1].Value;
            string fieldName = match.Groups[2].Value;
            object obj = this.heap[variableName];
            rhsValue = this.GetValueHelper(obj, fieldName);
        }
        else if (this.heap.ContainsKey(expression))
        {
            // this is meant for string and number variables, not object
            rhsValue = this.heap[expression].ToString() ?? string.Empty;
        }
        else if (numberLiteral.IsMatch(expression))
        {
            Match match = numberLiteral.Match(expression);
            rhsValue = match.Groups[1].Value;
        }
        else if (stringLiteral.IsMatch(expression))
        {
            Match match = stringLiteral.Match(expression);
            rhsValue = match.Groups[1].Value;
        }
        else if (arrayItem.IsMatch(expression))
        {
            // arrayItem expression is a subword of arrayItemField, so must be evaluated later
            Match match = arrayItem.Match(expression);
            string variableName = match.Groups[1].Value;
            int index = int.Parse(match.Groups[2].Value);
            IList list = (IList)this.heap[variableName];
            rhsValue = list[index]!;
        }
        return rhsValue;
    }

    protected Function InvokeFunction(string name, Dictionary<string, string> parameters)
    {
        Function function = this.functionMap[name];
        foreach(string paramName in parameters.Keys)
        {
            function.Inputs[paramName] = parameters[paramName];
        }

        return function;
    }

    protected string GetValueHelper(object? obj, string field)
    {
        if (obj == null)
        {
            return string.Empty;
        }

        PropertyInfo[] props = obj.GetType().GetProperties();
        FieldInfo[] fields = obj.GetType().GetFields();
        string val = string.Empty;

        PropertyInfo? prop = props.FirstOrDefault(
            prop => prop.Name.ToLowerInvariant() == field.ToLowerInvariant());
        if (prop != null && prop.GetValue(obj) != null)
        {
            val = prop.GetValue(obj)!.ToString() ?? string.Empty;
        }
        else
        {
            FieldInfo? fieldInfo = fields.FirstOrDefault(
                entry => entry.Name.ToLowerInvariant() == field.ToLowerInvariant());
            if (fieldInfo != null && fieldInfo.GetValue(obj) != null)
            {
                val = fieldInfo.GetValue(obj)!.ToString() ?? string.Empty;
            }
        }

        return val;
    }
}
