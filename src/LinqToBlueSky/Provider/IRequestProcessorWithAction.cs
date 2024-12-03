using System.Diagnostics.CodeAnalysis;

namespace LinqToBlueSky.Provider;

// TODO: might not be necessary anymore - originally conceived in refactoring from XML to JSON

// Declare that this request processor knows how to handle action
// responses, implies the request processor also wants native JSON objects.
public interface IRequestProcessorWithAction<T>
    : IRequestProcessorWantsJson
{
    [return: MaybeNull]
    T ProcessActionResult(string blueSkyResponse, Enum theAction);
}
