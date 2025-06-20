using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns.AmmoProviderRedirect;

public sealed class AmmoProviderRedirectSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    //[Dependency] private readonly SharedDeviceLinkSystem _link = default!;

    //public const string SourcePortId = "AmmoFeedPortSource";
    //public const string SinkPortId = "AmmoFeedPortSink";
    public override void Initialize()
    {
        SubscribeLocalEvent<ParentAmmoProviderComponent, TakeAmmoEvent>(RedirectParent);
        //SubscribeLocalEvent<AmmoProviderDeviceLinkRedirectComponent, LinkAttemptEvent>(OnNewLinkAttempt);
        //SubscribeLocalEvent<AmmoProviderDeviceLinkRedirectComponent, NewLinkEvent>(OnNewLink);
        //SubscribeLocalEvent<AmmoProviderDeviceLinkRedirectComponent, PortDisconnectedEvent>(OnRemoveLink);
        //SubscribeLocalEvent<AmmoProviderDeviceLinkRedirectComponent, TakeAmmoEvent>(Redirect);
    }

    // todo figure out a way to check bullets' caliber before firing
    // don't let people feed .50 cal rounds into methaphorical pea shooters
    private void RedirectParent(EntityUid uid, ParentAmmoProviderComponent comp, TakeAmmoEvent args)
    {
        if (_transform.GetParentUid(uid) is { Valid: true } parent)
            RaiseLocalEvent(uid, args);
    }

    //private void OnNewLinkAttempt(EntityUid uid, AmmoProviderDeviceLinkRedirectComponent comp, LinkAttemptEvent ev)
    //{
    //    if (ev.SinkPort != SinkPortId)
    //        return;
    //
    //    if (ev.SourcePort != SourcePortId ||
    //        !_transform.InRange(ev.Source, ev.Sink, comp.Range))
    //        ev.Cancel();
    //}
    //
    //private void OnNewLink(EntityUid uid, AmmoProviderDeviceLinkRedirectComponent comp, NewLinkEvent ev)
    //{
    //    comp.Link = ev.Source;
    //}
    //
    //private void OnRemoveLink(EntityUid uid, AmmoProviderDeviceLinkRedirectComponent comp, PortDisconnectedEvent ev)
    //{
    //    comp.Link = null;
    //}
    //
    //private void Redirect(EntityUid uid, AmmoProviderDeviceLinkRedirectComponent comp, TakeAmmoEvent ev)
    //{
    //    //if (comp.Link is EntityUid linked)
    //    //    RaiseLocalEvent(linked, ev);
    //
    //    if (!TryComp<DeviceLinkSinkComponent>(uid, out var sink))
    //        return;
    //
    //    sink.
    //}

}
