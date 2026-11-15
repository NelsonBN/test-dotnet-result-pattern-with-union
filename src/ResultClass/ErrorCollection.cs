using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultClass;

internal static class ErrorCollection
{
    public static ImmutableArray<Error> Create(IEnumerable<Error> errors)
    {
        switch(errors)
        {
            case Error[] array:
                return ImmutableArray.Create(array);
            case List<Error> list:
                return ImmutableArray.Create(CollectionsMarshal.AsSpan(list));
        }

        if(!errors.TryGetNonEnumeratedCount(out var count))
        {
            return ImmutableArray.CreateRange(errors);
        }

        if(count == 0)
        {
            return ImmutableArray<Error>.Empty;
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
