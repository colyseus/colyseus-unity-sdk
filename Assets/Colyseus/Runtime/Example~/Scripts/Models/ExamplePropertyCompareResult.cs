// Compare a property to determine changes of state
public class ExamplePropertyCompareResult
{
    // Name of property we're comparing
    public string Name { get; private set; }

    // The old value of the property
    public object OldValue { get; private set; }

    // The new value of the property
    public object NewValue { get; private set; }

    public ExamplePropertyCompareResult(string name, object oldValue, object newValue)
    {
        Name = name;
        OldValue = oldValue;
        NewValue = newValue;
    }
}