﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        // REVIEW: This is a non-standard LINQ operator, because we don't have a non-generic IAsyncEnumerable.
        //
        //         Unfortunately, this has limited use because it requires the source to be IAsyncEnumerable<object>,
        //         thus it doesn't bind for value types. Adding a first generic parameter for the element type of
        //         the source is not an option, because it would require users to specify two type arguments, unlike
        //         what's done in Enumerable.OfType. Should we move this method to Ix, thus doing away with OfType
        //         in the API surface altogether?

        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                await foreach (var obj in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    if (obj is TResult result)
                    {
                        yield return result;
                    }
                }
            }
#else
            return new OfTypeAsyncIterator<TResult>(source);
#endif
        }

#if !USE_ASYNC_ITERATOR
        private sealed class OfTypeAsyncIterator<TResult> : AsyncIterator<TResult>
        {
            private readonly IAsyncEnumerable<object> _source;
            private IAsyncEnumerator<object> _enumerator;

            public OfTypeAsyncIterator(IAsyncEnumerable<object> source)
            {
                _source = source;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new OfTypeAsyncIterator<TResult>(_source);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        while (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;
                            if (item is TResult res)
                            {
                                _current = res;
                                return true;
                            }
                        }

                        await DisposeAsync().ConfigureAwait(false);
                        break;
                }

                return false;
            }
        }
#endif
    }
}
