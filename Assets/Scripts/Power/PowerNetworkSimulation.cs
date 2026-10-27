using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class PowerNetworkSimulation : MonoBehaviour {
    [SerializeField] private GridWorld gridWorld;

    private const float ALLOCATION_QUANTUM_MW = 0.25f;

    private class Source {
        public EnergySourceState State;
        public readonly List<EnergyEndpoint> Outputs = new();
    }

    private class Consumer {
        public Machine Machine;
        public float RemainingDemandMW;
        public float SuppliedPowerMW;
        public readonly List<EnergyEndpoint> Inputs = new();
    }

    private class ChargeTarget {
        public PowerStorage Storage;
        public float RemainingDemandMW;
        public float ReceivedPowerMW;
        public readonly List<EnergyEndpoint> Inputs = new();
    }

    private readonly EnergyTransferBudget transferBudget = new();
    private int firstConsumerIndex;
    private int firstSourceIndex;

    private void Update() {
        float deltaTime = Time.deltaTime;
        
        if (deltaTime <= 0f) return;

        foreach (PowerStorage storage in gridWorld.GetComponentsInChildren<PowerStorage>()) {
            storage.BeginSimulationStep();
        }

        foreach (Machine machine in gridWorld.GetComponentsInChildren<Machine>()) {
            machine.SetSuppliedPower(0f);
        }

        EnergyCableSegment[] cables = gridWorld.GetComponentsInChildren<EnergyCableSegment>();
        
        transferBudget.BeginCalculation(cables);

        Dictionary<MonoBehaviour, Source> sourcesByOwner = new();
        Dictionary<Machine, Consumer> consumersByMachine = new();

        List<Source> sources = new();
        List<Consumer> consumers = new();

        HashSet<EnergyCableSegment> visited = new();

        foreach (EnergyCableSegment cable in cables) {
            if (cable.Definition == null || visited.Contains(cable)) continue;

            var network = EnergyNetworkTraversal.CollectConnected(cable, gridWorld);

            foreach (EnergyCableSegment member in network) {
                visited.Add(member);
            }

            var endpoints = EnergyEndpointDiscovery.CollectConnectedEndpoints(cable, gridWorld);

            foreach (EnergyEndpoint endpoint in endpoints) {
                if (endpoint.Port.Mode == PortMode.EnergyOutput) {
                    PowerGenerator generator = endpoint.Owner.GetComponent<PowerGenerator>();
                    PowerStorage storage = endpoint.Owner.GetComponent<PowerStorage>();
                    MonoBehaviour owner = generator != null ? generator : storage;

                    if (owner == null) continue;

                    if (!sourcesByOwner.TryGetValue(owner, out Source source)) {
                        source = new Source {
                            State = generator != null
                                ? new EnergySourceState(generator)
                                : new EnergySourceState(storage, deltaTime)
                        };

                        sourcesByOwner.Add(owner, source);
                        sources.Add(source);
                    }

                    source.Outputs.Add(endpoint);
                } else if (endpoint.Port.Mode == PortMode.EnergyInput) {
                    Machine machine = endpoint.Owner.GetComponent<Machine>();

                    if (machine == null) continue;

                    if (!consumersByMachine.TryGetValue(machine, out Consumer consumer)) {
                        consumer = new Consumer {
                            Machine = machine,
                            RemainingDemandMW = machine.RequestedPowerMW
                        };

                        consumersByMachine.Add(machine, consumer);
                        consumers.Add(consumer);
                    }

                    consumer.Inputs.Add(endpoint);
                }
            }
        }

        if (sources.Count == 0) return;

        firstSourceIndex %= sources.Count;

        if (consumers.Count > 0) {
            while (TripOverloadedNetworks(sources, consumers)) {
                /// ?????
            }

            firstConsumerIndex %= consumers.Count;

            bool madeProgress;

            do {
                madeProgress = false;

                for (int i = 0; i < consumers.Count; i++) {
                    int index = (firstConsumerIndex + i) % consumers.Count;

                    if (TryAllocate(consumers[index], sources) > 0f) {
                        madeProgress = true;
                    }
                }
            } while (madeProgress);

            foreach (Consumer consumer in consumers) {
                consumer.Machine.SetSuppliedPower(consumer.SuppliedPowerMW);
            }

            firstConsumerIndex = (firstConsumerIndex + 1) % consumers.Count;
        }

        foreach (Source source in sources) {
            if (!source.State.IsStorage) continue;

            source.State.Storage.SupplyPower(source.State.AllocatedPowerMW, deltaTime);
        }

        ChargeStorages(cables, sources, deltaTime);
    }

    private float TryAllocate(Consumer consumer, List<Source> sources) {
        if (consumer.RemainingDemandMW <= 0f) return 0f;

        for (int pass = 0; pass < 2; pass++) {
            bool useStorage = pass == 1;

            for (int i = 0; i < sources.Count; i++) {
                int sourceIndex = (firstSourceIndex + i) % sources.Count;
                Source source = sources[sourceIndex];

                if (source.State.IsStorage != useStorage) continue;

                float remainingPower = GetRemainingPower(source);

                if (remainingPower <= 0f) continue;

                foreach (EnergyEndpoint output in source.Outputs) {
                    foreach (EnergyEndpoint input in consumer.Inputs) {
                        EnergyRoute route = EnergyRouteTraversal.FindRoute(
                            output.Cable,
                            input.Cable,
                            gridWorld,
                            transferBudget
                        );

                        if (route == null) continue;

                        float requestedPower = Mathf.Min(ALLOCATION_QUANTUM_MW, consumer.RemainingDemandMW);

                        requestedPower = Mathf.Min(requestedPower, remainingPower);
                        requestedPower = Mathf.Min(requestedPower, transferBudget.GetAvailable(route.Cables));

                        if (requestedPower <= 0f) continue;

                        float allocatedPower = source.State.Allocate(requestedPower);

                        if (allocatedPower <= 0f) continue;

                        transferBudget.Consume(route.Cables, allocatedPower);

                        consumer.RemainingDemandMW -= allocatedPower;
                        consumer.SuppliedPowerMW += allocatedPower;

                        firstSourceIndex = (sourceIndex + 1) % sources.Count;

                        return allocatedPower;
                    }
                }
            }
        }

        return 0f;
    }

    private bool TripOverloadedNetworks(List<Source> sources, List<Consumer> consumers) {
        Dictionary<Object, Object> parents = new();

        Object FindRoot(Object node) {
            if (!parents.ContainsKey(node)) {
                parents.Add(node, node);
            }

            Object root = node;

            while (parents[root] != root) {
                root = parents[root];
            }

            while (parents[node] != node) {
                Object next = parents[node];
                parents[node] = root;
                node = next;
            }

            return root;
        }

        void Join(Object first, Object second) {
            Object firstRoot = FindRoot(first);
            Object secondRoot = FindRoot(second);

            if (firstRoot != secondRoot) {
                parents[secondRoot] = firstRoot;
            }
        }

        foreach (Source source in sources) {
            FindRoot(source.State.Owner);
        }

        Dictionary<Consumer, List<EnergyRoute>> routesByConsumer = new();

        foreach (Consumer consumer in consumers) {
            FindRoot(consumer.Machine);
            routesByConsumer.Add(consumer, new List<EnergyRoute>());

            foreach (Source source in sources) {
                if (GetRemainingPower(source) <= 0f) continue;

                foreach (EnergyEndpoint output in source.Outputs) {
                    foreach (EnergyEndpoint input in consumer.Inputs) {
                        EnergyRoute route = EnergyRouteTraversal.FindRoute(output.Cable, input.Cable, gridWorld);

                        if (route == null) continue;

                        Join(source.State.Owner, consumer.Machine);
                        routesByConsumer[consumer].Add(route);
                    }
                }
            }
        }

        Dictionary<Object, float> supplyByGroup = new();
        Dictionary<Object, float> demandByGroup = new();

        foreach (Source source in sources) {
            Object group = FindRoot(source.State.Owner);

            supplyByGroup.TryGetValue(group, out float supply);
            supplyByGroup[group] = supply + GetRemainingPower(source);
        }

        foreach (Consumer consumer in consumers) {
            Object group = FindRoot(consumer.Machine);

            demandByGroup.TryGetValue(group, out float demand);
            demandByGroup[group] = demand + consumer.RemainingDemandMW;
        }

        foreach (Source source in sources) {
            if (GetRemainingPower(source) <= 0f) continue;

            Object group = FindRoot(source.State.Owner);

            supplyByGroup.TryGetValue(group, out float supply);
            demandByGroup.TryGetValue(group, out float demand);

            if (demand <= supply) continue;

            foreach (Consumer consumer in consumers) {
                if (consumer.RemainingDemandMW <= 0f) continue;
                if (FindRoot(consumer.Machine) != group) continue;

                foreach (EnergyRoute route in routesByConsumer[consumer]) {
                    if (route.Breakers.Count == 0) continue;

                    PowerBreaker nearestBreaker = route.Breakers[route.Breakers.Count - 1];

                    nearestBreaker.Trip();

                    return true;
                }
            }

            bool trippedGenerator = false;

            foreach (Source affectedSource in sources) {
                PowerGenerator generator = affectedSource.State.Generator;

                if (generator == null) continue;
                if (GetRemainingPower(affectedSource) <= 0f) continue;
                if (FindRoot(affectedSource.State.Owner) != group) continue;

                generator.Trip();
                trippedGenerator = true;
            }
            
            if (trippedGenerator) return true;
        }

        return false;
    }

    private static float GetRemainingPower(Source source) {
        if (source.State.Generator != null) {
            return Mathf.Min(source.State.RemainingPowerMW, source.State.Generator.AvailablePowerMW);
        }

        return source.State.RemainingPowerMW;
    }

    private void ChargeStorages(EnergyCableSegment[] cables, List<Source> sources, float deltaTime) {
        Dictionary<PowerStorage, ChargeTarget> targetsByStorage = new();
        List<ChargeTarget> targets = new();

        HashSet<EnergyCableSegment> visited = new();

        foreach (EnergyCableSegment cable in cables) {
            if (cable.Definition == null || visited.Contains(cable)) continue;

            var network = EnergyNetworkTraversal.CollectConnected(cable, gridWorld);

            foreach (EnergyCableSegment member in network) {
                visited.Add(member);
            }

            var endpoints = EnergyEndpointDiscovery.CollectConnectedEndpoints(cable, gridWorld);

            foreach (EnergyEndpoint endpoint in endpoints) {
                if (endpoint.Port.Mode != PortMode.EnergyInput) continue;

                PowerStorage storage = endpoint.Owner.GetComponent<PowerStorage>();

                if (storage == null) continue;

                if (!targetsByStorage.TryGetValue(storage, out ChargeTarget target)) {
                    float remainingTransferLimit = storage.TransferLimitMW;

                    foreach (Source source in sources) {
                        if (source.State.Storage != storage) continue;

                        remainingTransferLimit = Mathf.Max(0f, remainingTransferLimit - source.State.AllocatedPowerMW);
                    }

                    target = new ChargeTarget {
                        Storage = storage,
                        RemainingDemandMW = Mathf.Min(storage.GetAvailableChargePowerMW(deltaTime), remainingTransferLimit)
                    };

                    targetsByStorage.Add(storage, target);
                    targets.Add(target);
                }

                target.Inputs.Add(endpoint);
            }
        }

        bool madeProgress;

        do {
            madeProgress = false;
            
            foreach (ChargeTarget target in targets) {
                if (TryAllocateCharge(target, sources) > 0f) {
                    madeProgress = true;
                }
            }
        } while (madeProgress);

        foreach (ChargeTarget target in targets) {
            target.Storage.ReceivePower(target.ReceivedPowerMW, deltaTime);
        }
    }

    private float TryAllocateCharge(ChargeTarget target, List<Source> sources) {
        if (target.RemainingDemandMW <= 0f) return 0f;

        foreach (Source source in sources) {
            if (source.State.Generator == null) continue;

            float remainingPower = GetRemainingPower(source);

            if (remainingPower <= 0f) continue;

            foreach (EnergyEndpoint output in source.Outputs) {
                foreach (EnergyEndpoint input in target.Inputs) {
                    EnergyRoute route = EnergyRouteTraversal.FindRoute(
                        output.Cable,
                        input.Cable,
                        gridWorld,
                        transferBudget
                    );

                    if (route == null) continue;

                    float requestedPower = Mathf.Min(ALLOCATION_QUANTUM_MW, target.RemainingDemandMW);

                    requestedPower = Mathf.Min(requestedPower, remainingPower);
                    requestedPower = Mathf.Min(requestedPower, transferBudget.GetAvailable(route.Cables));

                    if (requestedPower <= 0f) continue;

                    float allocatedPower = source.State.Allocate(requestedPower);

                    if (allocatedPower <= 0f) continue;

                    transferBudget.Consume(route.Cables, allocatedPower);

                    target.RemainingDemandMW -= allocatedPower;
                    target.ReceivedPowerMW += allocatedPower;

                    return allocatedPower;
                }
            }
        }

        return 0f;
    }

    private void OnDisable() {
        if (gridWorld == null) return;

        foreach (Machine machine in gridWorld.GetComponentsInChildren<Machine>()) {
            machine.SetSuppliedPower(0f);
        }
    }
}
