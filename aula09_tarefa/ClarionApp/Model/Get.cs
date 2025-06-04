using System;
using System.Collections.Generic;

namespace ClarionApp.Model
{
    public class Get
    {
        private Thing thing;
        private String creatureId;
        private WSProxy worldServer;

        public Get(WSProxy worldServer, String creatureId)
        {
            this.creatureId = creatureId;
            this.worldServer = worldServer;


        }
        public void UpdateNearestThing(IList<Thing>  listThings)
        {
            this.thing = getNearest(listThings);
            if (thing != null)
            {
                Console.WriteLine("UpdateNearestThing " + thing.Name);
            }

        }
        public bool DoAction()
        {
            Console.WriteLine("DO ACTION GET");
            if (thing == null)
            {
                return false;
            }

            if (thing.CategoryId == 3 )
            {
                worldServer.SendSackIt(creatureId, thing.Name);
            }
            if (thing.CategoryId == 22 || thing.CategoryId == 21 || thing.CategoryId == 2)
            {
                worldServer.SendEatIt(creatureId, thing.Name);
            }


            return true;
        }
        private Thing getNearest(IList<Thing> listThings)
        {
            Thing nearest = null;
            double minDistance = double.MaxValue;

            foreach (Thing item in listThings)
            {
                if (item.CategoryId != 3 && item.CategoryId != 22 && item.CategoryId != 21 && item.CategoryId != 2)
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
