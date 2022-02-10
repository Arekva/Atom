namespace SPIRVCross;

public struct EntryPoint
{
    private ExecutionModel _model;
    private string _name;

    public ExecutionModel ExecutionModel
    {
        get => _model;
        init => _model = value;
    }
    public string Name
    {
        get => _name;
        init => _name = value;
    }

    public EntryPoint(ExecutionModel model, string name) => (_model, _name) = (model, name);

    public override string ToString() => $"{_name} ({_model})";
}