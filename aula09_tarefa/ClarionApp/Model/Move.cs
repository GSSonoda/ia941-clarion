using System;
using System.Collections.Generic;
namespace ClarionApp.Model
{
    public class Move
    {
        private Thing thing;
        private String creatureId;
        private WSProxy worldServer;

        public Move(WSProxy worldServer, IList<Thing> listThings, String creatureId)
        { 
            this.creatureId = creatureId;
            this.worldServer = worldServer;
            this.thing = getNearestJewel(listThings);

        }
        public bool DoAction()
        {
            if (thing.DistanceToCreature <= 20)
            {
                return true;
            }

            this.worldServer.SendMoveTo(this.creatureId, 1, 1, thing.X1, thing.Y1);
            return false;
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

    }
}
