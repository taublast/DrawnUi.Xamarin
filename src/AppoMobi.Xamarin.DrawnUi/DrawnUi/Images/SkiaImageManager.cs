using AppoMobi.Specials;
using DrawnUi.Maui.Infrastructure.Xaml;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;

namespace DrawnUi.Maui.Draw;

/// <summary>
///  Represents a min priority queue.
/// </summary>
/// <typeparam name="TElement">Specifies the type of elements in the queue.</typeparam>
/// <typeparam name="TPriority">Specifies the type of priority associated with enqueued elements.</typeparam>
/// <remarks>
///  Implements an array-backed quaternary min-heap. Each element is enqueued with an associated priority
///  that determines the dequeue order: elements with the lowest priority get dequeued first.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
public class PriorityQueue<TElement, TPriority>
{
    public static bool ClearReferences = true;
    /// <summary>
    /// Represents an implicit heap-ordered complete d-ary tree, stored as an array.
    /// </summary>
    private (TElement Element, TPriority Priority)[] _nodes;

    /// <summary>
    /// Custom comparer used to order the heap.
    /// </summary>
    private readonly IComparer<TPriority>? _comparer;

    /// <summary>
    /// Lazily-initialized collection used to expose the contents of the queue.
    /// </summary>
    private UnorderedItemsCollection? _unorderedItems;

    /// <summary>
    /// The number of nodes in the heap.
    /// </summary>
    private int _size;

    /// <summary>
    /// Version updated on mutation to help validate enumerators operate on a consistent state.
    /// </summary>
    private int _version;

    /// <summary>
    /// Specifies the arity of the d-ary heap, which here is quaternary.
    /// It is assumed that this value is a power of 2.
    /// </summary>
    private const int Arity = 4;

    /// <summary>
    /// The binary logarithm of <see cref="Arity" />.
    /// </summary>
    private const int Log2Arity = 2;

#if DEBUG
    static PriorityQueue()
    {
        Debug.Assert(Log2Arity > 0 && Math.Pow(2, Log2Arity) == Arity);
    }
#endif

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class.
    /// </summary>
    public PriorityQueue()
    {
        _nodes = Array.Empty<(TElement, TPriority)>();
        _comparer = InitializeComparer(null);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The specified <paramref name="initialCapacity"/> was negative.
    /// </exception>
    public PriorityQueue(int initialCapacity)
        : this(initialCapacity, comparer: null)
    {
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  with the specified custom priority comparer.
    /// </summary>
    /// <param name="comparer">
    ///  Custom comparer dictating the ordering of elements.
    ///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
    /// </param>
    public PriorityQueue(IComparer<TPriority>? comparer)
    {
        _nodes = Array.Empty<(TElement, TPriority)>();
        _comparer = InitializeComparer(comparer);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  with the specified initial capacity and custom priority comparer.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
    /// <param name="comparer">
    ///  Custom comparer dictating the ordering of elements.
    ///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The specified <paramref name="initialCapacity"/> was negative.
    /// </exception>
    public PriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException();

        _nodes = new (TElement, TPriority)[initialCapacity];
        _comparer = InitializeComparer(comparer);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  that is populated with the specified elements and priorities.
    /// </summary>
    /// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="items"/> argument was <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///  Constructs the heap using a heapify operation,
    ///  which is generally faster than enqueuing individual elements sequentially.
    /// </remarks>
    public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items)
        : this(items, comparer: null)
    {
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="PriorityQueue{TElement, TPriority}"/> class
    ///  that is populated with the specified elements and priorities,
    ///  and with the specified custom priority comparer.
    /// </summary>
    /// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
    /// <param name="comparer">
    ///  Custom comparer dictating the ordering of elements.
    ///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="items"/> argument was <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///  Constructs the heap using a heapify operation,
    ///  which is generally faster than enqueuing individual elements sequentially.
    /// </remarks>
    public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority>? comparer)
    {
        if (items == null)
            throw new InvalidOperationException("Null");

        //_nodes = EnumerableHelpers.ToArray(items, out _size);
        _nodes = items.ToArray();
        _size = _nodes.Length;

        _comparer = InitializeComparer(comparer);

        if (_size > 1)
        {
            Heapify();
        }
    }

    /// <summary>
    ///  Gets the number of elements contained in the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    public int Count => _size;

    /// <summary>
    ///  Gets the priority comparer used by the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    public IComparer<TPriority> Comparer => _comparer ?? Comparer<TPriority>.Default;

    /// <summary>
    ///  Gets a collection that enumerates the elements of the queue in an unordered manner.
    /// </summary>
    /// <remarks>
    ///  The enumeration does not order items by priority, since that would require N * log(N) time and N space.
    ///  Items are instead enumerated following the internal array heap layout.
    /// </remarks>
    public UnorderedItemsCollection UnorderedItems => _unorderedItems ??= new UnorderedItemsCollection(this);

    /// <summary>
    ///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    public void Enqueue(TElement element, TPriority priority)
    {
        // Virtually add the node at the end of the underlying array.
        // Note that the node being enqueued does not need to be physically placed
        // there at this point, as such an assignment would be redundant.

        int currentSize = _size;
        _version++;

        if (_nodes.Length == currentSize)
        {
            Grow(currentSize + 1);
        }

        _size = currentSize + 1;

        if (_comparer == null)
        {
            MoveUpDefaultComparer((element, priority), currentSize);
        }
        else
        {
            MoveUpCustomComparer((element, priority), currentSize);
        }
    }

    /// <summary>
    ///  Returns the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/> without removing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{TElement, TPriority}"/> is empty.</exception>
    /// <returns>The minimal element of the <see cref="PriorityQueue{TElement, TPriority}"/>.</returns>
    public TElement Peek()
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("EmptyQueue");
        }

        return _nodes[0].Element;
    }

    /// <summary>
    ///  Removes and returns the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    /// <returns>The minimal element of the <see cref="PriorityQueue{TElement, TPriority}"/>.</returns>
    public TElement Dequeue()
    {
        if (_size == 0)
        {
            throw new InvalidOperationException();
        }

        TElement element = _nodes[0].Element;
        RemoveRootNode();
        return element;
    }

    /// <summary>
    ///  Removes the minimal element and then immediately adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>,
    /// </summary>
    /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    /// <returns>The minimal element removed before performing the enqueue operation.</returns>
    /// <remarks>
    ///  Implements an extract-then-insert heap operation that is generally more efficient
    ///  than sequencing Dequeue and Enqueue operations: in the worst case scenario only one
    ///  shift-down operation is required.
    /// </remarks>
    public TElement DequeueEnqueue(TElement element, TPriority priority)
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("EmptyQueue");
        }

        (TElement Element, TPriority Priority) root = _nodes[0];

        if (_comparer == null)
        {
            if (Comparer<TPriority>.Default.Compare(priority, root.Priority) > 0)
            {
                MoveDownDefaultComparer((element, priority), 0);
            }
            else
            {
                _nodes[0] = (element, priority);
            }
        }
        else
        {
            if (_comparer.Compare(priority, root.Priority) > 0)
            {
                MoveDownCustomComparer((element, priority), 0);
            }
            else
            {
                _nodes[0] = (element, priority);
            }
        }

        _version++;
        return root.Element;
    }

    /// <summary>
    ///  Removes the minimal element from the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  and copies it to the <paramref name="element"/> parameter,
    ///  and its associated priority to the <paramref name="priority"/> parameter.
    /// </summary>
    /// <param name="element">The removed element.</param>
    /// <param name="priority">The priority associated with the removed element.</param>
    /// <returns>
    ///  <see langword="true"/> if the element is successfully removed;
    ///  <see langword="false"/> if the <see cref="PriorityQueue{TElement, TPriority}"/> is empty.
    /// </returns>
    public bool TryDequeue(out TElement element, out TPriority priority)
    {
        if (_size != 0)
        {
            (element, priority) = _nodes[0];
            RemoveRootNode();
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    /// <summary>
    ///  Returns a value that indicates whether there is a minimal element in the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  and if one is present, copies it to the <paramref name="element"/> parameter,
    ///  and its associated priority to the <paramref name="priority"/> parameter.
    ///  The element is not removed from the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <param name="element">The minimal element in the queue.</param>
    /// <param name="priority">The priority associated with the minimal element.</param>
    /// <returns>
    ///  <see langword="true"/> if there is a minimal element;
    ///  <see langword="false"/> if the <see cref="PriorityQueue{TElement, TPriority}"/> is empty.
    /// </returns>
    public bool TryPeek(out TElement element, out TPriority priority)
    {
        if (_size != 0)
        {
            (element, priority) = _nodes[0];
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    /// <summary>
    ///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  and immediately removes the minimal element, returning the result.
    /// </summary>
    /// <param name="element">The element to add to the <see cref="PriorityQueue{TElement, TPriority}"/>.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    /// <returns>The minimal element removed after the enqueue operation.</returns>
    /// <remarks>
    ///  Implements an insert-then-extract heap operation that is generally more efficient
    ///  than sequencing Enqueue and Dequeue operations: in the worst case scenario only one
    ///  shift-down operation is required.
    /// </remarks>
    public TElement EnqueueDequeue(TElement element, TPriority priority)
    {
        if (_size != 0)
        {
            (TElement Element, TPriority Priority) root = _nodes[0];

            if (_comparer == null)
            {
                if (Comparer<TPriority>.Default.Compare(priority, root.Priority) > 0)
                {
                    MoveDownDefaultComparer((element, priority), 0);
                    _version++;
                    return root.Element;
                }
            }
            else
            {
                if (_comparer.Compare(priority, root.Priority) > 0)
                {
                    MoveDownCustomComparer((element, priority), 0);
                    _version++;
                    return root.Element;
                }
            }
        }

        return element;
    }

    /// <summary>
    ///  Enqueues a sequence of element/priority pairs to the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    /// <param name="items">The pairs of elements and priorities to add to the queue.</param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="items"/> argument was <see langword="null"/>.
    /// </exception>
    public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items)
    {

        if (items == null)
            throw new InvalidOperationException("Null");

        int count = 0;
        var collection = items as ICollection<(TElement Element, TPriority Priority)>;
        if (collection is not null && (count = collection.Count) > _nodes.Length - _size)
        {
            Grow(checked(_size + count));
        }

        if (_size == 0)
        {
            // build using Heapify() if the queue is empty.

            if (collection is not null)
            {
                collection.CopyTo(_nodes, 0);
                _size = count;
            }
            else
            {
                int i = 0;
                (TElement, TPriority)[] nodes = _nodes;
                foreach ((TElement element, TPriority priority) in items)
                {
                    if (nodes.Length == i)
                    {
                        Grow(i + 1);
                        nodes = _nodes;
                    }

                    nodes[i++] = (element, priority);
                }

                _size = i;
            }

            _version++;

            if (_size > 1)
            {
                Heapify();
            }
        }
        else
        {
            foreach ((TElement element, TPriority priority) in items)
            {
                Enqueue(element, priority);
            }
        }
    }

    /// <summary>
    ///  Enqueues a sequence of elements pairs to the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  all associated with the specified priority.
    /// </summary>
    /// <param name="elements">The elements to add to the queue.</param>
    /// <param name="priority">The priority to associate with the new elements.</param>
    /// <exception cref="ArgumentNullException">
    ///  The specified <paramref name="elements"/> argument was <see langword="null"/>.
    /// </exception>
    public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
    {
        if (elements == null)
            throw new InvalidDataException("Null");

        int count;
        if (elements is ICollection<TElement> collection &&
            (count = collection.Count) > _nodes.Length - _size)
        {
            Grow(checked(_size + count));
        }

        if (_size == 0)
        {
            // If the queue is empty just append the elements since they all have the same priority.

            int i = 0;
            (TElement, TPriority)[] nodes = _nodes;
            foreach (TElement element in elements)
            {
                if (nodes.Length == i)
                {
                    Grow(i + 1);
                    nodes = _nodes;
                }

                nodes[i++] = (element, priority);
            }

            _size = i;
            _version++;
        }
        else
        {
            foreach (TElement element in elements)
            {
                Enqueue(element, priority);
            }
        }
    }

    /// <summary>
    /// Removes the first occurrence that equals the specified parameter.
    /// </summary>
    /// <param name="element">The element to try to remove.</param>
    /// <param name="removedElement">The actual element that got removed from the queue.</param>
    /// <param name="priority">The priority value associated with the removed element.</param>
    /// <param name="equalityComparer">The equality comparer governing element equality.</param>
    /// <returns><see langword="true"/> if matching entry was found and removed, <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// The method performs a linear-time scan of every element in the heap, removing the first value found to match the <paramref name="element"/> parameter.
    /// In case of duplicate entries, what entry does get removed is non-deterministic and does not take priority into account.
    ///
    /// If no <paramref name="equalityComparer"/> is specified, <see cref="EqualityComparer{TElement}.Default"/> will be used instead.
    /// </remarks>
    public bool Remove(
        TElement element,
        out TElement removedElement,
        out TPriority priority,
        IEqualityComparer<TElement>? equalityComparer = null)
    {
        int index = FindIndex(element, equalityComparer);
        if (index < 0)
        {
            removedElement = default;
            priority = default;
            return false;
        }

        (TElement Element, TPriority Priority)[] nodes = _nodes;
        (removedElement, priority) = nodes[index];
        int newSize = --_size;

        if (index < newSize)
        {
            // We're removing an element from the middle of the heap.
            // Pop the last element in the collection and sift downward from the removed index.
            (TElement Element, TPriority Priority) lastNode = nodes[newSize];

            if (_comparer == null)
            {
                MoveDownDefaultComparer(lastNode, index);
            }
            else
            {
                MoveDownCustomComparer(lastNode, index);
            }
        }

        nodes[newSize] = default;
        _version++;
        return true;
    }

    /// <summary>
    ///  Removes all items from the <see cref="PriorityQueue{TElement, TPriority}"/>.
    /// </summary>
    public void Clear()
    {
        //if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
        if (ClearReferences)
        {
            // Clear the elements so that the gc can reclaim the references
            Array.Clear(_nodes, 0, _size);
        }
        _size = 0;
        _version++;
    }

    /// <summary>
    ///  Ensures that the <see cref="PriorityQueue{TElement, TPriority}"/> can hold up to
    ///  <paramref name="capacity"/> items without further expansion of its backing storage.
    /// </summary>
    /// <param name="capacity">The minimum capacity to be used.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The specified <paramref name="capacity"/> is negative.
    /// </exception>
    /// <returns>The current capacity of the <see cref="PriorityQueue{TElement, TPriority}"/>.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new InvalidDataException("Capacity");

        if (_nodes.Length < capacity)
        {
            Grow(capacity);
            _version++;
        }

        return _nodes.Length;
    }

    /// <summary>
    ///  Sets the capacity to the actual number of items in the <see cref="PriorityQueue{TElement, TPriority}"/>,
    ///  if that is less than 90 percent of current capacity.
    /// </summary>
    /// <remarks>
    ///  This method can be used to minimize a collection's memory overhead
    ///  if no new elements will be added to the collection.
    /// </remarks>
    public void TrimExcess()
    {
        int threshold = (int)(_nodes.Length * 0.9);
        if (_size < threshold)
        {
            Array.Resize(ref _nodes, _size);
            _version++;
        }
    }

    /// <summary>
    /// Grows the priority queue to match the specified min capacity.
    /// </summary>
    private void Grow(int minCapacity)
    {
        Debug.Assert(_nodes.Length < minCapacity);

        const int GrowFactor = 2;
        const int MinimumGrow = 4;

        int newcapacity = GrowFactor * _nodes.Length;

        if ((uint)newcapacity > int.MaxValue) newcapacity = int.MaxValue;

        // Ensure minimum growth is respected.
        newcapacity = Math.Max(newcapacity, _nodes.Length + MinimumGrow);

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < minCapacity) newcapacity = minCapacity;

        Array.Resize(ref _nodes, newcapacity);
    }

    /// <summary>
    /// Removes the node from the root of the heap
    /// </summary>
    private void RemoveRootNode()
    {
        int lastNodeIndex = --_size;
        _version++;

        if (lastNodeIndex > 0)
        {
            (TElement Element, TPriority Priority) lastNode = _nodes[lastNodeIndex];
            if (_comparer == null)
            {
                MoveDownDefaultComparer(lastNode, 0);
            }
            else
            {
                MoveDownCustomComparer(lastNode, 0);
            }
        }

        if (ClearReferences)
        //if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
        {
            _nodes[lastNodeIndex] = default;
        }
    }

    /// <summary>
    /// Gets the index of an element's parent.
    /// </summary>
    private static int GetParentIndex(int index) => (index - 1) >> Log2Arity;

    /// <summary>
    /// Gets the index of the first child of an element.
    /// </summary>
    private static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;

    /// <summary>
    /// Converts an unordered list into a heap.
    /// </summary>
    private void Heapify()
    {
        // Leaves of the tree are in fact 1-element heaps, for which there
        // is no need to correct them. The heap property needs to be restored
        // only for higher nodes, starting from the first node that has children.
        // It is the parent of the very last element in the array.

        (TElement Element, TPriority Priority)[] nodes = _nodes;
        int lastParentWithChildren = GetParentIndex(_size - 1);

        if (_comparer == null)
        {
            for (int index = lastParentWithChildren; index >= 0; --index)
            {
                MoveDownDefaultComparer(nodes[index], index);
            }
        }
        else
        {
            for (int index = lastParentWithChildren; index >= 0; --index)
            {
                MoveDownCustomComparer(nodes[index], index);
            }
        }
    }

    /// <summary>
    /// Moves a node up in the tree to restore heap order.
    /// </summary>
    private void MoveUpDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // Instead of swapping items all the way to the root, we will perform
        // a similar optimization as in the insertion sort.

        Debug.Assert(_comparer is null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        (TElement Element, TPriority Priority)[] nodes = _nodes;

        while (nodeIndex > 0)
        {
            int parentIndex = GetParentIndex(nodeIndex);
            (TElement Element, TPriority Priority) parent = nodes[parentIndex];

            if (Comparer<TPriority>.Default.Compare(node.Priority, parent.Priority) < 0)
            {
                nodes[nodeIndex] = parent;
                nodeIndex = parentIndex;
            }
            else
            {
                break;
            }
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Moves a node up in the tree to restore heap order.
    /// </summary>
    private void MoveUpCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // Instead of swapping items all the way to the root, we will perform
        // a similar optimization as in the insertion sort.

        Debug.Assert(_comparer is not null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        IComparer<TPriority> comparer = _comparer;
        (TElement Element, TPriority Priority)[] nodes = _nodes;

        while (nodeIndex > 0)
        {
            int parentIndex = GetParentIndex(nodeIndex);
            (TElement Element, TPriority Priority) parent = nodes[parentIndex];

            if (comparer.Compare(node.Priority, parent.Priority) < 0)
            {
                nodes[nodeIndex] = parent;
                nodeIndex = parentIndex;
            }
            else
            {
                break;
            }
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Moves a node down in the tree to restore heap order.
    /// </summary>
    private void MoveDownDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // The node to move down will not actually be swapped every time.
        // Rather, values on the affected path will be moved up, thus leaving a free spot
        // for this value to drop in. Similar optimization as in the insertion sort.

        Debug.Assert(_comparer is null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        (TElement Element, TPriority Priority)[] nodes = _nodes;
        int size = _size;

        int i;
        while ((i = GetFirstChildIndex(nodeIndex)) < size)
        {
            // Find the child node with the minimal priority
            (TElement Element, TPriority Priority) minChild = nodes[i];
            int minChildIndex = i;

            int childIndexUpperBound = Math.Min(i + Arity, size);
            while (++i < childIndexUpperBound)
            {
                (TElement Element, TPriority Priority) nextChild = nodes[i];
                if (Comparer<TPriority>.Default.Compare(nextChild.Priority, minChild.Priority) < 0)
                {
                    minChild = nextChild;
                    minChildIndex = i;
                }
            }

            // Heap property is satisfied; insert node in this location.
            if (Comparer<TPriority>.Default.Compare(node.Priority, minChild.Priority) <= 0)
            {
                break;
            }

            // Move the minimal child up by one node and
            // continue recursively from its location.
            nodes[nodeIndex] = minChild;
            nodeIndex = minChildIndex;
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Moves a node down in the tree to restore heap order.
    /// </summary>
    private void MoveDownCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
    {
        // The node to move down will not actually be swapped every time.
        // Rather, values on the affected path will be moved up, thus leaving a free spot
        // for this value to drop in. Similar optimization as in the insertion sort.

        Debug.Assert(_comparer is not null);
        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        IComparer<TPriority> comparer = _comparer;
        (TElement Element, TPriority Priority)[] nodes = _nodes;
        int size = _size;

        int i;
        while ((i = GetFirstChildIndex(nodeIndex)) < size)
        {
            // Find the child node with the minimal priority
            (TElement Element, TPriority Priority) minChild = nodes[i];
            int minChildIndex = i;

            int childIndexUpperBound = Math.Min(i + Arity, size);
            while (++i < childIndexUpperBound)
            {
                (TElement Element, TPriority Priority) nextChild = nodes[i];
                if (comparer.Compare(nextChild.Priority, minChild.Priority) < 0)
                {
                    minChild = nextChild;
                    minChildIndex = i;
                }
            }

            // Heap property is satisfied; insert node in this location.
            if (comparer.Compare(node.Priority, minChild.Priority) <= 0)
            {
                break;
            }

            // Move the minimal child up by one node and continue recursively from its location.
            nodes[nodeIndex] = minChild;
            nodeIndex = minChildIndex;
        }

        nodes[nodeIndex] = node;
    }

    /// <summary>
    /// Scans the heap for the first index containing an element equal to the specified parameter.
    /// </summary>
    private int FindIndex(TElement element, IEqualityComparer<TElement>? equalityComparer)
    {
        equalityComparer ??= EqualityComparer<TElement>.Default;
        ReadOnlySpan<(TElement Element, TPriority Priority)> nodes = _nodes.AsSpan(0, _size);

        // Currently the JIT doesn't optimize direct EqualityComparer<T>.Default.Equals
        // calls for reference types, so we want to cache the comparer instance instead.
        // TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
        if (typeof(TElement).IsValueType && equalityComparer == EqualityComparer<TElement>.Default)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (EqualityComparer<TElement>.Default.Equals(element, nodes[i].Element))
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (equalityComparer.Equals(element, nodes[i].Element))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Initializes the custom comparer to be used internally by the heap.
    /// </summary>
    private static IComparer<TPriority>? InitializeComparer(IComparer<TPriority>? comparer)
    {
        if (typeof(TPriority).IsValueType)
        {
            if (comparer == Comparer<TPriority>.Default)
            {
                // if the user manually specifies the default comparer,
                // revert to using the optimized path.
                return null;
            }

            return comparer;
        }
        else
        {
            // Currently the JIT doesn't optimize direct Comparer<T>.Default.Compare
            // calls for reference types, so we want to cache the comparer instance instead.
            // TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
            return comparer ?? Comparer<TPriority>.Default;
        }
    }

    /// <summary>
    ///  Enumerates the contents of a <see cref="PriorityQueue{TElement, TPriority}"/>, without any ordering guarantees.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class UnorderedItemsCollection : IReadOnlyCollection<(TElement Element, TPriority Priority)>, ICollection
    {
        internal readonly PriorityQueue<TElement, TPriority> _queue;

        internal UnorderedItemsCollection(PriorityQueue<TElement, TPriority> queue) => _queue = queue;

        public int Count => _queue._size;
        object ICollection.SyncRoot => this;
        bool ICollection.IsSynchronized => false;

        void ICollection.CopyTo(Array array, int index)
        {

            if (array == null)
            {
                throw new InvalidDataException("Null");
            }

            if (array.Rank != 1)
            {
                throw new InvalidDataException();
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new InvalidDataException();
            }

            if (index < 0 || index > array.Length)
            {
                throw new InvalidDataException();
            }

            if (array.Length - index < _queue._size)
            {
                throw new InvalidDataException();
            }

            try
            {
                Array.Copy(_queue._nodes, 0, array, index, _queue._size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new InvalidDataException();
            }
        }

        /// <summary>
        ///  Enumerates the element and priority pairs of a <see cref="PriorityQueue{TElement, TPriority}"/>,
        ///  without any ordering guarantees.
        /// </summary>
        public struct Enumerator : IEnumerator<(TElement Element, TPriority Priority)>
        {
            private readonly PriorityQueue<TElement, TPriority> _queue;
            private readonly int _version;
            private int _index;
            private (TElement, TPriority) _current;

            internal Enumerator(PriorityQueue<TElement, TPriority> queue)
            {
                _queue = queue;
                _index = 0;
                _version = queue._version;
                _current = default;
            }

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="UnorderedItems"/>.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                PriorityQueue<TElement, TPriority> localQueue = _queue;

                if (_version == localQueue._version && ((uint)_index < (uint)localQueue._size))
                {
                    _current = localQueue._nodes[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _queue._version)
                {
                    throw new InvalidOperationException();
                }

                _index = _queue._size + 1;
                _current = default;
                return false;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public (TElement Element, TPriority Priority) Current => _current;
            object IEnumerator.Current => _current;

            void IEnumerator.Reset()
            {
                if (_version != _queue._version)
                {
                    throw new InvalidOperationException();
                }

                _index = 0;
                _current = default;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="UnorderedItems"/>.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> for the <see cref="UnorderedItems"/>.</returns>
        public Enumerator GetEnumerator() => new Enumerator(_queue);

        IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() =>
            _queue.Count == 0 ? Enumerable.Empty<(TElement Element, TPriority Priority)>().GetEnumerator() :
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<(TElement Element, TPriority Priority)>)this).GetEnumerator();
    }
}

public interface IHasBanner
{
    /// <summary>
    /// Main image
    /// </summary>
    public string Banner { get; set; }

    /// <summary>
    /// Indicates that it's already preloading
    /// </summary>
    public bool BannerPreloadOrdered { get; set; }
}

public enum LoadPriority
{
    Low,
    Normal,
    High
}

public partial class SkiaImageManager : IDisposable
{

    #region HELPERS

    public virtual async Task PreloadImage(ImageSource source, CancellationTokenSource cancel = default)
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (source != null && !cancel.IsCancellationRequested)
            {
                await Preload(source, cancel);
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }
    }

    public virtual async Task PreloadImage(string source, CancellationTokenSource cancel = default)
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (!string.IsNullOrEmpty(source) && !cancel.IsCancellationRequested)
            {
                await Preload(FrameworkImageSourceConverter.FromInvariantString(source), cancel);
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }
    }

    public virtual async Task PreloadImages(IEnumerable<string> list, CancellationTokenSource cancel = default)
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (list!=null && !cancel.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                foreach (var source in list)
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        tasks.Add(Preload(source, cancel));
                    }
                }

                // Await all the preload tasks at once.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);

                if (tasks.Count > 0)
                {

                    var cancellationCompletionSource = new TaskCompletionSource<bool>();
                    cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true));

                    var whenAnyTask = Task.WhenAny(Task.WhenAll(tasks), cancellationCompletionSource.Task);

                    await whenAnyTask;

                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

    }

    public virtual async Task PreloadBanners<T>(IList<T> list, CancellationTokenSource cancel = default) where T : IHasBanner
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (list.Count > 0 && !cancel.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                foreach (var item in list)
                {
                    if (!cancel.IsCancellationRequested && !item.BannerPreloadOrdered)
                    {
                        item.BannerPreloadOrdered = true;
                        // Add the task to the list without awaiting it immediately.
                        tasks.Add(Preload(item.Banner, cancel));
                    }
                }

                //await Task.WhenAll(tasks);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);

                if (tasks.Count > 0)
                {

                    var cancellationCompletionSource = new TaskCompletionSource<bool>();
                    cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true));

                    var whenAnyTask = Task.WhenAny(Task.WhenAll(tasks), cancellationCompletionSource.Task);

                    await whenAnyTask;

                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

    }

    #endregion

    /// <summary>
    /// If set to true will not return clones for same sources, but will just return the existing cached SKBitmap reference. Useful if you have a lot on images reusing same sources, but you have to be carefull not to dispose the shared image. SkiaImage is aware of this setting and will keep a cached SKBitmap from being disposed.
    /// </summary>
    public static bool ReuseBitmaps = false;

    /// <summary>
    /// Caching provider setting
    /// </summary>
    public static int CacheLongevitySecs = 1800; //30mins

    /// <summary>
    /// Convention for local files saved in native platform. Shared resources from Resources/Raw/ do not need this prefix.
    /// </summary>
    public static string NativeFilePrefix = "file://";

    public event EventHandler CanReload;

    private readonly IEasyCachingProvider _cachingProvider;

    public static bool LogEnabled = false;

    public static void TraceLog(string message)
    {
        if (LogEnabled)
        {
#if WINDOWS
            Trace.WriteLine(message);
#else
            Console.WriteLine("*******************************************");
            Console.WriteLine(message);
#endif
        }
    }

    static SkiaImageManager _instance;
    private static int _loadingTasksCount;
    private static int _queuedTasksCount;

    public static SkiaImageManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new SkiaImageManager();

            return _instance;
        }
    }

    public SkiaImageManager()
    {
        _cachingProvider = new SimpleCachingProvider();

        var connected = Connectivity.NetworkAccess;
        if (connected != NetworkAccess.Internet
            && connected != NetworkAccess.ConstrainedInternet)
        {
            IsOffline = true;
        }

        Tasks.StartDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            LaunchProcessQueue();
        });
    }


    private SemaphoreSlim semaphoreLoad = new(16, 16);

    private readonly object lockPending = new object();

    private readonly object lockObject = new object();

    private bool _isLoadingLocked;
    public bool IsLoadingLocked
    {
        get => _isLoadingLocked;
        set
        {
            if (_isLoadingLocked != value)
            {
                _isLoadingLocked = value;
            }
        }
    }


    public void CancelAll()
    {
        //lock (lockObject)
        {
            while (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out var item, out LoadPriority priority))
                    item.Cancel.Cancel();
            }
        }
    }

    public record QueueItem
    {
        public QueueItem(ImageSource source, CancellationTokenSource cancel, TaskCompletionSource<SKBitmap> task)
        {
            Source = source;
            Cancel = cancel;
            Task = task;
        }

        public ImageSource Source { get; init; }
        public CancellationTokenSource Cancel { get; init; }
        public TaskCompletionSource<SKBitmap> Task { get; init; }
    }

    private readonly SortedDictionary<LoadPriority, Queue<QueueItem>> _priorityQueue = new();

    private readonly PriorityQueue<QueueItem, LoadPriority> _queue = new();

    private readonly ConcurrentDictionary<string, Task<SKBitmap>> _trackLoadingBitmapsUris = new();

    //todo avoid conflicts, cannot use concurrent otherwise will loose data
    private readonly Dictionary<string, Stack<QueueItem>> _pendingLoadsLow = new();
    private readonly Dictionary<string, Stack<QueueItem>> _pendingLoadsNormal = new();
    private readonly Dictionary<string, Stack<QueueItem>> _pendingLoadsHigh = new();

    private Dictionary<string, Stack<QueueItem>> GetPendingLoadsDictionary(LoadPriority priority)
    {
        return priority switch
        {
            LoadPriority.Low => _pendingLoadsLow,
            LoadPriority.Normal => _pendingLoadsNormal,
            LoadPriority.High => _pendingLoadsHigh,
            _ => _pendingLoadsNormal,
        };
    }


    /// <summary>
    /// Direct load, without any queue or manager cache, for internal use. Please use LoadImageManagedAsync instead.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public virtual Task<SKBitmap> LoadImageAsync(ImageSource source, CancellationToken token)
    {
        return Super.Native.LoadImageOnPlatformAsync(source, token);
    }

    /// <summary>
    /// Uses queue and manager cache
    /// </summary>
    /// <param name="source"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public virtual Task<SKBitmap> LoadImageManagedAsync(ImageSource source, CancellationTokenSource token, LoadPriority priority = LoadPriority.Normal)
    {

        var tcs = new TaskCompletionSource<SKBitmap>();

        string uri = null;

        if (!source.IsEmpty)
        {
            if (source is UriImageSource sourceUri)
            {
                uri = sourceUri.Uri.ToString();
            }
            else
            if (source is FileImageSource sourceFile)
            {
                uri = sourceFile.File;
            }

            // 1 Try to get from cache
            var cacheKey = uri;

            var cachedBitmap = _cachingProvider.Get<SKBitmap>(cacheKey);
            if (cachedBitmap.HasValue)
            {
                if (ReuseBitmaps)
                {
                    tcs.TrySetResult(cachedBitmap.Value);
                }
                else
                {
                    tcs.TrySetResult(cachedBitmap.Value.Copy());
                }
                TraceLog($"ImageLoadManager: Returning cached bitmap for UriImageSource {uri}");

                //if (pendingLoads.Any(x => x.Value.Count != 0))
                //{
                //    RunProcessQueue();
                //}

                return tcs.Task;
            }
            TraceLog($"ImageLoadManager: Not found cached UriImageSource {uri}");

            // 2 put to queue
            var tuple = new QueueItem(source, token, tcs);

            if (uri == null)
            {
                //no queue, maybe stream
                TraceLog($"ImageLoadManager: DIRECT ExecuteLoadTask !!!");
                Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                {
                    await ExecuteLoadTask(tuple);
                });
            }
            else
            {
                var urlAlreadyLoading = _trackLoadingBitmapsUris.ContainsKey(uri);
                if (urlAlreadyLoading)
                {
                    lock (lockPending)
                    {
                        // we're currently loading the same image, save the task to pendingLoads
                        TraceLog($"ImageLoadManager: Same image already loading, pausing task for UriImageSource {uri}");

                        var pendingLoads = GetPendingLoadsDictionary(priority);

                        if (pendingLoads.TryGetValue(uri, out var stack))
                        {
                            stack.Push(tuple);
                        }
                        else
                        {
                            var pendingStack = new Stack<QueueItem>();
                            pendingStack.Push(tuple);
                            pendingLoads[uri] = pendingStack;
                        }

                        Monitor.PulseAll(lockPending);
                    }
                }
                else
                {
                    // We're about to load this image, so add its Task to the loadingBitmaps dictionary
                    _trackLoadingBitmapsUris[uri] = tcs.Task;

                    lock (lockObject)
                    {
                        _queue.Enqueue(tuple, priority);
                    }

                    TraceLog($"ImageLoadManager: Enqueued {uri} (queue {_queue.Count})");
                }

            }



        }

        return tcs.Task;
    }

    void LaunchProcessQueue()
    {
        Task.Run(async () =>
        {
            ProcessQueue();

        }).ConfigureAwait(false);
    }


    private async Task ExecuteLoadTask(QueueItem queueItem)
    {
        if (queueItem != null)
        {
            //do not limit local file loads
            bool useSemaphore = queueItem.Source is UriImageSource;

            try
            {
                if (useSemaphore)
                    await semaphoreLoad.WaitAsync();

                TraceLog($"ImageLoadManager: LoadImageOnPlatformAsync {queueItem.Source}");

                SKBitmap bitmap = await Super.Native.LoadImageOnPlatformAsync(queueItem.Source, queueItem.Cancel.Token);


                // Add the loaded bitmap to the context cache
                if (bitmap != null)
                {
                    if (queueItem.Source is UriImageSource sourceUri)
                    {
                        string uri = sourceUri.Uri.ToString();
                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for UriImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }
                    else
                    if (queueItem.Source is FileImageSource sourceFile)
                    {
                        string uri = sourceFile.File;

                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for FileImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }

                    if (ReuseBitmaps)
                    {
                        queueItem.Task.TrySetResult(bitmap);
                    }
                    else
                    {
                        queueItem.Task.TrySetResult(bitmap.Copy());
                    }

                }
                else
                {
                    //might happen when task was canceled
                    //TraceLog($"ImageLoadManager: BITMAP NULL for {queueItem.Source}");
                    throw new OperationCanceledException("Platform bitmap returned null");
                }


            }
            catch (Exception ex)
            {
                TraceLog($"ImageLoadManager: Exception {ex}");

                if (ex is OperationCanceledException || ex is System.Threading.Tasks.TaskCanceledException)
                {
                    queueItem.Task.TrySetCanceled();
                }
                else
                {
                    queueItem.Task.TrySetException(ex);
                }

                if (queueItem.Source is UriImageSource sourceUri)
                {
                    _trackLoadingBitmapsUris.TryRemove(sourceUri.Uri.ToString(), out _);
                }
                else
                if (queueItem.Source is FileImageSource sourceFile)
                {
                    _trackLoadingBitmapsUris.TryRemove(sourceFile.File, out _);
                }
            }
            finally
            {
                if (useSemaphore)
                    semaphoreLoad.Release();
            }
        }
    }


    public bool IsDisposed { get; protected set; }

    private QueueItem GetPendingItemLoadsForPriority(LoadPriority priority)
    {
        var pendingLoads = GetPendingLoadsDictionary(priority);
        foreach (var pendingPair in pendingLoads)
        {
            try
            {
                if (pendingPair.Value.Count != 0)
                {
                    var nextTcs = pendingPair.Value.Pop();
                    TraceLog($"ImageLoadManager: [UNPAUSED] task for {pendingPair.Key}");
                    return nextTcs;
                }
            }
            catch
            {
            }
        }
        return null;
    }

    private async void ProcessQueue()
    {
        while (!IsDisposed)
        {
            try
            {
                if (IsLoadingLocked || semaphoreLoad.CurrentCount < 1)
                {
                    TraceLog($"ImageLoadManager: Loading Locked!");
                    await Task.Delay(50);
                    continue;
                }
                QueueItem queueItem = null;

                lock (lockPending)
                {
                    queueItem = GetPendingItemLoadsForPriority(LoadPriority.High);
                    if (queueItem == null && semaphoreLoad.CurrentCount > 1)
                        queueItem = GetPendingItemLoadsForPriority(LoadPriority.Normal);
                    if (queueItem == null && semaphoreLoad.CurrentCount > 7)
                        queueItem = GetPendingItemLoadsForPriority(LoadPriority.Low);

                    // If we didn't find a task in pendingLoads, try the main queue.
                    lock (lockObject)
                    {
                        if (queueItem == null && _queue.TryDequeue(out queueItem, out LoadPriority priority))
                        {
                            //if (queueItem!=null)
                            //    TraceLog($"[DEQUEUE]: {queueItem.Source} (queue {_queue.Count})");
                        }
                    }

                    Monitor.PulseAll(lockPending);
                }

                if (queueItem != null)
                {
                    //the only really async that works okay 
                    Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                    {
                        await ExecuteLoadTask(queueItem);
                    });
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
            finally
            {

            }

        }


    }


    public void UpdateInCache(string uri, SKBitmap bitmap, int cacheLongevityMinutes)
    {
        _cachingProvider.Set(uri, bitmap, TimeSpan.FromMinutes(cacheLongevityMinutes));
    }

    /// <summary>
    /// Returns false if key already exists
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="bitmap"></param>
    /// <param name="cacheLongevityMinutes"></param>
    /// <returns></returns>
    public bool AddToCache(string uri, SKBitmap bitmap, int cacheLongevitySecs)
    {
        if (_cachingProvider.Exists(uri))
            return false;

        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(cacheLongevitySecs));
        return true;
    }

    public SKBitmap GetFromCache(string url)
    {
        return _cachingProvider.Get<SKBitmap>(url)?.Value;
    }

    public async Task Preload(ImageSource source, CancellationTokenSource cts)
    {
        if (source.IsEmpty)
        {
            TraceLog($"Preload: Empty source");
            return;
        }
        string uri = null;
        if (source is UriImageSource sourceUri)
        {
            uri = sourceUri.Uri.ToString();
        }
        else
        if (source is FileImageSource sourceFile)
        {
            uri = sourceFile.File;
        }
        if (string.IsNullOrEmpty(uri))
        {
            TraceLog($"Preload: Invalid source {uri}");
            return;
        }

        var cacheKey = uri;

        // Check if the image is already cached or being loaded
        if (_cachingProvider.Get<SKBitmap>(cacheKey).HasValue || _trackLoadingBitmapsUris.ContainsKey(uri))
        {
            TraceLog($"Preload: Image already cached or being loaded for Uri {uri}");
            return;
        }

        var tcs = new TaskCompletionSource<SKBitmap>();
        var tuple = new QueueItem(source, cts, tcs);

        try
        {
            _queue.Enqueue(tuple, LoadPriority.Low);

            // Await the loading to ensure it's completed before returning
            await tcs.Task;
        }
        catch (Exception ex)
        {
            TraceLog($"Preload: Exception {ex}");
        }
    }

    private string GetUriFromImageSource(ImageSource source)
    {
        if (source is StreamImageSource)
            return Guid.NewGuid().ToString();
        else if (source is UriImageSource sourceUri)
            return sourceUri.Uri.ToString();
        else if (source is FileImageSource sourceFile)
            return sourceFile.File;

        return null;
    }



    public void Dispose()
    {
        IsDisposed = true;

        semaphoreLoad?.Dispose();

        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }

    public bool IsOffline { get; protected set; }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        var connected = e.NetworkAccess;
        bool isOffline = connected != NetworkAccess.Internet
                        && connected != NetworkAccess.ConstrainedInternet;
        if (IsOffline && !isOffline)
        {
            CanReload?.Invoke(this, null);
        }
        IsOffline = isOffline;
    }

    public static async Task<SKBitmap> LoadFromFile(string filename, CancellationToken cancel)
    {

        try
        {
            cancel.ThrowIfCancellationRequested();

            SKBitmap bitmap = SkiaImageManager.Instance.GetFromCache(filename);
            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap from cache {filename}");
                return bitmap;
            }

            TraceLog($"ImageLoadManager: Loading local {filename}");

            cancel.ThrowIfCancellationRequested();

            if (filename.SafeContainsInLower(SkiaImageManager.NativeFilePrefix))
            {
                var fullFilename = filename.Replace(SkiaImageManager.NativeFilePrefix, "");
                using var stream = new FileStream(fullFilename, FileMode.Open);
                cancel.Register(stream.Close);  // Register cancellation to close the stream
                bitmap = SKBitmap.Decode(stream);
            }
            else
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(filename);  // Pass cancellation token
                using var reader = new StreamReader(stream);
                bitmap = SKBitmap.Decode(stream);
            }

            cancel.ThrowIfCancellationRequested();

            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap {filename}");

                if (SkiaImageManager.Instance.AddToCache(filename, bitmap, SkiaImageManager.CacheLongevitySecs))
                {
                    return ReuseBitmaps ? bitmap : bitmap.Copy();
                }
            }
            else
            {
                TraceLog($"ImageLoadManager: FAILED to load local {filename}");
            }

            return bitmap;

        }
        catch (OperationCanceledException)
        {
            TraceLog("ImageLoadManager loading was canceled.");
            return null;
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

        return null;

    }

}

/*
public partial class SkiaImageManager : IDisposable
{
    private readonly IEasyCachingProvider _cachingProvider = new SimpleCachingProvider();

    public record QueueItem
    {
        public QueueItem(ImageSource source, CancellationTokenSource cancel, TaskCompletionSource<SKBitmap> task)
        {
            Source = source;
            Cancel = cancel;
            Task = task;
        }

        public ImageSource Source { get; set; }
        public CancellationTokenSource Cancel { get; set; }
        public TaskCompletionSource<SKBitmap> Task { get; set; }
    }

    /// <summary>
    /// If set to true will not return clones for same sources, but will just return the existing cached SKBitmap reference. Useful if you have a lot on images reusing same sources, but you have to be carefull not to dispose the shared image. SkiaImage is aware of this setting and will keep a cached SKBitmap from being disposed.
    /// </summary>
    public static bool ReuseBitmaps = false;

    /// <summary>
    /// Caching provider setting
    /// </summary>
    public static int CacheLongevitySecs = 1800; //30mins

    /// <summary>
    /// Convention for local files saved in native platform. Shared resources from Resources/Raw/ do not need this prefix.
    /// </summary>
    public static string NativeFilePrefix = "file://";

    public event EventHandler CanReload;

    public static bool LogEnabled = false;

    public static void TraceLog(string message)
    {
        if (LogEnabled)
        {
#if WINDOWS
            Trace.WriteLine(message);
#else
            Console.WriteLine("*******************************************");
            Console.WriteLine(message);
#endif
        }
    }

    static SkiaImageManager _instance;
    private static int _loadingTasksCount;
    private static int _queuedTasksCount;

    public static SkiaImageManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new SkiaImageManager();

            return _instance;
        }
    }

    public SkiaImageManager()
    {

        var connected = Connectivity.NetworkAccess;
        if (connected != NetworkAccess.Internet
            && connected != NetworkAccess.ConstrainedInternet)
        {
            IsOffline = true;
        }

        Tasks.StartDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            LaunchProcessQueue();
        });
    }


    private SemaphoreSlim semaphoreLoad = new(16, 16);

    private readonly object lockObject = new object();

    private bool _isLoadingLocked;
    public bool IsLoadingLocked
    {
        get => _isLoadingLocked;
        set
        {
            if (_isLoadingLocked != value)
            {
                _isLoadingLocked = value;
            }
        }
    }


    public void CancelAll()
    {
        lock (lockObject)
        {
            while (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out var item))
                    item.Cancel.Cancel();
            }
        }
    }

    private readonly ConcurrentQueue<QueueItem> _queue = new();

    private readonly ConcurrentDictionary<string, Task<SKBitmap>> _trackLoadingBitmapsUris = new();

    //todo avoid conflicts, cannot use concurrent otherwise will loose data
    private readonly Dictionary<string, Stack<QueueItem>> pendingLoads = new();

    public Task<SKBitmap> Enqueue(ImageSource source, CancellationTokenSource token)
    {

        var tcs = new TaskCompletionSource<SKBitmap>();

        string uri = null;

        if (!source.IsEmpty)
        {
            if (source is UriImageSource sourceUri)
            {
                uri = sourceUri.Uri.ToString();
            }
            else
            if (source is FileImageSource sourceFile)
            {
                uri = sourceFile.File;
            }

            // 1 Try to get from cache
            var cacheKey = uri;

            var cachedBitmap = _cachingProvider.Get<SKBitmap>(cacheKey);
            if (cachedBitmap.HasValue)
            {
                if (ReuseBitmaps)
                {
                    tcs.TrySetResult(cachedBitmap.Value);
                }
                else
                {
                    tcs.TrySetResult(cachedBitmap.Value.Copy());
                }
                TraceLog($"ImageLoadManager: Returning cached bitmap for UriImageSource {uri}");

                //if (pendingLoads.Any(x => x.Value.Count != 0))
                //{
                //    RunProcessQueue();
                //}

                return tcs.Task;
            }
            TraceLog($"ImageLoadManager: Not found cached UriImageSource {uri}");

            // 2 put to queue
            var tuple = new QueueItem(source, token, tcs);

            if (uri == null)
            {
                //no queue, maybe stream
                TraceLog($"ImageLoadManager: DIRECT ExecuteLoadTask !!!");
                Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                {
                    await ExecuteLoadTask(tuple);
                });
            }
            else
            {
                var urlAlreadyLoading = _trackLoadingBitmapsUris.ContainsKey(uri);
                if (urlAlreadyLoading)
                {
                    lock (pendingLoads)
                    {
                        // we're currently loading the same image, save the task to pendingLoads
                        TraceLog($"ImageLoadManager: Same image already loading, pausing task for UriImageSource {uri}");
                        if (pendingLoads.TryGetValue(uri, out var stack))
                        {
                            stack.Push(tuple);
                        }
                        else
                        {
                            var pendingStack = new Stack<QueueItem>();
                            pendingStack.Push(tuple);
                            pendingLoads[uri] = pendingStack;
                        }

                        Monitor.PulseAll(pendingLoads);
                    }
                }
                else
                {
                    // We're about to load this image, so add its Task to the loadingBitmaps dictionary
                    _trackLoadingBitmapsUris[uri] = tcs.Task;
                    lock (lockObject)
                    {
                        _queue.Enqueue(tuple);
                    }

                    TraceLog($"ImageLoadManager: Enqueued {uri} (queue {_queue.Count})");
                }

            }



        }

        return tcs.Task;
    }

    void LaunchProcessQueue()
    {
        Task.Run(async () =>
        {
            ProcessQueue();

        }).ConfigureAwait(false);
    }


    private async Task ExecuteLoadTask(QueueItem queueItem)
    {
        if (queueItem != null)
        {
            //do not limit local file loads
            bool useSemaphore = queueItem.Source is UriImageSource;

            try
            {
                if (useSemaphore)
                    await semaphoreLoad.WaitAsync();

                TraceLog($"ImageLoadManager: LoadSKBitmapAsync {queueItem.Source}");

                SKBitmap bitmap = await Super.Native.LoadSKBitmapAsync(queueItem.Source, queueItem.Cancel.Token);


                // Add the loaded bitmap to the context cache
                if (bitmap != null)
                {
                    if (queueItem.Source is UriImageSource sourceUri)
                    {
                        string uri = sourceUri.Uri.ToString();
                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for UriImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }
                    else
                    if (queueItem.Source is FileImageSource sourceFile)
                    {
                        string uri = sourceFile.File;

                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for FileImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }

                    if (ReuseBitmaps)
                    {
                        queueItem.Task.TrySetResult(bitmap);
                    }
                    else
                    {
                        queueItem.Task.TrySetResult(bitmap.Copy());
                    }

                }
                else
                {
                    TraceLog($"ImageLoadManager: BITMAP NULL for {queueItem.Source}");
                }


            }
            catch (Exception ex)
            {
                Super.Log($"ImageLoadManager: Exception {ex}");

                if (ex is OperationCanceledException)
                {
                    queueItem.Task.TrySetCanceled();
                }
                else
                {
                    queueItem.Task.TrySetException(ex);
                }

                if (queueItem.Source is UriImageSource sourceUri)
                {
                    _trackLoadingBitmapsUris.TryRemove(sourceUri.Uri.ToString(), out _);
                }
                else
                if (queueItem.Source is FileImageSource sourceFile)
                {
                    _trackLoadingBitmapsUris.TryRemove(sourceFile.File, out _);
                }
            }
            finally
            {
                if (useSemaphore)
                    semaphoreLoad.Release();
            }
        }
    }


    public bool IsDisposed { get; protected set; }


    private async void ProcessQueue()
    {
        while (!IsDisposed)
        {
            try
            {

                QueueItem queueItem = null;

                if (IsLoadingLocked)
                {
                    TraceLog($"ImageLoadManager: Loading Locked!");
                    await Task.Delay(50);
                    continue;
                }

                lock (pendingLoads)
                {
                    foreach (var pendingPair in pendingLoads)
                    {
                        if (pendingPair.Value.Count > 0)
                        {
                            var nextTcs = pendingPair.Value.Pop();

                            string uri = pendingPair.Key;

                            //_trackLoadingBitmapsUris[uri] = nextTcs.Item3.Task;

                            queueItem = nextTcs;

                            TraceLog($"ImageLoadManager: [UNPAUSED] task for {uri}");

                            break; // We only want to move one task to the main queue at a time.
                        }
                    }

                    Monitor.PulseAll(pendingLoads);
                }

                // If we didn't find a task in pendingLoads, try the main queue.
                lock (lockObject)
                {
                    if (queueItem == null && _queue.TryDequeue(out queueItem))
                    {
                        TraceLog($"[DEQUEUE]: {queueItem.Source} (queue {_queue.Count})");
                    }
                }

                if (queueItem != null)
                {
                    //the only really async that works okay 
                    Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                    {
                        await ExecuteLoadTask(queueItem);
                    });
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
            finally
            {

            }

        }


    }


    public void UpdateInCache(string uri, SKBitmap bitmap, int cacheLongevityMinutes)
    {
        _cachingProvider.Set(uri, bitmap, TimeSpan.FromMinutes(cacheLongevityMinutes));
    }

    /// <summary>
    /// Returns false if key already exists
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="bitmap"></param>
    /// <param name="cacheLongevityMinutes"></param>
    /// <returns></returns>
    public bool AddToCache(string uri, SKBitmap bitmap, int cacheLongevitySecs)
    {
        if (_cachingProvider.Exists(uri))
            return false;

        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(cacheLongevitySecs));
        return true;
    }

    public SKBitmap GetFromCache(string url)
    {
        return _cachingProvider.Get<SKBitmap>(url)?.Value;
    }

    public async Task Preload(string uri, CancellationTokenSource cts)
    {
        if (string.IsNullOrEmpty(uri))
        {
            TraceLog($"Preload: Invalid Uri {uri}");
            return;
        }

        ImageSource source = new UriImageSource()
        {
            Uri = new Uri(uri)
        };

        var cacheKey = uri;

        // Check if the image is already cached or being loaded
        if (_cachingProvider.Get<SKBitmap>(cacheKey).HasValue || _trackLoadingBitmapsUris.ContainsKey(uri))
        {
            TraceLog($"Preload: Image already cached or being loaded for Uri {uri}");
            return;
        }

        var tcs = new TaskCompletionSource<SKBitmap>();

        var tuple = new QueueItem(source, cts, tcs);

        lock (lockObject)
        {
            _queue.Enqueue(tuple);
        }

        try
        {
            // Await the loading to ensure it's completed before returning
            await tcs.Task;
        }
        catch (Exception ex)
        {
            TraceLog($"Preload: Exception {ex}");
        }
    }

    private string GetUriFromImageSource(ImageSource source)
    {
        if (source is StreamImageSource)
            return Guid.NewGuid().ToString();
        else if (source is UriImageSource sourceUri)
            return sourceUri.Uri.ToString();
        else if (source is FileImageSource sourceFile)
            return sourceFile.File;

        return null;
    }



#if ((NET7_0 || NET8_0) && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)

    public static async Task<SKBitmap> LoadSKBitmapAsync(ImageSource source, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

#endif

    public void Dispose()
    {
        IsDisposed = true;

        semaphoreLoad?.Dispose();

        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }

    public bool IsOffline { get; protected set; }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        var connected = e.NetworkAccess;
        bool isOffline = connected != NetworkAccess.Internet
                        && connected != NetworkAccess.ConstrainedInternet;
        if (IsOffline && !isOffline)
        {
            CanReload?.Invoke(this, null);
        }
        IsOffline = isOffline;
    }

    public static async Task<SKBitmap> LoadFromFile(string filename, CancellationToken cancel)
    {

        try
        {
            cancel.ThrowIfCancellationRequested();

            SKBitmap bitmap = SkiaImageManager.Instance.GetFromCache(filename);
            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap from cache {filename}");
                return bitmap;
            }

            TraceLog($"ImageLoadManager: Loading local {filename}");

            cancel.ThrowIfCancellationRequested();

            if (filename.SafeContainsInLower(SkiaImageManager.NativeFilePrefix))
            {
                var fullFilename = filename.Replace(SkiaImageManager.NativeFilePrefix, "");
                using var stream = new FileStream(fullFilename, FileMode.Open);
                cancel.Register(stream.Close);  // Register cancellation to close the stream
                bitmap = SKBitmap.Decode(stream);
            }
            else
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(filename);  // Pass cancellation token
                using var reader = new StreamReader(stream);
                bitmap = SKBitmap.Decode(stream);
            }

            cancel.ThrowIfCancellationRequested();

            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap {filename}");

                if (SkiaImageManager.Instance.AddToCache(filename, bitmap, SkiaImageManager.CacheLongevitySecs))
                {
                    return ReuseBitmaps ? bitmap : bitmap.Copy();
                }
            }
            else
            {
                TraceLog($"ImageLoadManager: FAILED to load local {filename}");
            }

            return bitmap;

        }
        catch (OperationCanceledException)
        {
            TraceLog("ImageLoadManager loading was canceled.");
            return null;
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

        return null;

    }

}
*/
