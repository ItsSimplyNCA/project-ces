using UnityEngine;

public class FluidPipeEndpoint {
    public IFluidTransferEndpoint Storage { get; }
    public MonoBehaviour Owner { get; }
    public UniversalPort Port { get; }
    public PipeSegment Pipe { get; }

    public FluidPipeEndpoint(IFluidTransferEndpoint storage, MonoBehaviour owner, UniversalPort port, PipeSegment pipe) {
        Storage = storage;
        Owner = owner;
        Port = port;
        Pipe = pipe;
    }
}