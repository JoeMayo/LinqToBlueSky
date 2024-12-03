using System.Linq.Expressions;

namespace LinqToBlueSky.Provider;

public interface IRequestProcessor<T>
{
    string? BaseUrl { get; set; }
    Dictionary<string, string> GetParameters(LambdaExpression lambdaExpression);
    Request BuildUrl(Dictionary<string, string> expressionParameters);
    List<T> ProcessResults(string blueSkyResponse);
}
