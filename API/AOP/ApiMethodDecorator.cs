using System.Diagnostics;
using System.Reflection;
using MethodDecorator.Fody.Interfaces;
using Serilog;
using ILogger = Serilog.ILogger;

namespace API.AOP;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly |
                AttributeTargets.Module)]
public class ApiMethodDecorator : Attribute, IMethodDecorator
{
    // Fody instantiates per call — mutable fields are safe here
    private ILogger _logger = Log.Logger;
    private string _methodName = "";
    private int _argCount;
    private Stopwatch _stopwatch = new();

    public void Init(object instance, MethodBase method, object[] args)
    {
        _logger = Log.Logger;
        _methodName = $"{method.DeclaringType?.FullName}.{method.Name}";
        _argCount = args.Length;
        _logger.Information("Entering {MethodName} ({ArgCount} args)", _methodName, _argCount);
        _stopwatch = Stopwatch.StartNew();
    }

    public void OnEntry()
    {
    }

    public void OnExit()
    {
        _stopwatch.Stop();
        _logger.Information(
            "Exiting {MethodName} after {Duration}ms",
            _methodName,
            _stopwatch.ElapsedMilliseconds);
    }

    public void OnException(Exception exception)
    {
        _stopwatch.Stop();
        _logger.Error(
            exception,
            "Exception in {MethodName} ({ArgCount} args) after {Duration}ms",
            _methodName,
            _argCount,
            _stopwatch.ElapsedMilliseconds);
    }
}