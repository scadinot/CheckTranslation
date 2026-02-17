using System.ComponentModel;

namespace CheckTranslation;

internal class SortableBindingList<T> : BindingList<T>
{
    private bool _isSorted;
    private PropertyDescriptor? _sortProperty;
    private ListSortDirection _sortDirection;

    public SortableBindingList(IList<T> list) : base(list) { }

    protected override bool SupportsSortingCore => true;
    protected override bool IsSortedCore => _isSorted;
    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;
    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        if (Items is List<T> list)
        {
            list.Sort((a, b) =>
            {
                var va = prop.GetValue(a);
                var vb = prop.GetValue(b);
                int result = Comparer<object?>.Default.Compare(va, vb);
                return direction == ListSortDirection.Descending ? -result : result;
            });
        }

        _sortProperty = prop;
        _sortDirection = direction;
        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    protected override void RemoveSortCore()
    {
        _isSorted = false;
        _sortProperty = null;
    }
}
