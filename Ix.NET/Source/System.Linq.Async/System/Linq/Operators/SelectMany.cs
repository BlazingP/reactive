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
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));

            return new SelectManyAsyncIterator<TSource, TResult>(source, selector);
        }

        // REVIEW: Should we keep these overloads that return ValueTask<IAsyncEnumerable<TResult>>? One could argue the selector is async twice.

        internal static IAsyncEnumerable<TResult> SelectManyAwaitCore<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<IAsyncEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));

            return new SelectManyAsyncIteratorWithTask<TSource, TResult>(source, selector);
        }

#if !NO_DEEP_CANCELLATION
        internal static IAsyncEnumerable<TResult> SelectManyAwaitWithCancellationCore<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<IAsyncEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));

            return new SelectManyAsyncIteratorWithTaskAndCancellation<TSource, TResult>(source, selector);
        }
#endif

        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, IAsyncEnumerable<TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                var index = -1;

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    checked
                    {
                        index++;
                    }

                    var inner = selector(element, index);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
#else
            return new SelectManyWithIndexAsyncIterator<TSource, TResult>(source, selector);
#endif
        }

        internal static IAsyncEnumerable<TResult> SelectManyAwaitCore<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, ValueTask<IAsyncEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                var index = -1;

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    checked
                    {
                        index++;
                    }

                    var inner = await selector(element, index).ConfigureAwait(false);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
#else
            return new SelectManyWithIndexAsyncIteratorWithTask<TSource, TResult>(source, selector);
#endif
        }

#if !NO_DEEP_CANCELLATION
        internal static IAsyncEnumerable<TResult> SelectManyAwaitWithCancellationCore<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, CancellationToken, ValueTask<IAsyncEnumerable<TResult>>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (selector == null)
                throw Error.ArgumentNull(nameof(selector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                var index = -1;

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    checked
                    {
                        index++;
                    }

                    var inner = await selector(element, index, cancellationToken).ConfigureAwait(false);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
#else
            return new SelectManyWithIndexAsyncIteratorWithTaskAndCancellation<TSource, TResult>(source, selector);
#endif
        }
#endif

        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    var inner = collectionSelector(element);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }
#else
            return new SelectManyAsyncIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
#endif
        }

        internal static IAsyncEnumerable<TResult> SelectManyAwaitCore<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, ValueTask<TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    var inner = await collectionSelector(element).ConfigureAwait(false);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement).ConfigureAwait(false);
                    }
                }
            }
#else
            return new SelectManyAsyncIteratorWithTask<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
#endif
        }

#if !NO_DEEP_CANCELLATION
        internal static IAsyncEnumerable<TResult> SelectManyAwaitWithCancellationCore<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    var inner = await collectionSelector(element, cancellationToken).ConfigureAwait(false);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
#else
            return new SelectManyAsyncIteratorWithTaskAndCancellation<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
#endif
        }
#endif

        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, IAsyncEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                var index = -1;

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    checked
                    {
                        index++;
                    }

                    var inner = collectionSelector(element, index);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }
#else
            return new SelectManyWithIndexAsyncIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
#endif
        }

        internal static IAsyncEnumerable<TResult> SelectManyAwaitCore<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, ValueTask<TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                var index = -1;

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    checked
                    {
                        index++;
                    }

                    var inner = await collectionSelector(element, index).ConfigureAwait(false);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement).ConfigureAwait(false);
                    }
                }
            }
#else
            return new SelectManyWithIndexAsyncIteratorWithTask<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
#endif
        }

#if !NO_DEEP_CANCELLATION
        internal static IAsyncEnumerable<TResult> SelectManyAwaitWithCancellationCore<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, CancellationToken, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (collectionSelector == null)
                throw Error.ArgumentNull(nameof(collectionSelector));
            if (resultSelector == null)
                throw Error.ArgumentNull(nameof(resultSelector));

#if USE_ASYNC_ITERATOR
#if HAS_ASYNC_ENUMERABLE_CANCELLATION
            return Core();

            async IAsyncEnumerable<TResult> Core([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellationToken = default)
#else
            return Create(Core);

            async IAsyncEnumerator<TResult> Core(CancellationToken cancellationToken)
#endif
            {
                var index = -1;

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(source, cancellationToken).ConfigureAwait(false))
                {
                    checked
                    {
                        index++;
                    }

                    var inner = await collectionSelector(element, index, cancellationToken).ConfigureAwait(false);

                    await foreach (var subElement in AsyncEnumerableExtensions.WithCancellation(inner, cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
#else
            return new SelectManyWithIndexAsyncIteratorWithTaskAndCancellation<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
#endif
        }
#endif

        private sealed class SelectManyAsyncIterator<TSource, TResult> : AsyncIterator<TResult>, IAsyncIListProvider<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, IAsyncEnumerable<TResult>> _selector;
            private readonly IAsyncEnumerable<TSource> _source;

            private int _mode;
            private IAsyncEnumerator<TResult> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyAsyncIterator(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyAsyncIterator<TSource, TResult>(_source, _selector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            public ValueTask<int> GetCountAsync(bool onlyIfCheap, CancellationToken cancellationToken)
            {
                if (onlyIfCheap)
                {
                    return new ValueTask<int>(-1);
                }

                return Core(cancellationToken);

                async ValueTask<int> Core(CancellationToken _cancellationToken)
                {
                    var count = 0;

                    await foreach (var element in AsyncEnumerableExtensions.WithCancellation(_source, _cancellationToken).ConfigureAwait(false))
                    {
                        checked
                        {
                            count += await _selector(element).CountAsync().ConfigureAwait(false);
                        }
                    }

                    return count;
                }
            }

            public async ValueTask<TResult[]> ToArrayAsync(CancellationToken cancellationToken)
            {
                // REVIEW: Substitute for SparseArrayBuilder<T> logic once we have access to that.

                var list = await ToListAsync(cancellationToken).ConfigureAwait(false);

                return list.ToArray();
            }

            public async ValueTask<List<TResult>> ToListAsync(CancellationToken cancellationToken)
            {
                var list = new List<TResult>();

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(_source, cancellationToken).ConfigureAwait(false))
                {
                    var items = _selector(element);

                    await list.AddRangeAsync(items, cancellationToken).ConfigureAwait(false);
                }

                return list;
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    var inner = _selector(_sourceEnumerator.Current);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultEnumerator.Current;
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

        private sealed class SelectManyAsyncIteratorWithTask<TSource, TResult> : AsyncIterator<TResult>, IAsyncIListProvider<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, ValueTask<IAsyncEnumerable<TResult>>> _selector;
            private readonly IAsyncEnumerable<TSource> _source;

            private int _mode;
            private IAsyncEnumerator<TResult> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyAsyncIteratorWithTask(IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<IAsyncEnumerable<TResult>>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyAsyncIteratorWithTask<TSource, TResult>(_source, _selector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            public ValueTask<int> GetCountAsync(bool onlyIfCheap, CancellationToken cancellationToken)
            {
                if (onlyIfCheap)
                {
                    return new ValueTask<int>(-1);
                }

                return Core(cancellationToken);

                async ValueTask<int> Core(CancellationToken _cancellationToken)
                {
                    var count = 0;

                    await foreach (var element in AsyncEnumerableExtensions.WithCancellation(_source, _cancellationToken).ConfigureAwait(false))
                    {
                        var items = await _selector(element).ConfigureAwait(false);

                        checked
                        {
                            count += await items.CountAsync().ConfigureAwait(false);
                        }
                    }

                    return count;
                }
            }

            public async ValueTask<TResult[]> ToArrayAsync(CancellationToken cancellationToken)
            {
                // REVIEW: Substitute for SparseArrayBuilder<T> logic once we have access to that.

                var list = await ToListAsync(cancellationToken).ConfigureAwait(false);

                return list.ToArray();
            }

            public async ValueTask<List<TResult>> ToListAsync(CancellationToken cancellationToken)
            {
                var list = new List<TResult>();

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(_source, cancellationToken).ConfigureAwait(false))
                {
                    var items = await _selector(element).ConfigureAwait(false);

                    await list.AddRangeAsync(items, cancellationToken).ConfigureAwait(false);
                }

                return list;
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    var inner = await _selector(_sourceEnumerator.Current).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultEnumerator.Current;
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

#if !NO_DEEP_CANCELLATION
        private sealed class SelectManyAsyncIteratorWithTaskAndCancellation<TSource, TResult> : AsyncIterator<TResult>, IAsyncIListProvider<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, CancellationToken, ValueTask<IAsyncEnumerable<TResult>>> _selector;
            private readonly IAsyncEnumerable<TSource> _source;

            private int _mode;
            private IAsyncEnumerator<TResult> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyAsyncIteratorWithTaskAndCancellation(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<IAsyncEnumerable<TResult>>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyAsyncIteratorWithTaskAndCancellation<TSource, TResult>(_source, _selector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            public ValueTask<int> GetCountAsync(bool onlyIfCheap, CancellationToken cancellationToken)
            {
                if (onlyIfCheap)
                {
                    return new ValueTask<int>(-1);
                }

                return Core(cancellationToken);

                async ValueTask<int> Core(CancellationToken _cancellationToken)
                {
                    var count = 0;

                    await foreach (var element in AsyncEnumerableExtensions.WithCancellation(_source, _cancellationToken).ConfigureAwait(false))
                    {
                        var items = await _selector(element, _cancellationToken).ConfigureAwait(false);

                        checked
                        {
                            count += await items.CountAsync().ConfigureAwait(false);
                        }
                    }

                    return count;
                }
            }

            public async ValueTask<TResult[]> ToArrayAsync(CancellationToken cancellationToken)
            {
                // REVIEW: Substitute for SparseArrayBuilder<T> logic once we have access to that.

                var list = await ToListAsync(cancellationToken).ConfigureAwait(false);

                return list.ToArray();
            }

            public async ValueTask<List<TResult>> ToListAsync(CancellationToken cancellationToken)
            {
                var list = new List<TResult>();

                await foreach (var element in AsyncEnumerableExtensions.WithCancellation(_source, cancellationToken).ConfigureAwait(false))
                {
                    var items = await _selector(element, cancellationToken).ConfigureAwait(false);

                    await list.AddRangeAsync(items, cancellationToken).ConfigureAwait(false);
                }

                return list;
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    var inner = await _selector(_sourceEnumerator.Current, _cancellationToken).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultEnumerator.Current;
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }
#endif

#if !USE_ASYNC_ITERATOR
        private sealed class SelectManyAsyncIterator<TSource, TCollection, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, IAsyncEnumerable<TCollection>> _collectionSelector;
            private readonly Func<TSource, TCollection, TResult> _resultSelector;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _currentSource;
            private int _mode;
            private IAsyncEnumerator<TCollection> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyAsyncIterator(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            {
                Debug.Assert(source != null);
                Debug.Assert(collectionSelector != null);
                Debug.Assert(resultSelector != null);

                _source = source;
                _collectionSelector = collectionSelector;
                _resultSelector = resultSelector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyAsyncIterator<TSource, TCollection, TResult>(_source, _collectionSelector, _resultSelector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                _currentSource = default;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    _currentSource = _sourceEnumerator.Current;
                                    var inner = _collectionSelector(_currentSource);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultSelector(_currentSource, _resultEnumerator.Current);
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

        private sealed class SelectManyAsyncIteratorWithTask<TSource, TCollection, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, ValueTask<IAsyncEnumerable<TCollection>>> _collectionSelector;
            private readonly Func<TSource, TCollection, ValueTask<TResult>> _resultSelector;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _currentSource;
            private int _mode;
            private IAsyncEnumerator<TCollection> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyAsyncIteratorWithTask(IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, ValueTask<TResult>> resultSelector)
            {
                Debug.Assert(source != null);
                Debug.Assert(collectionSelector != null);
                Debug.Assert(resultSelector != null);

                _source = source;
                _collectionSelector = collectionSelector;
                _resultSelector = resultSelector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyAsyncIteratorWithTask<TSource, TCollection, TResult>(_source, _collectionSelector, _resultSelector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                _currentSource = default;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    _currentSource = _sourceEnumerator.Current;
                                    var inner = await _collectionSelector(_currentSource).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = await _resultSelector(_currentSource, _resultEnumerator.Current).ConfigureAwait(false);
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

#if !NO_DEEP_CANCELLATION
        private sealed class SelectManyAsyncIteratorWithTaskAndCancellation<TSource, TCollection, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, CancellationToken, ValueTask<IAsyncEnumerable<TCollection>>> _collectionSelector;
            private readonly Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> _resultSelector;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _currentSource;
            private int _mode;
            private IAsyncEnumerator<TCollection> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyAsyncIteratorWithTaskAndCancellation(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
            {
                Debug.Assert(source != null);
                Debug.Assert(collectionSelector != null);
                Debug.Assert(resultSelector != null);

                _source = source;
                _collectionSelector = collectionSelector;
                _resultSelector = resultSelector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyAsyncIteratorWithTaskAndCancellation<TSource, TCollection, TResult>(_source, _collectionSelector, _resultSelector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                _currentSource = default;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    _currentSource = _sourceEnumerator.Current;
                                    var inner = await _collectionSelector(_currentSource, _cancellationToken).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = await _resultSelector(_currentSource, _resultEnumerator.Current, _cancellationToken).ConfigureAwait(false);
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }
#endif

        private sealed class SelectManyWithIndexAsyncIterator<TSource, TCollection, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, int, IAsyncEnumerable<TCollection>> _collectionSelector;
            private readonly Func<TSource, TCollection, TResult> _resultSelector;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _currentSource;
            private int _index;
            private int _mode;
            private IAsyncEnumerator<TCollection> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyWithIndexAsyncIterator(IAsyncEnumerable<TSource> source, Func<TSource, int, IAsyncEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            {
                Debug.Assert(source != null);
                Debug.Assert(collectionSelector != null);
                Debug.Assert(resultSelector != null);

                _source = source;
                _collectionSelector = collectionSelector;
                _resultSelector = resultSelector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyWithIndexAsyncIterator<TSource, TCollection, TResult>(_source, _collectionSelector, _resultSelector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                _currentSource = default;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _index = -1;
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    _currentSource = _sourceEnumerator.Current;

                                    checked
                                    {
                                        _index++;
                                    }

                                    var inner = _collectionSelector(_currentSource, _index);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultSelector(_currentSource, _resultEnumerator.Current);
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

        private sealed class SelectManyWithIndexAsyncIteratorWithTask<TSource, TCollection, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, int, ValueTask<IAsyncEnumerable<TCollection>>> _collectionSelector;
            private readonly Func<TSource, TCollection, ValueTask<TResult>> _resultSelector;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _currentSource;
            private int _index;
            private int _mode;
            private IAsyncEnumerator<TCollection> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyWithIndexAsyncIteratorWithTask(IAsyncEnumerable<TSource> source, Func<TSource, int, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, ValueTask<TResult>> resultSelector)
            {
                Debug.Assert(source != null);
                Debug.Assert(collectionSelector != null);
                Debug.Assert(resultSelector != null);

                _source = source;
                _collectionSelector = collectionSelector;
                _resultSelector = resultSelector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyWithIndexAsyncIteratorWithTask<TSource, TCollection, TResult>(_source, _collectionSelector, _resultSelector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                _currentSource = default;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _index = -1;
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    _currentSource = _sourceEnumerator.Current;

                                    checked
                                    {
                                        _index++;
                                    }

                                    var inner = await _collectionSelector(_currentSource, _index).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = await _resultSelector(_currentSource, _resultEnumerator.Current).ConfigureAwait(false);
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

#if !NO_DEEP_CANCELLATION
        private sealed class SelectManyWithIndexAsyncIteratorWithTaskAndCancellation<TSource, TCollection, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, int, CancellationToken, ValueTask<IAsyncEnumerable<TCollection>>> _collectionSelector;
            private readonly Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> _resultSelector;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _currentSource;
            private int _index;
            private int _mode;
            private IAsyncEnumerator<TCollection> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyWithIndexAsyncIteratorWithTaskAndCancellation(IAsyncEnumerable<TSource> source, Func<TSource, int, CancellationToken, ValueTask<IAsyncEnumerable<TCollection>>> collectionSelector, Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
            {
                Debug.Assert(source != null);
                Debug.Assert(collectionSelector != null);
                Debug.Assert(resultSelector != null);

                _source = source;
                _collectionSelector = collectionSelector;
                _resultSelector = resultSelector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyWithIndexAsyncIteratorWithTaskAndCancellation<TSource, TCollection, TResult>(_source, _collectionSelector, _resultSelector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                _currentSource = default;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _index = -1;
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    _currentSource = _sourceEnumerator.Current;

                                    checked
                                    {
                                        _index++;
                                    }

                                    var inner = await _collectionSelector(_currentSource, _index, _cancellationToken).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = await _resultSelector(_currentSource, _resultEnumerator.Current, _cancellationToken).ConfigureAwait(false);
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }
#endif

        private sealed class SelectManyWithIndexAsyncIterator<TSource, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, int, IAsyncEnumerable<TResult>> _selector;
            private readonly IAsyncEnumerable<TSource> _source;

            private int _index;
            private int _mode;
            private IAsyncEnumerator<TResult> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyWithIndexAsyncIterator(IAsyncEnumerable<TSource> source, Func<TSource, int, IAsyncEnumerable<TResult>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyWithIndexAsyncIterator<TSource, TResult>(_source, _selector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _index = -1;
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    checked
                                    {
                                        _index++;
                                    }

                                    var inner = _selector(_sourceEnumerator.Current, _index);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultEnumerator.Current;
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

        private sealed class SelectManyWithIndexAsyncIteratorWithTask<TSource, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, int, ValueTask<IAsyncEnumerable<TResult>>> _selector;
            private readonly IAsyncEnumerable<TSource> _source;

            private int _index;
            private int _mode;
            private IAsyncEnumerator<TResult> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyWithIndexAsyncIteratorWithTask(IAsyncEnumerable<TSource> source, Func<TSource, int, ValueTask<IAsyncEnumerable<TResult>>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyWithIndexAsyncIteratorWithTask<TSource, TResult>(_source, _selector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _index = -1;
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    checked
                                    {
                                        _index++;
                                    }

                                    var inner = await _selector(_sourceEnumerator.Current, _index).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultEnumerator.Current;
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

#if !NO_DEEP_CANCELLATION
        private sealed class SelectManyWithIndexAsyncIteratorWithTaskAndCancellation<TSource, TResult> : AsyncIterator<TResult>
        {
            private const int State_Source = 1;
            private const int State_Result = 2;

            private readonly Func<TSource, int, CancellationToken, ValueTask<IAsyncEnumerable<TResult>>> _selector;
            private readonly IAsyncEnumerable<TSource> _source;

            private int _index;
            private int _mode;
            private IAsyncEnumerator<TResult> _resultEnumerator;
            private IAsyncEnumerator<TSource> _sourceEnumerator;

            public SelectManyWithIndexAsyncIteratorWithTaskAndCancellation(IAsyncEnumerable<TSource> source, Func<TSource, int, CancellationToken, ValueTask<IAsyncEnumerable<TResult>>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override AsyncIteratorBase<TResult> Clone()
            {
                return new SelectManyWithIndexAsyncIteratorWithTaskAndCancellation<TSource, TResult>(_source, _selector);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_resultEnumerator != null)
                {
                    await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                    _resultEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    await _sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                    _sourceEnumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _sourceEnumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _index = -1;
                        _mode = State_Source;
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        switch (_mode)
                        {
                            case State_Source:
                                if (await _sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    if (_resultEnumerator != null)
                                    {
                                        await _resultEnumerator.DisposeAsync().ConfigureAwait(false);
                                    }

                                    checked
                                    {
                                        _index++;
                                    }

                                    var inner = await _selector(_sourceEnumerator.Current, _index, _cancellationToken).ConfigureAwait(false);
                                    _resultEnumerator = inner.GetAsyncEnumerator(_cancellationToken);

                                    _mode = State_Result;
                                    goto case State_Result;
                                }
                                break;

                            case State_Result:
                                if (await _resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    _current = _resultEnumerator.Current;
                                    return true;
                                }

                                _mode = State_Source;
                                goto case State_Source; // loop
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }
#endif
#endif
    }
}
