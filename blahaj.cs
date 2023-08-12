using System;
using System.Linq;
using XRL.Rules;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
    /// <summary>
    ///   Has a chance of spawning a Blåhaj
    /// </summary>
    [Serializable]
    public class Books_BlahajSpawner : IPart
    {
        /// <summary>Spawning blueprint</summary>
        private const string Blueprint = "Books_Blahaj";

        /// <summary>Chances of spawning (percentage)</summary>
        public int Chance = 1;

        /// <summary>Chances of revealing when entering the ParentObject's cell (percentage)</summary>
        public int TrampleChance = 50;

        /// <summary>Difficulty of searching roll (INT)</summary>
        public int Search_Difficulty = 14;

        /// <summary>Whether to delete parent</summary>
        public bool DeleteParent = false;

        public override bool SameAs(IPart p) =>
            (p is Books_BlahajSpawner o)
            && o.Chance == Chance
            && o.Search_Difficulty == Search_Difficulty
            && o.DeleteParent == DeleteParent
            && base.SameAs(p);

        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade)
            || ID == ObjectCreatedEvent.ID
            || ID == ObjectEnteredCellEvent.ID;

        public override bool HandleEvent(ObjectCreatedEvent E)
        {
            if (!Chance.in100())
            {
                ParentObject.RemovePart(this);

                if (DeleteParent)
                {
                    ParentObject.Obliterate(Silent: true);
                }
            }

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            Cell currentCell = ParentObject.CurrentCell;

            if (
                E.Object.Blueprint != Blueprint
                && E.Object.IsCombatObject()
                && currentCell != null
                && TrampleChance.in100()
            )
            {
                Reveal(AdjacentCell: true);
            }

            return base.HandleEvent(E);
        }


        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "CustomRender");
            Object.RegisterPartEvent(this, "Searched");
            base.Register(Object);
        }



        public override bool FireEvent(Event E)
        {
            if (E.ID == "CustomRender")
            {
                if (
                    E.GetParameter("RenderEvent") is RenderEvent renderEvent
                    && (renderEvent.Lit == LightLevel.Radar
                        || renderEvent.Lit == LightLevel.LitRadar)
                )
                {
                    Reveal();
                }
            }
            else if (E.ID == "Searched")
            {
                GameObject Searcher = E.GetGameObjectParameter("Searcher");

                if (
                    Searcher.CurrentCell != ParentObject.CurrentCell
                    && Stat.MakeSave(Searcher, "Intelligence", Search_Difficulty)
                )
                {
                    Reveal(Searcher);
                }
            }
            return base.FireEvent(E);
        }

        public void Reveal(GameObject who = null, bool AdjacentCell = false)
        {
            who = who ?? The.Player;

            var cell = ParentObject.CurrentCell;

            if (AdjacentCell)
            {
                cell = ParentObject.CurrentCell
                    .YieldAdjacentCells(1)
                    .Where((c) => c.HasWadingDepthLiquid())
                    .GetRandomElement() ?? currentCell;
            }

            GameObject shonk = GameObject.create(Blueprint);
            shonk.MakeActive();
            cell.AddObject(shonk);

            XDidYToZ(
                Actor: who,
                Verb: "spot",
                Object: shonk,
                Extra: who.DescribeDirectionToward(shonk),
                EndMark: "!",
                ColorAsGoodFor: who,
                IndefiniteObject: true,
                UseVisibilityOf: shonk
            );

            if (
                Visible(shonk)
                && AutoAct.IsActive()
                && The.Player.IsRelevantHostile(shonk))
            {
                AutoAct.Interrupt(IndicateObject: shonk);
            }

            ParentObject.RemovePart(this);

            if (DeleteParent)
            {
                ParentObject.Obliterate(Silent: true);
            }
        }
    }

}
