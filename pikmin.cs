using System;
using System.Collections.Generic;
using System.Linq;
//using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using CrowdControl.Common;
using JetBrains.Annotations;
using System.Runtime.InteropServices;

//test moments SUS

namespace CrowdControl.Games.Packs
{
    [UsedImplicitly]
    public class Pikmin : WiiEffectPack
    {
        public Pikmin([NotNull] IPlayer player, [NotNull] Func<CrowdControlBlock, bool> responseHandler, [NotNull] Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler) { }

        private volatile bool _quitting = false;
        protected override void Dispose(bool disposing)
        {
            _quitting = true;
            base.Dispose(disposing);
        }


        private const uint SDA = 0x803e4d20;
        private const uint TOC = 0x803f0200;
        private const uint PIKIMGR = SDA + 0x3068;
        private const uint NAVIMGR = SDA + 0x3120;
        private const uint NAVIGENERATOR = SDA + 0x3080;
        private const uint AICONSTANT = SDA + 0x2f80;
        private const uint PIKICOLORS = 0x803d1e18;
        private const uint GAMEFLOW = 0x8039d7b8;
        private const uint PUFFMIN_INSTANCE = 0x803d1e54;
        private const uint DEMODRAW_PIKI_PTR = 0x802ba564;
        private const uint DEMODRAW_PIKI = 0x800d9a68;
        private const uint HUD_FLOATVAL = 0x803EB96C;

        //most actPiki functions are designed to just harm the pikmin
        //bury 0x802ad11c, unincluded cuz I think it may cause glitches to remove
        private uint[] ACTPIKI_BADSTATES = { 0x802cd9f8, 0x802acf34, 0x802acd80, 0x802aceec, 0x802acd38, 0x802acff0, 0x802acf7c, 0x802acdc8 };
        private uint[] ACTPIKI_BADSTATES_ORIG = new uint[10];
        //pointer to machine code that functions as a return 1
        private const uint FUNC_RETURN_ONE = 0x800082b8;

        private float origDayMultiply;

        //parms offsets
        private readonly uint[] parmsPikiMgr = { PIKIMGR, 0x68 };
        public override List<Effect> Effects
        {
            get
            {
                List<Effect> effects = new List<Effect>
                {
                    //Ideas remaining:
                    //crush olimar
                    //plant the pikmin
                    //make the pickles stop in their tracks while carrying
                    //disable C stick
                    //disable dismissing
                    //force dismiss squad
                    //Drop bombs of yellow pickles immediately

                    new Effect("Blue Pikmin Color", "blupikicolor", ItemKind.BidWar) {Price = 10},
                    new Effect("Red Pikmin Color", "redpikicolor", ItemKind.BidWar) {Price = 10},
                    new Effect("Yellow Pikmin Color", "yelpikicolor", ItemKind.BidWar) {Price = 10},

                    new Effect("Flower field pikmin", "pikiallflower") {Price = 10 },
                    new Effect("Bud field pikmin", "pikiallbud") {Price = 10},
                    new Effect("Leaf field pikmin", "pikiallleaf") {Price = 10},
                    new Effect("Fast Pikmin", "pikiallfast") {Price = 10},
                    new Effect("Slow Pikmin", "pikiallslow") {Price = 10},
                    new Effect("Strong Pikmin", "pikiallstrong") {Price = 10},
                    new Effect("Weak Pikmin", "pikiallweak") {Price = 10},
                    new Effect("Forward Time", "forwardtime") {Price = 10},
                    new Effect("Rewind Time", "rewindtime") {Price = 10},
                    new Effect("Disable Whistle", "disablewhistle") {Price = 10},
                    new Effect("Grant Pluckaphone", "grantpluckaphone") {Price = 10},
                    new Effect("Revoke Pluckaphone", "revokepluckaphone") {Price = 10},
                    new Effect("Heal Olimar", "navifullhealth") {Price = 10},
                    new Effect("One-Hit KO", "ohko") {Price = 10},
                    new Effect("Disable Hud", "disablehud") {Price = 10},
                    new Effect("Hyper Olimar", "navifast") {Price = 10},
                    new Effect("Lethargic Olimar", "navislow") {Price = 10},
                    new Effect("Send Olimar to Spawn", "resetolimarpos") {Price = 10},
                    new Effect("Thanos Snap", "thanossnap") {Price = 10},
                    new Effect("Invincible Pikmin!", "invinciblepikmin") {Price = 10}, // This sometimes stutter the game for some reason may want to remove from final product
                    new Effect("Invisible Pikmin?", "invisiblepikmin") {Price = 10},
                    new Effect("\"Wiimote\" Controls", "wiimotecontrols") {Price = 10}
                };
                effects.AddRange(_piki_colors.Select(t => new Effect(t.Value.name, $"blupikicolor_{t.Key}", ItemKind.BidWarValue, "blupikicolor")));
                effects.AddRange(_piki_colors.Select(t => new Effect(t.Value.name, $"redpikicolor_{t.Key}", ItemKind.BidWarValue, "redpikicolor")));
                effects.AddRange(_piki_colors.Select(t => new Effect(t.Value.name, $"yelpikicolor_{t.Key}", ItemKind.BidWarValue, "yelpikicolor")));
                return effects;
            }
        }

        private static readonly Dictionary<string, (string name, byte[] color)> _piki_colors =
        new Dictionary<string, (string, byte[])>
        {
            {"blue", ("Blue", new byte[] {0x00, 0x32, 0xff})},
            {"red", ("Red", new byte[] {0xff, 0x1e, 0x00})},
            {"yellow", ("Yellow", new byte[] {0xFf, 0xd2, 0x01})},
            {"cyan", ("Cyan", new byte[] {0x31, 0xE2, 0xE8})},
            {"orange", ("Orange", new byte[] {0xFF, 0x91, 0x01})},
            {"magenta", ("Magenta", new byte[] {0xE8, 0x0C, 0xCF})},
            {"black", ("Black", new byte[] {0x00, 0x00, 0x00})},
            {"white", ("White", new byte[] {0xFF, 0xFF, 0xFF})}
        };


        public override List<ROMInfo> ROMTable => new List<ROMInfo>(new[]
        {
            new ROMInfo("Pikmin", null, Patching.Ignore, ROMStatus.ValidPatched,s => Patching.MD5(s, "f23b67c759767142f220f2674c29ff68")),
        });

        public override List<(string, Action)> MenuActions => new List<(string, Action)>();

        public override Game Game { get; } = new Game(0xDEADBEEF, "Pikmin", "Pikmin", "GC", ConnectorType.WiiConnector);

        protected override bool IsReady(EffectRequest request) => true;

        protected override void RequestData(DataRequest request) => Respond(request, request.Key, null, false, $"Variable name \"{request.Key}\" not known");

        [StructLayout(LayoutKind.Explicit)]
        public struct FloatIntUnion
        {
            [FieldOffset(0)]
            public int Int32Bits;
            [FieldOffset(0)]
            public uint UInt32Bits;
            [FieldOffset(0)]
            public float FloatValue;
        }

        //Everyone knows it's called a hex float
        private uint hexFloat(float aFloat)
        {
            FloatIntUnion f2i = default(FloatIntUnion);
            f2i.FloatValue = aFloat;
            return f2i.UInt32Bits;
        }


        //simplified way to get a bunch of sequential values
        private uint getAddressInner(params uint[] args)
        {
            uint addr = 0;
            for (int i = 0; i < args.Length; i++)
            {
                Connector.Read32(addr + args[i], out addr);
            }
            return addr;
        }

        //return the number of objects/pikis in the pikiMgr
        private uint getPikiCount()
        {
            Connector.Read32(PIKIMGR, out uint pikiMgr);
            if (pikiMgr == 0) return 0;
            Connector.Read32(pikiMgr + 0x2c, out uint pikiCount);
            return pikiCount;
        }
        //there is a possibility of more than 1 Olimar
        private uint[] getNavis()
        {
            Connector.Read32(NAVIMGR, out uint naviMgr);
            Connector.Read32(naviMgr+0x30, out uint naviNum);
            uint[] toRet = new uint[naviNum];
            Connector.Read32(naviMgr + 0x28, out uint naviObjList);
            for (uint i = 0; i != naviNum; i++)
            {
                Connector.Read32(naviObjList + i*4, out toRet[i]);
            }
            return toRet;
        }

        //set the leaf/bud/flower of pikmin (in that sequential order 0,1,2)
        private bool pikiallSetFlower(EffectRequest request, uint happaId, string msg)
        {
            TryEffect(request,
            () => true,
            () => 
            { 
                Connector.Read32(PIKIMGR, out uint pikimgr);
                Connector.Read32(pikimgr + 0x28, out uint pikimgrInner);
                for (uint i = 0; i != getPikiCount(); i++)
                {
                    Connector.Read32(pikimgrInner + i * 4, out uint piki);
                    Connector.Write32(piki + 0x520, happaId); // happa flower state
                    Connector.Read32(pikimgr + 0x3c + happaId*4, out uint happaModel);
                    Connector.Write32(piki + 0x598, happaModel); // happa flower state
                }
                return true;
            },
            () => { Connector.SendMessage(msg); });
            return true;
        }

        private bool setParm(EffectRequest request, uint[] parmLoc, string parmNameS, uint parmVal, string msg="SUSSY")
        {
            uint parmName = 0;
            //cause an exception if parmNameS is !3 if you want i don't care enough to, just don't write that!
            parmName |= (uint)(parmNameS[0]) << 24;
            parmName |= (uint)(parmNameS[1]) << 16;
            parmName |= (uint)(parmNameS[2]) << 8;
            //TryEffect(request,
            //() => true,
            //() => 
            //{
                uint parmList = getAddressInner(parmLoc);
                //first u32 will never be the right parm name (is not a parm)
                for (uint i = 4; i != 3000; i += 4)
                {
                    Connector.Read32(parmList + i, out uint parmCheckName);
                    if(parmCheckName == parmName)
                    {
                        Connector.Write32(parmList + i + 0xc, parmVal);
                        return true;
                    }
                }
                return false;
            //},
            //() => { Connector.SendMessage(msg); });
            return true;
        }
        
        private bool setPikiColor(byte[] color, uint typeId=0)
        {
            Connector.SendMessage($"make le {typeId} pickle le {color[0]:X}");
            uint p = PIKICOLORS + typeId * 4;
            Connector.Write8(p, color[0]);
            Connector.Write8(p+1, color[1]);
            Connector.Write8(p+2, color[2]);
            return true;
        }


        protected override void StartEffect(EffectRequest request)
        {
            if (!IsReady(request))
            {
                DelayEffect(request, TimeSpan.FromSeconds(5));
                return;
            }

            Connector.Read32(PIKIMGR, out uint pikimgr);
            Connector.Read32(pikimgr + 0x28, out uint pikimgrInner);

            string[] codeParams = request.FinalCode.Split('_');
            Connector.SendMessage($"Running: {codeParams[0]}");

            switch (codeParams[0])
            {
                case "pikiallleaf":
                    pikiallSetFlower(request, 0, "Leafing all Pikmin!");
                    return;
                case "pikiallbud":
                    pikiallSetFlower(request, 1, "Budding all Pikmin!");
                    return;
                case "pikiallflower":
                    pikiallSetFlower(request, 2, "Flowering all Pikmin!");
                    return;
                case "pikiallfast":
                    StartTimed(request, () => true, () => {
                        //make leafs 220, buds 240, and flowers 270
                        setParm(request, parmsPikiMgr, "p01", hexFloat(220));
                        setParm(request, parmsPikiMgr, "p54", hexFloat(270));
                        setParm(request, parmsPikiMgr, "p65", hexFloat(240));
                        //Connector.SendMessage("The Pickles are do be fast rn!");
                        return true;
                    }, TimeSpan.FromSeconds(15), "pikispeed");
                    return;
                case "pikiallslow":
                    StartTimed(request, () => true, () => {
                        //make leafs 60, buds 70, and flowers 85
                        setParm(request, parmsPikiMgr, "p01", hexFloat(60));
                        setParm(request, parmsPikiMgr, "p54", hexFloat(85));
                        setParm(request, parmsPikiMgr, "p65", hexFloat(70));
                        //Connector.SendMessage("The Pickles are do be slow rn!");
                        return true;
                    }, TimeSpan.FromSeconds(15), "pikispeed");
                    return;
                case "pikiallstrong":
                    StartTimed(request, () => true, () => {
                        setParm(request, parmsPikiMgr, "p04", hexFloat(15));
                        setParm(request, parmsPikiMgr, "p12", hexFloat(20));
                        setParm(request, parmsPikiMgr, "p13", hexFloat(15));
                       // Connector.SendMessage("The Pickles are do be strong rn!");
                        return true;
                    }, TimeSpan.FromSeconds(15), "pikistrength");
                    return;
                case "pikiallweak":
                    StartTimed(request, () => true, () => {
                        setParm(request, parmsPikiMgr, "p04", hexFloat(5));
                        setParm(request, parmsPikiMgr, "p12", hexFloat(7.5f));
                        setParm(request, parmsPikiMgr, "p13", hexFloat(5));
                       // Connector.SendMessage("The Pickles are do be weak rn!");
                        return true;
                    }, TimeSpan.FromSeconds(15), "pikistrength");
                    return;
                //The game processes time really weird. -0x7-0x6 is the unused night time mode... 0x7 - 0x13 is day time. The day ends once the dayTimeInt hits 0x13.
                case "forwardtime":
                    Connector.Read32(GAMEFLOW + 0x2f8, out uint dayTimeInt);
                    dayTimeInt += 1;
                    //If you want to be kind add a check herer for if dayTimeInt == 0x12 to not jump to day end
                    Connector.Write32(GAMEFLOW + 0x2f8, dayTimeInt);
                    return;
                case "rewindtime":
                    Connector.Read32(GAMEFLOW + 0x2f8, out dayTimeInt);
                    TryEffect(request,
                        () => dayTimeInt != 0,
                        () =>
                        {
                            dayTimeInt -= 1;
                            Connector.Write32(GAMEFLOW + 0x2f8, dayTimeInt);
                            return true;
                        },
                        () => { Connector.SendMessage($"{request.DisplayViewer} has established rewind."); });
                    return;
                case "blupikicolor":
                    BidWar(request, _piki_colors.Select(c => (c.Key, (Func<bool>)(() => setPikiColor(c.Value.color, 0)))).ToDictionary(t => "blupikicolor_" + t.Item1, t => t.Item2));
                    return;
                case "redpikicolor":
                    BidWar(request, _piki_colors.Select(c => (c.Key, (Func<bool>)(() => setPikiColor(c.Value.color, 1)))).ToDictionary(t => "redpikicolor_" + t.Item1, t => t.Item2));
                    return;
                case "yelpikicolor":
                    BidWar(request, _piki_colors.Select(c => (c.Key, (Func<bool>)(() => setPikiColor(c.Value.color, 2)))).ToDictionary(t => "yelpikicolor_" + t.Item1, t => t.Item2));
                    return;
                case "thanossnap":
                    List<uint> toKill = new List<uint>();
                    for (uint i = 0; i != getPikiCount(); i++)
                    {
                        Connector.Read32((ulong)(pikimgrInner + i * 4), out uint piki);
                        Connector.Read32(piki + 0x52c, out uint unk);
                        Connector.Read32(piki + 0x4, out uint unk2);
                        Connector.ReadFloat(piki + 0x58, out float pikiHealth);
                        if (unk2 != 6 || unk != 7 || pikiHealth >= 0) toKill.Add(i);
                    }
                    toKill.Shuffle();
                    for (uint i = 0; i < toKill.Count; i += 2)
                    {
                        Connector.Read32((ulong)(pikimgrInner + toKill[(int)i] * 4), out uint piki);
                        Connector.Write32(piki + 0x58, 0);
                    }
                    return;
                case "invinciblepikmin":
                    StartTimed(request, () => true, () => {
                        for (uint i = 0; i != ACTPIKI_BADSTATES.Length; i++)
                        {
                            Connector.Read32(ACTPIKI_BADSTATES[i], out uint pikiBadState);
                            ACTPIKI_BADSTATES_ORIG[i] = pikiBadState;
                            Connector.Write32(ACTPIKI_BADSTATES[i], FUNC_RETURN_ONE);
                        }
                        return true;
                    }, TimeSpan.FromSeconds(10), request.FinalCode);
                    return;
                case "invisiblepikmin":
                    StartTimed(request, () => true, () => {
                        Connector.Write32(DEMODRAW_PIKI_PTR, FUNC_RETURN_ONE);
                        return true;
                    }, TimeSpan.FromSeconds(10), request.FinalCode);
                    return;
                case "disablewhistle":
                    StartTimed(request, () => true, () => {
                        foreach (uint navi in getNavis())
                        {
                            Connector.Write32(navi + 0x310, 0);
                        }
                        return true;
                    }, TimeSpan.FromSeconds(5), request.FinalCode);
                    return;
                case "grantpluckaphone":
                    Connector.Read32(AICONSTANT, out uint aiConst);
                    Connector.Write32(aiConst + 0x100, 1);
                    return;
                case "revokepluckaphone":
                    Connector.Read32(AICONSTANT, out uint aiConst2);
                    Connector.Write32(aiConst2 + 0x100, 0);
                    return;
                case "ohko":
                    foreach (uint navi in getNavis())
                    {
                        Connector.Write32(navi + 0x58, hexFloat(1));
                    }
                    return;
                case "navifullhealth":
                    foreach (uint navi in getNavis())
                    {
                        Connector.Write32(navi + 0x58, hexFloat(100));
                    }
                    return;
                case "navifast":
                    StartTimed(request, () => true, () => {
                        foreach (uint navi in getNavis())
                        {
                            Connector.Read32(navi + 0x224, out uint parmsNaviMgr);
                            uint[] arr = { parmsNaviMgr };
                            setParm(request, arr, "p56", hexFloat(320));
                        }
                        return true;
                    }, TimeSpan.FromSeconds(10), "navispeed");
                    return;
                case "navislow":
                    StartTimed(request, () => true, () => {
                        foreach (uint navi in getNavis())
                        {
                            Connector.Read32(navi + 0x224, out uint parmsNaviMgr);
                            uint[] arr = { parmsNaviMgr };
                            setParm(request, arr, "p56", hexFloat(80));
                        }
                        return true;
                    }, TimeSpan.FromSeconds(10), "navispeed");
                    return;
                case "resetolimarpos":
                    Connector.Read32(NAVIGENERATOR, out uint naviGenerator);
                    Connector.Read32(naviGenerator+0x4c, out uint x);
                    Connector.Read32(naviGenerator+0x50, out uint y);
                    Connector.Read32(naviGenerator+0x54, out uint z);
                    foreach(uint navi in getNavis())
                    {
                        Connector.Write32(navi + 0x94, x);
                        Connector.Write32(navi + 0x98, y);
                        Connector.Write32(navi + 0x9c, z);
                    }
                    return;
                case "wiimotecontrols":
                    StartTimed(request, () => true, () => {
                        foreach(uint navi in getNavis())
                        {
                            Connector.Read32(navi + 0x224, out uint parmsNaviMgr);
                            uint[] arr = { parmsNaviMgr };
                            setParm(request, arr, "p46", hexFloat(300));
                            setParm(request, arr, "p47", hexFloat(900));
                        }
                        return true;
                    }, TimeSpan.FromSeconds(15), request.FinalCode);
                    return;
                case "disablehud":
                    StartTimed(request, () => true, () => {
                        Connector.Write32(HUD_FLOATVAL, 0);
                        return true;
                    }, TimeSpan.FromSeconds(20), request.FinalCode);
                    return;
            }
        }

        protected override bool StopEffect(EffectRequest request)
        {
            string[] codeParams = request.FinalCode.Split('_');
            switch (codeParams[0])
            {
                case "pikiallfast":
                case "pikiallslow":
                    setParm(request, parmsPikiMgr, "p01", hexFloat(120));
                    setParm(request, parmsPikiMgr, "p54", hexFloat(170));
                    setParm(request, parmsPikiMgr, "p65", hexFloat(140));
                    //Connector.SendMessage("The Pickles are do be normal speed rn!");
                    return true;
                case "pikiallstrong":
                case "pikiallweak":
                    setParm(request, parmsPikiMgr, "p04", hexFloat(10));
                    setParm(request, parmsPikiMgr, "p12", hexFloat(15));
                    setParm(request, parmsPikiMgr, "p13", hexFloat(10));
                    //Connector.SendMessage("The Pickles are do be normaL STRENGTH rn!");
                    return true;
                case "invinciblepikmin":
                    for (uint i = 0; i != ACTPIKI_BADSTATES.Length; i++)
                    {
                        Connector.Write32(ACTPIKI_BADSTATES[i], ACTPIKI_BADSTATES_ORIG[i]);
                    }
                    return true;
                case "disablewhistle":
                    foreach (uint navi in getNavis())
                    {
                        Connector.Write32(navi + 0x310, 1);
                    }
                    return true;
                case "wiimotecontrols":
                    foreach (uint navi in getNavis())
                    {
                        Connector.Read32(navi + 0x224, out uint parmsNaviMgr);
                        uint[] arr = { parmsNaviMgr };
                        setParm(request, arr, "p46", hexFloat(100));
                        setParm(request, arr, "p47", hexFloat(300));
                        //make the whistle go on top of navi to prevent it getting stuck!
                        Connector.Write32(navi + 0x6d4, 0);
                        Connector.Write32(navi + 0x6dc, 0);
                    }
                    return true;
                case "invisiblepikmin":
                    Connector.Write32(DEMODRAW_PIKI_PTR, DEMODRAW_PIKI);
                    return true;
                case "disablehud":
                    Connector.Write32(HUD_FLOATVAL, hexFloat(30));
                    return true;
                case "navifast":
                case "navislow":
                    foreach (uint navi in getNavis())
                    {
                        Connector.Read32(navi + 0x224, out uint parmsNaviMgr);
                        uint[] arr = { parmsNaviMgr };
                        setParm(request, arr, "p56", hexFloat(160));
                    }
                    return true;
                default:
                    return true;
            }
    }
}
}
