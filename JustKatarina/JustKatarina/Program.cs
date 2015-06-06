﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;

namespace JustKatarina
{
    internal class Program
    {
        public const string ChampName = "Katarina";
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        private static bool InUlt = false;
        private static SpellSlot Ignite;
        //private static GameObject _ward;
        //private static int lastWardCast;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (player.ChampionName != ChampName)
                return;

            Notifications.AddNotification("JustKatarina - [V.1.0.0.0]", 8000);

            //Ability Information - Range - Variables.
            Q = new Spell(SpellSlot.Q, 675f);
            W = new Spell(SpellSlot.W, 375f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 550f);

            Config = new Menu(player.ChampionName, player.ChampionName, true);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));

            //Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassmode", "Harass Mode").SetValue(
                new StringList(new[] {"Q-E-W", "E-Q-W"}, 1)));
            Config.SubMenu("Harass").AddItem(new MenuItem("hQ", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("hW", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("hE", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("AutoHarass", "Auto Harass", true).SetValue(new KeyBind("J".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Harass").AddItem(new MenuItem("aQ", "Use Q for Auto Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("aW", "Use W for Auto Harass").SetValue(true));

            //Item
            Config.AddSubMenu(new Menu("Item", "Item"));
            Config.SubMenu("Item").AddItem(new MenuItem("useGhostblade", "Use Youmuu's Ghostblade").SetValue(true));
            Config.SubMenu("Item").AddItem(new MenuItem("UseBOTRK", "Use Blade of the Ruined King").SetValue(true));
            Config.SubMenu("Item").AddItem(new MenuItem("eL", "  Enemy HP Percentage").SetValue(new Slider(80, 0, 100)));
            Config.SubMenu("Item").AddItem(new MenuItem("oL", "  Own HP Percentage").SetValue(new Slider(65, 0, 100)));
            Config.SubMenu("Item").AddItem(new MenuItem("UseBilge", "Use Bilgewater Cutlass").SetValue(true));
            Config.SubMenu("Item")
                .AddItem(new MenuItem("HLe", "  Enemy HP Percentage").SetValue(new Slider(80, 0, 100)));
            Config.SubMenu("Item").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));

            //Laneclear
            Config.AddSubMenu(new Menu("Clear", "Clear"));
            Config.SubMenu("Clear").AddItem(new MenuItem("lQ", "Use Q").SetValue(true));
            Config.SubMenu("Clear").AddItem(new MenuItem("lW", "Use W").SetValue(true));
            Config.SubMenu("Clear").AddItem(new MenuItem("lE", "Use E").SetValue(true));

            //Draw
            Config.AddSubMenu(new Menu("Draw", "Draw"));
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw_Disabled", "Disable All Spell Drawings").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Wdraw", "Draw W Range").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Edraw", "Draw E Range").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(true));

            //Misc
            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("KsQ", "Killsteal with Q").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("KsW", "Killsteal with W").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("KsE", "Killsteal with E").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("autokill", "Enable E hop for Killsteal (to minion, ally, target)").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("combodamage", "Damage Indicator").SetValue(true));

            Config.AddToMainMenu();
            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            LeagueSharp.Obj_AI_Base.OnPlayAnimation += PlayAnimation;
        }
        
        private static float GetComboDamage(Obj_AI_Hero Target)
        {
            if (Target != null)
            {
                float ComboDamage = new float();

                ComboDamage += Q.IsReady() ? Q.GetDamage(Target) : 0;
                ComboDamage += W.IsReady() ? W.GetDamage(Target) : 0;
                ComboDamage += E.IsReady() ? E.GetDamage(Target) : 0;
                ComboDamage += R.IsReady() ? R.GetDamage(Target) : 0;
                ComboDamage += Ignite.IsReady() ? IgniteDamage(Target) : 0;
                ComboDamage += player.TotalAttackDamage;
                return ComboDamage;
            }
            return 0;
        }

        private static void PlayAnimation(GameObject sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation == "Spell4")
                {
                    InUlt = true;
                }
                else if (args.Animation == "Run" || args.Animation == "Idle1" || args.Animation == "Attack2" ||
                         args.Animation == "Attack1")
                {
                    InUlt = false;
                }
            }
        }

        private static float[] GetLength()
        {
            var Target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (Target != null)
            {
                float[] Length =
                {
                    GetComboDamage(Target) > Target.Health
                        ? 0
                        : (Target.Health - GetComboDamage(Target))/Target.MaxHealth,
                    Target.Health/Target.MaxHealth
                };
                return Length;
            }
            return new float[] {0, 0};
        }

        private static void combo()
        {
            var Target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (Target == null || !Target.IsValidTarget() || !InUlt)
                return;

            if (Q.IsReady() && Config.Item("UseQ").GetValue<bool>() && Target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(Target);
                }
            if (E.IsReady() && Target.IsValidTarget(E.Range) && Config.Item("UseE").GetValue<bool>())
                {
                    E.CastOnUnit(Target);
                }
            if (W.IsReady() && Target.IsValidTarget(W.Range) && Config.Item("UseW").GetValue<bool>())
                {
                    W.Cast();
                }

            if (R.IsReady() && !InUlt && !E.IsReady() && Target.IsValidTarget(R.Range) && Config.Item("UseR").GetValue<bool>())
            {
                Orbwalker.SetAttack(false);
                Orbwalker.SetMovement(false);
                R.Cast();
                InUlt = true;
                return;
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                items();
        }

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return 0f;
            return (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static void Killsteal()
        {
            if (Config.Item("ksQ").GetValue<bool>() && Q.IsReady())
            {
                var target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(
                            enemy =>
                                enemy.IsValidTarget(Q.Range) && enemy.Health < player.GetSpellDamage(enemy, SpellSlot.Q));
                if (target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(target);
                }
            }

            if (Config.Item("ksW").GetValue<bool>() && W.IsReady())
            {
                var target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(
                            enemy =>
                                enemy.IsValidTarget(W.Range) && enemy.Health < player.GetSpellDamage(enemy, SpellSlot.W));
                if (target.IsValidTarget(W.Range))
                {
                    W.Cast();
                }
            }

            if (Config.Item("ksE").GetValue<bool>() && E.IsReady())
            {
                var target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(
                            enemy =>
                                enemy.IsValidTarget(E.Range) && enemy.Health < player.GetSpellDamage(enemy, SpellSlot.E));
                if (target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }
            }

            if (Config.Item("autokill").GetValue<bool>())
            {
                foreach (Obj_AI_Base target in ObjectManager.Get<Obj_AI_Base>().Where(
                    target =>
                        ObjectManager.Player.Distance(target.ServerPosition) <= E.Range
                        && !target.IsMe
                        && target.IsTargetable
                        && !target.IsInvulnerable
                    ))
                {
                    foreach (Obj_AI_Hero focus in ObjectManager.Get<Obj_AI_Hero>().Where(
                        focus =>
                            focus.Distance(target.ServerPosition) <= Q.Range
                            && focus.IsEnemy
                            && !focus.IsMe
                            && !focus.IsInvulnerable
                            && focus.IsValidTarget()
                        ))
                    {
                        var Qdmg = Q.GetDamage(focus);
                        var Wdmg = W.GetDamage(focus);
                        var MarkDmg = Damage.CalcDamage(player, focus, Damage.DamageType.Magical,
                            player.FlatMagicDamageMod*0.15 + player.Level*15);
                        float Ignitedmg;
                        if (Ignite != SpellSlot.Unknown)
                        {
                            Ignitedmg =
                                (float) Damage.GetSummonerSpellDamage(player, focus, Damage.SummonerSpell.Ignite);
                        }
                        else
                        {
                            Ignitedmg = 0f;
                        }
                        
                        if (Config.Item("ksQ").GetValue<bool>() && focus.Health - Qdmg < 0 && E.IsReady() && Q.IsReady() &&
                            focus.Distance(target.ServerPosition) <= Q.Range)
                        {
                            E.Cast(target);
                            Q.Cast(focus);
                        }
                       
                        if (Config.Item("ksQ").GetValue<bool>() && Config.Item("ksW").GetValue<bool>() &&
                            focus.Distance(target.ServerPosition) <= W.Range && focus.Health - Qdmg - Wdmg < 0 &&
                            E.IsReady() && Q.IsReady())
                        {
                            E.Cast(target);
                            Q.Cast(focus);
                            W.Cast();
                        }
                        
                        if (Config.Item("ksQ").GetValue<bool>() && focus.Distance(target.ServerPosition) <= E.Range &&
                            focus.Health - Qdmg - Ignitedmg < 0 && E.IsReady() && Q.IsReady() && Ignite.IsReady())
                        {
                            E.Cast(target);
                            Q.Cast(focus);
                            player.Spellbook.CastSpell(Ignite, focus);
                        }
                        
                        if (Config.Item("ksQ").GetValue<bool>() && Config.Item("ksW").GetValue<bool>() &&
                            focus.Distance(target.ServerPosition) <= W.Range && focus.Health - Qdmg - Wdmg - MarkDmg < 0 &&
                            E.IsReady() && Q.IsReady() && W.IsReady())
                        {
                            E.Cast(target);
                            Q.Cast(focus);
                        }
                        
                        if (Config.Item("ksW").GetValue<bool>() && Config.Item("ksQ").GetValue<bool>() &&
                            focus.Distance(target.ServerPosition) <= W.Range &&
                            focus.Health - Qdmg - Wdmg - Ignitedmg < 0 && E.IsReady() && Q.IsReady() && W.IsReady() &&
                            Ignite.IsReady())
                        {
                            E.Cast(target);
                            Q.Cast(focus);
                            W.Cast();
                            player.Spellbook.CastSpell(Ignite, focus);
                            return;
                        }
                       
                        if (Config.Item("ksQ").GetValue<bool>() && focus.Distance(target.ServerPosition) <= W.Range &&
                            focus.Health - Qdmg - Wdmg - MarkDmg - Ignitedmg < 0 && E.IsReady() && Q.IsReady() &&
                            W.IsReady())
                        {
                            E.Cast(target);
                            Q.Cast(focus);
                            player.Spellbook.CastSpell(Ignite, focus);
                            return;
                        }
                    }
                }
            }
        }

        private static void items()
        {
            Ignite = player.GetSpellSlot("summonerdot");
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValidTarget())
                return;

            var botrk = ItemData.Blade_of_the_Ruined_King.GetItem();
            var Ghost = ItemData.Youmuus_Ghostblade.GetItem();
            var cutlass = ItemData.Bilgewater_Cutlass.GetItem();

            if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
                && target.HealthPercent <= Config.Item("eL").GetValue<Slider>().Value
                && Config.Item("UseBOTRK").GetValue<bool>())

                botrk.Cast(target);

            if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
                && target.HealthPercent <= Config.Item("oL").GetValue<Slider>().Value
                && Config.Item("UseBOTRK").GetValue<bool>())

                botrk.Cast(target);

            if (cutlass.IsReady() && cutlass.IsOwned(player) && cutlass.IsInRange(target) &&
                target.HealthPercent <= Config.Item("HLe").GetValue<Slider>().Value
                && Config.Item("UseBilge").GetValue<bool>())

                cutlass.Cast(target);

            if (Ghost.IsReady() && Ghost.IsOwned(player) && target.IsValidTarget(E.Range)
                && Config.Item("useGhostblade").GetValue<bool>())

                Ghost.Cast();

            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) >= target.Health &&
                Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (player.IsDead || MenuGUI.IsChatOpen || player.IsRecalling())
            {
                return;
            }

            if (InUlt)
            {
                Orbwalker.SetAttack(false);
                Orbwalker.SetMovement(false);
                return;
            }

            Orbwalker.SetAttack(true);
            Orbwalker.SetMovement(true);

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    harass();
                   break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Clear();
                    break;
            }
            
            Killsteal();
            var autoHarass = Config.Item("AutoHarass", true).GetValue<KeyBind>().Active;
            if (autoHarass)
                AutoHarass();
        }

        private static void AutoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValidTarget())
                return;

            if (Q.IsReady() && Config.Item("aQ").GetValue<bool>() && target.IsValidTarget(Q.Range))
                Q.CastOnUnit(target);

            if (W.IsReady() && Config.Item("aW").GetValue<bool>() && target.IsValidTarget(W.Range))
                W.Cast();
        }

        private static void harass()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            int mode = Config.Item("harassmode", true).GetValue<StringList>().SelectedIndex;
            if (target == null || !target.IsValidTarget())
                return;

            if (mode == 0)
            {
                if (Q.IsReady() && Config.Item("hQ").GetValue<bool>() && target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(target);
                }
                if (E.IsReady() && Config.Item("hE").GetValue<bool>() && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }
                if (W.IsReady() && Config.Item("hW").GetValue<bool>() && target.IsValidTarget(W.Range))
                {
                    W.Cast();
                }
            }

            else if (mode == 1)
            {
                if (E.IsReady() && Config.Item("hE").GetValue<bool>() && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }

                if (Q.IsReady() && Config.Item("hQ").GetValue<bool>() && target.IsValidTarget(player.AttackRange))
                {
                    Q.CastOnUnit(target);
                }

                if (W.IsReady() && Config.Item("hW").GetValue<bool>() && target.IsValidTarget(W.Range))
                {
                    W.Cast();
                }
            }
        }

        private static void Clear()
        {
            var minionCount = MinionManager.GetMinions(player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            {
                foreach (var minion in minionCount)
                {
                    if (Config.Item("lQ").GetValue<bool>()
                        && Q.IsReady()
                        && minion.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(minion);
                    }

                    if (Config.Item("lW").GetValue<bool>()
                        && W.IsReady()
                        && minion.IsValidTarget(W.Range)
                        )
                    {
                        W.Cast();
                    }

                    if (Config.Item("lE").GetValue<bool>()
                        && E.IsReady()
                        && minion.IsValidTarget(E.Range)
                        )
                    {
                        E.CastOnUnit(minion);
                    }

                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            var Target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (Config.Item("Qdraw").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, Q.Range, System.Drawing.Color.White, 3);
            if (Config.Item("Wdraw").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, W.Range, System.Drawing.Color.White, 3);
            if (Config.Item("Edraw").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, E.Range, System.Drawing.Color.White, 3);
            if (Config.Item("Rdraw").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, R.Range, System.Drawing.Color.White, 3);
            if (Config.Item("combodamage").GetValue<bool>() && E.IsInRange(Target))
            {
                float[] Positions = GetLength();
                Drawing.DrawLine
                    (
                        new Vector2(Target.HPBarPosition.X + 10 + Positions[0]*104, Target.HPBarPosition.Y + 20),
                        new Vector2(Target.HPBarPosition.X + 10 + Positions[1]*104, Target.HPBarPosition.Y + 20),
                        9,
                        Color.Orange
                    );
            }
        }
    }
}


    
