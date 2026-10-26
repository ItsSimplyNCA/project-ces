using UnityEngine;

public class ItemPipeEndpoint {
    public IItemTransferEndpoint Storage { get; }
    public MonoBehaviour Owner { get; }
    public UniversalPort Port { get; }
    public PipeSegment Pipe { get; }

    public ItemPipeEndpoint(IItemTransferEndpoint storage, MonoBehaviour owner, UniversalPort port, PipeSegment pipe) {
        Storage = storage;
        Owner = owner;
        Port = port;
        Pipe = pipe;
    }
}
