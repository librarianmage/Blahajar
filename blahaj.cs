using System;
using System.Linq;
using XRL.Rules;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
    [Serializable]
    public class Books_BlahajSpawn : IPart
    {
        private const string Blahaj_Blueprint = "Books_Blahaj";
        private const int Search_Difficulty = 16;

        public override bool SameAs(IPart p) => base.SameAs(p);

        public override void Initialize()
        {
            base.Initialize();
            ParentObject.pRender.CustomRender = true;
            ParentObject.pRender.Visible = false;
        }

        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade) || ID == ObjectEnteredCellEvent.ID;

        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            Cell currentCell = ParentObject.CurrentCell;

            if (E.Object.Blueprint != Blahaj_Blueprint && E.Object.IsCombatObject() && currentCell != null && currentCell.HasWadingDepthLiquid() && 50.in100())
            {
                var cell = currentCell
                    .GetAdjacentCells()
                    .Where((c) => c.HasWadingDepthLiquid())
                    .GetRandomElement();

                GameObject shonk = GameObject.create(Blahaj_Blueprint);
                shonk.MakeActive();
                cell.AddObject(shonk);

                if (IComponent<GameObject>.Visible(shonk))
                {
                    GameObject thePlayer = IComponent<GameObject>.ThePlayer;
                    IComponent<GameObject>.XDidY(thePlayer, "spot", $"a blahaj { thePlayer.DescribeDirectionToward(shonk) }", "!", null, thePlayer);
                }

                ParentObject.Obliterate();
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
                if (E.GetParameter("RenderEvent") is RenderEvent renderEvent && (renderEvent.Lit == LightLevel.Radar || renderEvent.Lit == LightLevel.LitRadar))
                {
                    Reveal();
                }
            }
            else if (E.ID == "Searched")
            {
                GameObject gameObjectParameter = E.GetGameObjectParameter("Searcher");
                if (gameObjectParameter.CurrentCell != ParentObject.CurrentCell
                    && ParentObject.CurrentCell.HasWadingDepthLiquid()
                    && E.GetIntParameter("Bonus") + Stat.Random(1, gameObjectParameter.Stat("Intelligence")) >= Search_Difficulty)
                {
                    Reveal(gameObjectParameter);
                }
            }
            return base.FireEvent(E);
        }

        public void Reveal(GameObject who = null)
        {
            if (who == null)
            {
                who = IComponent<GameObject>.ThePlayer;
            }
            if (who != null)
            {
                IComponent<GameObject>.XDidY(who, "spot", $"a blahaj { who.DescribeDirectionToward(ParentObject) }", "!", null, who);
            }
            GameObject gameObject = GameObject.create(Blahaj_Blueprint);
            gameObject.MakeActive();
            ParentObject.CurrentCell.AddObject(gameObject);
            if (IComponent<GameObject>.Visible(gameObject) && AutoAct.IsActive() && IComponent<GameObject>.ThePlayer.IsRelevantHostile(gameObject))
            {
                AutoAct.Interrupt(null, null, gameObject);
            }
            ParentObject.Destroy();
        }

    }

    [Serializable]
    public class Books_BlahajSpawn_Spawner : IPart
    {
        public override bool SameAs(IPart p) => base.SameAs(p);

        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade) || ID == ObjectEnteredCellEvent.ID;

        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            if (4.in100())
            {
                var obj = GameObject.create("Books_BlahajSpawner");
                obj.MakeActive();
                ParentObject.CurrentCell.AddObject(obj);
                MetricsManager.LogInfo($"Created spawner at { ParentObject.CurrentCell.ToString() }");
            }
            else
            {

            }

            ParentObject.RemovePart(this);

            return base.HandleEvent(E);

        }
    }

}
