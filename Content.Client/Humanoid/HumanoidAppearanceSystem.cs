using System.Numerics;
using Content.Client._White.Body.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly BodySystem _body = default!; // WD EDIT

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, AfterAutoHandleStateEvent>(OnHandleState);
        Subs.CVar(_configurationManager, CCVars.AccessibilityClientCensorNudity, OnCvarChanged, true);
        Subs.CVar(_configurationManager, CCVars.AccessibilityServerCensorNudity, OnCvarChanged, true);
    }

    private void OnHandleState(EntityUid uid, HumanoidAppearanceComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(component, Comp<SpriteComponent>(uid));
    }

    private void OnCvarChanged(bool value)
    {
        var humanoidQuery = EntityManager.AllEntityQueryEnumerator<HumanoidAppearanceComponent, SpriteComponent>();
        while (humanoidQuery.MoveNext(out var _, out var humanoidComp, out var spriteComp))
        {
            UpdateSprite(humanoidComp, spriteComp);
        }
    }

    private void UpdateSprite(HumanoidAppearanceComponent component, SpriteComponent sprite)
    {
        var speciesPrototype = _prototypeManager.Index(component.Species);

        var height = Math.Clamp(component.Height, speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        var width = Math.Clamp(component.Width, speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        component.Height = height;
        component.Width = width;

        sprite.Scale = new Vector2(width, height);
    }

    /// <summary>
    ///     Loads a profile directly into a humanoid.
    /// </summary>
    /// <param name="uid">The humanoid entity's UID</param>
    /// <param name="profile">The profile to load.</param>
    /// <param name="humanoid">The humanoid entity's humanoid component.</param>
    /// <remarks>
    ///     This should not be used if the entity is owned by the server. The server will otherwise
    ///     override this with the appearance data it sends over.
    /// </remarks>
    public override void LoadProfile(EntityUid uid,
        HumanoidCharacterProfile? profile,
        HumanoidAppearanceComponent? humanoid = null,
        bool loadExtensions = true,
        bool generateLoadouts = true)
    {
        if (profile == null)
            return;

        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var markings = new MarkingSet(speciesPrototype.MarkingPoints, _markingManager, _prototypeManager);

        // Add markings that doesn't need coloring. We store them until we add all other markings that doesn't need it.
        var markingFColored = new Dictionary<Marking, MarkingPrototype>();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                if (!prototype.ForcedColoring)
                {
                    markings.AddBack(prototype.MarkingCategory, marking);
                }
                else
                {
                    markingFColored.Add(marking, prototype);
                }
            }
        }

        // legacy: remove in the future?
        //markings.RemoveCategory(MarkingCategories.Hair);
        //markings.RemoveCategory(MarkingCategories.FacialHair);

        // We need to ensure hair before applying it or coloring can try depend on markings that can be invalid
        var hairColor = _markingManager.MustMatchSkin(profile.Species, MarkingCategories.Hair, out var hairAlpha, _prototypeManager) // WD EDIT
            ? profile.Appearance.SkinColor.WithAlpha(hairAlpha)
            : profile.Appearance.HairColor;
        var hair = new Marking(profile.Appearance.HairStyleId,
            new[] { hairColor });

        var facialHairColor = _markingManager.MustMatchSkin(profile.Species, MarkingCategories.FacialHair, out var facialHairAlpha, _prototypeManager) // WD EDIT
            ? profile.Appearance.SkinColor.WithAlpha(facialHairAlpha)
            : profile.Appearance.FacialHairColor;
        var facialHair = new Marking(profile.Appearance.FacialHairStyleId,
            new[] { facialHairColor });

        if (_markingManager.CanBeApplied(profile.Species, profile.Sex, hair, _prototypeManager))
        {
            markings.AddBack(MarkingCategories.Hair, hair);
        }
        if (_markingManager.CanBeApplied(profile.Species, profile.Sex, facialHair, _prototypeManager))
        {
            markings.AddBack(MarkingCategories.FacialHair, facialHair);
        }

        // Finally adding marking with forced colors
        foreach (var (marking, prototype) in markingFColored)
        {
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                profile.Appearance.SkinColor,
                profile.Appearance.EyeColor,
                markings
            );
            markings.AddBack(prototype.MarkingCategory, new Marking(marking.MarkingId, markingColors));
        }

        markings.EnsureSpecies(profile.Species, profile.Appearance.SkinColor, _markingManager, _prototypeManager);
        markings.EnsureSexes(profile.Sex, _markingManager);
        markings.EnsureDefault(
            profile.Appearance.SkinColor,
            profile.Appearance.EyeColor,
            _markingManager);

        DebugTools.Assert(IsClientSide(uid));

        humanoid.MarkingSet = markings;
        // WD EDIT START
        humanoid.PermanentlyHidden = new HashSet<Enum>();
        humanoid.HiddenLayers = new HashSet<Enum>();
        // WD EDIT END
        humanoid.Sex = profile.Sex;
        humanoid.Gender = profile.Gender;
        humanoid.DisplayPronouns = profile.DisplayPronouns;
        humanoid.StationAiName = profile.StationAiName;
        humanoid.CyborgName = profile.CyborgName;
        humanoid.Age = profile.Age;
        humanoid.BodyType = profile.BodyType; // WD EDIT
        humanoid.Species = profile.Species;
        humanoid.SkinColor = profile.Appearance.SkinColor;
        humanoid.EyeColor = profile.Appearance.EyeColor;
        humanoid.Height = profile.Height;
        humanoid.Width = profile.Width;

        UpdateSprite(humanoid, Comp<SpriteComponent>(uid));
        // WD EDIT START
        _body.SetupBodyAppearance((uid, null, null, humanoid));
        humanoid.ClientOldMarkings.Clear();
        humanoid.ClientOldMarkings = new (humanoid.MarkingSet);
        // WD EDIT END
    }

    public override void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, bool verify = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || humanoid.SkinColor == skinColor)
            return;

        base.SetSkinColor(uid, skinColor, false, verify, humanoid);
    }

    protected override void SetLayerVisibility(
        EntityUid uid,
        HumanoidAppearanceComponent humanoid,
        Enum layer, // WD EDIT
        bool visible,
        bool permanent,
        ref bool dirty)
    {
        base.SetLayerVisibility(uid, humanoid, layer, visible, permanent, ref dirty);

        var sprite = Comp<SpriteComponent>(uid);
        if (!sprite.LayerMapTryGet(layer, out var index))
        {
            if (!visible)
                return;
            else
                index = sprite.LayerMapReserveBlank(layer);
        }

        var spriteLayer = sprite[index];
        if (spriteLayer.Visible == visible)
            return;

        spriteLayer.Visible = visible;
    }
}
