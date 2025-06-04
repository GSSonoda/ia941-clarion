using System;
using System.Collections.Generic;

namespace ClarionApp.Model
{
    public class Get
    {
        private Thing thing;
        private String creatureId;
        private WSProxy worldServer;

        public Get(WSProxy worldServer, IList<Thing> listThings, String creatureId)
        {
            this.creatureId = creatureId;
            this.worldServer = worldServer;
            this.thing = getNearestJewel(listThings);

        }
        public bool DoAction()
        {
            if (thing == null)
            {
                return false;
            }
            worldServer.SendSackIt(creatureId, thing.Name);
            return true;
        }
        private Thing getNearestJewel(IList<Thing> listThings)
        {
            Thing nearest = null;
            double minDistance = double.MaxValue;

            foreach (Thing item in listThings)
            {
                if (item.CategoryId != 3)
                {
                    continue;
                }
                double distance = item.DistanceToCreature;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = item;
                }
            }

            return nearest;
        }
        public Thing GetThing()
        {
            return thing;
        }
    }
}
