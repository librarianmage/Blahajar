using System;
using System.Linq;
using XRL.Rules;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
    /// <summary>
    ///   Object has a chance of spawning a hidden Blåhaj
    /// </summary>
    /// <remarks>
    ///   Object has a <see cref="SpawnChance"/>% chance of activating upon creation. When active, the spawning can be triggered under three circumstances:
    ///   <list type="number">
    ///     <item>
    ///       <description>The cell is lit with <c>Omniscient</c>, <c>LitRadar</c>, or <c>Radar</c> light.</description>
    ///     </item>
    ///     <item>
    ///       <description>A creature enters the same cell, with <see cref="TrampleRevealChance"/>% chance.</description>
    ///     </item>
    ///     <item>
    ///       <description>The player makes an INT save of difficulty <see cref="SearchDifficulty"/> while searching.</description>
    ///     </item>
    ///   </list>
    ///   When the part fails to activate or successfully spawns, it will remove itself from its parent and optionally obliterate its parent if <see cref="ObliterateParent"/> is set.
    /// </remarks>
    /// <seealso cref="LightLevel"/>
    /// <seealso cref="Physics.Search"/>
    [Serializable]
    public class Books_BlahajSpawner : IPart
    {
        private const string BlahajBlueprint = "Books_Blahaj";

        public int SpawnChance = 1;
        public int TrampleRevealChance = 50;
        public int SearchDifficulty = 14;
        public bool ObliterateParent = false;

        public override bool SameAs(IPart p) =>
            base.SameAs(p)
            && p is Books_BlahajSpawner o
            && o.SpawnChance == SpawnChance
            && o.TrampleRevealChance == TrampleRevealChance
            && o.SearchDifficulty == SearchDifficulty
            && o.ObliterateParent == ObliterateParent;

        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade)
            || ID == ObjectCreatedEvent.ID
            || ID == ObjectEnteredCellEvent.ID;

        public override bool HandleEvent(ObjectCreatedEvent E)
        {
            if (!SpawnChance.in100()) RemoveSelf();

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            var currentCell = ParentObject.CurrentCell;

            if (E.Object.Blueprint != BlahajBlueprint
                && E.Object.IsCombatObject()
                && currentCell != null
                && TrampleRevealChance.in100())
            {
                Reveal(adjacentCell: true);
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
                if (E.GetParameter("RenderEvent") is RenderEvent renderEvent
                    && (renderEvent.Lit == LightLevel.Omniscient
                        || renderEvent.Lit == LightLevel.Radar
                        || renderEvent.Lit == LightLevel.LitRadar))
                {
                    Reveal();
                }
            }
            else if (E.ID == "Searched")
            {
                var searcher = E.GetGameObjectParameter("Searcher");

                if (searcher.CurrentCell != ParentObject.CurrentCell
                    && Stat.MakeSave(searcher, "Intelligence", SearchDifficulty))
                {
                    Reveal(searcher);
                }
            }

            return base.FireEvent(E);
        }

        private void Reveal(GameObject finder = null, bool adjacentCell = false)
        {
            finder = finder ?? The.Player;

            var cell = currentCell;

            if (adjacentCell)
            {
                cell = cell
                    .YieldAdjacentCells(1)
                    .Where((c) => c.HasWadingDepthLiquid())
                    .GetRandomElement() ?? currentCell;
            }

            var shonk = GameObject.create(BlahajBlueprint);
            shonk.MakeActive();
            cell.AddObject(shonk);

            XDidYToZ(
                Actor: finder,
                Verb: "spot",
                Object: shonk,
                Extra: finder.DescribeDirectionToward(shonk),
                EndMark: "!",
                ColorAsGoodFor: finder,
                IndefiniteObject: true,
                UseVisibilityOf: shonk
            );

            if (Visible(shonk)
                && AutoAct.IsActive()
                && The.Player.IsRelevantHostile(shonk))
            {
                AutoAct.Interrupt(IndicateObject: shonk);
            }

            RemoveSelf();
        }

        private void RemoveSelf()
        {
            ParentObject.RemovePart(this);

            if (ObliterateParent) ParentObject.Obliterate(Silent: true);
        }
    }

}
