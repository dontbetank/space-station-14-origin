using Content.Server.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class RehydratableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RehydratableComponent, SolutionChangedEvent>(OnSolutionChange);
    }

    private void OnSolutionChange(EntityUid uid, RehydratableComponent comp, SolutionChangedEvent args)
    {
        foreach (var prototype in comp.CatalystPrototypes)
        {
            var quantity = _solutions.GetReagentQuantity(uid, prototype);
            if (quantity == FixedPoint2.Zero || quantity < comp.CatalystMinimum)
                continue;
            Expand(uid, comp);
            return;
        }
    }

    // Try not to make this public if you can help it.
    private void Expand(EntityUid uid, RehydratableComponent comp)
    {
        _popups.PopupEntity(Loc.GetString("rehydratable-component-expands-message", ("owner", uid)), uid);

        var target = Spawn(comp.TargetPrototype, Transform(uid).Coordinates);
        _transform.AttachToGridOrMap(target);
        var ev = new GotRehydratedEvent(target);
        RaiseLocalEvent(uid, ref ev);

        // prevent double hydration while queued
        RemComp<RehydratableComponent>(uid);
        QueueDel(uid);
    }
}
