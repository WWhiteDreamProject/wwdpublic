using Content.Shared.Examine;
using Content.Shared.Inventory;

namespace Content.Server._White.Examine
{
    public sealed class ExaminableCharacterSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ExaminableCharacterComponent, ExaminedEvent>(HandleExamine);
        }

        private void HandleExamine(EntityUid uid, ExaminableCharacterComponent comp, ExaminedEvent args)
        {
            var selfaware = args.Examiner == args.Examined;

            var canseeloc = selfaware ? "examine-can-see-selfaware" : "examine-can-see";

            var cansee = Loc.GetString(canseeloc, ("ent", uid));

            var slotLabels = new Dictionary<string, string>
            {
                { "head", "head-" },
                { "eyes", "eyes-" },
                { "mask", "mask-" },
                { "neck", "neck-" },
                { "ears", "ears-" },
                { "jumpsuit", "jumpsuit-" },
                { "outerClothing", "outer-" },
                { "back", "back-" },
                { "gloves", "gloves-" },
                { "belt", "belt-" },
                { "id", "id-" },
                { "shoes", "shoes-" },
                { "suitstorage", "suitstorage-" }
            };

            var priority = 13;

            foreach (var slotEntry in slotLabels)
            {
                var slotName = slotEntry.Key;
                var slotLabel = slotEntry.Value;

                slotLabel += "examine";

                if (selfaware)
                    slotLabel += "-selfaware";

                if (!_inventorySystem.TryGetSlotEntity(uid, slotName, out var slotEntity)
                    || HasComp<ExaminableCharacterHideIconComponent>(slotEntity.Value))
                    continue;

                if (_entityManager.TryGetComponent<MetaDataComponent>(slotEntity, out var metaData))
                {
                    var item = Loc.GetString(slotLabel, ("item", metaData.EntityName), ("ent", uid));
                    args.PushMarkup($"[font size=10]{item}[/font]", priority);
                    priority--;
                }
            }

            if (priority < 13) // If nothing is worn dont show
            {
                args.PushMarkup($"[font size=10]{cansee}[/font]", 14);
            }
            else
            {
                var canseenothingloc = selfaware ? "examine-can-see-nothing-selfaware" : "examine-can-see-nothing";

                var canseenothing = Loc.GetString(canseenothingloc, ("ent", uid));

                args.PushMarkup($"[font size=10]{canseenothing}[/font]", 14);
            }
        }
    }
}
