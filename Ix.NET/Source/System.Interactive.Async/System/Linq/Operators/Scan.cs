﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerableEx
    {
        // NB: Implementations of Scan never yield the first element, unlike the behavior of Aggregate on a sequence with one
        //     element, which returns the first element (or the seed if given an empty sequence). This is compatible with Rx
        //     but one could argue whether it was the right default.

        public static IAsyncEnumerable<TSource> Scan<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (accumulator == null)
                throw Error.ArgumentNull(nameof(accumulator));

#if USE_ASYNC_ITERATOR
            return AsyncEnumerable.Create(Core);

            async IAsyncEnumerator<TSource> Core(CancellationToken cancellationToken)
            {
                await using (var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false))
                {
                    if (!await e.MoveNextAsync())
                    {
                        yield break;
                    }

                    var res = e.Current;

                    while (await e.MoveNextAsync())
                    {
                        res = accumulator(res, e.Current);

                        yield return res;
                    }
                }
            }
#else
            return new ScanAsyncEnumerable<TSource>(source, accumulator);
#endif
        }

        public static IAsyncEnumerable<TAccumulate> Scan<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (accumulator == null)
                throw Error.ArgumentNull(nameof(accumulator));

#if USE_ASYNC_ITERATOR
            return AsyncEnumerable.Create(Core);

            async IAsyncEnumerator<TAccumulate> Core(CancellationToken cancellationToken)
            {
                var res = seed;

                await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    res = accumulator(res, item);

                    yield return res;
                }
            }
#else
            return new ScanAsyncEnumerable<TSource, TAccumulate>(source, seed, accumulator);
#endif
        }

        public static IAsyncEnumerable<TSource> Scan<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, ValueTask<TSource>> accumulator)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (accumulator == null)
                throw Error.ArgumentNull(nameof(accumulator));

#if USE_ASYNC_ITERATOR
            return AsyncEnumerable.Create(Core);

            async IAsyncEnumerator<TSource> Core(CancellationToken cancellationToken)
            {
                await using (var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false))
                {
                    if (!await e.MoveNextAsync())
                    {
                        yield break;
                    }

                    var res = e.Current;

                    while (await e.MoveNextAsync())
                    {
                        res = await accumulator(res, e.Current).ConfigureAwait(false);

                        yield return res;
                    }
                }
            }
#else
            return new ScanAsyncEnumerableWithTask<TSource>(source, accumulator);
#endif
        }

#if !NO_DEEP_CANCELLATION
        public static IAsyncEnumerable<TSource> Scan<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, CancellationToken, ValueTask<TSource>> accumulator)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (accumulator == null)
                throw Error.ArgumentNull(nameof(accumulator));

#if USE_ASYNC_ITERATOR
            return AsyncEnumerable.Create(Core);

            async IAsyncEnumerator<TSource> Core(CancellationToken cancellationToken)
            {
                await using (var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false))
                {
                    if (!await e.MoveNextAsync())
                    {
                        yield break;
                    }

                    var res = e.Current;

                    while (await e.MoveNextAsync())
                    {
                        res = await accumulator(res, e.Current, cancellationToken).ConfigureAwait(false);

                        yield return res;
                    }
                }
            }
#else
            return new ScanAsyncEnumerableWithTaskAndCancellation<TSource>(source, accumulator);
#endif
        }
#endif

        public static IAsyncEnumerable<TAccumulate> Scan<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, ValueTask<TAccumulate>> accumulator)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (accumulator == null)
                throw Error.ArgumentNull(nameof(accumulator));

#if USE_ASYNC_ITERATOR
            return AsyncEnumerable.Create(Core);

            async IAsyncEnumerator<TAccumulate> Core(CancellationToken cancellationToken)
            {
                var res = seed;

                await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    res = await accumulator(res, item).ConfigureAwait(false);

                    yield return res;
                }
            }
#else
            return new ScanAsyncEnumerableWithTask<TSource, TAccumulate>(source, seed, accumulator);
#endif
        }

#if !NO_DEEP_CANCELLATION
        public static IAsyncEnumerable<TAccumulate> Scan<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> accumulator)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (accumulator == null)
                throw Error.ArgumentNull(nameof(accumulator));

#if USE_ASYNC_ITERATOR
            return AsyncEnumerable.Create(Core);

            async IAsyncEnumerator<TAccumulate> Core(CancellationToken cancellationToken)
            {
                var res = seed;

                await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    res = await accumulator(res, item, cancellationToken).ConfigureAwait(false);

                    yield return res;
                }
            }
#else
            return new ScanAsyncEnumerableWithTaskAndCancellation<TSource, TAccumulate>(source, seed, accumulator);
#endif
        }
#endif

#if !USE_ASYNC_ITERATOR
        private sealed class ScanAsyncEnumerable<TSource> : AsyncIterator<TSource>
        {
            private readonly Func<TSource, TSource, TSource> _accumulator;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _accumulated;
            private IAsyncEnumerator<TSource> _enumerator;

            private bool _hasSeed;

            public ScanAsyncEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator)
            {
                Debug.Assert(source != null);
                Debug.Assert(accumulator != null);

                _source = source;
                _accumulator = accumulator;
            }

            public override AsyncIteratorBase<TSource> Clone()
            {
                return new ScanAsyncEnumerable<TSource>(_source, _accumulator);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                    _accumulated = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _hasSeed = false;
                        _accumulated = default;

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:

                        while (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;

                            if (!_hasSeed)
                            {
                                _hasSeed = true;
                                _accumulated = item;
                                continue; // loop
                            }

                            _accumulated = _accumulator(_accumulated, item);
                            _current = _accumulated;
                            return true;
                        }

                        break; // case

                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

        private sealed class ScanAsyncEnumerable<TSource, TAccumulate> : AsyncIterator<TAccumulate>
        {
            private readonly Func<TAccumulate, TSource, TAccumulate> _accumulator;
            private readonly TAccumulate _seed;
            private readonly IAsyncEnumerable<TSource> _source;

            private TAccumulate _accumulated;
            private IAsyncEnumerator<TSource> _enumerator;

            public ScanAsyncEnumerable(IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
            {
                Debug.Assert(source != null);
                Debug.Assert(accumulator != null);

                _source = source;
                _seed = seed;
                _accumulator = accumulator;
            }

            public override AsyncIteratorBase<TAccumulate> Clone()
            {
                return new ScanAsyncEnumerable<TSource, TAccumulate>(_source, _seed, _accumulator);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                    _accumulated = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _accumulated = _seed;

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;
                            _accumulated = _accumulator(_accumulated, item);
                            _current = _accumulated;
                            return true;
                        }

                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

        private sealed class ScanAsyncEnumerableWithTask<TSource> : AsyncIterator<TSource>
        {
            private readonly Func<TSource, TSource, ValueTask<TSource>> _accumulator;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _accumulated;
            private IAsyncEnumerator<TSource> _enumerator;

            private bool _hasSeed;

            public ScanAsyncEnumerableWithTask(IAsyncEnumerable<TSource> source, Func<TSource, TSource, ValueTask<TSource>> accumulator)
            {
                Debug.Assert(source != null);
                Debug.Assert(accumulator != null);

                _source = source;
                _accumulator = accumulator;
            }

            public override AsyncIteratorBase<TSource> Clone()
            {
                return new ScanAsyncEnumerableWithTask<TSource>(_source, _accumulator);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                    _accumulated = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _hasSeed = false;
                        _accumulated = default;

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:

                        while (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;

                            if (!_hasSeed)
                            {
                                _hasSeed = true;
                                _accumulated = item;
                                continue; // loop
                            }

                            _accumulated = await _accumulator(_accumulated, item).ConfigureAwait(false);
                            _current = _accumulated;
                            return true;
                        }

                        break; // case

                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

#if !NO_DEEP_CANCELLATION
        private sealed class ScanAsyncEnumerableWithTaskAndCancellation<TSource> : AsyncIterator<TSource>
        {
            private readonly Func<TSource, TSource, CancellationToken, ValueTask<TSource>> _accumulator;
            private readonly IAsyncEnumerable<TSource> _source;

            private TSource _accumulated;
            private IAsyncEnumerator<TSource> _enumerator;

            private bool _hasSeed;

            public ScanAsyncEnumerableWithTaskAndCancellation(IAsyncEnumerable<TSource> source, Func<TSource, TSource, CancellationToken, ValueTask<TSource>> accumulator)
            {
                Debug.Assert(source != null);
                Debug.Assert(accumulator != null);

                _source = source;
                _accumulator = accumulator;
            }

            public override AsyncIteratorBase<TSource> Clone()
            {
                return new ScanAsyncEnumerableWithTaskAndCancellation<TSource>(_source, _accumulator);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                    _accumulated = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _hasSeed = false;
                        _accumulated = default;

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:

                        while (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;

                            if (!_hasSeed)
                            {
                                _hasSeed = true;
                                _accumulated = item;
                                continue; // loop
                            }

                            _accumulated = await _accumulator(_accumulated, item, _cancellationToken).ConfigureAwait(false);
                            _current = _accumulated;
                            return true;
                        }

                        break; // case

                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }
#endif

        private sealed class ScanAsyncEnumerableWithTask<TSource, TAccumulate> : AsyncIterator<TAccumulate>
        {
            private readonly Func<TAccumulate, TSource, ValueTask<TAccumulate>> _accumulator;
            private readonly TAccumulate _seed;
            private readonly IAsyncEnumerable<TSource> _source;

            private TAccumulate _accumulated;
            private IAsyncEnumerator<TSource> _enumerator;

            public ScanAsyncEnumerableWithTask(IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, ValueTask<TAccumulate>> accumulator)
            {
                Debug.Assert(source != null);
                Debug.Assert(accumulator != null);

                _source = source;
                _seed = seed;
                _accumulator = accumulator;
            }

            public override AsyncIteratorBase<TAccumulate> Clone()
            {
                return new ScanAsyncEnumerableWithTask<TSource, TAccumulate>(_source, _seed, _accumulator);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                    _accumulated = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _accumulated = _seed;

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;
                            _accumulated = await _accumulator(_accumulated, item).ConfigureAwait(false);
                            _current = _accumulated;
                            return true;
                        }

                        break;

                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
        }

#if !NO_DEEP_CANCELLATION
        private sealed class ScanAsyncEnumerableWithTaskAndCancellation<TSource, TAccumulate> : AsyncIterator<TAccumulate>
        {
            private readonly Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> _accumulator;
            private readonly TAccumulate _seed;
            private readonly IAsyncEnumerable<TSource> _source;

            private TAccumulate _accumulated;
            private IAsyncEnumerator<TSource> _enumerator;

            public ScanAsyncEnumerableWithTaskAndCancellation(IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> accumulator)
            {
                Debug.Assert(source != null);
                Debug.Assert(accumulator != null);

                _source = source;
                _seed = seed;
                _accumulator = accumulator;
            }

            public override AsyncIteratorBase<TAccumulate> Clone()
            {
                return new ScanAsyncEnumerableWithTaskAndCancellation<TSource, TAccumulate>(_source, _seed, _accumulator);
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator != null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                    _accumulated = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override async ValueTask<bool> MoveNextCore()
            {
                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
                        _accumulated = _seed;

                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var item = _enumerator.Current;
                            _accumulated = await _accumulator(_accumulated, item, _cancellationToken).ConfigureAwait(false);
                            _current = _accumulated;
                            return true;
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
