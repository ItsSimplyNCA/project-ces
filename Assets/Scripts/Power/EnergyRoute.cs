using System.Collections.Generic;

public class EnergyRoute  {
    public IReadOnlyList<EnergyCableSegment> Cables { get; }
    public IReadOnlyList<PowerBreaker> Breakers { get; }

    public EnergyRoute(
        IEnumerable<EnergyCableSegment> cables,
        IEnumerable<PowerBreaker> breakers
    ) {
        Cables = new List<EnergyCableSegment>(cables);
        Breakers = new List<PowerBreaker>(breakers);
    }
}
