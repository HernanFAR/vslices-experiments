using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace VSlicesLanguageExtIntegration;

interface IFeature<TResult> { }
record Feature(string Name) : IFeature<Unit>;

interface IFailure
{
    string Title { get; }

    string? Detail { get; }

    Dictionary<string, object?> Extensions { get; }

}

public sealed record DefaultFailure(
    string Title,
    string? Detail,
    Dictionary<string, object?> Extensions) : IFailure
{
    public DefaultFailure(string title, string? detail)
        : this(title, detail, new Dictionary<string, object?>()) { }
}

interface IHandler<in TRequest, TResult> : IFeature<TResult> 
    where TRequest : IFeature<TResult>
{
    Aff<TResult> HandlerAsync(TRequest request, CancellationToken cancellationToken);

}

class Handler : IHandler<Feature, Unit>
{
    public Aff<Unit> HandlerAsync(Feature request, CancellationToken cancellationToken)
    {
        return Aff(async () =>
        {
            return unit;
        });
    }
}