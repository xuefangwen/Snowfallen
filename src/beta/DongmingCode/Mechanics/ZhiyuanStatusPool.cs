using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace SnowfallenMod.DongmingCode.Mechanics;

// This allowlist belongs only to ZhouFu (织渊); it does not register a shared card pool.
internal static class ZhiyuanStatusPool
{
    internal static List<CardModel> GetCandidates() =>
    [
        ModelDb.Card<Beckon>(),
        ModelDb.Card<Burn>(),
        ModelDb.Card<Dazed>(),
        ModelDb.Card<Debris>(),
        ModelDb.Card<Infection>(),
        ModelDb.Card<Slimed>(),
        ModelDb.Card<Soot>(),
        ModelDb.Card<Toxic>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Void>(),
        ModelDb.Card<Wound>()
    ];
}
