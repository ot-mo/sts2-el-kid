using ElKid.ElKidCode.Time;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace ElKid.ElKidCode.Relics;

public class PocketWatch() : ElKidRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Time", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [TimeResource.Tip];

    public override Task BeforeCombatStart()
    {
        Flash();
        TimeResource.Gain(Owner, DynamicVars["Time"].IntValue);
        return Task.CompletedTask;
    }
}
