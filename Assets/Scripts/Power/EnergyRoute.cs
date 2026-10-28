using System.Collections.Generic;

public class EnergyRoute  {
    public IReadOnlyList<EnergyCableSegment> Cables { get; }
    public IReadOnlyList<PowerBreaker> Breakers { get; }
    public IReadOnlyList<PowerStorage> Storages { get; }

    public EnergyRoute(
        IEnumerable<EnergyCableSegment> cables,
        IEnumerable<PowerBreaker> breakers,
        IEnumerable<PowerStorage> storages = null
    ) {
        Cables = new List<EnergyCableSegment>(cables);
        Breakers = new List<PowerBreaker>(breakers);

        Storages = storages != null
            ? new List<PowerStorage>(storages)
            : new List<PowerStorage>();
    }
}
