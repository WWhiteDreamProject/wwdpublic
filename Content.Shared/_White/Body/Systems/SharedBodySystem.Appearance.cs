using Content.Shared._White.Body.Components;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeAppearance()
    {
    }

    #region Private API

    private List<Marking> ResolveMarkings(List<Marking> markings, Color? color, Dictionary<Enum, MarkingsAppearance> appearances)
    {
        var ret = new List<Marking>();
        var forcedColors = new List<(Marking, MarkingPrototype)>();

        // This method uses two loops since some marking with constrained colors care about the colors of previous markings.
        // As such we want to ensure we can apply the markings they rely on first.
        foreach (var marking in markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (!proto.ForcedColoring && appearances.GetValueOrDefault(proto.BodyPart)?.MatchSkin != true)
                ret.Add(marking);
            else
                forcedColors.Add((marking, proto));
        }

        foreach (var (marking, prototype) in forcedColors)
        {
            var colors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                skinColor,
                eyeColor,
                ret);

            var markingWithColor = new Marking(marking.MarkingId, colors)
            {
                Forced = marking.Forced,
            };
            if (appearances.GetValueOrDefault(prototype.BodyPart) is { MatchSkin: true } appearance && skinColor is { } color)
            {
                markingWithColor = markingWithColor.WithColor(color.WithAlpha(appearance.LayerAlpha));
            }
            ret.Add(markingWithColor);
        }

        return ret;
    }

    protected virtual void SetProviderColor(Entity<BodyAppearanceProviderComponent> ent, Color color)
    {
        ent.Comp.Data.Color = color;
        Dirty(ent);
    }

    protected virtual void SetProviderAppearance(Entity<BodyAppearanceProviderComponent> ent, PrototypeLayerData data)
    {
        ent.Comp.Data.Data = data;
        Dirty(ent);
    }

    protected virtual void SetProviderMarkings(Entity<BodyAppearanceProviderComponent> ent, Dictionary<Enum, List<Marking>> markings)
    {
        ent.Comp.Markings = markings;
        Dirty(ent);
    }

    #endregion
}
