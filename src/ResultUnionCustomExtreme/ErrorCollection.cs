using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultUnionCustomExtreme;

internal static class ErrorCollection
{
    public static ImmutableArray<Error> Create(IEnumerable<Error> errors)
    {
        switch(errors)
        {
            case Error[] array:
                return [.. array];
            case List<Error> list:
                return [.. CollectionsMarshal.AsSpan(list)];
        }

        if(!errors.TryGetNonEnumeratedCount(out var count))
        {
            return [.. errors];
        }

        if(count == 0)
        {
            return [];
        }

        // The array is private to this method, so the immutable wrapper can take it over without a second copy.
        var copy = new Error[count];
        var index = 0;
        foreach(var error in errors)
        {
            copy[index++] = error;
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(copy);
    }
}
