namespace GroupProject.Model.LogicModel;

public class LogicGateCard : Card, IOutputProvider
{
    public LogicGateCard(GateTypeEnum gateType)
    {
        GateType = gateType;
    }

    public LogicGateCard(string gateTypeString) : this((GateTypeEnum)Enum.Parse(typeof(GateTypeEnum), gateTypeString,
        true))
    {
    }

    public GateTypeEnum GateType { get; set; }
    public bool Input1 { get; set; }
    public IOutputProvider? Input1Card { get; set; }
    public bool Input2 { get; set; }
    public IOutputProvider? Input2Card { get; set; }
    public bool Output { get; set; }

    public void CalculateOutput()
    {
        if (Input1Card != null) Input1 = Input1Card.Output;

        if (Input2Card != null) Input2 = Input2Card.Output;

        switch (GateType)
        {
            case GateTypeEnum.And:
                Output = Input1 && Input2;
                break;
            case GateTypeEnum.Or:
                Output = Input1 || Input2;
                break;
            case GateTypeEnum.Not:
                Output = !Input1;
                break;
            case GateTypeEnum.Xor:
                Output = Input1 ^ Input2;
                break;
            case GateTypeEnum.Nand:
                Output = !(Input1 && Input2);
                break;
            case GateTypeEnum.Nor:
                Output = !(Input1 || Input2);
                break;
            case GateTypeEnum.Xnor:
                Output = !(Input1 ^ Input2);
                break;
            default:
                throw new InvalidOperationException("Invalid gate type");
        }
    }
}