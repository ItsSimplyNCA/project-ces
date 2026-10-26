public class EnergyEndpoint {
    public UniversalIO Owner { get; }
    public UniversalPort Port { get; }
    public EnergyCableSegment Cable { get; }

    public EnergyEndpoint(UniversalIO owner, UniversalPort port, EnergyCableSegment cable) {
        Owner = owner;
        Port = port;
        Cable = cable;
    }
}