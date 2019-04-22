﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static IAsyncEnumerable<TSource> Intersect<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second) =>
            Intersect(first, second, comparer: null);

        public static IAsyncEnumerable<TSource> Intersect<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null)
                throw Error.ArgumentNull(nameof(first));
            if (second == null)
                throw Error.ArgumentNull(nameof(second));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TSource> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TSource> Core(CancellationToken cancellationToken)
#endif
            {
                var set = new Set<TSource>(comparer);

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(second, cancellationToken).ConfigureAwait(false))
                {
                    set.Add(element);
                }

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(first, cancellationToken).ConfigureAwait(false))
                {
                    if (set.Remove(element))
                    {
                        yield return element;
                    }
                }
            }
#else
            return new IntersectAsyncIterator<TSource>(first, second, comparer);
#endif
        }

#if !USE_ASYNC_ITERATOR
        private sealed class IntersectAsyncIterator<TSource> : AsyncIterator<TSource>
        {
            private readonly IEqualityComparer<TSource> _comparer;
            private readonly IAsyncEnumerable<TSource> _first;
            private readonly IAsyncEnumerable<TSource> _second;

            private IAsyncEnumerator<TSource> _firstEnumerator;
            private Set<TSource> _set;

            public IntersectAsyncIterator(IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);

                _first = first;
                _second = second;
                _comparer = comparer;
            }

            public override AsyncIteratorBase<TSource> Clone()
            {
                return new IntersectAsyncIterator<TSource>(_first, _second, _comparer);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_firstEnumerator != null)
                {
                    await _firstEnumerator.DisposeAsync().ConfigureAwait(false);
                    _firstEnumerator = null;
                }

                _set = null;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                // NB: Earlier implementations of this operator constructed the set for the second source concurrently
                //     with the first MoveNextAsync call on the first source. This resulted in an unexpected source of
                //     concurrency, which isn't a great default behavior because it's very hard to suppress or control
                //     this behavior.

                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _set = await AsyncEnumerableHelpers.ToSet(_second, _comparer, _cancellationToken).ConfigureAwait(false);
                        _firstEnumerator = _first.GetAsyncEnumerator(_cancellationToken);

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:

                        bool moveNext;
                        do
                        {
                            moveNext = await _firstEnumerator.MoveNextAsync().ConfigureAwait(false);

                            if (moveNext)
                            {
                                var item = _firstEnumerator.Current;
                                if (_set.Remove(item))
                                {
                                    _current = item;
                                    return true;
                                }
                            }
                        } while (moveNext);

                        await DisposeAsync().ConfigureAwait(false);
                        break;
                }

                return false;
            }
        }
#endif
    }
}
