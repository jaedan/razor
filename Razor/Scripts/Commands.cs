﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assistant.Core;
using UOSteam;

namespace Assistant.Scripts
{
    public class Commands
    {
        public static void Register()
        {
            // Commands based on Actions.cs
            Interpreter.RegisterCommandHandler("cast", Cast); //BookcastAction, etc

            // Dress
            Interpreter.RegisterCommandHandler("dress", DressCommand); //DressAction
            Interpreter.RegisterCommandHandler("undress", UnDressCommand); //UndressAction
            Interpreter.RegisterCommandHandler("dressconfig", DressConfig);

            // Targets
            Interpreter.RegisterCommandHandler("target", Target); //Absolute Target
            Interpreter.RegisterCommandHandler("targettype", TargetType); //TargetTypeAction
            Interpreter.RegisterCommandHandler("targetrelloc", TargetRelLoc); //TargetRelLocAction

            Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget); //WaitForTargetAction
            Interpreter.RegisterCommandHandler("wft", WaitForTarget); //WaitForTargetAction

            // Using stuff
            Interpreter.RegisterCommandHandler("dclicktype", UseType); // DoubleClickTypeAction
            Interpreter.RegisterCommandHandler("dclick", UseObject); //DoubleClickAction
            Interpreter.RegisterCommandHandler("dclickvar", DummyCommand); //DoubleClickVariableAction -- this needed?

            Interpreter.RegisterCommandHandler("usetype", UseType); // DoubleClickTypeAction
            Interpreter.RegisterCommandHandler("useobject", UseObject); //DoubleClickAction

            // Moving stuff
            Interpreter.RegisterCommandHandler("drop", DropItem); //DropAction
            Interpreter.RegisterCommandHandler("droprelloc", DropRelLoc); //DropAction
            Interpreter.RegisterCommandHandler("lift", LiftItem); //LiftAction
            Interpreter.RegisterCommandHandler("lifttype", LiftType); //LiftTypeAction

            // Gump
            Interpreter.RegisterCommandHandler("waitforgump", WaitForGump); // WaitForGumpAction
            Interpreter.RegisterCommandHandler("gumpresponse", GumpResponse); // GumpResponseAction
            Interpreter.RegisterCommandHandler("gumpclose", GumpClose); // GumpResponseAction

            // Menu
            Interpreter.RegisterCommandHandler("menu", ContextMenu); //ContextMenuAction
            Interpreter.RegisterCommandHandler("menuresponse", MenuResponse); //MenuResponseAction
            Interpreter.RegisterCommandHandler("waitformenu", WaitForMenu); //WaitForMenuAction

            // Prompt
            Interpreter.RegisterCommandHandler("promptresponse", PromptResponse); //PromptAction
            Interpreter.RegisterCommandHandler("waitforprompt", WaitForPrompt); //WaitForPromptAction

            // Hotkey execution
            Interpreter.RegisterCommandHandler("hotkey", Hotkey); //HotKeyAction

            // Messages
            Interpreter.RegisterCommandHandler("say", Msg); //SpeechAction
            Interpreter.RegisterCommandHandler("msg", Msg); //SpeechAction
            Interpreter.RegisterCommandHandler("overhead", HeadMsg); //OverheadMessageAction
            Interpreter.RegisterCommandHandler("sysmsg", SysMsg); //SystemMessageAction

            // General Waits/Pauses
            Interpreter.RegisterCommandHandler("wait", Pause); //PauseAction
            Interpreter.RegisterCommandHandler("pause", Pause); //PauseAction
            Interpreter.RegisterCommandHandler("waitforstat", DummyCommand); //WaitForStatAction

            // Misc
            Interpreter.RegisterCommandHandler("setability", SetAbility); //SetAbilityAction
            Interpreter.RegisterCommandHandler("setlasttarget", DummyCommand); //SetLastTargetAction
            Interpreter.RegisterCommandHandler("lasttarget", LastTarget); //LastTargetAction
            Interpreter.RegisterCommandHandler("setvar", DummyCommand); //SetMacroVariableTargetAction
            Interpreter.RegisterCommandHandler("skill", UseSkill); //SkillAction
            Interpreter.RegisterCommandHandler("useskill", UseSkill); //SkillAction
            Interpreter.RegisterCommandHandler("walk", Walk); //Move/WalkAction
        }

        private static bool DummyCommand(string command, Argument[] args, bool quiet, bool force)
        {
            ScriptManager.Error($"Unimplemented command: {command}");

            return true;
        }

        private static bool Target(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: target (serial) OR (x) (y) (z)");
                return true;
            }

            if (args.Length == 1)
            {
                Serial serial = args[0].AsSerial();

                if (serial != Serial.Zero) // Target a specific item or mobile
                {
                    Item item = World.FindItem(serial);

                    if (item != null)
                    {
                        Targeting.Target(item);
                        return true;
                    }

                    Mobile mobile = World.FindMobile(serial);

                    if (mobile != null)
                    {
                        Targeting.Target(mobile);
                    }
                }
            }
            else if (args.Length == 3) // target ground at specific x/y/z
            {
                Targeting.Target(new TargetInfo
                {
                    Type = 1,
                    Flags = 0,
                    Serial = Serial.Zero,
                    X = args[0].AsInt(),
                    Y = args[1].AsInt(),
                    Z = args[2].AsInt(),
                    Gfx = 0
                });

            }

            return true;
        }

        private static bool TargetType(string command, Argument[] args, bool quiet, bool force)
        {
            if (Targeting.FromGrabHotKey)
                return false;

            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: targettype (graphic) [isMobile]");
                return true;
            }
            
            ushort gfx = Utility.ToUInt16(args[0].AsString(), 0);
            bool.TryParse(args[1].AsString(), out var isMobile);

            ArrayList list = new ArrayList();
            if (isMobile)
            {
                foreach (Mobile find in World.MobilesInRange())
                {
                    if (find.Body == gfx)
                    {
                        list.Add(find);
                    }
                }
            }
            else
            {
                foreach (Item i in World.Items.Values)
                {
                    if (i.ItemID == gfx && !i.IsInBank)
                    {
                        list.Add(i);
                    }
                }
            }

            if (list.Count > 0)
            {
                Targeting.Target(list[Utility.Random(list.Count)]);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.NoItemOfType,
                    isMobile ? $"Character [{gfx}]" : ((ItemID) gfx).ToString());
            }

            return true;
        }

        private static bool TargetRelLoc(string command, Argument[] args, bool quiet, bool force)
        {
            if (Targeting.FromGrabHotKey)
                return false;

            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: targetrelloc (x-offset) (y-offset)");
                return true;
            }

            int xoffset = Utility.ToInt32(args[0].AsString(), 0);
            int yoffset = Utility.ToInt32(args[1].AsString(), 0);

            ushort x = (ushort) (World.Player.Position.X + xoffset);
            ushort y = (ushort) (World.Player.Position.Y + yoffset);
            short z = (short) World.Player.Position.Z;

            try
            {
                Ultima.HuedTile tile = Map.GetTileNear(World.Player.Map, x, y, z);
                Targeting.Target(new Point3D(x, y, tile.Z), tile.ID);
            }
            catch (Exception e)
            {
                ScriptManager.Error($"Error Executing TargetRelLoc: {e.Message}");
            }

            return true;
        }

        private static bool WaitForTarget(string command, Argument[] args, bool quiet, bool force)
        {
            switch (args.Length)
            {
                case 0:
                    return Targeting.HasTarget || ScriptManager.Pause(); // use default timeout
                case 1:
                    return Targeting.HasTarget || ScriptManager.Pause(args[0].AsInt()); // user provided timeout
                default:
                    return Targeting.HasTarget || ScriptManager.Pause(); // use default just in case
            }
        }

        private static bool Hotkey(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: hotkey ('name of hotkey') OR (hotkeyId)");
                return true;
            }

            string query = args[0].AsString();

            KeyData hk = HotKey.GetByNameOrId(query);

            if (hk == null)
            {
                ScriptManager.Error($"Hotkey '{query}' not found");
                return true;
            }

            hk.Callback();

            return true;
        }

        private static bool WaitForGump(string command, Argument[] args, bool quiet, bool force)
        {
            uint gumpId = 0;
            bool strict = false;

            // Look for a specific gump
            if (args.Length == 1)
            {
                gumpId = Utility.ToUInt32(args[0].AsString(), 0);

                if (gumpId > 0)
                    strict = true;
            }

            return !((World.Player.HasGump || World.Player.HasCompressedGump) &&
                     (World.Player.CurrentGumpI == gumpId || !strict || gumpId == 0));
        }

        private static bool WaitForMenu(string command, Argument[] args, bool quiet, bool force)
        {
            uint menuId = 0;

            // Look for a specific menu
            if (args.Length == 1)
            {
                menuId = Utility.ToUInt32(args[0].AsString(), 0);
            }

            return !(World.Player.HasMenu && (World.Player.CurrentGumpI == menuId || menuId == 0));
        }

        private static bool WaitForPrompt(string command, Argument[] args, bool quiet, bool force)
        {
            uint promptId = 0;
            bool strict = false;

            // Look for a specific gump
            if (args.Length == 1)
            {
                promptId = Utility.ToUInt32(args[0].AsString(), 0);

                if (promptId > 0)
                    strict = true;
            }

            return !(World.Player.HasPrompt && (World.Player.PromptID == promptId || !strict || promptId == 0));
        }

        private static string[] abilities = new string[4] {"primary", "secondary", "stun", "disarm"};

        private static bool SetAbility(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1 || !abilities.Contains(args[0].AsString()))
            {
                ScriptManager.Error("Usage: setability ('primary'/'secondary'/'stun'/'disarm') ['on'/'off']");
                return true;
            }

            if (args.Length == 2 && args[1].AsString() == "on" || args.Length == 1)
            {
                switch (args[0].AsString())
                {
                    case "primary":
                        SpecialMoves.SetPrimaryAbility();
                        break;
                    case "secondary":
                        SpecialMoves.SetSecondaryAbility();
                        break;
                    case "stun":
                        Client.Instance.SendToServer(new StunRequest());
                        break;
                    case "disarm":
                        Client.Instance.SendToServer(new DisarmRequest());
                        break;
                    default:
                        break;
                }
            }
            else if (args.Length == 2 && args[1].AsString() == "off")
            {
                Client.Instance.SendToServer(new UseAbility(AOSAbility.Clear));
                Client.Instance.SendToClient(ClearAbility.Instance);
            }

            return true;
        }

        private static string[] hands = new string[3] {"left", "right", "both"};

        private static bool ClearHands(string command, Argument[] args, bool quiet, bool force)
        {
            // expect one STRING node

            if (args.Length == 0 || !hands.Contains(args[0].AsString()))
            {
                ScriptManager.Error("Usage: clearhands ('left'/'right'/'both')");
                return true;
            }

            switch (args[0].AsString())
            {
                case "left":
                    Dress.Unequip(Layer.LeftHand);
                    break;
                case "right":
                    Dress.Unequip(Layer.RightHand);
                    break;
                default:
                    Dress.Unequip(Layer.LeftHand);
                    Dress.Unequip(Layer.RightHand);
                    break;
            }

            return true;
        }

        private static bool UseType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: dclicktype|usetype ('name of item') OR (graphicID)");
                return true;
            }

            string gfxStr = args[0].AsString();
            ushort gfx = Utility.ToUInt16(gfxStr, 0);

            Serial click = Serial.Zero;
            bool isItem = false;
            Item item;

            // No graphic id, maybe searching by name?
            if (gfx == 0)
            {
                item = World.FindItemByName(gfxStr);

                if (item == null)
                {
                    ScriptManager.Error($"Script Error: Couldn't find '{gfxStr}'");
                    return true;
                }
            }
            else // Check backpack first
            {
                item = World.Player.Backpack != null ? World.Player.Backpack.FindItemByID(gfx) : null;
            }

            // Not in backpack? Lets check the world
            if (item == null)
            {
                List<Item> list = new List<Item>();
                foreach (Item i in World.Items.Values)
                {
                    if (i.ItemID == gfx && i.RootContainer == null)
                    {
                        isItem = true;

                        list.Add(i);
                    }
                }

                if (list.Count == 0)
                {
                    foreach (Item i in World.Items.Values)
                    {
                        if (i.ItemID == gfx && !i.IsInBank)
                        {
                            isItem = true;

                            list.Add(i);
                        }
                    }
                }

                if (list.Count > 0)
                    click = list[Utility.Random(list.Count)].Serial;
            }
            else
            {
                isItem = true;
                click = item.Serial;
            }

            // Still no item? Mobile check!
            if (item == null)
            {
                List<Mobile> list = new List<Mobile>();
                foreach (Mobile m in World.MobilesInRange())
                {
                    if (m.Body == gfx)
                    {
                        list.Add(m);
                    }
                }

                if (list.Count > 0)
                    click = list[Utility.Random(list.Count)].Serial;
            }

            if (click != Serial.Zero)
                PlayerData.DoubleClick(click);
            else
                World.Player.SendMessage(MsgLevel.Force, LocString.NoItemOfType,
                    isItem ? ((ItemID) gfx).ToString() : $"(Character) 0x{gfx:X}");

            return true;
        }

        private static bool UseObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: useobject (serial)");
                return true;
            }

            Serial serial = args[0].AsSerial();

            if (!serial.IsValid)
            {
                ScriptManager.Error("useobject - invalid serial");
                return true;
            }
            
            PlayerData.DoubleClick(serial);

            return true;
        }
        
        private static bool DropItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: drop (serial) (x y z/layername)");
                return true;
            }

            Serial serial = args[0].AsSerial();
            Point3D to = new Point3D(0, 0, 0);
            Layer layer = Layer.Invalid;

            switch (args.Length)
            {
                case 1: // drop at feet
                    to = new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z);
                    break;
                case 2: // dropping on a layer
                    layer = (Layer) Enum.Parse(typeof(Layer), args[1].AsString(), true);
                    break;
                
                default: // dropping at x/y/z
                    to = new Point3D(Utility.ToInt32(args[1].AsString(), 0), Utility.ToInt32(args[2].AsString(), 0),
                        Utility.ToInt32(args[3].AsString(), 0));
                    break;
            }

            if (DragDropManager.Holding != null)
            {
                if (layer > Layer.Invalid && layer <= Layer.LastUserValid)
                {
                    Mobile m = World.FindMobile(serial);
                    if (m != null)
                        DragDropManager.Drop(DragDropManager.Holding, m, layer);
                }
                else
                {
                    DragDropManager.Drop(DragDropManager.Holding, serial, to);
                }
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.MacroNoHold);
            }

            return true;
        }

        private static bool DropRelLoc(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 3)
            {
                ScriptManager.Error("Usage: droprelloc (x) (y) (z)");
                return true;
            }

            int x = args[0].AsInt();
            int y = args[1].AsInt();
            int z = args[2].AsInt();

            if (DragDropManager.Holding != null)
                DragDropManager.Drop(DragDropManager.Holding, null,
                    new Point3D((ushort)(World.Player.Position.X + x),
                        (ushort)(World.Player.Position.Y + y), (short)(World.Player.Position.Z + z)));
            else
                World.Player.SendMessage(LocString.MacroNoHold);

            return true;
        }

        private static bool LiftItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: lift (serial) [amount]");
                return true;
            }

            Serial serial = args[0].AsSerial();

            if (!serial.IsValid)
            {
                ScriptManager.Error("lift - invalid serial");
                return true;
            }

            ushort amount = Utility.ToUInt16(args[1].AsString(), 1);

            Item item = World.FindItem(serial);
            if (item != null)
            {
                DragDropManager.Drag(item, amount <= item.Amount ? amount : item.Amount);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.MacroItemOutRange);
            }

            return true;
        }

        private static bool LiftType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: lifttype (gfx/'name of item') [amount]");
                return true;
            }

            string gfxStr = args[0].AsString();
            ushort gfx = Utility.ToUInt16(gfxStr, 0);
            ushort amount = Utility.ToUInt16(args[1].AsString(), 1);

            Item item;

            // No graphic id, maybe searching by name?
            if (gfx == 0)
            {
                item = World.Player.Backpack != null ? World.Player.Backpack.FindItemByName(gfxStr, true) : null;

                if (item == null)
                {
                    ScriptManager.Error($"Script Error: Couldn't find '{gfxStr}'");
                    return true;
                }
            }
            else
            {
                item = World.Player.Backpack != null ? World.Player.Backpack.FindItemByID(gfx) : null;
            }

            if (item != null)
            {
                if (item.Amount < amount)
                    amount = item.Amount;

                DragDropManager.Drag(item, amount);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.NoItemOfType, (ItemID)gfx);
            }

            return true;
        }

        private static bool Walk(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: walk (direction)");
                return true;
            }
            
            Direction dir = (Direction)Enum.Parse(typeof(Direction), args[0].AsString(), true);

            Client.Instance.RequestMove(dir);

            return true;
        }

        private static Dictionary<string, int> UsableSkills = new Dictionary<string, int>()
        {
            {"anatomy", 1}, // anatomy
            {"animallore", 2}, // animal lore
            {"itemidentification", 3}, // item identification
            {"itemid", 3}, // item identification
            {"armslore", 4}, // arms lore
            {"begging", 6}, // begging
            {"peacemaking", 9}, // peacemaking
            {"peace", 9}, // peacemaking
            {"cartography", 12}, // cartography
            {"detectinghidden", 14}, // detect hidden
            {"discord", 15}, // Discordance
            {"discordance", 15}, // Discordance
            {"evaluatingintelligence", 16}, // evaluate intelligence
            {"evalint", 16}, // evaluate intelligence
            {"forensicevaluation", 19}, // forensic evaluation
            {"forensiceval", 19}, // forensic evaluation
            {"hiding", 21}, // hiding
            {"provocation", 22}, // provocation
            {"provo", 22}, // provocation
            {"inscription", 23}, // inscription
            {"poisoning", 30}, // poisoning
            {"spiritspeak", 32}, // spirit speak
            {"stealing", 33}, // stealing
            {"taming", 35}, // taming
            {"tasteidentification", 36}, // taste id
            {"tasteid", 36}, // taste id
            {"tracking", 38}, // tracking
            {"meditation", 46}, // Meditation
            {"stealth", 47}, // Stealth
            {"removetrap", 48} // RemoveTrap
        };

        private static bool UseSkill(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: useskill ('skill name'/'last')");
                return true;
            }

            if (args[0].AsString() == "last")
                Client.Instance.SendToServer(new UseSkill(World.Player.LastSkill));
            else if (UsableSkills.TryGetValue(args[0].AsString(), out int skillId))
                Client.Instance.SendToServer(new UseSkill(skillId));

            return true;
        }

        private static bool SetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Error("Usage: setalias ('name') [serial]");
                return true;
            }

            Interpreter.SetAlias(args[0].AsString(), args[1].AsSerial());

            return true;
        }

        private static bool UnsetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: unsetalias (string)");
                return true;
            }

            Interpreter.SetAlias(args[0].AsString(), 0);

            return true;
        }

        public static bool EquipItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: equipitem (serial) (layer)");
                return true;
            }

            Item equip = World.FindItem(args[0].AsSerial());
            byte layer = (byte) Utility.ToInt32(args[1].AsString(), 0);
            if (equip != null && (Layer) layer != Layer.Invalid)
                Dress.Equip(equip, (Layer) layer);

            return true;
        }

        private static bool Pause(string command, Argument[] args, bool quiet, bool force)
        {
            // If we return false, the script engine will run this command again
            return args.Length < 1 ? !ScriptManager.Pause() : !ScriptManager.Pause(args[0].AsInt());
        }

        public static bool Msg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: msg ('text') [color]");
                return true;
            }
            
            if (args.Length == 1)
                World.Player.Say(Config.GetInt("SysColor"), args[0].AsString());
            else
                World.Player.Say(Utility.ToInt32(args[1].AsString(), 0), args[0].AsString());

            return true;
        }

        public static bool Cast(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: cast 'name of spell'");
                return true;
            }

            Spell spell = int.TryParse(args[0].AsString(), out int spellnum) ? Spell.Get(spellnum) : Spell.GetByName(args[0].AsString());

            if (spell != null)
            {
                spell.OnCast(new CastSpellFromMacro((ushort) spell.GetID()));
            }
            else if (!quiet)
            {
                ScriptManager.Error("cast - spell name or number not valid");
            }

            return true;
        }

        public static bool HeadMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: headmsg ('text') [color] [serial]");
                return true;
            }

            if (!Client.Instance.ClientRunning)
                return true;

            if (args.Length == 1)
                World.Player.OverheadMessage(Config.GetInt("SysColor"), args[0].AsString());
            else
            {
                int hue = Utility.ToInt32(args[1].AsString(), 0);

                if (args.Length == 3)
                {
                    uint serial = args[2].AsSerial();
                    Mobile m = World.FindMobile(serial);
                    m?.OverheadMessage(hue, args[0].AsString());
                }
                else
                    World.Player.OverheadMessage(hue, args[0].AsString());
            }

            return true;
        }

        public static bool SysMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: sysmsg ('text') [color]");
                return true;
            }
            
            if (args.Length == 1)
                World.Player.SendMessage(Config.GetInt("SysColor"), args[0].AsString());
            else if (args.Length == 2)
                World.Player.SendMessage(Utility.ToInt32(args[1].AsString(), 0), args[0].AsString());

            return true;
        }

        public static bool DressCommand(string command, Argument[] args, bool quiet, bool force)
        {
            //we're using a named dresslist or a temporary dresslist?
            if (args.Length == 0)
            {
                if (DressList._Temporary != null)
                    DressList._Temporary.Dress();
                else if (!quiet)
                    ScriptManager.Error(
                        "No dresslist specified and no temporary dressconfig present - usage: dress ['dresslist']");
            }
            else
            {
                var d = DressList.Find(args[0].AsString());
                if (d != null)
                    d.Dress();
                else if (!quiet)
                    ScriptManager.Error($"dresslist {args[0].AsString()} not found");
            }

            return true;
        }

        public static bool UnDressCommand(string command, Argument[] args, bool quiet, bool force)
        {
            //we're using a named dresslist or a temporary dresslist?
            if (args.Length == 0)
            {
                if (DressList._Temporary != null)
                    DressList._Temporary.Undress();
                else if (!quiet)
                    ScriptManager.Error(
                        "No dresslist specified and no temporary dressconfig present - usage: undress ['dresslist']");
            }
            else
            {
                var d = DressList.Find(args[0].AsString());
                if (d != null)
                    d.Undress();
                else if (!quiet)
                    ScriptManager.Error($"dresslist {args[0].AsString()} not found");
            }

            return true;
        }

        public static bool DressConfig(string command, Argument[] args, bool quiet, bool force)
        {
            if (DressList._Temporary == null)
                DressList._Temporary = new DressList("dressconfig");

            DressList._Temporary.Items.Clear();
            for (int i = 0; i < World.Player.Contains.Count; i++)
            {
                Item item = World.Player.Contains[i];
                if (item.Layer <= Layer.LastUserValid && item.Layer != Layer.Backpack && item.Layer != Layer.Hair &&
                    item.Layer != Layer.FacialHair)
                    DressList._Temporary.Items.Add(item.Serial);
            }

            return true;
        }
        
        public static bool GumpResponse(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: gumpresponse (buttondId)");
                //ScriptManager.Error("Usage: gumpresponse (buttondId) [option] ['text1'|fieldId] ['text2'|fieldId]");
                return true;
            }

            int buttonId = args[0].AsInt();

            /*private int m_ButtonID;
                    private int[] m_Switches;
                    private GumpTextEntry[] m_TextEntries;*/

            //Assistant.Macros.GumpResponseAction|9|0|0
            //Assistant.Macros.GumpResponseAction|1|0|1|0&Hello How are you?
            //Assistant.Macros.GumpResponseAction|501|0|2|1&box2|0&box1

            Client.Instance.SendToClient(new CloseGump(World.Player.CurrentGumpI));
            Client.Instance.SendToServer(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, buttonId, new int[] {}, new GumpTextEntry[] { }));

            World.Player.HasGump = false;
            World.Player.HasCompressedGump = false;

            return true;
        }

        public static bool GumpClose(string command, Argument[] args, bool quiet, bool force)
        {
            Client.Instance.SendToClient(new CloseGump(World.Player.CurrentGumpI));
            Client.Instance.SendToServer(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, 0, new int[] { }, new GumpTextEntry[] { }));

            World.Player.HasGump = false;
            World.Player.HasCompressedGump = false;

            return true;
        }
        public static bool ContextMenu(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: menu (serial) (index)");
                return true;
            }

            Serial s = args[0].AsSerial();
            ushort index = args[0].AsUShort();

            if (s == Serial.Zero && World.Player != null)
                s = World.Player.Serial;

            Client.Instance.SendToServer(new ContextMenuRequest(s));
            Client.Instance.SendToServer(new ContextMenuResponse(s, index));
            return true;
        }
        
        public static bool MenuResponse(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: menuresponse (index) (menuId) [hue]");
                return true;
            }

            ushort index = args[0].AsUShort();
            ushort menuId = args[1].AsUShort();
            ushort hue = 0;

            if (args.Length == 3)
                hue = args[2].AsUShort();

            Client.Instance.SendToServer(new MenuResponse(World.Player.CurrentMenuS, World.Player.CurrentMenuI, index,
                menuId, hue));
            World.Player.HasMenu = false;
            return true;
        }

        public static bool PromptResponse(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: promptresponse ('response to the prompt')");
                return true;
            }

            World.Player.ResponsePrompt(args[0].AsString());
            return true;
        }
        
        public static bool LastTarget(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: lasttarget");
                return true;
            }

            if (!Targeting.DoLastTarget())
                Targeting.ResendTarget();
            
            return true;
        }
    }
}